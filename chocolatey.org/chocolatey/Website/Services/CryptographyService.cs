// Copyright 2011 - Present RealDimensions Software, LLC, the original 
// authors/contributors from ChocolateyGallery
// at https://github.com/chocolatey/chocolatey.org,
// and the authors/contributors of NuGetGallery 
// at https://github.com/NuGet/NuGetGallery
//  
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//   http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Helpers;

namespace NuGetGallery
{
    public class CryptographyService : ICryptographyService
    {
        private const int SaltLengthInBytes = 16;

        public string GenerateHash(
            byte[] input,
            string hashAlgorithmId = Constants.Sha512HashAlgorithmId)
        {
            byte[] hashBytes;

            using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmId))
            {
                hashBytes = hashAlgorithm.ComputeHash(input);
            }

            var hash = Convert.ToBase64String(hashBytes);
            return hash;
        }

        public string GenerateSaltedHash(
            string input,
            string hashAlgorithmId)
        {
            if (hashAlgorithmId.Equals(Constants.PBKDF2HashAlgorithmId, StringComparison.OrdinalIgnoreCase)) return Crypto.HashPassword(input);

            return GenerateLegacySaltedHash(input, hashAlgorithmId);
        }

        private static string GenerateLegacySaltedHash(string input, string hashAlgorithmId)
        {
            var saltBytes = new byte[SaltLengthInBytes];

            using (var cryptoProvider = new RNGCryptoServiceProvider())
            {
                cryptoProvider.GetNonZeroBytes(saltBytes);
            }

            var textBytes = Encoding.Unicode.GetBytes(input);

            var saltedTextBytes = new byte[saltBytes.Length + textBytes.Length];
            Array.Copy(saltBytes, saltedTextBytes, saltBytes.Length);
            Array.Copy(textBytes, 0, saltedTextBytes, saltBytes.Length, textBytes.Length);

            byte[] hashBytes;
            using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmId))
            {
                hashBytes = hashAlgorithm.ComputeHash(saltedTextBytes);
            }

            var saltPlusHashBytes = new byte[saltBytes.Length + hashBytes.Length];
            Array.Copy(saltBytes, saltPlusHashBytes, saltBytes.Length);
            Array.Copy(hashBytes, 0, saltPlusHashBytes, saltBytes.Length, hashBytes.Length);

            var saltedHash = Convert.ToBase64String(saltPlusHashBytes);
            return saltedHash;
        }

        public string GenerateToken()
        {
            var data = new byte[0x10];

            using (var crypto = new RNGCryptoServiceProvider())
            {
                crypto.GetBytes(data);

                return HttpServerUtility.UrlTokenEncode(data).Replace('_', '-');
            }
        }

        public bool ValidateHash(
            string hash,
            byte[] input,
            string hashAlgorithmId = Constants.Sha512HashAlgorithmId)
        {
            return hash.Equals(GenerateHash(input));
        }

        public bool ValidateSaltedHash(
            string hash,
            string input,
            string hashAlgorithmId)
        {
            if (hashAlgorithmId.Equals(Constants.PBKDF2HashAlgorithmId, StringComparison.OrdinalIgnoreCase)) return Crypto.VerifyHashedPassword(hashedPassword: hash, password: input);

            return ValidateLegacySaltedHash(hash, input, hashAlgorithmId);
        }

        private static bool ValidateLegacySaltedHash(string hash, string input, string hashAlgorithmId)
        {
            var saltPlusHashBytes = Convert.FromBase64String(hash);

            var saltBytes = saltPlusHashBytes.Take(SaltLengthInBytes).ToArray();
            var hashToValidateBytes = saltPlusHashBytes.Skip(SaltLengthInBytes).ToArray();

            var textBytes = Encoding.Unicode.GetBytes(input);

            var saltedTextBytes = new byte[saltBytes.Length + textBytes.Length];
            Array.Copy(saltBytes, saltedTextBytes, saltBytes.Length);
            Array.Copy(textBytes, 0, saltedTextBytes, saltBytes.Length, textBytes.Length);

            byte[] hashBytes;
            using (var hashAlgorithm = HashAlgorithm.Create(hashAlgorithmId))
            {
                hashBytes = hashAlgorithm.ComputeHash(saltedTextBytes);
            }

            for (int i = 0; i < hashBytes.Length; i++)
            {
                if (!hashBytes[i].Equals(hashToValidateBytes[i])) return false;
            }

            return true;
        }
    }
}
