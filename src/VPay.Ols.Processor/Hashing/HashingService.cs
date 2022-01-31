using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace VPay.Ols.Processor.Hashing;

/// <summary>
/// Implementation of the <see cref="IHashingService{THashAlgorithm}"/>.
/// </summary>
/// <typeparam name="THashAlgorithm">The type of the <see cref="HashAlgorithm"/> to use for hashing.</typeparam>
/// <example>
/// This sample shows how to use the <see cref="HashingService{THashAlgorithm}"/>.
/// <code>
/// class TestClass
/// {
///     static int Main()
///     {
///         var hashingService = new HashingService{MD5CryptoServiceProvider}();
///         Console.WriteLine(hashingService.ComputeHash("a test string"));
///     }
/// }
/// </code>
/// </example>
public class HashingService<THashAlgorithm> : IHashingService<THashAlgorithm>
    where THashAlgorithm : HashAlgorithm, new()
{
    /// <summary>
    /// Hashes the input using the hashing algorithm.
    /// </summary>
    /// <param name="input">The value to be hashed.</param>
    /// <returns>The hashed value</returns>
    public string ComputeHash(string input)
    {
        using var algorithm = new THashAlgorithm();
        byte[] bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

        return bytes.Aggregate(
            new StringBuilder(),
            (sb, @byte) => sb.Append(@byte.ToString("x2")))
            .ToString();
    }

    /// <summary>
    /// Hashes the stream using the hashing algorithm.
    /// </summary>
    /// <param name="stream">The stream to be hashed.</param>
    /// <returns>The hashed value</returns>
    public string ComputeHash(Stream stream)
    {
        using var algorithm = new THashAlgorithm();
        byte[] bytes = algorithm.ComputeHash(stream);

        return bytes.Aggregate(
            new StringBuilder(),
            (sb, @byte) => sb.Append(@byte.ToString("x2")))
            .ToString();
    }

    /// <summary>
    /// Hashes the byte array using the hashing algorithm.
    /// </summary>
    /// <param name="buffer">The byte array to be hashed.</param>
    /// <returns>The hashed value</returns>
    public string ComputeHash(byte[] buffer)
    {
        using var algorithm = new THashAlgorithm();
        byte[] bytes = algorithm.ComputeHash(buffer);

        return bytes.Aggregate(
            new StringBuilder(),
            (sb, @byte) => sb.Append(@byte.ToString("x2")))
            .ToString();
    }
}
