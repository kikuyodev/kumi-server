using osu.Framework.Allocation;

namespace Kumi.Server;

public static class Program
{
    public static Server Server { get; set; } = new();
    
    public static void Main(string[] args)
    {
        Server.Run();
    }
}