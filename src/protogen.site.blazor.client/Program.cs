using Microsoft.AspNetCore.Blazor.Hosting;

namespace ProtoBuf {
    public class Program {
        public static void Main (string[] args) {
            CreateHostBuilder (args).Build ().Run ();
        }

#pragma warning disable IDE0060 // Remove unused parameter
        public static IWebAssemblyHostBuilder CreateHostBuilder (string[] args) =>
#pragma warning restore IDE0060 // Remove unused parameter
            BlazorWebAssemblyHost.CreateDefaultBuilder ()
            .UseBlazorStartup<Startup> ();
    }
}