using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JabyLib.Other;
using SharedMoleRes.Server;

namespace MolePushServerLibPcl
{
    //public class PartOfMessage<TMes> where TMes: class
    //{
    //    /// <exception cref="SerializationException">Во время десиарелизации объекта произошла ошибка. 
    //    /// Source base type: <see cref="CustomBinarySerializerBase"/>.</exception>
    //    public PartOfMessage(TMes message, CustomBinarySerializerBase serializer, short numbOfPart = -1)
    //    {
    //        if (message == null) throw new ArgumentNullException(nameof(message)) {Source = GetType().AssemblyQualifiedName};
    //        if (serializer == null) throw new ArgumentNullException(nameof(serializer)) {Source = GetType().AssemblyQualifiedName};

    //        EncryptedMessage = serializer.Serialize(message, false);
    //        NextAddress = "none";
    //        NumbOfPart = numbOfPart;

    //    }
    //    public PartOfMessage(TMes message, ICryptoTransform encryptor, CustomBinarySerializerBase serializer, short numbOfPart = -1) : this(message, serializer, numbOfPart)
    //    {
    //        if (encryptor == null) throw new ArgumentNullException(nameof(encryptor)) { Source = GetType().AssemblyQualifiedName };

    //        EncryptedMessage = encryptor.TransformFinalBlock(EncryptedMessage, 0, EncryptedMessage.Length);
    //        IsEncryptedMessage = true;
    //    }
    //    /// <exception cref="NotImplementedException">Метод RSA.Encrypt не реализован.</exception>
    //    public PartOfMessage(TMes message, RSA rsa, CustomBinarySerializerBase serializer, short numbOfPart = -1) : this(message, serializer, numbOfPart)
    //    {
    //        if (rsa == null) throw new ArgumentNullException(nameof(rsa)) { Source = GetType().AssemblyQualifiedName };

    //        try
    //        {
    //            EncryptedMessage = rsa.Encrypt(EncryptedMessage, RSAEncryptionPadding.Pkcs1);
    //            IsAssymAlg = true;
    //            IsEncryptedMessage = true;
    //        }
    //        catch (NotImplementedException ex)
    //        {
    //            throw new NotImplementedException("Метод RSA.Encrypt не реализован.") {Source = GetType().AssemblyQualifiedName};
    //        }
            
    //    }


    //    public string NextAddress { get; protected set; }
    //    public byte[] EncryptedMessage { get; protected set; }
    //    public bool IsAssymAlg { get; protected set; }
    //    public CryptoProvider CryptoProvider { get; protected set; } = CryptoProvider.CngMicrosoft;
    //    public bool IsEncryptedMessage { get; protected set; }
    //    public short NumbOfPart { get; protected set; }


    //    public static PartOfMessage<TMes>[] CreateParts(TMes message, CustomBinarySerializerBase serializer, ICryptoTransform encryptor = null)
    //    {
    //        if (message == null) throw new ArgumentNullException(nameof(message)) {Source = typeof(PartOfMessage<>).FullName };
    //        if (serializer == null) throw new ArgumentNullException(nameof(serializer)) { Source = typeof(PartOfMessage<>).FullName };

    //        if (encryptor == null)
    //        {
    //            using (var aes = new AesCng())
    //            {
    //                aes.GenerateIV();
    //                aes.GenerateKey();
    //                encryptor = aes.CreateEncryptor();
    //            }
    //        }
    //        var bytesOfMessage = serializer.Serialize(message, false);
    //        var bytesEncOfMess = encryptor.TransformFinalBlock(bytesOfMessage, 0, bytesOfMessage.Length);
    //        var N_u_min = 5;
    //        var N_u_max = 250 * (int) Math.Pow(10, 6);
    //        var N_p_max = 10;
    //        var N_p = 0;

    //        if (bytesEncOfMess.Length < 5)
    //            N_p = 1;
    //        else
    //        {
    //            if (bytesEncOfMess.Length >= N_u_min && bytesEncOfMess.Length < N_u_max)
    //            {
    //                while (bytesEncOfMess.Length / N_u_min > N_p_max)
    //                    N_u_min *= 2;
    //                if (bytesEncOfMess.Length%N_u_min > 0)
    //                    N_p += 1;
    //                N_p += bytesEncOfMess.Length/N_u_min;
    //            }
    //            else
    //            {
    //                while (bytesEncOfMess.Length / N_u_max > 3)
    //                    N_u_max *= 2;
    //                if (bytesEncOfMess.Length % N_u_max > 0)
    //                    N_p += 1;
    //                N_p += bytesEncOfMess.Length / N_u_max;
    //            }
    //        }

