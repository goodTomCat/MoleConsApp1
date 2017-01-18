using System;
using System.Security.Cryptography;

namespace SharedMoleRes.Client.Crypto
{
    public interface IAsymmetricKeysExchange
    {
        /// <summary>
        /// Размер ключа в битах.
        /// </summary>
        int KeySize { get; }
        /// <summary>
        /// Размеры ключа в битах, которые поддерживаются данным алгоритмом.
        /// </summary>
        KeySizes[] LegalKeySizes { get; }
        ///// <summary>
        ///// Возвращает размер ключа, выбирающийся по умолчанию.
        ///// </summary>
        //int DefaultKeySize { get; }

        /// <summary>
        /// Задает асимметричные ключи, которые будут использоваться для шифрования.
        /// </summary>
        /// <param name="keys">Асимметричный ключ или ключи, заданные в виде массива байтов.</param>
        /// <exception cref="ArgumentNullException">keys == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Длина массива с асимметричными ключами равна нулю.</exception>
        /// <exception cref="CryptographicException">Любая криптографическая ошибка.</exception>
        void Import(byte[] keys);
        /// <summary>
        /// Задает асимметричный ключ или ключи в виде массива байтов.
        /// </summary>
        /// <param name="includePrivateKey">Задает, нужно ли включать в массив приватные ключи.</param>
        /// <returns></returns>
        byte[] Export(bool includePrivateKey);
    }
}
