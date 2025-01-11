using System.Security.Cryptography;
using System.Text;

namespace Nethostfire{
    class Security {
        static Aes AES = Aes.Create();

        public static bool CheckDDOS(DataClient dataClient, bool background){
            long TimerNow = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            // background is allow only 1000ms for packets, to presev performance and atack DDOS
            if(dataClient.LimitMaxPPS == 0 && !background || TimerNow > dataClient.MaxPPSTimer + (background ? 1000 : (1000 / dataClient.LimitMaxPPS))){
                dataClient.MaxPPSTimer = TimerNow;
                return false;
            }
            return true;
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