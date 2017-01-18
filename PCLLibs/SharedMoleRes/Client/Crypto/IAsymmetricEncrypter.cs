using System;
using System.Threading.Tasks;

namespace SharedMoleRes.Client.Crypto
{
    public interface IAsymmetricEncrypter : IAsymmetricKeysExchange, IDisposable
    {
        byte[] Encrypt(byte[] dataForEncryption);
        byte[] Encrypt<TParams>(byte[] dataForEncryption, TParams additionalParams);
        Task<byte[]> EncryptAsync(byte[] dataForEncryption);
        Task<byte[]> EncryptAsync<TParams>(byte[] dataForEncryption, TParams additionalParams);
        byte[] Decrypt(byte[] dataForDecryption);
        byte[] Decrypt<TParams>(byte[] dataForDecryption, TParams additionalParams);
        Task<byte[]> DecryptAsync(byte[] dataForDecryption);
        Task<byte[]> DecryptAsync<TParams>(byte[] dataForDecryption, TParams additionalParams);
    }
}
