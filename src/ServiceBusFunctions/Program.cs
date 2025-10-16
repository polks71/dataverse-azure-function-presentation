using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using ServiceBusFunctions;
using System.Reflection;

var builder = FunctionsApplication.CreateBuilder(args);


builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton(new DefaultAzureCredential());
builder.Services.AddSingleton<IOrganizationService, ServiceClient>(provider =>
{
    var config = provider.GetRequiredService<IConfiguration>();
    var dataverseurl = config["dataverseurl"];
    var managedIdentity = provider.GetRequiredService<DefaultAzureCredential>();
    var cache = provider.GetService<IMemoryCache>();
    return new ServiceClient(
            tokenProviderFunction: f => GetDataverseToken(dataverseurl, managedIdentity, cache),
            instanceUrl: new Uri(dataverseurl),
            useUniqueInstance: true);
});
builder.Build().Run();



async Task<string> GetDataverseToken(string environment, DefaultAzureCredential credential, IMemoryCache cache)
{
    var accessToken = await cache.GetOrCreateAsync(environment, async (cacheEntry) => {
        cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
        var token = (await credential.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" })));
        return token;
    });
    return accessToken.Token;
}