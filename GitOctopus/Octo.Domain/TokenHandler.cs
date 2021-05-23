using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace Octo.Domain
{
    [Serializable]
    public class TokenHandler
    {
        const string MagicFileName = "magic";
        public string Token { get; set; }
        public bool isTokenValid = false;
        public string CurDir { get; set; }

        public TokenHandler()
        {
            CurDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string MagicFilePath = Path.Combine(CurDir, MagicFileName);

            if (File.Exists(MagicFilePath))
            {
                Token = LoadTokenFromDisc();
                isTokenValid = true;
            }
        }
        /// <summary>
        /// Saves Encrypted code to Token variable
        /// </summary>
        /// <param name="InToken">Unencrypted Token</param>
        public void SaveEncryptedToken(string InToken)
        {
            byte[] iv = new byte[16];
            byte[] array;
            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Octo.Domain.Properties.Resources.PassvordToken);
                aes.IV = iv;

                ICryptoTransform Encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memstrm = new MemoryStream())
                {
                    using (CryptoStream cstream = new CryptoStream((Stream)memstrm, Encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter wrt = new StreamWriter((Stream)cstream))
                        {
                            wrt.WriteLine(InToken);
                        }
                        array = memstrm.ToArray();
                    }
                }
            }

            isTokenValid = true;
            Token = Convert.ToBase64String(array);
        }

        /// <summary>
        /// Gets Encrypted Token
        /// </summary>
        /// <returns>Encrypted Token</returns>
        public string GetEncryptedToken()
        {
            byte[] iv = new byte[16];
            byte[] buf = Convert.FromBase64String(Token);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(Octo.Domain.Properties.Resources.PassvordToken);
                aes.IV = iv;
                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream strm = new MemoryStream(buf))
                {
                    using (CryptoStream cstrm = new CryptoStream((Stream)strm, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader streamReader = new StreamReader((Stream)cstrm))
                        {
                            string a = streamReader.ReadToEnd();
                            a = a.Substring(0, a.Length - 2);
                            return a;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Saves token to disc
        /// </summary>
        public void SaveTokenToDisc()
        {
            string MagicFilePath = Path.Combine(CurDir, MagicFileName);
            List<byte> MagicBuf = new List<byte>();

            MagicBuf.AddRange(Encoding.UTF8.GetBytes(Token));
            MagicBuf.AddRange(Encoding.UTF8.GetBytes(GetMD5Checksum(MagicBuf.ToArray())));

            isTokenValid = true;

            File.WriteAllBytes(MagicFilePath, MagicBuf.ToArray());
        }

        /// <summary>
        /// Loads encrypted token from disc
        /// </summary>
        /// <returns>Encrypted token</returns>
        public string LoadTokenFromDisc()
        {
            try
            {
                string MagicFilePath = Path.Combine(CurDir, MagicFileName);

                List<byte> MagicFileComplete = new List<byte>();
                MagicFileComplete.AddRange(File.ReadAllBytes(MagicFilePath));

                List<byte> MagicCode = new List<byte>();

                for (int i = 0; i < MagicFileComplete.Count - 32; i++)
                {
                    MagicCode.Add(MagicFileComplete[i]);
                }

                string CheckSumOne = Encoding.UTF8.GetString(MagicCode.ToArray());
                string CheckSumTwo = GetMD5Checksum(MagicCode.ToArray());
                MagicCode.Clear();

                for (int i = MagicFileComplete.Count - 32; i < MagicFileComplete.Count; i++)
                {
                    MagicCode.Add(MagicFileComplete[i]);
                }

                string CheckSumThree = Encoding.UTF8.GetString(MagicCode.ToArray());

                if (CheckSumThree != CheckSumTwo)
                {
                    //hellup
                    return "";
                }

                isTokenValid = true;
                return CheckSumOne;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return "";
            }
        }

        /// <summary>
        /// Calculates MD5 Checksum of byte array
        /// </summary>
        /// <param name="Dat">Data to be parsed</param>
        /// <returns>MD5 checksum</returns>
        public string GetMD5Checksum(byte[] Dat)
        {
            using (MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Dat);
                return BitConverter.ToString(hash).Replace("-", "");
            }
        }

        /// <summary>
        /// Checks if you're properly logged in
        /// </summary>
        /// <returns>True if logged in</returns>
        public bool checkLoggedIn()
        {
            if (GetEncryptedToken() != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Deletes token && logs out.
        /// </summary>
        /// <returns>True if successful</returns>
        public bool deleteToken()
        {
            try
            {
                string MagicFilePath = Path.Combine(CurDir, MagicFileName);
                File.Delete(MagicFilePath);
                Controller.Instance.OnLoggedIn(false);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }
    }
}