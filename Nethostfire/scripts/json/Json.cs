// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

using System.Text;

namespace Nethostfire{
    public class Json {
        /// <summary>
        /// Convert object to JSON.
        /// </summary>
        public static string ToJson(object data){
            return AssemblyDynamic.Get("Newtonsoft.Json", "Newtonsoft.Json.JsonConvert", new(){MethodName =  "SerializeObject", Params = [data]}) ?? "";
        }

        /// <summary>
        /// Convert object to JSON.
        /// </summary>
        public static byte[] GetBytes(object data){
            return Encoding.UTF8.GetBytes(AssemblyDynamic.Get("Newtonsoft.Json", "Newtonsoft.Json.JsonConvert", new(){MethodName =  "SerializeObject", Params = [data]}) ?? "");
        }

        /// <summary>
        /// Desconvert JSON to object.
        /// </summary>
        public static T? FromJson<T>(string json){
            return (T?)AssemblyDynamic.Get("Newtonsoft.Json", "Newtonsoft.Json.JsonConvert", new(){MethodName =  "DeserializeObject", Params = [json]})?.ToObject<T>();
        }

        /// <summary>
        /// Desconvert JSON to object.
        /// </summary>
        public static T? FromJson<T>(byte[] bytes){
            return (T?)AssemblyDynamic.Get("Newtonsoft.Json", "Newtonsoft.Json.JsonConvert", new(){MethodName =  "DeserializeObject", Params = [Encoding.UTF8.GetString(bytes)]})?.ToObject<T>();
        }
    }
}