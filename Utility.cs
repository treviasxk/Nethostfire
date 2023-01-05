// Software desenvolvido por Trevias Xk
// Redes sociais:       treviasxk
// Github:              https://github.com/treviasxk

using System.Collections.Concurrent;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Nethostfire{
        public class Utility{
        static readonly ConcurrentQueue<Action> ListRunOnMainThread = new ConcurrentQueue<Action>();
        /// <summary>
        /// Executa ações dentro da thread principal do software, é utilizado para manipular objetos 3D na Unity.
        /// </summary>
        public static void RunOnMainThread(Action action){
            ListRunOnMainThread.Enqueue(action);
        }
        /// <summary>
        /// Utilizado para definir a thread principal que irá executar as ações do RunOnMainThread(). Coloque essa ação dentro da função void Update() na Unity.
        /// </summary>
        public static void ThisMainThread() {
            if (ListRunOnMainThread.Count > 0) {
                while (ListRunOnMainThread.TryDequeue(out var action)) {
                    try{
                        action?.Invoke();
                    }catch{
                        throw;
                    }
                }
            }
        }
        /// <summary>
        /// Computar o valor hash MD5 de bytes.
        /// </summary>
        public static byte[] GetHashMD5(byte[] _byte){
            try{
                MD5 md5 = MD5.Create();
                return md5.ComputeHash(_byte);
            }catch{
                return new byte[]{};
            }
        }
        /// <summary>
        /// Criptografar bytes em MD5.
        /// </summary>
        public static string EncryptMD5(byte[] _byte){
            try{
                MD5 md5 = MD5.Create();
                return BitConverter.ToString(md5.ComputeHash(_byte)).Replace("-", string.Empty).ToLower();
            }catch{
                return "";
            }
        }
        /// <summary>
        /// Criptografar bytes em Base64.
        /// </summary>
        public static string EncryptBase64(byte[] _byte){
            return Convert.ToBase64String(_byte);
        }
        /// <summary>
        /// Descriptografar Base64 em bytes.
        /// </summary>
        public static byte[] DecryptBase64(string _text){
            return Convert.FromBase64String(_text);
        }

        /// <summary>
        /// Compactar bytes.
        /// </summary>
        public static byte[] Compress(byte[] _byte){
            try{
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(output, CompressionMode.Compress)){
                    dstream.Write(_byte, 0, _byte.Length);
                }
                return output.ToArray();
            }catch{
                return new byte[]{};
            }
        }
        /// <summary>
        /// Descompactar bytes.
        /// </summary>
        public static byte[] Decompress(byte[] data){
            try{
                MemoryStream input = new MemoryStream(data);
                MemoryStream output = new MemoryStream();
                using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress)){
                    dstream.CopyTo(output);
                }
                return output.ToArray();
            }catch{
                return new byte[]{};
            }
        }
    }
}