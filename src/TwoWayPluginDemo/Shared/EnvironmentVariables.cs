using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static TwoWayPluginDemo.Shared.Constants;

namespace TwoWayPluginDemo.Shared
{
    public static class EnvironmentVariables
    {
        private static ConcurrentDictionary<string, string> Variables = new ConcurrentDictionary<string, string>();
        private static object lockObject = new object();

        public static decimal GetDecimalValue(IOrganizationService service, TraceDelegate trace, string schemaName, decimal defaultValue, bool throwError = false)
        {
            var val = defaultValue.ToString();
            val = Shared.EnvironmentVariables.GetVariable(schemaName, service, trace);
            if (!decimal.TryParse(val, out decimal decimalVal))
            {
                trace("unable to parse val");
                if (throwError)
                {
                    trace($"Parsing {val} for {schemaName} to a decimal failed");
                    throw new InvalidPluginExecutionException($"Parsing {val} for {schemaName} to a decimal failed");
                }
                decimalVal = defaultValue;
            }
            return decimalVal;
        }
        public static string GetVariable(string schemaName, IOrganizationService service, TraceDelegate trace)
        {
            var value = string.Empty;
            if (!Variables.TryGetValue(schemaName, out value))
            {
                lock (lockObject)
                {
                    if (!Variables.TryGetValue(schemaName, out value))
                    {
                        trace($"retrieving value for {schemaName}");
                        var fetchData = new
                        {
                            schemaname = schemaName
                        };
                        var fetchXml = $@"
                                <fetch>
                                  <entity name='environmentvariabledefinition'>
                                    <attribute name='defaultvalue' />
                                    <filter type='and'>
                                      <condition attribute='schemaname' operator='eq' value='{fetchData.schemaname/*tru_ReferralPercentageDefault*/}'/>
                                    </filter>
                                    <link-entity name='environmentvariablevalue' from='environmentvariabledefinitionid' to='environmentvariabledefinitionid' link-type='outer' alias='currentvalue'>
                                      <attribute name='value' />
                                    </link-entity>
                                  </entity>
                                </fetch>";
                        var variables = service.RetrieveMultiple(new FetchExpression(fetchXml));
                        trace($"retrieved environmentvariables count = {variables.Entities.Count}");
                        if (variables.Entities.Count != 0)
                        {
                            var variable = variables.Entities[0];
                            if (variable.TryGetAttributeValue<AliasedValue>("currentvalue.value", out AliasedValue currentValue))
                            {
                                trace("Retrieved aliased value");
                                value = (string)currentValue.Value;
                                trace($"value = {value}");
                            }
                            else if (variable.TryGetAttributeValue<string>("defaultvalue", out string defaultValue))
                            {
                                trace("use default value");
                                value = defaultValue;
                                trace($"value = {value}");
                            }
                            else
                            {
                                trace("unable to retrieve either value");
                                trace($"val = {value}");
                            }
                        }
                    }

                }
            }
            return value;
        }
    }
}
