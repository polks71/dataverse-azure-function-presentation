using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace WebhookMessage
{
    public class HttpTriggeredFunction
    {
        private readonly ILogger<HttpTriggeredFunction> _logger;

        public HttpTriggeredFunction(ILogger<HttpTriggeredFunction> logger)
        {
            _logger = logger;
        }

        [Function("Function1")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "dataverse")] 
            HttpRequest req,
            ILogger log,
            Microsoft.Azure.WebJobs.ExecutionContext executionContext)
        {
            log.LogInformation($"Message received InvocationID: {executionContext.InvocationId}");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                log.LogInformation($"InvocationID: {executionContext.InvocationId} Message body: {requestBody}");
                
                
                return new OkResult();
            }
            catch (Exception e)
            {
                log.LogError(e, $"Exception sending message to queue");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
