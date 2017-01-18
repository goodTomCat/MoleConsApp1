using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabyLib.Other;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;

namespace MolePushServerLibPcl
{
    //using CloseAsync =
    //    Func<int /* closeStatus */,
    //        string /* closeDescription */,
    //        CancellationToken /* cancel */,
    //        Task>;
    //using ReceiveMessageAsync = Func<int /*numb of bytes for receiving*/, 
    //    CancellationToken, 
    //    Task<byte[]>>;
    //using SendMessageAsync = Func<ArraySegment<byte>/*message as bytes*/, 
    //    CancellationToken, 
    //    Task>;

    public class MolePushServerReciever
    {
        protected ICryptoTransform EncrypterF;
        protected ICryptoTransform DecryptorF;
        protected IAsymmetricEncrypter AsymmetricEncrypterF;
        protected IAsymmetricEncrypter AsymmetricDecrypterF;
        protected CryptoFactoryBase CryptoFactoryF;
        protected NetworkStream NetStreamF;
        protected CustomBinarySerializerBase SerializerF;
        protected TcpClient ClientF;


        /// <exception cref="ArgumentNullException">websocketContext == null. -или- closeFunc == null. -или- recieveFunc == null. 
        /// -или- sendFunc == null. -или- core == null. -or- ipOfClient == null.</exception>
        public MolePushServerReciever(TcpClient clientF, MolePushServerCoreBase core,
            CustomBinarySerializerBase serializer)
        {
            if (clientF == null)
                throw new ArgumentNullException(nameof(clientF)) {Source = GetType().AssemblyQualifiedName};
            if (core == null)
                throw new ArgumentNullException(nameof(core)) {Source = GetType().AssemblyQualifiedName};
            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer)) {Source = GetType().AssemblyQualifiedName};

