using System.Security.Cryptography;
using System.Text;

namespace Midnight.EC.Plant.WEB.Utility.Hash;

public static class HashHelper
{
    public static string ComputeSha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
