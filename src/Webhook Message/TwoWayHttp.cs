using DataverseAzureFunctionsCommon.Dataverse.Model;
using DataverseAzureFunctionsCommon;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;
using WebhookMessage.Model;

namespace WebhookMessage
{
    public class TwoWayHttp
    {
        private readonly ILogger<TwoWayHttp> _logger;
        private readonly ServiceClient _serviceClient;
        private readonly IConfiguration _configuration;

        public TwoWayHttp(ILogger<TwoWayHttp> logger, IOrganizationService organizationService, IConfiguration config)
        {
            _logger = logger;
            _serviceClient = (ServiceClient)organizationService;
            _configuration = config;
        }

        [Function("TwoWayHttp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function,"post", Route = "twowayhttp")] 
            HttpRequest req,
            FunctionContext executionContext)
        {

            _logger.LogInformation($"Message received Context: {executionContext.InvocationId}");
            
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Message Body {requestBody}");
            var twoWayRequest = JsonConvert.DeserializeObject<TwoWayRequest>(requestBody);

            _logger.LogInformation($"Primary Entity {twoWayRequest.EntityName} Name: {twoWayRequest.RecordNameValue}");
            //write back a date/time just to prove the function was triggered
            if (twoWayRequest.DemoLogType == (int)RjB_TypeOfAzureFunction.TwowayHttp)
            {
                return new OkObjectResult($"HTTP TwoWay Writing Back to the log record");
            }
            else
            {
                return new OkObjectResult("HTTP TwoWay Wrong Message Type Received");
            }
        }
    }
}