            Core = core;
            ClientF = clientF;
            NetStreamF = ClientF.GetStream();
            SerializerF = serializer;
        }


        public virtual uint BytesNumbForNewThread { get; set; } = 100000;
        public MolePushServerCoreBase Core { get; protected set; }
        public bool UserIsAuth { get; protected set; }
        public UserForm Form { get; protected set; }
        public CryptoInfo ChoosenCrypto { get; protected set; }
        public virtual IPEndPoint RemoteEndPoint => (IPEndPoint) ClientF.Client.RemoteEndPoint;
        public ICryptoTransform Encrypter => EncrypterF;
        public ICryptoTransform Decryptor => DecryptorF;
        public IAsymmetricEncrypter AsymmetricEncrypter => AsymmetricEncrypterF;
        public IAsymmetricEncrypter AsymmetricDecrypter => AsymmetricDecrypterF;


        public async Task RunAsync(CancellationToken cancellationToken)
        {
            const int lengthMax = 104857600;
            while (ClientF.Connected)
            {
                try
                {
                    var encByte = new byte[1];
                    await NetStreamF.ReadAsync(encByte, 0, encByte.Length, cancellationToken).ConfigureAwait(false);
                    var isEnc = Convert.ToBoolean(encByte[0]);

                    var bytesOfLength = new byte[4];
                    await
                        NetStreamF.ReadAsync(bytesOfLength, 0, bytesOfLength.Length, cancellationToken)
                            .ConfigureAwait(false);
                    var length = BitConverter.ToInt32(bytesOfLength, 0);
                    if (length > lengthMax || length < 2)
                        await ReturnResultToClientAsync(
                            new ResultOfOperation() {ErrorMessage = "Во время распознавания сообщения возникла ошибка."},
                            true).ConfigureAwait(false);

                    var bytesOfContent = new byte[length];
                    var numbOfReadedBytes =
                        await
                            NetStreamF.ReadAsync(bytesOfContent, 0, bytesOfContent.Length, cancellationToken)
                                .ConfigureAwait(false);
                    while (numbOfReadedBytes != bytesOfContent.Length)
                    {
                        numbOfReadedBytes +=
                            await
                                NetStreamF.ReadAsync(bytesOfContent, numbOfReadedBytes,
                                    bytesOfContent.Length - numbOfReadedBytes, cancellationToken).ConfigureAwait(false);
                    }
                    Stream streamDec;
                    if (isEnc)
                        streamDec = DecryptorF != null
                            ? await ReadCryptoDataAsync(bytesOfContent, false).ConfigureAwait(false)
                            : await ReadCryptoDataAsync(bytesOfContent, true).ConfigureAwait(false);
                    else
                        streamDec = new MemoryStream(bytesOfContent);

                    await ReadCommandAndMapAsync(streamDec).ConfigureAwait(false);

                }
                catch (SocketException ex)
                {

                }
                catch (Exception ex)
                {
                    var type = Type.GetType(ex.Source, false);
                    if (type == null)
                    {
                        ex.Source = GetType().AssemblyQualifiedName;
                        throw;
                    }

                    if (!type.Equals(GetType()))
                    {
                        ex.Source = GetType().AssemblyQualifiedName;
                        throw;
                    }
                }
            }
        }

        private Task ReadCommandAndMapAsync(Stream stream)
        {
            Task resultTask = Task.CompletedTask;
            if (stream.Length >= 2) //UInt16 - 2 byte
            {
                //var stream = new MemoryStream(bytesFromSocket);
                using (stream)
                {
                    var readerBin = new BinaryReader(stream);
                    var command = readerBin.ReadUInt16();
                    switch (command)
                    {
                        case 0:
                            resultTask = GetPublicKeyCommandAsync();
                            break;
                        case 1:
                            resultTask = RegisterNewUserCommandAsync(stream);
                            break;
                        case 2:
                            resultTask = AuthenticateUserCommandAsync(stream);
                            break;
                        case 3:
                            resultTask = SetCryptoAlgs(stream);
                            break;
                        case 4:
                            resultTask = FinedUserCommandAsync(stream);
                            break;
                        case 5:
                            resultTask = UpdateUserDataCommandAsync(stream);
                            break;
                        case 6:
                            resultTask = SendOfflineMessageCommandAsync(stream);
                            break;
                        case 7:
                            resultTask = GetSessionKey(stream);
                            break;
                        case 8:
                            resultTask = GetPossibleCryptoAlgs();
                            break;
                        case 9:
                            resultTask = SetPublicKey(stream);
                            break;
                        default:
                            var result = new ResultOfOperation()
                            {
                                ErrorMessage = "Команды с таким номером не существует."
                            };
                            resultTask = ReturnResultToClientAsync(result, false);
                            //var resultAsBytes = SerializerF.Serialize(result);
                            //resultTask = NetStreamF.WriteAsync(resultAsBytes, 0, resultAsBytes.Length);
                            break;
                    }
                }
            }
            else
            {
                var result = new ResultOfOperation() {ErrorMessage = "Команда для сервера не задана."};
                resultTask = ReturnResultToClientAsync(result, false);
                //var resultAsBytes = SerializerF.Serialize(result);
                //resultTask = NetStreamF.WriteAsync(resultAsBytes, 0, resultAsBytes.Length);
            }
            return resultTask;
        }

        private async Task SetPublicKey(Stream stream)
        {
            try
            {
                AsymmetricEncrypterF = CryptoFactoryF.CreateAsymmetricAlgoritm(ChoosenCrypto.Provider,
                    ChoosenCrypto.Asymmetric);
                var memStream = new MemoryStream();
                await stream.CopyToAsync(memStream).ConfigureAwait(false);
                AsymmetricEncrypterF.Import(memStream.ToArray());
                await
                    ReturnResultToClientAsync(new ResultOfOperation() {OperationWasFinishedSuccessful = true}, false)
                        .ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException ex)
                when (Type.GetType(ex.Source, false).IsAssignableFrom(typeof(IAsymmetricEncrypter)))
            {
                await SendResultWithError(2, 0, false).ConfigureAwait(false);
            }
            catch (CryptographicException ex)
            {
                await SendResultWithError(2, 1, false).ConfigureAwait(false);
            }
            
        }
        private Task SendOfflineMessageCommandAsync(Stream stream)
        {
            try
            {
                if (UserIsAuth)
                {
                    var offlineMessage = SerializerF.Deserialize<OfflineMessageForm>(stream, false);
                    var result = new ResultOfOperation();
                    Core.SendOfflineMessage(offlineMessage, Form, result);
                    return ReturnResultToClientAsync(result, true);
                }
                else
                    return SendResultWithError(1, 1, false);
            }
            catch (SerializationException ex)
            {
                return SendResultWithError(1, 0, true);
            }
            catch (Exception)
            {
                return SendResultWithError(1, 2, true);
            }
            

        }
        private Task UpdateUserDataCommandAsync(Stream stream)
        {
            try
            {
                if (UserIsAuth)
                {
                    var formNew = SerializerF.Deserialize<UserForm>(stream, false);
                    var result = new ResultOfOperation();
                    Core.UpdateUserData(ref formNew, result);
                    Form = formNew;
                    return ReturnResultToClientAsync(result, true);
                }
                else
                    return SendResultWithError(1, 1, false);
            }
            catch (Exception)
            {
                var result = new ResultOfOperation()
                {
                    ErrorMessage = "Во время выполнения команды, об отправке offline сообщения," +
                                   " произошла непредвиденная ошибка."
                };
                return ReturnResultToClientAsync(result, UserIsAuth);
            }

        }
        private async Task FinedUserCommandAsync(Stream stream)
        {
            try
            {
                if (UserIsAuth)
                {
                    var strName = SerializerF.Deserialize<string>(stream, false);
                    var forms = await Core.FinedUserAsync(strName, Form.Login).ConfigureAwait(false);
                    var result = new CurrentResult<ICollection<UserFormSurrogate>>()
                    {
                        OperationWasFinishedSuccessful = true,
                        Result = forms
                    };
                    await ReturnResultToClientAsync(result, true).ConfigureAwait(false);
                }
                else
                    await SendResultWithError(1, 1, false).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                var result = new ResultOfOperation()
                {
                    ErrorMessage = "Во время выполнения команды, по нахождению пользователя по логину," +
                                   " произошла непредвиденная ошибка."
                };
                await ReturnResultToClientAsync(result, UserIsAuth).ConfigureAwait(false);
            }

        }
        private async Task AuthenticateUserCommandAsync(Stream stream)
        {
            try
            {
                var form = SerializerF.Deserialize<IAuthenticationForm>(stream, false);
                var result = new ResultOfOperation();
                OfflineMessagesConcurent mess;
                var formAuthed = await Core.AuthenticateUserAsync(form, CryptoFactoryF, ChoosenCrypto, result).ConfigureAwait(false);
                var resultCust = new CurrentResult<OfflineMessagesConcurentSur>(result)
                {
                    Result = (OfflineMessagesConcurentSur) formAuthed.Item2
                };
                if (result.OperationWasFinishedSuccessful)
                {
                    Form = formAuthed.Item1;
                    UserIsAuth = true;
                }
                await ReturnResultToClientAsync(resultCust, true).ConfigureAwait(false);

            }
            catch (Exception ex)
            {
                var result = new ResultOfOperation()
                {
                    ErrorMessage = "Во время выполнения команды, по аутентификации," +
                                   " произошла непредвиденная ошибка."
                };
                await ReturnResultToClientAsync(result, UserIsAuth).ConfigureAwait(false);
            }

        }
        private async Task GetSessionKey(Stream stream)
        {
            try
            {
                Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> encSym =
                    CryptoFactoryF.CreateSymmetricAlgoritm(ChoosenCrypto.Provider, ChoosenCrypto.Symmetric);
                var resultForClient = new CurrentResult<KeyDataForSymmetricAlgorithm>()
                {
                    OperationWasFinishedSuccessful = true,
                    Result = encSym.Item3
                };
                await ReturnResultToClientAsync(resultForClient, true).ConfigureAwait(false);
                EncrypterF = encSym.Item1;
                DecryptorF = encSym.Item2;
            }
            catch (Exception ex)
            {
                var result = new ResultOfOperation()
                {
                    ErrorMessage =
                        "Во время выполнения операции получения сессионного ключа, возникла непредвиденная ошибка."
                };
                await ReturnResultToClientAsync(result, true).ConfigureAwait(false);
            }

        }
        private Task SetCryptoAlgs(Stream stream)
        {
            var cryptoInfo = SerializerF.Deserialize<CryptoInfo>(stream, false);
            var resultOfCheck = CheckCryptoInfo(cryptoInfo);
            if (!resultOfCheck.OperationWasFinishedSuccessful)
                return ReturnResultToClientAsync(resultOfCheck, false);

            ChoosenCrypto = cryptoInfo;
            var factory =
                Core.CryptoFactories.First(baseC => baseC.PossibleCryptoAlgs.Providers.Contains(cryptoInfo.Provider));
            //AsymmetricEncrypterF = factory.CreateAsymmetricAlgoritm(cryptoInfo.Provider, cryptoInfo.Asymmetric,
            //            factory.KeySizes[cryptoInfo.Asymmetric], factory);
            CryptoFactoryF = factory;
            //var encS = factory.CreateSymmetricAlgoritm(ChoosenCrypto.Provider, ChoosenCrypto.Symmetric);
            //EncrypterF = encS.Item1;
            //DecryptorF = encS.Item2;
            
            
            return ReturnResultToClientAsync(new ResultOfOperation() {OperationWasFinishedSuccessful = true}, false);
        }
        private ResultOfOperation CheckCryptoInfo(CryptoInfo cryptoInfo)
        {
            var CryptoPossible = Core.PossibleCryptoInfo;
            var messError = "";
            if (!CryptoPossible.Asymmetric.Contains(cryptoInfo.Asymmetric))
                messError = $"Не был найден ассиметричный алгоритм с таким названием {cryptoInfo.Asymmetric}.";
            if (!CryptoPossible.Hash.Contains(cryptoInfo.Hash))
                messError = $"Не был найден алгоритм хеширования с таким названием {cryptoInfo.Hash}.";
            if (!CryptoPossible.Providers.Contains(cryptoInfo.Provider))
                messError = $"Не был найден крипто провайдер с таким названием {cryptoInfo.Provider}.";
            if (!CryptoPossible.Sign.Contains(cryptoInfo.Sign))
                messError = $"Не был найден алгоритм цифровой подписи с таким названием {cryptoInfo.Sign}.";
            if (!CryptoPossible.Symmetric.Contains(cryptoInfo.Symmetric))
                messError = $"Не был найден симметричный алгоритм с таким названием {cryptoInfo.Symmetric}.";

            var result = new ResultOfOperation();
            if (messError != "")
                result.ErrorMessage = messError;
            else
                result.OperationWasFinishedSuccessful = true;
            return result;
        }
        private Task GetPossibleCryptoAlgs()
        {
            var bytesToSend = SerializerF.Serialize(Core.PossibleCryptoInfo, true);
            return NetStreamF.WriteAsync(bytesToSend, 0, bytesToSend.Length);
        }
        private async Task RegisterNewUserCommandAsync(Stream stream)
        {
            try
            {
                var form = SerializerF.Deserialize<UserForm>(stream, false);
                form.Ip = ((IPEndPoint) ClientF.Client.RemoteEndPoint).Address;
                var result = new ResultOfOperation();
                if (await Core.RegisterNewUserAsync(form, RemoteEndPoint.Address, result).ConfigureAwait(false))
                {
                    Form = form;
                    UserIsAuth = true;
                }
                await ReturnResultToClientAsync(result, true).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var result = new ResultOfOperation()
                {
                    ErrorMessage =
                        "Во время выполнения команды, регистрации нового пользователя, возникла непредвиденная ошибка."
                };
                await ReturnResultToClientAsync(result, false).ConfigureAwait(false);
            }

        }
        private async Task<Stream> ReadCryptoDataAsync(byte[] bytesEnc, bool useAsymmetricCrypt)
        {
            if (CryptoFactoryF == null || ChoosenCrypto == null)
            {
                await SendResultWithError(0, 0, false).ConfigureAwait(false);
                return null;
            }

            try
            {
                var streamNew = new MemoryStream();
                byte[] bytesDecrypt;
                if (useAsymmetricCrypt)
                {
                    if (AsymmetricDecrypterF == null)
                    {
                        await SendResultWithError(0, 2, false).ConfigureAwait(false);
                        return null;
                    }

                    bytesDecrypt = await AsymmetricDecrypterF.DecryptAsync(bytesEnc).ConfigureAwait(false);
                }
                else
                    bytesDecrypt = DecryptorF.TransformFinalBlock(bytesEnc, 0, bytesEnc.Length);
                streamNew.Write(bytesDecrypt, 0, bytesDecrypt.Length);
                streamNew.Seek(0, SeekOrigin.Begin);
                return streamNew;
            }
            catch (CryptographicException ex)
            {
                await SendResultWithError(0, 1, useAsymmetricCrypt).ConfigureAwait(false);
                return null;
            }

        }
        private async Task GetPublicKeyCommandAsync()
        {
            try
            {
                var encAss = CryptoFactoryF.CreateAsymmetricAlgoritm(ChoosenCrypto.Provider, ChoosenCrypto.Asymmetric);
                var keyPublic = encAss.Export(false);
                var resultForClient = new CurrentResult<byte[]>()
                {
                    OperationWasFinishedSuccessful = true,
                    Result = keyPublic
                };
                await ReturnResultToClientAsync(resultForClient, false).ConfigureAwait(false);
                AsymmetricDecrypterF = encAss;
            }
            catch (Exception ex)
            {
                var result = new ResultOfOperation()
                {
                    ErrorMessage =
                        "Во время выполнения операции получения открытого ключа, возникла непредвиденная ошибка."
                };
                await ReturnResultToClientAsync(result, false).ConfigureAwait(false);
            }
        }
        /// <exception cref="SerializationException"></exception>
        private async Task ReturnResultToClientAsync(ResultOfOperation result, bool useEncryption)
        {
            try
            {
                var contentForEnc = SerializerF.Serialize(result, false);
                byte[] contentEnc;
                var useEnc = false;
                if (useEncryption)
                {
                    if (EncrypterF == null)
                    {
                        if (AsymmetricEncrypterF == null)
                            contentEnc = contentForEnc;
                        else
                        {
                            useEnc = true;
                            contentEnc = await AsymmetricEncrypterF.EncryptAsync(contentForEnc).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        useEnc = true;
                        contentEnc = EncrypterF.TransformFinalBlock(contentForEnc, 0, contentForEnc.Length);
                    }
                }
                else
                    contentEnc = contentForEnc;
                var writer = new BinaryWriter(NetStreamF);
                writer.Write(useEnc);
                writer.Write(contentEnc.Length);
                await NetStreamF.WriteAsync(contentEnc, 0, contentEnc.Length).ConfigureAwait(false);
            }
            catch (SerializationException ex)
            {
                ex.Source = GetType().AssemblyQualifiedName;
                throw;
            }
            catch (CryptographicException ex)
            {
                var result2 = new ResultOfOperation()
                {
                    ErrorMessage = "При подготовке к отправке сообщения возникла криптографическая ошибка."
                };
                await ReturnResultToClientAsync(result2, false).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
            }
            catch (Exception ex)
            {
                var result2 = new ResultOfOperation()
                {
                    ErrorMessage = "При подготовке к отправке сообщения возникла непредвиденная ошибка."
                };
                await ReturnResultToClientAsync(result2, false).ConfigureAwait(false);
            }
        }
        private Task SendResultWithError(int numb, int innerNumb, bool useEnc, params object[] objs)
        {
            ResultOfOperation result = null;
            var str = new StringBuilder();
            switch (numb)
            {
                case 0:
                    #region ReadCryptoDataAsync(byte[] bytesEnc, bool useAsymmetricCrypt)
                    switch (innerNumb)
                    {
                        case 0:
                            result = new ResultOfOperation()
                            {
                                ErrorMessage = "Заранее не были определены криптографические алгоритмы."
                            };
                            break;
                        case 1:
                            //SendResultWithError(0, 1, 0useAsymmetricCrypt)
                            str.AppendLine("Во время расшифровки сообщения возникла криптографическая ошибка.");
                            str.Append($"Ассиметричное шифрование включено {objs[0]}.");
                            result = new ResultOfOperation() { ErrorMessage = str.ToString() };
                            break;
                        case 2:
                            result = new ResultOfOperation()
                            {
                                ErrorMessage =
                                    "Были определены криптографические алгоритмы, но не были запрошены ключи."
                            };
                            break;
                    }
                    #endregion
                    break;
                case 1:
                    #region SendOfflineMessageCommandAsync(Stream stream)
                    switch (innerNumb)
                    {
                        case 0:
                            result = new ResultOfOperation()
                            {
                                ErrorMessage = "Произошла ошибка во время десеарелизации. Ожидался тип OfflineMessageForm."
                            };
                            break;
                        case 1:
                            result = new ResultOfOperation() { ErrorMessage = "Авторизация не была проведена." };
                            break;
                        case 2:
                            result = new ResultOfOperation()
                            {
                                ErrorMessage =
                                    "При обработке команды на отправку offline сообщения возникла непредвиденная ошибка."
                            };
                            break;
                    }
                    #endregion
                    break;
                case 2:
                    #region SetPublicKey(Stream stream)
                    switch (innerNumb)
                    {
                        case 0:
                            result = new ResultOfOperation()
                            {
                                ErrorMessage = "Длина массива байт, представляющая открытый ключ, равна 0."
                            };
                            break;
                        case 1:
                            result = new ResultOfOperation() {ErrorMessage = "Не удалось импортировать открытый ключ."};
                            break;
                    }
                    #endregion
                    break;
            }
            if (result == null)
                result = new ResultOfOperation() { ErrorMessage = "Не нашлось нормального описания ошибки." };

            return ReturnResultToClientAsync(result, useEnc);
        }
    }
}
