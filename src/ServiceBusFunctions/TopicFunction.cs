using Azure.Messaging.ServiceBus;
using DataverseAzureFunctionsCommon;
using DataverseAzureFunctionsCommon.Dataverse.Model;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using System;
using System.Threading.Tasks;

namespace ServiceBusFunctions;

public class TopicFunction
{
    private readonly ILogger<TopicFunction> _logger;
    private readonly ServiceClient _serviceClient;
    private readonly IConfiguration _configuration;

    public TopicFunction(ILogger<TopicFunction> logger, IOrganizationService serviceClient, IConfiguration config)
    {
        _serviceClient = (ServiceClient)serviceClient;
        _configuration = config;
        _logger = logger;
    }

    [Function(nameof(TopicFunction))]
    public async Task Run(
        [ServiceBusTrigger("%TopicName%", "%SubscriptionName%", Connection = "ServiceBusQueueNameSpace")]
        ServiceBusReceivedMessage message,
        ServiceBusMessageActions messageActions,
                FunctionContext executionContext)
    {
        _logger.LogInformation("Message ID: {id}", message.MessageId);
        _logger.LogInformation("Message Body: {body}", message.Body);
        _logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

        try
        {
            if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/Organization"))
            {
                var orgValue = (string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/Organization"];
                _logger.LogInformation($"Organization: {orgValue}");
                if (orgValue.Contains(_configuration.GetValue<string>("expectedOrg")))
                {
                    _logger.LogInformation("Expected Org");
                }
                else
                {
                    _logger.LogWarning($"Mesage org not expected: {orgValue}");
                    await messageActions.DeadLetterMessageAsync(message);
                }
            }
            else
            {
                _logger.LogWarning("Missing Org Header Value");
                await messageActions.DeadLetterMessageAsync(message);
            }
            //log Message properties for demostration
            if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/User"))
                _logger.LogInformation($"User: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/User"]}");

            if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUser"))
                _logger.LogInformation($"InitiatingUser: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUser"]}");

            if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName"))
                _logger.LogInformation($"EntityLogicalName: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName"]}");

            if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/RequestName"))
                _logger.LogInformation($"RequestName: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/RequestName"]}");

            if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUserAgent"))
                _logger.LogInformation($"InitiatingUserAgent: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUserAgent"]}");
            //Validate if the max message size was exceeded for down stream processes
            var messageDataExeeded = false;
            if (message.ApplicationProperties.Keys.Any(ap => ap.Contains("MessageMaxSizeExceeded")))
            {
                _logger.LogWarning("MessageMaxSizeExceeded");
                messageDataExeeded = true;
            }

            var remoteContext = RemoteContextDeserializer.DeserializeJsonString<RemoteExecutionContext>(message.Body.ToString());

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
            if (demoLog.RjB_Type == RjB_TypeOfAzureFunction.ServiceBusTopic)
            {
                var addNote = new Annotation();
                addNote.NoteText = $"Service Bus Topic Azure Function Writing Back to the log record";
                addNote.ObjectId = new EntityReference(RjB_DemoAzureFunctionLog.EntityLogicalName, remoteContext.PrimaryEntityId);
                await _serviceClient.CreateAsync(addNote);
                _logger.LogInformation("WriteBack Successful");
            }

            // Complete the message
            await messageActions.CompleteMessageAsync(message);

        }
        catch (Exception ex)
        {

            _logger.LogError(ex, $"Error Occured {executionContext.InvocationId}");
            await messageActions.AbandonMessageAsync(message);
            throw;
        }
    }
}