    //        var parts = new PartOfMessage<byte[]>[N_p];
    //        var stream = new MemoryStream(bytesEncOfMess);
    //        var lengthOfOnePart = bytesEncOfMess.Length/N_p;
    //        for (int i = 0; i < N_p; i++)
    //        {
    //            var part = lengthOfOnePart > stream.Length - stream.Position
    //                ? new byte[stream.Length - stream.Position]
    //                : new byte[lengthOfOnePart];
    //            parts[i] = new PartOfMessage<byte[]>(part, serializer, (short)i);
    //        }
    //        //var 
    //        throw new NotImplementedException();

    //    }

    //    /// <exception cref="InvalidOperationException">Свойство <see cref="CryptoProvider"/> не равно CngMicrosoft. -or- 
    //    /// Сообщение зашифрованное симметричным алгоритмом не может быть расшифровано ассиметричным.</exception>
    //    /// <exception cref="ArgumentNullException"><see cref="EncryptedMessage"/> == null. -or- serializer == null.</exception>
    //    /// <exception cref="ArgumentException">parameters contains neither an exponent nor a modulus.</exception>
    //    /// <exception cref="CryptographicException">parameters is not a valid RSA key. -or- 
    //    /// parameters is a full key pair and the default KSP is used.</exception>
    //    /// <exception cref="SerializationException">Во время десиарелизации объекта произошла ошибка. 
    //    /// Source base class: <see cref="CustomBinarySerializerBase"/>.</exception>
    //    public TMes GetDecryptedMessage(RSAParameters rsaParameters, CustomBinarySerializerBase serializer)
    //    {
    //        if (CryptoProvider != CryptoProvider.CngMicrosoft)
    //            throw new InvalidOperationException("Свойство CryptoProvider не равно CryptoProvider.CngMicrosoft.") {Source = GetType().AssemblyQualifiedName};
    //        if (!IsAssymAlg)
    //            throw new InvalidOperationException(
    //                "Сообщение зашифрованное симметричным алгоритмом не может быть расшифровано ассиметричным.")
    //            { Source = GetType().AssemblyQualifiedName };
    //        if (EncryptedMessage == null)
    //            throw new ArgumentNullException(nameof(EncryptedMessage)) { Source = GetType().AssemblyQualifiedName };
    //        if (serializer == null)
    //            throw new ArgumentNullException(nameof(serializer)) { Source = GetType().AssemblyQualifiedName };

    //        try
    //        {
    //            var rsa = new RSACng();
    //            rsa.ImportParameters(rsaParameters);
    //            var decryptedBytes = rsa.Decrypt(EncryptedMessage, RSAEncryptionPadding.OaepSHA1);
    //            return serializer.Deserialize<TMes>(decryptedBytes, false);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            throw CreateException(0, ex, rsaParameters);
    //        }
    //        catch (CryptographicException ex)
    //        {
    //            throw CreateException(0, ex, rsaParameters);
    //        }

    //    }
    //    /// <exception cref="InvalidOperationException">Свойство <see cref="CryptoProvider"/> не равно CngMicrosoft. -or- 
    //    /// Сообщение зашифрованное симметричным алгоритмом не может быть расшифровано ассиметричным.</exception>
    //    /// <exception cref="ArgumentNullException"><see cref="EncryptedMessage"/> == null. -or- serializer == null.</exception>
    //    /// <exception cref="ArgumentException">parameters contains neither an exponent nor a modulus.</exception>
    //    /// <exception cref="CryptographicException">parameters is not a valid RSA key. -or- 
    //    /// parameters is a full key pair and the default KSP is used.</exception>
    //    /// <exception cref="SerializationException">Во время десиарелизации объекта произошла ошибка. 
    //    /// Source base class: <see cref="CustomBinarySerializerBase"/>.</exception>
    //    public async Task<TMes> GetDecryptedMessageAsync(RSAParameters rsaParameters,
    //        CustomBinarySerializerBase serializer)
    //    {
    //        if (CryptoProvider != CryptoProvider.CngMicrosoft)
    //            throw new InvalidOperationException("Свойство CryptoProvider не равно CryptoProvider.CngMicrosoft.") { Source = GetType().AssemblyQualifiedName };
    //        if (!IsAssymAlg)
    //            throw new InvalidOperationException(
    //                "Сообщение зашифрованное симметричным алгоритмом не может быть расшифровано ассиметричным.")
    //            { Source = GetType().AssemblyQualifiedName };
    //        if (EncryptedMessage == null)
    //            throw new ArgumentNullException(nameof(EncryptedMessage)) { Source = GetType().AssemblyQualifiedName };
    //        if (serializer == null)
    //            throw new ArgumentNullException(nameof(serializer)) { Source = GetType().AssemblyQualifiedName };

