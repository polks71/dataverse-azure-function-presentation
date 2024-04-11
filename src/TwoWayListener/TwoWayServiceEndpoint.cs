using Microsoft.Xrm.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TwoWayListener
{
    internal class TwoWayServiceEndpoint : ITwoWayServiceEndpointPlugin
    {
        string ITwoWayServiceEndpointPlugin.Execute(RemoteExecutionContext executionContext)
        {
            string message = $"TwoWay Listener, a lame console app, Writing Back to the log record";
            

            Console.WriteLine($"MessageType: {executionContext.MessageName} Entity: {executionContext.PrimaryEntityName} EnityId: {executionContext.PrimaryEntityId}");
            return message;
        }
    }
}
