using Kumi.Game.Online.Server;

namespace Kumi.Server.WebSockets.Hubs;


[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HubAttribute : Attribute
{
    /// <summary>
    /// The opcode that this hub handles.
    /// </summary>
    public OpCode OpCode { get; }
    
    /// <summary>
    /// The dispatch type that this hub handles, if any.
    /// </summary>
    public DispatchType? DispatchType { get; }
    
    /// <summary>
    /// Whether this hub expects data.
    /// </summary>
    public bool ExpectsData { get; }
    
    public HubAttribute(OpCode opCode)
    {
        OpCode = opCode;
    }

    public HubAttribute(OpCode opCode, DispatchType dispatchType)
    {
        OpCode = opCode;
        DispatchType = dispatchType;
    }
    
    public HubAttribute(OpCode opCode, bool expectsData)
    {
        OpCode = opCode;
        ExpectsData = expectsData;
    }
    
    public HubAttribute(OpCode opCode, DispatchType dispatchType, bool expectsData)
    {
        OpCode = opCode;
        DispatchType = dispatchType;
        ExpectsData = expectsData;
    }
}
