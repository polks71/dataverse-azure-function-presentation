using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.Security.Policy;
using TwoWayPluginDemo.Shared;
using System.Net.Http;
using System.Runtime.Serialization;

namespace TwoWayPluginDemo
{
    [DataContract(Namespace = "")]
    internal class TwoWayRequest
    {
        [DataMember]
        public string RecordNameValue { get; set; }
        [DataMember]
        public string EntityName { get; set; }
        [DataMember]
        public int DemoLogType { get; set; }
    }

    public class CreateLogRecordTwoWayHttp : TruPluginBase
    {
        #region Constructor
        public CreateLogRecordTwoWayHttp(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
        {

        }
        #endregion

        #region IPlugin
        public override void Execute(IServiceProvider serviceProvider)
        {
            try
            {
                InitializePlugin(serviceProvider);

                var createReq = new CreateRequest() { Parameters = context.InputParameters };
                var target = createReq.Target;


                if (context.PrimaryEntityName != "rjb_demoazurefunctionlog") { return; }
                if (context.MessageName != "Create") { return; }

                var logType = target.GetAttributeValue<OptionSetValue>("rjb_type").Value;

                if (logType == 911620004)//TwoWay HTTP Log Type
                {
                    Trace($"LogType expected value {logType}");
                    var twoWayRequest = new TwoWayRequest()
                    {
                        DemoLogType = target.GetAttributeValue<OptionSetValue>("rjb_type").Value,
                        EntityName = context.PrimaryEntityName,
                        RecordNameValue = target.GetAttributeValue<string>("rjb_name")
                    };

                    var url = EnvironmentVariables.GetVariable("rjb_TwoWayHTTPURL", service, Trace);
                    using (var httpClient = new HttpClient())
                    {
                        Trace($"url={url}");
                        //rjb_TwoWayHTTPFunctionKey
                        var functionKey = EnvironmentVariables.GetVariable("rjb_TwoWayHTTPFunctionKey", service, Trace);
                        Trace($"functionkey={functionKey}");
                        httpClient.DefaultRequestHeaders.Add("x-functions-key", functionKey);
                        var httpClientResponse = httpClient.PostAsync(url, new StringContent(JsonSerializer.SerializeItem(twoWayRequest))).Result;
                        httpClientResponse.EnsureSuccessStatusCode();

                        string content = httpClientResponse.Content.ReadAsStringAsync().Result;
                        Trace($"content= {content}");

                        if (!String.IsNullOrEmpty(content))
                        {
                            var newNote = new Entity("annotation");
                            newNote["objectid"] = target.ToEntityReference();
                            newNote["notetext"] = content;
                            service.Create(newNote);

                        }
                        Trace("Plug-in completed");
                    }
                }
                else
                {
                    Trace($"LogType not expected value {logType}");
                }
            }
            catch (InvalidPluginExecutionException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Trace(ex.ToString());
                throw new InvalidPluginExecutionException(string.Format("An error occurred in the plug-in. Message: {0}", ex.Message), ex);
            }

        }
        #endregion
    }
}
