using System;
using System.Threading.Tasks;

namespace SharedMoleRes.Client.Crypto
{
    public interface ISign : IAsymmetricKeysExchange, IDisposable
    {
        byte[] SignData(byte[] dataForSign);
        byte[] SignData<TParams>(byte[] dataForSign, TParams additionalParams);
        Task<byte[]> SignDataAsync(byte[] dataForSign);
        Task<byte[]> SignDataAsync<TParams>(byte[] dataForSign, TParams additionalParams);
        bool VerifySign(byte[] dataForSign, byte[] sign);
        bool VerifySign<TParams>(byte[] dataForSign, byte[] sign, TParams additionalParams);
        Task<bool> VerifySignAsync(byte[] dataForSign, byte[] sign);
        Task<bool> VerifySignAsync<TParams>(byte[] dataForSign, byte[] sign, TParams additionalParams);
    }
}
