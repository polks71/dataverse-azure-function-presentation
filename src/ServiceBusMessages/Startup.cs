using Azure.Core;
using Azure.Identity;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

[assembly: FunctionsStartup(typeof(ServiceBusMessages.Startup))]
namespace ServiceBusMessages
{
    
    internal class Startup : FunctionsStartup
    {
        private static string dataverseurl;
        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            builder.ConfigurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly());
            var config = builder.ConfigurationBuilder.Build();
            dataverseurl = config["dataverseurl"];
            base.ConfigureAppConfiguration(builder);
        }

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var x = builder.GetContext();
            builder.Services.AddMemoryCache();
            builder.Services.AddSingleton(new DefaultAzureCredential());
            builder.Services.AddSingleton<IOrganizationService, ServiceClient>(provider =>
            {
                var managedIdentity = provider.GetRequiredService<DefaultAzureCredential>();
                var environment = dataverseurl;
                var cache = provider.GetService<IMemoryCache>();
                return new ServiceClient(
                        tokenProviderFunction: f => GetDataverseToken(environment, managedIdentity, cache),
                        instanceUrl: new Uri(environment),
                        useUniqueInstance: true);
            });
        }

        private async Task<string> GetDataverseToken(string environment, DefaultAzureCredential credential, IMemoryCache cache)
        {
            var accessToken = await cache.GetOrCreateAsync(environment, async (cacheEntry) => {
                cacheEntry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20);
                var token = (await credential.GetTokenAsync(new TokenRequestContext(new[] { $"{environment}/.default" })));
                return token;
            });
            return accessToken.Token;
        }
    }
}