    //        try
    //        {
    //            var rsa = new RSACng();
    //            rsa.ImportParameters(rsaParameters);
    //            var decryptedBytes = await Task.Run(() => rsa.Decrypt(EncryptedMessage, RSAEncryptionPadding.OaepSHA1));
    //            return serializer.Deserialize<TMes>(decryptedBytes, false);
    //        }
    //        catch (ArgumentException ex)
    //        {
    //            throw CreateException(0, ex, rsaParameters);
    //        }
    //        catch (CryptographicException ex)
    //        {
    //            throw CreateException(0, ex, rsaParameters);
    //        }
    //    }
    //    /// <exception cref="ArgumentNullException">keyData == null. -or- serializer == null.</exception>
    //    /// <exception cref="InvalidOperationException">Сообщение зашифрованное ассиметричным алгоритмом не может 
    //    /// быть расшифровано симметричным.</exception>
    //    /// <exception cref="ArgumentException">Длина переданного симметричного ключа не верна для этого алгоритма. -or- 
    //    /// Длина переданного вектора инициализации не верна для этого алгоритма.</exception>
    //    /// <exception cref="CryptographicException">Переданный ключ симметричного шифрования не является криптографически стойким.</exception>
    //    public TMes GetDecryptedMessage(KeyDataForSymmetricAlgorithm keyData,
    //        CustomBinarySerializerBase serializer)
    //    {
    //        if (keyData == null)
    //            throw new ArgumentNullException(nameof(keyData)) {Source = GetType().AssemblyQualifiedName};
    //        if (serializer == null)
    //            throw new ArgumentNullException(nameof(serializer)) { Source = GetType().AssemblyQualifiedName };
    //        if (IsAssymAlg)
    //            throw new InvalidOperationException(
    //                "Сообщение зашифрованное ассиметричным алгоритмом не может быть расшифровано симметричным.")
    //            { Source = GetType().AssemblyQualifiedName };

    //        try
    //        {
    //            if (CryptoProvider == CryptoProvider.CngMicrosoft)
    //            {
    //                var aes = new AesCng();
    //                var decryptor = aes.CreateDecryptor(keyData.SymmetricKeyBlob, keyData.SymmetricIvBlob);
    //                var decryptedBytes = decryptor.TransformFinalBlock(EncryptedMessage, 0, EncryptedMessage.Length);
    //                return serializer.Deserialize<TMes>(decryptedBytes, false);
    //            }
    //            throw new NotImplementedException() {Source = GetType().AssemblyQualifiedName};
    //        }
    //        catch (Exception ex) when (ex is ArgumentException || ex is CryptographicException)
    //        {
    //            throw CreateException(1, ex, keyData);
    //        }
            
