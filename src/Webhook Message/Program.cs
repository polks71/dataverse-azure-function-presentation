using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Identity.Client;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System.Reflection;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        var settings = config.Build();
        if (context.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets(Assembly.GetExecutingAssembly());
        }
    }
    )
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddMemoryCache();
        services.AddLogging();
        services.AddSingleton<IOrganizationService, ServiceClient>(provider =>
        {
            var appid = provider.GetService<IConfiguration>().GetValue<string>("registeredappid");
            var appsecret = provider.GetService<IConfiguration>().GetValue<string>("registeredappsecret");
            var tenantId = provider.GetService<IConfiguration>().GetValue<string>("tenantid");
            var environment = provider.GetService<IConfiguration>().GetValue<string>("dataverseUrl");
            var cache = provider.GetService<IMemoryCache>();
            return new ServiceClient(
                    tokenProviderFunction: f => GetDataverseToken(environment, appid, appsecret, tenantId, cache),
                    instanceUrl: new Uri(environment),
                    useUniqueInstance: true);
        });
    })
    .Build();

host.Run();

//Authentication using AppId and Secret
async Task<string> GetDataverseToken(string environment, string appid, string appsecret, string tenantid, IMemoryCache cache)
{
    var accessToken = await cache.GetOrCreateAsync(environment, async (cacheEntry) => {
        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
        var token = await GetAppIdToken(appid, appsecret, tenantid, environment);
        return token;
    });
    return accessToken;
}

async Task<string> GetAppIdToken(string appid, string appsecret, string tenantid, string url)
{
    var app = ConfidentialClientApplicationBuilder.Create(appid)
        .WithClientSecret(appsecret)
        .WithAuthority($"https://login.microsoftonline.com/{tenantid}")
        .Build();


    var authResult = await app.AcquireTokenForClient(new[] { $"{url}/.default" })///.default
            .ExecuteAsync()
            .ConfigureAwait(false);
    return authResult.AccessToken;
}
