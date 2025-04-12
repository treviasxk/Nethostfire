// Software Developed by Trevias Xk
// Social Networks:     treviasxk
// Github:              https://github.com/treviasxk

using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Nethostfire{
    public class DataSecurity {
        static Aes AES = Aes.Create();
        public static string? PublicKeyRSA, PrivateKeyRSA;
        public static byte[]? PrivateKeyAES;
        
        internal static void GenerateKey(int SymmetricSize){
            if(PrivateKeyAES == null){
                using(var RSA = new RSACryptoServiceProvider()){
                    int value = (SymmetricSize - 6) * 8 + 384;
                    if(value >= 464 && value <= 4096){
                        PrivateKeyRSA = RSA.ToXmlString(true);
                        PublicKeyRSA = RSA.ToXmlString(false);
                        PrivateKeyAES = GetHashMD5(Encoding.ASCII.GetBytes(PrivateKeyRSA));
                    }else
                        throw new Nethostfire("RSA SymmetricSize cannot be less than " + ((464 - 384) / 8 + 6) + " or greater than " + ((4096 - 384) / 8 + 6));
                }
            }
        }


        public static byte[] Compress(byte[] bytes){
            try{
                MemoryStream output = new();
                using(DeflateStream dstream = new(output, CompressionMode.Compress)){
                    dstream.Write(bytes, 0, bytes.Length);
                }
                return output.ToArray();
            }catch{
                return [];
            }
        }

        public static byte[] Decompress(byte[] data){
            try{
                MemoryStream input = new(data);
                MemoryStream output = new();
                using(DeflateStream dstream = new(input, CompressionMode.Decompress))
                    dstream.CopyTo(output);
                return output.ToArray();
            }catch{
                return [];
            }
        }

        public static byte[] EncryptRSA(byte[] bytes, string? publicKeyRSA){
            using(var RSA = new RSACryptoServiceProvider())
                try{
                    if(publicKeyRSA != null){
                        RSA.FromXmlString(publicKeyRSA);
                        return RSA.Encrypt(bytes, true);
                    }else
                        return [];
                }catch{
                    var b = ((RSA.KeySize - 384) / 8) + 6;
                    if(b < bytes.Length)
                        throw new Nethostfire("The key size defined in KeySizeBytesRSA, can only encrypt at most " + b + " bytes.");
                    return [];
                }
        }

        public static byte[] GetHashMD5(byte[] bytes){
            try{
                MD5 md5 = MD5.Create();
                return md5.ComputeHash(bytes);
            }catch{
                return [];
            }
        }

        public static byte[] DecryptRSA(byte[] bytes, string? privateKeyRSA){
            using(var RSA = new RSACryptoServiceProvider())
            try{
                if(privateKeyRSA != null){
                    RSA.FromXmlString(privateKeyRSA);
                    return RSA.Decrypt(bytes, true);
                }else
                    return [];
            }catch{
                var b = ((RSA.KeySize - 384) / 8) + 6;
                if(b < bytes.Length)
                    throw new Nethostfire("The key size defined in KeySizeBytesRSA, can only decrypt at most " + b + " bytes.");
                return [];
            }
        }

        public static byte[] EncryptAES(byte[] bytes, byte[]? privateKeyAES){
            try{
                if(privateKeyAES != null){
                    using var encryptor = AES.CreateEncryptor(privateKeyAES, privateKeyAES);
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }else
                    return [];
            }
            catch{
                return [];
            }
        }
        public static byte[] DecryptAES(byte[] bytes, byte[]? privateKeyAES){
            try{
                if(privateKeyAES != null){
                    using var encryptor = AES.CreateDecryptor(privateKeyAES, privateKeyAES);
                    return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
                }else
                    return [];
            }
            catch{
                return [];
            }
        }

        public static byte[] EncryptBase64(byte[] bytes){
            try{
                return Encoding.ASCII.GetBytes(Convert.ToBase64String(bytes));
            }catch{
                return [];
            }
        }

        public static byte[] DecryptBase64(byte[] bytes){
            try{
                return Convert.FromBase64String(Encoding.ASCII.GetString(bytes));
            }catch{
                return [];
            }
        }
    }
}