using System.Text;
using System.Text.Json;

namespace Nethostfire {
    public class Json {
        public static string Serialize(object value) => JsonSerializer.Serialize(value);
        public static T? Deserialize<T>(byte[] _byte) => JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(_byte));
        public static string Deserialize(byte[] _byte) => Encoding.UTF8.GetString(_byte);
    }
}