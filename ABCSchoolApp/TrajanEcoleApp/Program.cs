using App.Infrastructure;
using App.Infrastructure.Extensions;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace TrajanEcoleApp
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddSingleton(builder.Configuration.GetSection("ApiSettings").Get<ApiSettings>());
            builder.AddClientServices();

            await builder.Build().RunAsync();
        }
    }
}
