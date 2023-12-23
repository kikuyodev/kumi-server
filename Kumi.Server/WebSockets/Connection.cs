using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Kumi.Game.Extensions;
using Kumi.Game.Online.Server;
using Kumi.Game.Online.Server.Packets;
using Kumi.Server.Database.Models;
using Newtonsoft.Json;

namespace Kumi.Server.WebSockets;

/// <summary>
/// A class that represents a connection to a WebSocket client.
/// </summary>
public class Connection : IDisposable
{
    /// <summary>
    /// The client that is connected to the server.
    /// </summary>
    public TcpClient Client { get; set; }
    
    /// <summary>
    /// The stream that is used to communicate with the client.
    /// </summary>
    public NetworkStream Stream => Client.GetStream();

    /// <summary>
    /// Whether the client is connected to the server.
    /// </summary>
    public bool Connected { get; set; }
    
    /// <summary>
    /// The ID of the connection.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// The account that is connected to the server.
    /// </summary>
    public Account? Account { get; set; }

    public Connection(TcpClient client)
    {
        Client = client;
    }

    public void Send(Packet sendablePacket)
    {
        // Serialize the packet.
        var serialized = JsonConvert.SerializeObject(sendablePacket);

        using (var packet = new MemoryStream())
        {
            byte op = 0b0_0_0_0_0000;
            op |= 0b1_0_0_0_0000; // FIN
            op |= 0b0_0_0_0_0001; // Opcode (text)
            
            packet.WriteByte(op);
            
            if (serialized.Length <= 125)
            {
                packet.WriteByte((byte) serialized.Length);
            }
            else if (serialized.Length <= ushort.MaxValue)
            {
                packet.WriteByte(126);
                packet.Write(intToByteArray((ushort) serialized.Length), 0, 2);
            }
            else
            {
                packet.WriteByte(127);
                packet.Write(intToByteArray((ushort) serialized.Length), 0, 8);
            }
            
            var data = Encoding.UTF8.GetBytes(serialized);
            // write data at correct index
            packet.Write(data, 0, data.Length);
            
            // Send the packet.
            Stream.Write(packet.ToArray(), 0, (int) packet.Length);
        }
    }

    
    private byte[] intToByteArray(ushort value)
    {
        var ary = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(ary);
        }

        return ary;
    }

    public void ValidateHandshake(string handshake)
    {
        // Get the key from the request.
        var key = handshake.Split("\r\n").FirstOrDefault(x => x.StartsWith("Sec-WebSocket-Key"))?.Split(": ")[1];
                            
        // Generate the response.
        var response = Encoding.UTF8.GetBytes($"HTTP/1.1 101 Switching Protocols\r\n" +
                                              $"Connection: Upgrade\r\n" +
                                              $"Upgrade: websocket\r\n" +
                                              $"Sec-WebSocket-Accept: {Convert.ToBase64String(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes($"{key}258EAFA5-E914-47DA-95CA-C5AB0DC85B11")))}\r\n\r\n");
                            
        // Send the response.
        this.Stream.Write(response, 0, response.Length);
                            
        // Start the WebSocket connection.
        this.Connected = true;
        this.Send(new HelloPacket());
    }

    public string ProcessIncoming(byte[] buffer)
    {
        bool fin = (buffer[0] & 0b10000000) != 0,
            mask = (buffer[1] & 0b10000000) != 0;
                            
        if (!fin)
            throw new Exception("Fragmented frames are not supported."); // for now
                            
        int opcode = buffer[0] & 0b00001111,
            msglen = buffer[1] - 128,
            offset = 2;

        if (msglen == 126)
        {
            msglen = BitConverter.ToUInt16(new byte[] { buffer[3], buffer[2] }, 0);
            offset = 4;
        } else if (msglen == 127)
        {
            throw new Exception("Messages over 64KB are not supported."); // for now
        }

        if (msglen == 0 || !mask)
            return null;
                            
        byte[] decoded = new byte[msglen];
        byte[] masks = new byte[4] { buffer[offset], buffer[offset + 1], buffer[offset + 2], buffer[offset + 3] };
        offset += 4;
                            
        for (int i = 0; i < msglen; ++i)
            decoded[i] = (byte)(buffer[offset + i] ^ masks[i % 4]);

        return Encoding.UTF8.GetString(decoded);
    }
    
    public void Dispose()
    {
        Client.Dispose();
        Thread.CurrentThread.Interrupt(); // Usually, the connection will be in a separate thread.
    }
}
