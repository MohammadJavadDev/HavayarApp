
using Newtonsoft.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Serialization;

namespace Common.Utilities
{
    public static class ObjectExtensions
    {
        public static string JsonSerialize(this object? obj , bool notCamelCase = false)
        {
               if (obj == null)
               {
                    return "";
               }
            if (notCamelCase == false)
            {
                DefaultContractResolver contractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                    {
                        ProcessDictionaryKeys = true
                    }
                };

                var jsonSerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = contractResolver,
                    Formatting = Formatting.Indented
                };
                return JsonConvert.SerializeObject(obj,jsonSerializerSettings);

            }
            return JsonConvert.SerializeObject(obj);
          
        }

        public static T? JsonDeserialize<T>(this string? obj)
        {
               if(obj == null)
               {
                    return default(T?);
               }

            return JsonConvert.DeserializeObject<T>(obj);
        }

    }
}
