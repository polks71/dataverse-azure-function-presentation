using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebhookMessage.Model
{
    internal class TwoWayRequest
    {
        public string RecordNameValue {  get; set; }
        public string EntityName { get; set; }
        public int DemoLogType { get; set; }
    }
}