    //    }
    //    /// <exception cref="ArgumentNullException">keyData == null. -or- serializer == null.</exception>
    //    /// <exception cref="InvalidOperationException">Сообщение зашифрованное ассиметричным алгоритмом не может 
    //    /// быть расшифровано симметричным.</exception>
    //    /// <exception cref="ArgumentException">Длина переданного симметричного ключа не верна для этого алгоритма. -or- 
    //    /// Длина переданного вектора инициализации не верна для этого алгоритма.</exception>
    //    /// <exception cref="CryptographicException">Переданный ключ симметричного шифрования не является криптографически стойким.</exception>
    //    public async Task<TMes> GetDecryptedMessageAsync(KeyDataForSymmetricAlgorithm keyData,
    //        CustomBinarySerializerBase serializer)
    //    {
    //        if (keyData == null)
    //            throw new ArgumentNullException(nameof(keyData)) { Source = GetType().AssemblyQualifiedName };
    //        if (serializer == null)
    //            throw new ArgumentNullException(nameof(serializer)) { Source = GetType().AssemblyQualifiedName };
    //        if (IsAssymAlg)
    //            throw new InvalidOperationException(
    //                "Сообщение зашифрованное ассиметричным алгоритмом не может быть расшифровано симметричным.")
    //            { Source = GetType().AssemblyQualifiedName };

    //        try
    //        {
    //            if (CryptoProvider == CryptoProvider.CngMicrosoft)
    //            {
    //                var aes = new AesCng();
    //                var decryptor = aes.CreateDecryptor(keyData.SymmetricKeyBlob, keyData.SymmetricIvBlob);
    //                var decryptedBytes = await Task.Run(() => decryptor.TransformFinalBlock(EncryptedMessage, 0, EncryptedMessage.Length));
    //                return serializer.Deserialize<TMes>(decryptedBytes, false);
    //            }
    //            throw new NotImplementedException() { Source = GetType().AssemblyQualifiedName };
    //        }
    //        catch (Exception ex) when (ex is ArgumentException || ex is CryptographicException)
    //        {
    //            throw CreateException(1, ex, keyData);
    //        }
    //    }


    //    private Exception CreateException(int numb, params object[] objs)
    //    {
    //        var result = new Exception($"Исключение не было обработано. numb: {numb}.");
    //        var strBuilder = new StringBuilder();
    //        switch (numb)
    //        {
    //            case 0:
    //                var exceptInner = (Exception) objs[0];
    //                switch (exceptInner.Message)
    //                {
    //                    case "parameters contains neither an exponent nor a modulus.":
    //                        result = new ArgumentException(exceptInner.Message, exceptInner) {Source = GetType().AssemblyQualifiedName};
    //                        result.Data.Add("rsaParameters", (RSAParameters)objs[1]);
    //                        break;
    //                    case "parameters is not a valid RSA key.":
    //                        result = new CryptographicException(exceptInner.Message, exceptInner) { Source = GetType().AssemblyQualifiedName };
    //                        result.Data.Add("rsaParameters", (RSAParameters)objs[1]);
    //                        break;
    //                    case "parameters is a full key pair and the default KSP is used.":
    //                        result = new CryptographicException(exceptInner.Message, exceptInner) { Source = GetType().AssemblyQualifiedName };
    //                        result.Data.Add("rsaParameters", (RSAParameters)objs[1]);
    //                        break;
    //                }
    //                break;
    //            case 1:
    //                exceptInner = (Exception) objs[0];
    //                var symmData = (KeyDataForSymmetricAlgorithm) objs[1];
    //                switch (exceptInner.Message)
    //                {
    //                    case "rgbKey is not a valid size for this algorithm.":
    //                        strBuilder.AppendLine("Длина переданного симметричного ключа не верна для этого алгоритма.");
    //                        strBuilder.Append($"Длина переданного симметричного ключа: {symmData.SymmetricKeyBlob}.");
    //                        result = new ArgumentException(strBuilder.ToString(), exceptInner) { Source = GetType().AssemblyQualifiedName };
    //                        break;
    //                    case "rgbIV size does not match the block size for this algorithm.":
    //                        strBuilder.AppendLine("Длина переданного вектора инициализации не верна для этого алгоритма.");
    //                        strBuilder.Append($"Длина переданного вектора инициализации: {symmData.SymmetricIvBlob}.");
    //                        result = new ArgumentException(strBuilder.ToString(), exceptInner) { Source = GetType().AssemblyQualifiedName };
    //                        break;
    //                    case "rgbKey is a known weak key for this algorithm and cannot be used.":
    //                        result =
    //                            new ArgumentException(
    //                                "Переданный ключ симметричного шифрования не является криптографически стойким.",
    //                                exceptInner) {Source = GetType().AssemblyQualifiedName};
    //                        break;
                            
    //                }
    //                break;
    //        }
    //        return result;
    //    }
    //}
}
