// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.Text;

namespace Nethostfire{
    public class Json {
        /// <summary>
        /// Convert object to JSON.
        /// </summary>
        public static string? ToJson(object data){
            try{
                return AssemblyDynamic.Get("Newtonsoft.Json", "JsonConvert", new(){MethodName =  "SerializeObject", Params = [data]}) ?? "";
            }catch{
                return default;
            }
        }

        /// <summary>
        /// Convert object to JSON.
        /// </summary>
        public static byte[]? GetBytes(object data){
            try{
                return Encoding.UTF8.GetBytes(AssemblyDynamic.Get("Newtonsoft.Json", "JsonConvert", new(){MethodName =  "SerializeObject", Params = [data]}) ?? "");
            }catch{
                return default;
            }
        }

        /// <summary>
        /// Desconvert JSON to object.
        /// </summary>
        public static T? FromJson<T>(string json){
            try{
                return (T)AssemblyDynamic.Get("Newtonsoft.Json", "JsonConvert", new() {MethodName = "DeserializeObject", Params = [json]})?.ToObject<T>()! ?? default;
            }catch{
                return default;
            }
        }

        /// <summary>
        /// Desconvert JSON to object.
        /// </summary>
        public static T? FromJson<T>(byte[] bytes){
            try{
                return FromJson<T>(Encoding.UTF8.GetString(bytes));
            }catch{
                return default;
            }
        }
    }
}