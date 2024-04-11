using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using System;
using System.ServiceModel.Description;

namespace TwoWayPluginDemo
{
    public class CreateLogRecord : TruPluginBase
    {
        //ideally this should be stored as an Environment Variable
        private readonly string SERVICE_ENDPOINT_VAR_NAME = "rjb_TwoWayServiceEndpoint";
        #region Constructor
        public CreateLogRecord(string unsecureConfig, string secureConfig) : base(unsecureConfig, secureConfig)
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

                // TODO Test for an entity type and message supported by your plug-in.
                if (context.PrimaryEntityName != "rjb_demoazurefunctionlog") { return; }
                if (context.MessageName != "Create") { return; }

                var endpointID = Guid.Parse(Shared.EnvironmentVariables.GetVariable(SERVICE_ENDPOINT_VAR_NAME, service, Trace));

                IServiceEndpointNotificationService endpointService = (IServiceEndpointNotificationService)serviceProvider.GetService(typeof(IServiceEndpointNotificationService));
                try
                {
                    Trace("Passing Context to Service Endpoint");
                    string response = endpointService.Execute(new EntityReference("serviceendpoint", endpointID), context);
                    if (target.TryGetAttributeValue<OptionSetValue>("rjb_type", out OptionSetValue typeValue))
                    {
                        if (typeValue.Value == 911620002)//TwoWay
                        {
                            if (!String.IsNullOrEmpty(response))
                            {
                                var newNote = new Entity("annotation");
                                newNote["objectid"] = target.ToEntityReference();
                                newNote["notetext"] = response;
                                service.Create(newNote);

                            }
                            Trace("Plug-in completed");
                        }
                        else
                        {
                            Trace("Type is not TwoWay");
                        }
                    }
                    else
                    {
                        Trace("Type Missing");
                    }
                }

                catch (Exception ex)
                {
                    Trace("Error: {0}", ex.Message);
                    throw;
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
