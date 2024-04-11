using DataverseAzureFunctionsCommon;
using DataverseAzureFunctionsCommon.Dataverse.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Newtonsoft.Json;

namespace WebhookMessage
{
    public class HttpTriggeredFunction
    {
        

        private readonly ServiceClient _serviceClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HttpTriggeredFunction> _logger;

        public HttpTriggeredFunction(IOrganizationService organizationService, IConfiguration config, ILogger<HttpTriggeredFunction> logger)
        {            
            _serviceClient = (ServiceClient)organizationService;
            _configuration = config;
            _logger = logger;
        }

        [Function("HttpTriggeredFunctionDemo")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "dataverse")] 
            HttpRequest req,
            FunctionContext executionContext)
        {
            
            try
            {
                var invocationId = "UnknownInvocationId";
                if (executionContext != null)
                {
                    _logger.LogInformation($"Message received Context: {executionContext.InvocationId}");
                    invocationId = executionContext.InvocationId;
                }
                else
                    _logger.LogWarning("Message Received but context is null");
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                _logger.LogInformation($"InvocationID: {invocationId} Message body: {requestBody}");

                //check if the message is received from the correct org
                StringValues headerValues;
                if (req.Headers.TryGetValue("x-ms-dynamics-organization", out headerValues))
                {
                    var org = headerValues.First().ToLower();
                    if (!org.Contains(_configuration.GetValue<string>("expectedOrg").ToLower()))
                    {
                        this._logger.LogWarning($"{org} is not the expected org {_configuration.GetValue<string>("expectedOrg")}");
                        return new UnauthorizedResult();
                    }
                    else
                    {
                        _logger.LogInformation($"Expected Org {org} is correct");
                    }
                }
                else
                {
                    _logger.LogWarning("Request with missing header.");
                    return new UnauthorizedResult();
                }
                //check if the message size exceeded to flag future operations
                bool messageDataExeeded = false;
                if (req.Headers.ContainsKey("x-ms-dynamics-msg-size-exceeded"))
                {
                    _logger.LogWarning("Message Data Exceeded");
                    messageDataExeeded = true;
                }

                var remoteContext = RemoteContextDeserializer.DeserializeJsonString<RemoteExecutionContext>(requestBody);
                RjB_DemoAzureFunctionLog demoLog;
                //Check if the size was exceeded. If not retrieve the entity from the Target otherwise retrieve the row
                if (!messageDataExeeded)
                {
                    demoLog = ((Entity)remoteContext.InputParameters["Target"]).ToEntity<RjB_DemoAzureFunctionLog>();
                }
                else
                {
                    demoLog = (await _serviceClient.RetrieveAsync(RjB_DemoAzureFunctionLog.EntityLogicalName, remoteContext.PrimaryEntityId, new Microsoft.Xrm.Sdk.Query.ColumnSet(
                        RjB_DemoAzureFunctionLog.Fields.RjB_AzureFunctionWriteBack, RjB_DemoAzureFunctionLog.Fields.RjB_Type
                        ))).ToEntity<RjB_DemoAzureFunctionLog>();
                }

                _logger.LogInformation($"Primary Entity {demoLog.LogicalName} {demoLog.RjB_Name} {demoLog.Id}");
                //write back a date/time just to prove the function was triggered
                if (demoLog.RjB_Type == RjB_TypeOfAzureFunction.Httptriggered)
                {
                    var addNote = new Annotation();
                    addNote.NoteText = $"Azure Function Writing Back to the log record";
                    addNote.ObjectId = new EntityReference(RjB_DemoAzureFunctionLog.EntityLogicalName, remoteContext.PrimaryEntityId);
                    await _serviceClient.CreateAsync(addNote);
                    _logger.LogInformation("WriteBack Successful");
                }
                return new OkResult();
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"HttpTriggered Function Failed");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
