// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk
// Paypal:              trevias@live.com

namespace Nethostfire{
    public class Json {
        /// <summary>
        /// Convert object to JSON.
        /// </summary>
        public static string Convert(object data){
            return AssemblyDynamic.Get("Newtonsoft.Json", "Newtonsoft.Json.JsonConvert", new(){MethodName =  "SerializeObject", Params = [data]}) ?? "";
        }

        /// <summary>
        /// Desconvert JSON to object.
        /// </summary>
        public static T? Deconvert<T>(string json){
            return (T?)AssemblyDynamic.Get("Newtonsoft.Json", "Newtonsoft.Json.JsonConvert", new(){MethodName =  "DeserializeObject", Params = [json]})?.ToObject<T>();
        }
    }
}