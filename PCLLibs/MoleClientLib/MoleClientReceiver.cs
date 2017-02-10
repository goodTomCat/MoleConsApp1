using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JabyLib.Other;
using MoleClientLib.RemoteFileStream;
using Newtonsoft.Json;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;

namespace MoleClientLib
{
    public class MoleClientReceiver
    {
        private ContactForm _form;
        private bool _userIsAuth;
        //protected ISign Signn;
        protected CryptoFactoryBase CurrentCryptoFactoryBase;
        protected IPEndPoint RemIp;
        private MolePushServerSender _servSender;
        protected NetworkStream NetStream;
        public event EventHandler<ExceptionCatchedEventArgs> ExceptionCatchedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;
        public event EventHandler<FalseClientEventArgs> UnauthorizedClientDetectedEvent;
        public event EventHandler<FalseClientEventArgs> SuspiciousClientDetectedEvent;
        private QueueOfAsyncActionsAdvanced _queueOfAsyncActions = new QueueOfAsyncActionsAdvanced();
        private JsonSerializerSettings _jsonSettings;


        /// <exception cref="ArgumentNullException">client == null. -or- core == null. -or- servSender == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">!client.Connected</exception>
        public MoleClientReceiver(TcpClient client, MoleClientCoreBase core, MolePushServerSender servSender)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client)) {Source = GetType().AssemblyQualifiedName};
            if (!client.Connected)
                throw new ArgumentOutOfRangeException(nameof(client), "Клиент не подключен.");
            if (core == null) throw new ArgumentNullException(nameof(core)) {Source = GetType().AssemblyQualifiedName};
            if (servSender == null)
                throw new ArgumentNullException(nameof(servSender)) {Source = GetType().AssemblyQualifiedName};

            Client = client;
            NetStream = client.GetStream();
            Core = core;
            _servSender = servSender;
            RemIp = (IPEndPoint) client.Client.RemoteEndPoint;
            _jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All
            };
        }


        public ICryptoTransform SymEncryptor { get; protected set; }
        public ICryptoTransform SymDecryptor { get; protected set; }
        public TcpClient Client { get; }
        public virtual uint BytesNumbForNewThread { get; set; } = 100000;
        public MoleClientCoreBase Core { get; protected set; }
        public IPEndPoint RemoteEndPoint
        {
            get { return RemIp; }
            protected set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                RemIp = value;
            }
        }
        public CryptoInfo ChoosenCrypto { get; protected set; }
        public IAsymmetricEncrypter AsymmetricEncrypter { get; protected set; }
        public IAsymmetricEncrypter AsymmetricDecrypter { get; protected set; }


        public async Task RunAsync(CancellationToken cancellationToken)
        {
            const int lengthMax = 104857600;
            while (Client.Connected)
            {
                try
                {
                    var bb = new byte[5];
                    await NetStream.ReadAsync(bb, 0, bb.Length).ConfigureAwait(false);
                    var encType = bb[0];

                    //var bytesOfLength = new byte[4];
                    //await
                    //    NetStream.ReadAsync(bytesOfLength, 0, bytesOfLength.Length, cancellationToken)
                    //        .ConfigureAwait(false);
                    var length = BitConverter.ToInt32(bb, 1);
                    if (length > lengthMax || length < 2)
                        continue;
                    var bytesOfContent = new byte[length];
                    var numbOfReadedBytes =
                        await
                            NetStream.ReadAsync(bytesOfContent, 0, bytesOfContent.Length, cancellationToken)
                                .ConfigureAwait(false);
                    while (numbOfReadedBytes != bytesOfContent.Length)
                    {
                        numbOfReadedBytes +=
                            await
                                NetStream.ReadAsync(bytesOfContent, numbOfReadedBytes,
                                    bytesOfContent.Length - numbOfReadedBytes, cancellationToken).ConfigureAwait(false);
                    }
                    Stream streamDec;
                    switch (encType)
                    {
                        case 0:
                            streamDec = new MemoryStream(bytesOfContent);
                            break;
                        case 1:
                            streamDec = ReadCryptoData(bytesOfContent, true, true);
                            break;
                        case 2:
                            streamDec = ReadCryptoData(bytesOfContent, false, true);
                            break;
                        default:
                            //
                            continue;
                    }

                    await ReadCommandAndMapAsync(streamDec).ConfigureAwait(false);
                }
                catch (SocketException ex) when (!GetType().Equals(Type.GetType(ex.Source, false)))
                {
                    _queueOfAsyncActions.Add(
                        () => InvokeEvet(ClientDisconnectedEvent, new ClientDisconnectedEventArgs(_form, RemoteEndPoint)));
                }
                catch (Exception ex) when (!GetType().Equals(Type.GetType(ex.Source, false)))
                {
                    _queueOfAsyncActions.Add(
                        () => InvokeExceptionCatchedEvent(CreateException(0, 1, RemoteEndPoint, _form.Login, ex)));
                }
            }
            
        }


        private Task ReadCommandAndMapAsync(Stream stream)
        {
            Task resultTask;
            if (stream.Length >= 2) //UInt16 - 2 byte
            {
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
                            resultTask = AuthenticateUserCommandAsync(stream);
                            break;
                        case 2:
                            resultTask = RegisterNewContactAsync(stream);
                            break;
                        case 3:
                            resultTask = RequestForFileRecievingAsync(stream);
                            break;
                        case 4:
                            resultTask = ReturnPartOfFileAsync(stream);
                            break;
                        case 5:
                            resultTask = RecieveTextMessageAsync(stream);
                            break;
                        case 6:
                            resultTask = CheckPrivateKey(stream);
                            break;
                        case 7:
                            resultTask = GetPossibleCryptoAlgs();
                            break;
                        case 8:
                            resultTask = SetCryptoAlgs(stream);
                            break;
                        case 9:
                            resultTask = SetPublicKey(stream);
                            break;
                        case 10:
                            resultTask = GetSessionKey();
                            break;
                        default:
                            var result = new ResultOfOperation()
                            {
                                ErrorMessage = "Команды с таким номером не существует."
                            };
                            resultTask = Task.CompletedTask;
                            break;
                    }
                }
            }
            else
            {
                var result = new ResultOfOperation() {ErrorMessage = "Команда для сервера не задана."};
                resultTask = Task.CompletedTask;
            }
            return resultTask;
        }
        private Task CheckPrivateKey(Stream stream)
        {
            var bytesEnc = new byte[stream.Length - stream.Position];
            stream.Read(bytesEnc, 0, bytesEnc.Length);
            if (SymDecryptor == null)
                return ReturnErrorMessageToClientAsync(11, 0);
            //Симметричный ключ еще не был задан, поэтому его не возможно проверить.

            var bytesDec = SymDecryptor.TransformFinalBlock(bytesEnc, 0, bytesEnc.Length);
            var result = new CurrentResult<byte[]> { Result = bytesDec };
            return ReturnResultToClientAsync(result, false);
        }
        private async Task RecieveTextMessageAsync(Stream stream)
        {
            await CheckMessage(stream, 5000).ConfigureAwait(false);
            try
            {
                var reader = new BinaryReader(stream);
                var str = reader.ReadString();
                var result = new ResultOfOperation();
                await Core.TextMessageRecieved(_form.Login, str, result, _form).ConfigureAwait(false);
                await ReturnResultToClientAsync(result).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                //reader.ReadString();
                await ReturnErrorMessageToClientAsync(10, 0).ConfigureAwait(false);
            }
            catch (Exception)
            {
                await ReturnErrorMessageToClientAsync(10, 1).ConfigureAwait(false);
                //Возникла непредвиденная ошибка.
            }
            
        }
        private Task GetSessionKey()
        {
            if (AsymmetricEncrypter == null)
                return ReturnErrorMessageToClientAsync(12, 0);
            //Нет необходимости передавать симметричный ключ без предварительно согласованного асимметричного шифрования.
            if (CurrentCryptoFactoryBase == null || ChoosenCrypto == null)
                return ReturnErrorMessageToClientAsync(12, 1);
            //Еще не были согласованы криптографические алгоритмы.

            var symTupl = CurrentCryptoFactoryBase.CreateSymmetricAlgoritm(ChoosenCrypto.Provider,
                ChoosenCrypto.Symmetric);
            SymEncryptor = symTupl.Item1;
            SymDecryptor = symTupl.Item2;
            return ReturnResultToClientAsync(
                new CurrentResult<KeyDataForSymmetricAlgorithm>()
                {
                    Result = symTupl.Item3,
                    OperationWasFinishedSuccessful = true
                }, true, true);
        }
        private async Task AuthenticateUserCommandAsync(Stream stream)
        {
            await CheckMessage(stream, 500).ConfigureAwait(false);
            var result = new ResultOfOperation();
            try
            {
                var authForm = Core.Serializer.Deserialize<ClientToClientAuthForm>(stream, false);
                var userForms = await _servSender.FinedUserAsync(authForm.Login).ConfigureAwait(false);
                var contacts = userForms.Select(surrogate => (ContactForm)surrogate).ToArray();
                Tuple<bool, ContactForm> authResult = Core.AuthenticateContacnt(authForm, CurrentCryptoFactoryBase,
                    RemoteEndPoint,
                    contacts,
                    result);
                if (authResult.Item1)
                {
                    _userIsAuth = true;
                    _form = authResult.Item2;
                }
                else
                    _queueOfAsyncActions.Add(
                            () =>
                                InvokeEvet(UnauthorizedClientDetectedEvent,
                                    new FalseClientEventArgs(authForm.Login, RemoteEndPoint)));
                await ReturnResultToClientAsync(result, false).ConfigureAwait(false);

            }
            catch (SocketException ex) when (GetType() != Type.GetType(ex.Source, false))
            {
                //_servSender.FinedUserAsync(authForm.Login)
                await ReturnErrorMessageToClientAsync(8, 0).ConfigureAwait(false);
            }
            catch (SerializationException ex)
            {
                result.ErrorMessage = "Расшифрованные данные повреждены, либо имеют не верный формат. Ошибка сериализации.";
                await ReturnResultToClientAsync(result, false);
            }
            catch (Exception ex)
            {
                //_servSender.FinedUserAsync(authForm.Login)
                await ReturnErrorMessageToClientAsync(8, 1).ConfigureAwait(false);
            }
        }
        private async Task RequestForFileRecievingAsync(Stream stream)
        {
            await CheckMessage(stream, 78).ConfigureAwait(false);
            try
            {
                var req = Core.Serializer.Deserialize<FileRecieveRequest>(stream, false);
                var result = new ResultOfOperation();
                if (_userIsAuth)
                {
                    await
                        Core.RecieveOfFileTransferRequest(req,
                                new IPEndPoint(RemoteEndPoint.Address, _form.PortClientToClient1), _form, result)
                            .ConfigureAwait(false);
                }
                else
                    result.ErrorMessage = "Авторизация не была произведена.";

                await ReturnResultToClientAsync(result).ConfigureAwait(false);
            }
            catch (SerializationException ex)
            {
                await ReturnErrorMessageToClientAsync(2).ConfigureAwait(false);
            }

        }
        private async Task ReturnPartOfFileAsync(Stream stream)
        {
            await CheckMessage(stream, 100).ConfigureAwait(false);
            try
            {
                var reqPartOfFile = Core.Serializer.Deserialize<RequestPartOfFile>(stream, false);
                FileStream fileStream;
                if (Core.FilesSending.TryGetValue(reqPartOfFile.NameOfFile, out fileStream))
                {
                    if (!fileStream.CanSeek || !fileStream.CanRead)
                    {
                        var fileStreamNew = new FileStream(fileStream.Name, FileMode.Open, FileAccess.Read);
                        Core.FilesSending.AddOrUpdate(reqPartOfFile.NameOfFile, fileStreamNew,
                            (s, stream1) => fileStreamNew);
                        fileStream = fileStreamNew;
                    }

                    fileStream.Seek(reqPartOfFile.Position, SeekOrigin.Begin);
                    var content = new byte[reqPartOfFile.Length];
                    await fileStream.ReadAsync(content, 0, content.Length).ConfigureAwait(false);
                    await
                        ReturnResultToClientAsync(new ResultOfOperation() {OperationWasFinishedSuccessful = true}, true,
                            false).ConfigureAwait(false);
                    await ReturnResultToClientAsync(content, true, false).ConfigureAwait(false);
                }
                else
                    await ReturnErrorMessageToClientAsync(3, 0).ConfigureAwait(false);
            }
            catch (SerializationException ex)
            {
                await ReturnErrorMessageToClientAsync(3, 1).ConfigureAwait(false);
            }
            catch (IOException ex)
            {
                await ReturnErrorMessageToClientAsync(3, 2).ConfigureAwait(false);
            }
        }
        //private async Task<Tuple<bool, Stream>> ReadCryptoDataAsync(Stream stream, bool isBegin,
        //    bool useAsymmetricCrypt = false)
        //{
        //    try
        //    {
        //        var contentAsBytes = new byte[stream.Length];
        //        await stream.ReadAsync(contentAsBytes, 0, contentAsBytes.Length);
        //        Task<byte[]> resultAsDecBytesTask;
        //        if (useAsymmetricCrypt)
        //            resultAsDecBytesTask = Core.AsymmetricEncrypter.DecryptAsync(contentAsBytes);
        //        else
        //        {
        //            resultAsDecBytesTask = contentAsBytes.Length > BytesNumbForNewThread*2
        //                ? Task.Run(() => SymEncryptor.TransformFinalBlock(contentAsBytes, 0, contentAsBytes.Length))
        //                : Task.FromResult(SymEncryptor.TransformFinalBlock(contentAsBytes, 0, contentAsBytes.Length));
        //        }
        //        return new Tuple<bool, Stream>(true, new MemoryStream(await resultAsDecBytesTask));
        //    }
        //    catch (CryptographicException ex)
        //    {
        //        await ReturnErrorMessageToClientAsync(1);
        //        return new Tuple<bool, Stream>(false, null);
        //    }

        //}
        private Stream ReadCryptoData(byte[] dataEnc, bool useAsymmetricCrypt = false, bool deCrypt = true)
        {
            var bytesCrypt = dataEnc;
            var streamNew = new MemoryStream();
            try
            {
                if (deCrypt)
                {
                    var bytesDecrypt = useAsymmetricCrypt
                        ? AsymmetricDecrypter.Decrypt(bytesCrypt)
                        : SymDecryptor.TransformFinalBlock(bytesCrypt, 0, bytesCrypt.Length);
                    streamNew.Write(bytesDecrypt, 0, bytesDecrypt.Length);
                }
                else
                    streamNew.Write(bytesCrypt, 0, bytesCrypt.Length);
                streamNew.Seek(0, SeekOrigin.Begin);
                return streamNew;
            }
            catch (CryptographicException ex)
            {
                _queueOfAsyncActions.Add(
                    () =>
                        InvokeExceptionCatchedEvent(CreateException(1, 0, ex, ChoosenCrypto, useAsymmetricCrypt,
                            deCrypt)));
                ReturnErrorMessageToClientAsync(4, 1).Wait();
                return streamNew;
            }
            catch (Exception ex)
            {
                _queueOfAsyncActions.Add(
                    () =>
                        InvokeExceptionCatchedEvent(CreateException(1, 1, ex, this, useAsymmetricCrypt,
                            deCrypt)));
                ReturnErrorMessageToClientAsync(4, 2).Wait();
                return streamNew;
            }
        }
        private async Task RegisterNewContactAsync(Stream stream)
        {
            if (!_userIsAuth)
            {
                await ReturnResultToClientAsync(
                        new ResultOfOperation() {ErrorMessage = "Аутентификация еще не была проведена."}, false, false)
                    .ConfigureAwait(false);
            }

            await CheckMessage(stream, 70).ConfigureAwait(false);
            var result = new ResultOfOperation();
            try
            {
                var streamTemp = stream as MemoryStream;
                if (streamTemp == null)
                {
                    streamTemp = new MemoryStream();
                    stream.CopyTo(streamTemp);
                }
                var login = Encoding.UTF8.GetString(streamTemp.ToArray());
                await Core.RegisterNewContactAsync(_form, RemoteEndPoint, result).ConfigureAwait(false);
                //else
                //{
                //    var formsPublic =
                //        (await _servSender.GetUsersPublicData(new[] {login})).Select(form => (ContactForm) form)
                //            .ToArray();
                //    await Core.RegisterNewContactAsync(login, RemoteEndPoint, formsPublic, result).ConfigureAwait(false);
                //}
                await ReturnResultToClientAsync(result).ConfigureAwait(false);
            }
            catch (DecoderFallbackException ex)
            {
                //var login = Encoding.UTF8.GetString(streamTemp.ToArray());
                await ReturnErrorMessageToClientAsync(9, 0).ConfigureAwait(false);
            }
            catch (ArgumentException ex)
            {
                //var login = Encoding.UTF8.GetString(streamTemp.ToArray());
                await ReturnErrorMessageToClientAsync(9, 0).ConfigureAwait(false);
            }
            catch (SocketException ex)
            {
                // _servSender.GetUsersPublicData(new[] {login}))
                await ReturnErrorMessageToClientAsync(8, 0).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // _servSender.GetUsersPublicData(new[] {login}))
                await ReturnErrorMessageToClientAsync(8, 1).ConfigureAwait(false);
            }


        }
        private async Task GetPossibleCryptoAlgs()
        {
            var curentResult = new CurrentResult<PossibleCryptoInfo>() {Result = Core.PossibleAlgs};
            await ReturnResultToClientAsync(curentResult, true, true).ConfigureAwait(false);
        }
        private async Task SetCryptoAlgs(Stream stream)
        {
            await CheckMessage(stream, 300).ConfigureAwait(false);
            try
            {
                var cryptoInfo = Core.Serializer.Deserialize<CryptoInfo>(stream, false);
                if (!Core.PossibleAlgs.Asymmetric.Contains(cryptoInfo.Asymmetric))
                    await ReturnErrorMessageToClientAsync(5, 0, "Asymmetric").ConfigureAwait(false);
                if (!Core.PossibleAlgs.Hash.Contains(cryptoInfo.Hash))
                    await ReturnErrorMessageToClientAsync(5, 0, "Hash").ConfigureAwait(false);
                if (!Core.PossibleAlgs.Providers.Contains(cryptoInfo.Provider))
                    await ReturnErrorMessageToClientAsync(5, 0, "Provider").ConfigureAwait(false);
                if (!Core.PossibleAlgs.Sign.Contains(cryptoInfo.Sign))
                    await ReturnErrorMessageToClientAsync(5, 0, "Sign").ConfigureAwait(false);
                if (!Core.PossibleAlgs.Symmetric.Contains(cryptoInfo.Symmetric))
                    await ReturnErrorMessageToClientAsync(5, 0, "Symmetric").ConfigureAwait(false);

                ChoosenCrypto = cryptoInfo;
                CurrentCryptoFactoryBase =
                    Core.Factories.First(e => e.PossibleCryptoAlgs.Providers.Contains(ChoosenCrypto.Provider));
                await ReturnResultToClientAsync(new ResultOfOperation() {OperationWasFinishedSuccessful = true}, false,
                    false).ConfigureAwait(false);
            }
            catch (SerializationException ex)
            {
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                await ReturnErrorMessageToClientAsync(5, 1).ConfigureAwait(false);
                //Произошла ошибка сериализации, не удалось получить информацию о выбранных алгиритмах.
            }
            catch (InvalidOperationException ex)
            {
                //Core.Factories.First(e => e.PossibleCryptoAlgs.Providers.Contains(ChoosenCrypto.Provider))   
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                await ReturnErrorMessageToClientAsync(10, 1).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                await ReturnErrorMessageToClientAsync(10, 1).ConfigureAwait(false);
            }
        }
        private Task GetPublicKeyCommandAsync()
        {
            ResultOfOperation result;
            if (ChoosenCrypto == null)
                return ReturnErrorMessageToClientAsync(-1);
            try
            {
                AsymmetricDecrypter = CurrentCryptoFactoryBase.CreateAsymmetricAlgoritm(ChoosenCrypto.Provider,
                    ChoosenCrypto.Asymmetric);
                var publickey = AsymmetricDecrypter.Export(false);
                var hashAlg =
                    CurrentCryptoFactoryBase.CreateHashAlgorithm(Core.MyUserForm.KeyParametrsBlob.CryptoProvider,
                        Core.MyUserForm.KeyParametrsBlob.HashAlg);
                var hash = hashAlg.ComputeHash(publickey);
                //var signAlg =
                //    CurrentCryptoFactoryBase.CreateSignAlgoritm(Core.MyUserForm.KeyParametrsBlob.CryptoProvider,
                //        Core.MyUserForm.KeyParametrsBlob.CryptoAlg);
                //signAlg.Import(Core.MyUserForm.KeyParametrsBlob.Key);
                var publicKeySign = Core.SignAlgImpl.Export(false);
                var sign = Core.SignAlgImpl.SignData(hash);
                var publicKeyForm = new PublicKeyForm()
                {
                    CryptoAlg = Core.MyUserForm.KeyParametrsBlob.CryptoAlg,
                    CryptoProvider = Core.MyUserForm.KeyParametrsBlob.CryptoProvider,
                    Hash = hash,
                    HashAlg = Core.MyUserForm.KeyParametrsBlob.HashAlg,
                    Key = publickey,
                    Sign = sign
                };

                return
                    ReturnResultToClientAsync(
                        new CurrentResult<PublicKeyForm>()
                        {
                            Result = publicKeyForm,
                            OperationWasFinishedSuccessful = true
                        }, false, false);
                //return ReturnResultToClientAsync(new CurrentResult<byte[]>() {Result = publickey}, false, false);
            }
            catch (Exception ex)
            {
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                result = new ResultOfOperation()
                {
                    ErrorMessage = "Возникла непредвиденная ошибка.",
                    OperationWasFinishedSuccessful = false
                };
                return ReturnResultToClientAsync(result, false, false);
            }
        }
        private async Task SetPublicKey(Stream stream)
        {
            if (!_userIsAuth)
            {
                await ReturnResultToClientAsync(
                        new ResultOfOperation() { ErrorMessage = "Аутентификация еще не была проведена." }, false, false)
                    .ConfigureAwait(false);
            }

            await CheckMessage(stream, 1000).ConfigureAwait(false);
            try
            {
                //var publicKey = new byte[stream.Length - 2];
                //stream.Read(publicKey, 0, publicKey.Length);
                var publicKeyForm = Core.Serializer.Deserialize<PublicKeyForm>(stream, false);
                if (!publicKeyForm.ValidateSign(CurrentCryptoFactoryBase, _form.PublicKey.Key))
                    await ReturnErrorMessageToClientAsync(6, 2).ConfigureAwait(false);

                AsymmetricEncrypter = CurrentCryptoFactoryBase.CreateAsymmetricAlgoritm(ChoosenCrypto.Provider,
                    ChoosenCrypto.Asymmetric);
                AsymmetricEncrypter.Import(publicKeyForm.Key);
                await ReturnResultToClientAsync(new ResultOfOperation() {OperationWasFinishedSuccessful = true}, false,
                    false).ConfigureAwait(false);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                await ReturnErrorMessageToClientAsync(6, 0).ConfigureAwait(false);
            }
            catch (CryptographicException ex)
            {
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                await ReturnErrorMessageToClientAsync(6, 1).ConfigureAwait(false);
            }
            catch (SerializationException ex)
            {
                
            }
            catch (Exception ex)
            {
                _queueOfAsyncActions.Add(() => InvokeExceptionCatchedEvent(ex));
                await 
                    ReturnResultToClientAsync(
                        new ResultOfOperation() {ErrorMessage = "Возникла непредвиденная ошибка.",}, false, false).ConfigureAwait(false);
            }

        }

        private async Task<bool> CheckMessage(Stream stream, int maxLength)
        {
            if (stream.Length > maxLength)
            {
                //Длина сообщения превышает допустимые пределы.
                await ReturnErrorMessageToClientAsync(13, 0).ConfigureAwait(false);
                _queueOfAsyncActions.Add(
                    () =>
                        InvokeEvet(SuspiciousClientDetectedEvent,
                            new FalseClientEventArgs(_form?.Login, RemoteEndPoint)));
                return false;
            }
            return true;
        }
        private Task ReturnResultToClientAsync(byte[] content, bool useEnc = true, bool useAssEnc = false)
        {
            byte[] resultAsEncBetes;
            //var useEncryption = false;
            byte usedEncryptionType = 0;
            if (useEnc)
            {
                if (useAssEnc)
                {
                    if (AsymmetricEncrypter == null)
                        resultAsEncBetes = content;
                    else
                    {
                        resultAsEncBetes = AsymmetricEncrypter.Encrypt(content);
                        usedEncryptionType = 1;
                    }
                }
                else
                {
                    if (SymEncryptor == null)
                    {
                        if (AsymmetricEncrypter == null)
                            resultAsEncBetes = content;
                        else
                        {
                            resultAsEncBetes = AsymmetricEncrypter.Encrypt(content);
                            usedEncryptionType = 1;
                        }
                    }
                    else
                    {
                        resultAsEncBetes = SymEncryptor.TransformFinalBlock(content, 0, content.Length);
                        usedEncryptionType = 2;
                    }
                }
            }
            else
                resultAsEncBetes = content;

            NetStream.WriteByte(usedEncryptionType);
            NetStream.Write(BitConverter.GetBytes(resultAsEncBetes.Length), 0, 4);
            return NetStream.WriteAsync(resultAsEncBetes, 0, resultAsEncBetes.Length);
        }
        private int InvokeEvet<TArgs>(EventHandler<TArgs> handler, TArgs args)
        {
            var list = handler?.GetInvocationList();
            if (list == null)
                return 0;

            var invokedDel = 0;
            foreach (Delegate del in list)
            {
                try
                {
                    ((EventHandler<TArgs>)del).Invoke(this, args);
                    invokedDel++;
                }
                catch
                {
                    // ignored
                }
            }
            return invokedDel;
        }
        private Task ReturnResultToClientAsync(ResultOfOperation result, bool useEnc = true,
            bool useAssEnc = false)
        {
            try
            {
                var resultAsBytes = Core.Serializer.Serialize(result, false);
                return ReturnResultToClientAsync(resultAsBytes, useEnc, useAssEnc);
            }
            catch (Exception ex)
            {
                
                throw;
            }
            
        }
        //private void ReturnEncResultToClient(ResultOfOperation resultOfOperation)
        //{
        //    var resultAsBytes = Core.Serializer.Serialize(resultOfOperation, false);
        //    var resultAsEcnBytes = SymEncryptor.TransformFinalBlock(resultAsBytes, 0, resultAsBytes.Length);
        //    var stream = new MemoryStream();
        //    var writer = new BinaryWriter(stream);
        //    using (stream)
        //    {
        //        writer.Write(resultAsEcnBytes.Length);
        //        writer.Write(resultAsEcnBytes);
        //        stream.CopyTo(NetStream);
        //        //SendAsyncFunc(new ArraySegment<byte>(stream.ToArray()), CancellationToken.None).Wait();
        //    }
        //}

        //private async Task ReturnEncResultToClientAsync(ResultOfOperation resultOfOperation)
        //{
        //    var resultAsBytes = Core.Serializer.Serialize(resultOfOperation, false);
        //    var resultAsEcnBytesTask = resultAsBytes.Length > BytesNumbForNewThread
        //        ? Task.Run(() => SymEncryptor.TransformFinalBlock(resultAsBytes, 0, resultAsBytes.Length))
        //        : Task.FromResult(SymEncryptor.TransformFinalBlock(resultAsBytes, 0, resultAsBytes.Length));
        //    var stream = new MemoryStream();
        //    var writer = new BinaryWriter(stream);
        //    using (stream)
        //    {
        //        var resultAsEcnBytes = await resultAsEcnBytesTask;
        //        writer.Write(resultAsEcnBytes.Length);
        //        writer.Write(resultAsEcnBytes);
        //        await stream.CopyToAsync(NetStream);
        //        //await SendAsyncFunc(new ArraySegment<byte>(stream.ToArray()), CancellationToken.None);
        //    }
        //}
        private async Task ReturnErrorMessageToClientAsync(int numb, params object[] objs)
        {
            var stream = new MemoryStream();
            ResultOfOperation resultOfOperation = null;
            int innerNumb;
            switch (numb)
            {
                case 0:
                    var numbInner = (int) objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            resultOfOperation = new ResultOfOperation()
                            {
                                ErrorMessage = "Сообщение повреждено, либо имеет неверный формат. " +
                                               "Невозможно определить является ли сообщение зашифрованным.",
                                OperationWasFinishedSuccessful = false
                            };
                            await ReturnResultToClientAsync(resultOfOperation, false);
                            break;
                        case 1:
                            resultOfOperation = new ResultOfOperation()
                            {
                                ErrorMessage = "Сообщение повреждено, либо имеет неверный формат. " +
                                               "Указанная длина полезных данных меньше нуля.",
                                OperationWasFinishedSuccessful = false
                            };
                            await ReturnResultToClientAsync(resultOfOperation, false);
                            break;
                        case 2:
                            #region Task RequestForFileRecievingAsync(Stream stream)
                            
                            resultOfOperation = new ResultOfOperation()
                            {
                                ErrorMessage = "Сообщение повреждено, либо имеет неверный формат. " +
                                               "Указанная длина полезных данных несовпадает.",
                                OperationWasFinishedSuccessful = false
                            };
                            await ReturnResultToClientAsync(resultOfOperation, false);
                            #endregion
                            break;
                    }

                    break;
                case 1:
                    resultOfOperation = new ResultOfOperation()
                    {
                        ErrorMessage = "Во время криптографической операции возникли ошибки.",
                        OperationWasFinishedSuccessful = false
                    };
                    await ReturnResultToClientAsync(resultOfOperation, false);
                    break;
                case 2:
                    #region Task RequestForFileRecievingAsync(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            var mes = "Ошибка десериализации.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 3:
                    #region ReturnPartOfFileAsync

                    innerNumb = (int) objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            resultOfOperation = new ResultOfOperation()
                            {
                                ErrorMessage = "Отправка файла не была запланирована."
                            };
                            break;
                        case 1:
                            //RequestPartOfFile
                            resultOfOperation = new ResultOfOperation()
                            {
                                ErrorMessage = "Возникла ошибка десериализации, ожидалась структура RequestPartOfFile."
                            };
                            break;
                        case 2:
                            resultOfOperation = new ResultOfOperation()
                            {
                                ErrorMessage = "Возникла ошибка ввода/вывода."
                            };
                            break;
                    }

                    #endregion
                    break;
                case 4:
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 1:
                            var mes = "Во время расшифровки возникла криптографическая ошибка.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 2:
                            mes = "Во время расшифровки возникла непредвиденная ошибка.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    break;
                case 5:
                    #region Task SetCryptoAlgs(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //return ReturnErrorMessageToClientAsync(5, 1);
                            var mes = $"Произошла ошибка сериализации, не удалось получить информацию о выбранных алгоритмах.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            //ReturnErrorMessageToClientAsync(5, 0, 1"Asymmetric")
                            mes = $"Имя алгоритма с типом {objs[1]} не был найден в списке возможных.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 2:
                            //ReturnErrorMessageToClientAsync(5, 0, 1"Asymmetric")
                            mes = $"Имя алгоритма с типом {objs[1]} не был найден в списке возможных.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;

                    }
                    #endregion
                    break;
                case 6:
                    #region Task SetPublicKey(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            var mes = "Во время импорта ключа в криптографический объект возникла ошибка. Возможно длина ключа равна 0.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            mes = "Во время импорта ключа в криптографический объект возникла ошибка.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 2:
                            mes = "Не удалось подтвердить подлинность открытого ключа.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 7:
                    #region Task GetSessionKey()
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            var mes = "Передавать симметричные ключи бесполезно если не использовать асимметричные алгоритмы.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            mes = "Криптографические алгоритмы еще не были согласованы.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 8:
                    #region Task AuthenticateUserCommandAsync(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //ReturnErrorMessageToClientAsync(8, 0).ConfigureAwait(false);
                            var mes = "Отсутствует соединение с сервером авторизации.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            //ReturnErrorMessageToClientAsync(8, 1).ConfigureAwait(false);
                            mes = "У сервера авторизации произошла ошибка, потому невозможно её провести.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 9:
                    #region Task RegisterNewContactAsync(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //await ReturnErrorMessageToClientAsync(9, 0).ConfigureAwait(false);
                            var mes = "Во время преобразования массива байтов в строку кодировки utf-8, возникла ошибка.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            //ReturnErrorMessageToClientAsync(8, 1).ConfigureAwait(false);
                            mes = "У сервера авторизации произошла ошибка, потому невозможно её провести.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 2:
                            break;
                    }
                    #endregion
                    break;
                case 10:
                    #region Task RecieveTextMessageAsync(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //await ReturnErrorMessageToClientAsync(10, 0).ConfigureAwait(false);
                            var mes = "Ошибка чтения строки.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            //await ReturnErrorMessageToClientAsync(10, 1).ConfigureAwait(false);
                            mes = "Возникла непредвиденная ошибка.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 11:
                    #region Task CheckPrivateKey(Stream stream)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //return ReturnErrorMessageToClientAsync(11, 0);
                            var mes = "Симметричный ключ еще не был задан, поэтому его не возможно проверить.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 12:
                    #region Task GetSessionKey()
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //return ReturnErrorMessageToClientAsync(12, 0);
                            var mes = "Нет необходимости передавать симметричный ключ без предварительно согласованного асимметричного шифрования.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                        case 1:
                            //return ReturnErrorMessageToClientAsync(12, 1);
                            mes = "Еще не были согласованы криптографические алгоритмы.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
                case 13:
                    #region Task CheckMessageLength(Stream stream, int maxLength)
                    numbInner = (int)objs[0];
                    switch (numbInner)
                    {
                        case 0:
                            //return ReturnErrorMessageToClientAsync(13, 0);
                            var mes = "Длина сообщения превышает допустимые пределы.";
                            resultOfOperation = new ResultOfOperation() { ErrorMessage = mes };
                            break;
                    }
                    #endregion
                    break;
            }
            resultOfOperation.OperationWasFinishedSuccessful = false;
            await ReturnResultToClientAsync(resultOfOperation, false);
        }
        private int InvokeExceptionCatchedEvent(Exception ex)
        {
            var list = ExceptionCatchedEvent?.GetInvocationList();
            if (list == null)
                return 0;

            var invokedDel = 0;
            foreach (Delegate del in list)
            {
                try
                {
                    ((EventHandler<ExceptionCatchedEventArgs>)del).Invoke(this, new ExceptionCatchedEventArgs(ex));
                    invokedDel++;
                }
                catch
                {
                    // ignored
                }
            }
            return invokedDel;
        }
        private Exception CreateException(int numb, int innerNumb, params object[] objs)
        {
            var str = new StringBuilder();
            Exception result = null;
            switch (numb)
            {
                case 0:
                    #region Task RunAsync(CancellationToken cancellationToken)
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(0, 0, 0RemoteEndPoint, 1_form.Login, 2ex)
                            result = new SocketException((int)((SocketException)objs[2]).SocketErrorCode);
                            result.Data.Add("RemoteEndPoint", JsonConvert.SerializeObject(objs[0], Formatting.Indented, _jsonSettings));
                            result.Data.Add("_form.Login", objs[1]);
                            break;
                        case 1:
                            //CreateException(0, 0, 0RemoteEndPoint, 1_form.Login, 2ex)
                            str.AppendLine(
                                "Во время получеия входящего трафика со стороны удаленного пользователя, произошла непредвиденная ошибка.");
                            str.Append($"_form.Login: {objs[1]}.");
                            result = new Exception(str.ToString(), (Exception)objs[2]);
                            result.Data.Add("RemoteEndPoint", JsonConvert.SerializeObject(objs[0], Formatting.Indented, _jsonSettings));
                            break;
                    }
                    #endregion
                    break;
                case 1:
                    #region Stream ReadCryptoData(byte[] dataEnc, bool useAsymmetricCrypt = false, bool deCrypt = true)
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(1, 0, 0ex, 1ChoosenCrypto, 2useAsymmetricCrypt, 3deCrypt)
                            str.AppendLine(
                                "Во время дешифровки произошла криптографическая ошибка.");
                            str.AppendLine($"useAsymmetricCrypt: {objs[2]}.");
                            str.Append($"deCrypt: {objs[3]}.");
                            result = new CryptographicException(str.ToString(), (Exception)objs[0]);
                            result.Data.Add("ChoosenCrypto", JsonConvert.SerializeObject(objs[1], Formatting.Indented, _jsonSettings));
                            break;
                        case 1:
                            //CreateException(1, 1, 0ex, 1this, 2useAsymmetricCrypt, 3deCrypt)
                            str.AppendLine("Во время дешифровки возникла непредвиденная ошибка.");
                            str.AppendLine($"useAsymmetricCrypt: {objs[2]}.");
                            str.Append($"deCrypt: {objs[3]}.");
                            result = new Exception(str.ToString(), (Exception)objs[0]);
                            result.Data.Add("this",
                                JsonConvert.SerializeObject(objs[1], Formatting.Indented, _jsonSettings));
                            break;
                    }
                    #endregion
                    break;
                case 2:
                    #region Task SetCryptoAlgs(Stream stream)
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(2, 0, 0ex)
                            str.Append(
                                "Во время десеарелизации, при применении выбранных алгоритмов, возникла ошибка.");
                            result = new SerializationException(str.ToString(), (Exception)objs[0]);
                            break;
                    }
                    #endregion
                    break;
            }
            return result;
        }

        //public Task ReturnResultToClient(ResultOfOperation result)
        //{
        //    var stream = new MemoryStream();
        //    var writer = new BinaryWriter(stream);
        //    writer.Write(false);
        //    var resultAsBytes = Core.Serializer.Serialize(result, false);
        //    writer.Write(resultAsBytes.Length);
        //    writer.Write(resultAsBytes);
        //    return NetStream.WriteAsync(stream.ToArray(), 0, (int)stream.Length);
        //}
    }
}
