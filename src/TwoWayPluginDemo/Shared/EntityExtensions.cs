using Microsoft.Xrm.Sdk;
using static TwoWayPluginDemo.Shared.Constants;

namespace TwoWayPluginDemo.Shared
{
    public static class EntityExtensions
    {
        public static T GetAttributeValueWithDefault<T>(this Entity entity, string attribute, T defaultValue, TraceDelegate trace)
        {
            if (entity.Contains(attribute))
            {
                return entity.GetAttributeValue<T>(attribute);
            }
            else
            {
                trace($"{attribute} was null using default value of {defaultValue}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Retrieve value from the entity, from an alternate entity such as a pre or post image, then use the default value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <param name="image"></param>
        /// <param name="attribute"></param>
        /// <param name="defaultValue"></param>
        /// <param name="trace"></param>
        /// <param name="nullException">if true then an exception is thrown</param>
        /// <returns></returns>
        public static T GetAttributeValueWithImageAndDefault<T>(this Entity entity, Entity image, string attribute, T defaultValue, TraceDelegate trace, bool nullException = false)
        {
            if (entity.Contains(attribute))
            {
                trace("contains attribute");
                if (entity.TryGetAttributeValue(attribute, out T result))
                {
                    trace("try get attribute worked");
                    return result;
                }
                else
                {
                    var value = entity[attribute];
                    if (value == null)
                    {
                        trace($"value null using default");
                        return defaultValue;
                    }
                    else
                    {
                        throw new InvalidPluginExecutionException($"Value for {attribute} could not be determined");
                    }
                }
            }
            else
            if (image.TryGetAttributeValue(attribute, out T result))
            {
                trace($"{attribute} was null using image value");
                return result;
            }
            else
            {
                trace($"{attribute} was null using default value of {defaultValue}");
                if (nullException)
                    throw new InvalidPluginExecutionException(OperationStatus.Failed, $"Value on {entity.LogicalName} for {attribute} is null");
                return defaultValue;
            }
        }
    }
}
