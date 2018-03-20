using System;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;

namespace DOTNET_CHIP_8
{
    class HashProvider
    {
        private static readonly string ProfileDir = AppDomain.CurrentDomain.BaseDirectory + "profiles\\";

        private static List<string> HashLibrary
        {
            get
            {
                List<string> lib = new List<string>();
                foreach(string dir in Directory.EnumerateDirectories(ProfileDir))
                {
                    Console.WriteLine(dir.Replace(ProfileDir, String.Empty));
                }
                return lib;
            }
        }

        private static void CreateProfile(string checksum)
        {
            Console.WriteLine("Creating new profile");
            Directory.CreateDirectory(ProfileDir + checksum);
        }

        public static string GenerateHash(byte[] gameData)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = new MemoryStream(gameData))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    string stringHash = BitConverter.ToString(hash).Replace("-", String.Empty).ToLowerInvariant();
                    Console.WriteLine($"Game Data compute hash: " + stringHash);

                    //Create a new profile for each unique rom
                    if(!HashLibrary.Contains(stringHash))
                    {
                        CreateProfile(stringHash);
                    }

                    return stringHash;
                }
            }
        }


    }
}
