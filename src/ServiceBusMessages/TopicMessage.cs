using System;
using System.Linq;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using DataverseAzureFunctionsCommon.Dataverse.Model;
using DataverseAzureFunctionsCommon;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;

namespace ServiceBusMessages
{
    public class TopicMessage
    {

        private readonly ServiceClient _serviceClient;
        private readonly IConfiguration _configuration;

        public TopicMessage(IOrganizationService serviceClient, IConfiguration config)
        {
            _serviceClient = (ServiceClient)serviceClient;
            _configuration = config;
        }

        [FunctionName("TopicMessage")]
        public async Task Run([ServiceBusTrigger("%TopicName%", "%SubscriptionName%", Connection = "ServiceBusQueueNameSpace")] 
                ServiceBusReceivedMessage message,
                ServiceBusMessageActions messageActions,
                ExecutionContext executionContext,
                ILogger log)
        {
            try
            {
                log.LogInformation($"InvocationId: {executionContext.InvocationId} QueueMessage: {message.Body}");

                if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/Organization"))
                {
                    var orgValue = (string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/Organization"];
                    log.LogInformation($"Organization: {orgValue}");
                    if (orgValue.Contains(_configuration.GetValue<string>("expectedOrg")))
                    {
                        log.LogInformation("Expected Org");
                    }
                    else
                    {
                        log.LogWarning($"Mesage org not expected: {orgValue}");
                        await messageActions.DeadLetterMessageAsync(message);
                    }
                }
                else
                {
                    log.LogWarning("Missing Org Header Value");
                    await messageActions.DeadLetterMessageAsync(message);
                }
                if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/User"))
                    log.LogInformation($"User: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/User"]}");

                if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUser"))
                    log.LogInformation($"InitiatingUser: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUser"]}");

                if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName"))
                    log.LogInformation($"EntityLogicalName: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/EntityLogicalName"]}");

                if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/RequestName"))
                    log.LogInformation($"RequestName: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/RequestName"]}");

                if (message.ApplicationProperties.ContainsKey("http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUserAgent"))
                    log.LogInformation($"InitiatingUserAgent: {(string)message.ApplicationProperties["http://schemas.microsoft.com/xrm/2011/Claims/InitiatingUserAgent"]}");

                var messageDataExeeded = false;
                if (message.ApplicationProperties.Keys.Any(ap => ap.Contains("MessageMaxSizeExceeded ")))
                {
                    log.LogWarning("MessageMaxSizeExceeded");
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

                log.LogInformation($"Primary Entity {demoLog.LogicalName} {demoLog.RjB_Name} {demoLog.Id}");
                //write back a date/time just to prove the function was triggered
                if (demoLog.RjB_Type == RjB_TypeOfAzureFunction.ServiceBusTopic)
                {
                    var addNote = new Annotation();
                    addNote.NoteText = $"Service Bus Topic Azure Function Writing Back to the log record";
                    addNote.ObjectId = new EntityReference(RjB_DemoAzureFunctionLog.EntityLogicalName, remoteContext.PrimaryEntityId);
                    await _serviceClient.CreateAsync(addNote);
                    log.LogInformation("WriteBack Successful");
                }

                await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"Error Occured {executionContext.InvocationId}");
                await messageActions.AbandonMessageAsync(message);
                throw;
            }
        }
    }
}
