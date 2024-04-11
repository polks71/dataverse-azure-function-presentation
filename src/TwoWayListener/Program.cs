using Microsoft.ServiceBus;
using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace TwoWayListener
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var serviceHost = new ServiceHost(typeof(TwoWayServiceEndpoint));

            var serviceBusHost = ConfigurationManager.AppSettings.Get("ServiceBusHostName");
            var serviceBusKey = ConfigurationManager.AppSettings.Get("ServiceBusSecret");
            var serviceBusKeyName = ConfigurationManager.AppSettings.Get("ServiceBusAccessKeyName");
            var transportClient = new TransportClientEndpointBehavior(TokenProvider.CreateSharedAccessSignatureTokenProvider(serviceBusKeyName, serviceBusKey));
            serviceHost.AddServiceEndpoint(typeof(ITwoWayServiceEndpointPlugin), new WS2007HttpRelayBinding(), serviceBusHost).EndpointBehaviors.Add(transportClient);
            serviceHost.Open();
            Console.ReadLine();
        }
    }
}
