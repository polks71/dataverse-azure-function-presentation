using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.PluginTelemetry;
using System;
using System.Xml.Linq;

namespace TwoWayPluginDemo
{
    public abstract class TruPluginBase : IPlugin
    {
        protected string secureConfig = null;
        protected string unsecureConfig = null;
        protected ITracingService tracingService = null;
        protected IOrganizationServiceFactory serviceFactory;
        protected IOrganizationService service;
        protected IPluginExecutionContext context;
        protected ILogger logger;

        public TruPluginBase(string unsecureConfig, string secureConfig)
        {
            this.secureConfig = secureConfig;
            this.unsecureConfig = unsecureConfig;
        }
        public abstract void Execute(IServiceProvider serviceProvider);

        protected void InitializePlugin(IServiceProvider serviceProvider)
        {
            context = (IPluginExecutionContext)serviceProvider.GetService(typeof(IPluginExecutionContext));

            logger = (ILogger)serviceProvider.GetService(typeof(ILogger));
            //if we are in Debug mode initialize _tracer to a value. If not leave it null
            if (DebugMode)
            {
                tracingService = (ITracingService)serviceProvider.GetService(typeof(ITracingService));
                if (tracingService == null)
                    throw new InvalidPluginExecutionException("Failed to retrieve the tracing service");
                Trace("Got the tracing service");
            }

            serviceFactory = (IOrganizationServiceFactory)serviceProvider.GetService(typeof(IOrganizationServiceFactory));
            service = serviceFactory.CreateOrganizationService(context.UserId);
        }

        protected string RetrieveValueFromUnsecureConfig(string elementName)
        {
            var nodes = XElement.Parse(unsecureConfig);
            var element = nodes.Element(elementName);
            if (element == null)
            {
                var message = $"Unsecure Config value {elementName} was not found";
                Trace(message);
                throw new InvalidPluginExecutionException(OperationStatus.Failed, message);
            }
            else
            {
                return element.Value;
            }

        }

        /// <summary>
        /// Check the _unsecureConfig for XML that determines debug mode.
        /// A root XML Element must exist that is called "Debug"
        /// Default will always be false.
        /// </summary>
        protected bool DebugMode
        {
            get
            {
                try
                {
                    if (!string.IsNullOrEmpty(unsecureConfig))
                    {
                        var debug = RetrieveValueFromUnsecureConfig("Debug");
                        bool def = false;
                        if (bool.TryParse(debug, out def))
                        {
                            return def;
                        }
                        else
                            return false;

                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    //we will default false here
                    return false;
                }
            }
        }

        protected void Trace(string message, params object[] args)
        {
            try
            {
                if (logger != null)
                    logger.LogInformation(message, args);
                if (tracingService != null)
                    tracingService.Trace(message, args);
            }
            catch (Exception ex)
            {
                throw new InvalidPluginExecutionException("Error attempting to trace", ex);
            }
        }
    }
}
