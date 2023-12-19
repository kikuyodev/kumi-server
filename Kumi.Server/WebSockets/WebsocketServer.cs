using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Kumi.Game.Online.Server;
using Kumi.Server.IO;
using Kumi.Server.WebSockets.Hubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Framework.Allocation;
using osu.Framework.Threading;

namespace Kumi.Server.WebSockets;

/// <summary>
/// A home-baked implementation of the WebSocket protocol as per RFC 6455.
/// </summary>
public partial class WebsocketServer : IDependencyInjectionCandidate
{
    /// <summary>
    /// The address that this server will listen on.
    /// </summary>
    public string Address { get; set; } = "127.0.0.1";

    /// <summary>
    /// The port that this server will listen on.
    /// </summary>
    public int Port { get; } = 8080;

    /// <summary>
    /// The list of connections that are currently connected to the server.
    /// </summary>
    public ConnectionList Connections => new ConnectionList(_connectionThreads.Keys.ToList());

    public WebsocketServer(int port)
    {
        Port = port;
    }
    
    [Resolved]
    private Server _server { get; set; }

    private TcpListener? _listener;
    private Thread? _listenerThread;
    private Dictionary<Connection, Thread> _connectionThreads = new();
    private Dictionary<OpCode, List<Hub>> _hubs = new();

    public void Start()
    {
        var hubs = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.BaseType != null && !t.IsAbstract && (t.BaseType.Name == "Hub`1" || t.BaseType.Name == "Hub"))
            .ToList();
        
        foreach (var hub in hubs)
            registerHub(hub);
        
        _listener = new TcpListener(IPAddress.Parse(Address), Port);
        _listenerThread = new Thread(Listen);
        _listenerThread.IsBackground = true;
        _listenerThread.Name = "WebSocketServer";

        Listen();
    }

    public void Listen()
    {
        _listener.Start();

        while (true)
        {
            if (!_listener.Pending())
            {
                // Prevents the thread from spinning out of control and killing the CPU.
                Thread.Sleep(100);
                continue;
            }

            // Client is a blocking call.
            var client = _listener.AcceptTcpClient();
            var connection = new Connection(client);

            _connectionThreads.Add(connection, new Thread(() =>
            {
                while (true)
                {
                    if (!connection.Client.Connected)
                    {
                        connection.Dispose();
                        return;
                    }
                    
                    if (!connection.Stream.DataAvailable || connection.Client.Available < 3) // The minimum size of a WebSocket frame is 3 bytes, as the first two bytes are an initial GET request.
                        continue;

                    var buffer = new byte[connection.Client.Available];
                    connection.Stream.Read(buffer, 0, buffer.Length);
                    var request = Encoding.UTF8.GetString(buffer);

                    // Check if the request is a WebSocket handshake.
                    if (request.StartsWith("GET"))
                    {
                        connection.ValidateHandshake(request);
                    }
                    else
                    {
                        if (!connection.Connected)
                        {
                            connection.Dispose();
                            return;
                        }
                        
                        // Get the websocket frame opcode.
                        var opCode = buffer[0] & ((1 << 4) - 1);

                        switch (opCode)
                        {
                            case 0x0: // continuation frame, which we don't support sadly.
                            case 0x2: // we don't support binary frames yet
                            case 0x9:
                            case 0xA:
                                break;
                            
                            case 0x1:
                                handleMessage(connection, connection.ProcessIncoming(buffer));
                                break;
                            
                            case 0x8: // close connection
                                connection.Dispose();
                                return;
                                
                            default:
                                break;
                        }
                    }
                }
            }));

            _connectionThreads[connection].Start();
        }
    }
    
    public Connection? GetConnection(string id) => GetConnection(Guid.Parse(id));
    public Connection? GetConnection(Guid id) => Connections.FirstOrDefault(c => c.Id == id);
    
    public IConnectionEnumerable GetConnections(Expression<Func<Connection, bool>> query)
    {
        return new ConnectionList(Connections.Where(query.Compile()));
    }

    private void handleMessage(Connection conn, string message)
    {
        // This requires brutal reflection, but it's the only way to do it; since theres no "generics" here.
        var packet = JObject.Parse(message);
        var packetOpInt = packet["op"].Value<int>();
        
        // Check if the opcode is valid.
        if (!Enum.IsDefined(typeof(OpCode), packetOpInt))
            return;
        
        var opCode = (OpCode)packetOpInt;
        if (!_hubs.ContainsKey(opCode))
            return;
        
        // Parse the packet into a generic type, if possible.
        int hubIdx = -1;
        Packet? packetObj = null;
        Hub? hub = null;
        bool hasData = false;
        foreach (var potentialHub in _hubs[opCode])
        {
            hubIdx++;
            
            // Cast the packet into the hub's generic type.
            var hubType = potentialHub.GetType();
            Type? wantedType = null;
            var genericTypes = hubType.BaseType.GetGenericArguments();
            
            // Find the generic type that we can JSON parse the packet data into.
            foreach (var type in genericTypes)
            {
                if (packet.ToObject(type) != null)
                {
                    wantedType = type;
                    break;
                }
            }
            
            if (wantedType == null)
                continue;
            
            // Check if the hub expects data.
            hasData = hubType.GetCustomAttribute<HubAttribute>()?.ExpectsData ?? false;
                
            if (potentialHub.ExpectsData && packet["d"] == null)
                continue;
            
            // Parse the packet into the generic type.
            packetObj = (Packet)packet.ToObject(wantedType);
            hub = potentialHub;
            break;
        }
        
        if (hub == null || packetObj == null)
            return;

        // Invoke the hub's Handle method.
        // Need to hackily use reflection to invoke Handle(Packet<T> packet).
        var methods = hub.GetType().GetMethods();
            
        foreach (var method in methods)
        {
            if (method.Name != "Handle")
                continue;
                
            var parameters = method.GetParameters();
            if (parameters.Length < 1)
                continue;
            
            if (hasData) {
                var parameter = parameters[0];
                if (parameter.ParameterType == typeof(Packet))
                    continue;
            }

            method.Invoke(hub, new [] { conn, (object)packetObj });
            break;
        }
    }

    private void registerHub(Type hubType)
    {
        var hub = (Hub)Activator.CreateInstance(hubType);
        var hubAttribute = hubType.GetCustomAttribute<HubAttribute>();
        
        if (hubAttribute == null)
            return;
        
        if (!_hubs.ContainsKey(hubAttribute.OpCode))
            _hubs.Add(hubAttribute.OpCode, new List<Hub>());
        
        // TODO: Support dispatch type and dispatch methods
        
        hub.OpCode = hubAttribute.OpCode;
        hub.ExpectsData = hubAttribute.ExpectsData;
        
        _hubs[hubAttribute.OpCode].Add(hub);
        _server.Dependencies.Inject(hub);
    }
}
