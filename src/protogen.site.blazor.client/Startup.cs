using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;

namespace ProtoBuf {
    public class Startup {
#pragma warning disable IDE0060 // Remove unused parameter
        public void ConfigureServices (IServiceCollection services) { }
#pragma warning restore IDE0060 // Remove unused parameter

        public void Configure (IComponentsApplicationBuilder app) {
            app.AddComponent<App> ("app");
        }
    }
}