namespace Kumi.Server;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSignalR();
    }
    
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.UseWebSockets();
        
        app.UseEndpoints(endpoints =>
        {
            //endpoints.MapHub<>();
        });
    }
}
