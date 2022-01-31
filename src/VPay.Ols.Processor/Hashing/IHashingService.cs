using System.IO;
using System.Security.Cryptography;

namespace VPay.Ols.Processor.Hashing;

/// <summary>
/// Definition of a service providing the ability to hash a value.
/// </summary>
/// <typeparam name="THashAlgorithm">The type of the <see cref="HashAlgorithm"/> to use for hashing.</typeparam>
public interface IHashingService<THashAlgorithm>
    where THashAlgorithm : HashAlgorithm, new()
{
    /// <summary>
    /// Hashes the input using the hashing algorithm.
    /// </summary>
    /// <param name="input">The value to be hashed.</param>
    /// <returns>The hashed value</returns>
    string ComputeHash(string input);

    /// <summary>
    /// Hashes the stream using the hashing algorithm.
    /// </summary>
    /// <param name="stream">The stream to be hashed.</param>
    /// <returns>The hashed value</returns>
    string ComputeHash(Stream stream);

    /// <summary>
    /// Hashes the byte array using the hashing algorithm.
    /// </summary>
    /// <param name="buffer">The byte array to be hashed.</param>
    /// <returns>The hashed value</returns>
    string ComputeHash(byte[] buffer);
}
