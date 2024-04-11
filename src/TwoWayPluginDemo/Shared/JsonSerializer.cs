using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace TwoWayPluginDemo.Shared
{
    public static class JsonSerializer
    {
        public static string SerializeItem<T>(T item)
        {
            string jsonReference;
            using (var ms = new System.IO.MemoryStream())
            {
                var js = new DataContractJsonSerializer(typeof(T));
                js.WriteObject(ms, item);
                ms.Position = 0;
                var sr = new StreamReader(ms);
                jsonReference = sr.ReadToEnd();
            }

            return jsonReference;
        }

        public static T DeserializeItem<T>(string item)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(item));
            return (T)serializer.ReadObject(stream);
        }
    }
}
