using System;
using System.Security.Cryptography;

namespace GarageJobCards.Infrastructure
{
 // Simple, dependency-free password hashing. Good enough for a small internal
 // tool; if this ever handles real customer data at scale, swap to ASP.NET Identity.
 public static class PasswordHelper
 {
 private const int SaltSize = 16;
 private const int HashSize = 32;
 private const int Iterations = 100000;

 public static void CreateHash(string password, out string hash, out string salt)
 {
 byte[] saltBytes = new byte[SaltSize];
 using (var rng = RandomNumberGenerator.Create())
 rng.GetBytes(saltBytes);

 byte[] hashBytes = HashPassword(password, saltBytes);

 hash = Convert.ToBase64String(hashBytes);
 salt = Convert.ToBase64String(saltBytes);
 }

 public static bool VerifyPassword(string password, string storedHash, string storedSalt)
 {
 byte[] saltBytes = Convert.FromBase64String(storedSalt);
 byte[] hashBytes = HashPassword(password, saltBytes);
 string computedHash = Convert.ToBase64String(hashBytes);
 return computedHash == storedHash;
 }

 private static byte[] HashPassword(string password, byte[] saltBytes)
 {
 using (var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, Iterations, HashAlgorithmName.SHA256))
 {
 return pbkdf2.GetBytes(HashSize);
 }
 }
 }
}
