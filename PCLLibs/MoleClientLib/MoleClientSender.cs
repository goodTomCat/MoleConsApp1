using System;
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
using JabyLib.Other.ObjectAsDictionary;
using MoleClientLib.RemoteFileStream;
using Newtonsoft.Json;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;

namespace MoleClientLib
{
    public class MoleClientSender
    {
        protected TcpClient ClientF;
        protected NetworkStream NetStreamF;
        protected CustomBinarySerializerBase Serializer = new ProtoBufSerializer();
        protected MoleClientCoreBase CoreF;
        protected KeyDataForSymmetricAlgorithm PrivateKeyF;
        private JsonSerializerSettings _jsonSettings;
        private Task _sendRecieveAwait = Task.CompletedTask;


        /// <exception cref="ArgumentNullException">formRemouteUser == null. -or- possibleCryptoInfo == null. -or- core == null.</exception>
        public MoleClientSender(ContactForm formRemouteUser, PossibleCryptoInfo possibleCryptoInfo,
            MoleClientCoreBase core)
        {
            if (formRemouteUser == null)
                throw new ArgumentNullException(nameof(formRemouteUser)) {Source = GetType().AssemblyQualifiedName};
            if (possibleCryptoInfo == null)
                throw new ArgumentNullException(nameof(possibleCryptoInfo)) {Source = GetType().AssemblyQualifiedName};
            if (core == null) throw new ArgumentNullException(nameof(core)) {Source = GetType().AssemblyQualifiedName};

            RemouteUserForm = formRemouteUser;
            PossibleCryptoInfo = possibleCryptoInfo;
            CoreF = core;
            _jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All,
                Error = (sender, args) => args.ErrorContext.Handled = true
            };
        }
        public MoleClientSender(ContactForm formRemouteUser, PossibleCryptoInfo possibleCryptoInfo,
            MoleClientCoreBase coreF, bool isReg) : this(formRemouteUser, possibleCryptoInfo, coreF)
        {
            IsRegistered = isReg;
        }


        public IPEndPoint RemoteEndPoint { get; protected set; }
        public bool IsAuth { get; protected set; }
        public CryptoInfo CryptoInfoChoosen { get; protected set; }
        public CryptoFactoryBase CryptoFactory { get; protected set; }
        public ICryptoTransform SymmetricEncrypter { get; protected set; }
        public ICryptoTransform SymmetricDencrypter { get; protected set; }
        public IAsymmetricEncrypter AsymmetricEncrypter { get; protected set; }
        public IAsymmetricEncrypter AsymmetricDecrypter { get; protected set; }
        public ContactForm RemouteUserForm { get; protected set; }
        public bool IsConnected => ClientF.Connected;
        public bool IsRegistered { get; protected set; }
        public PossibleCryptoInfo PossibleCryptoInfo { get; }


        public async Task SendText(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str)) {Source = GetType().AssemblyQualifiedName};

            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);
            writer.Write(str);
            var resultAsBytes = await SendAndRecieveAsync(stream.ToArray(), 5, true, false).ConfigureAwait(false);
            //await SendContent(stream.ToArray(), 5);
            var result = Serializer.Deserialize<ResultOfOperation>(resultAsBytes, false);
            if (!result.OperationWasFinishedSuccessful)
                throw CreateException(0);
        }
        /// <exception cref="ArgumentNullException">info == null. Source: <see cref="FileRecieveRequest"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Файла не существует. -or- 
        /// Длина файла равна нулю. <see cref="FileRecieveRequest"/>.</exception>
        public async Task<Tuple<bool, string>> SendFileRecieveRequest(FileInfo info)
        {
            var req = new FileRecieveRequest(info);
            FileStream str;
            if (!CoreF.FilesSending.TryGetValue(info.Name, out str))
                CoreF.FilesSending.TryAdd(info.Name, info.Open(FileMode.Open, FileAccess.Read));
            var result = await SendAndRecieveAsync(req, 3, true, false).ConfigureAwait(false);
            return result.OperationWasFinishedSuccessful
                ? new Tuple<bool, string>(true, null)
                : new Tuple<bool, string>(false, result.ErrorMessage);
        }
        /// <exception cref="SocketException">An error occurred when accessing the socket.</exception>
        /// <exception cref="Exception">При подключении к удаленному пользователю, возникла непредвиденная ошибка.</exception>
        /// <exception cref="ArgumentNullException">endPoint == null</exception>
        public async Task Inicialize(IPEndPoint endPoint)
        {
            if (ClientF != null && ClientF.Connected)
                return;
            if (endPoint == null)
                throw new ArgumentNullException(nameof(endPoint)) {Source = GetType().AssemblyQualifiedName};

            try
            {
                var client = new TcpClient();
                await client.ConnectAsync(endPoint.Address, endPoint.Port).ConfigureAwait(false);
                ClientF = client;
                NetStreamF = client.GetStream();
                RemoteEndPoint = endPoint;
            }
            catch (SocketException ex)
            {
                throw CreateException(7, 0, ex, endPoint.Address.ToString(), endPoint.Port);
            }
            catch (Exception ex)
            {
                throw CreateException(7, 1, endPoint, ex, RemouteUserForm);
            }

        }
        /// <exception cref="InvalidOperationException">Сервер отверг запрос на регистрацию. -or- 
        /// Длина ответа присланного сервером выходит за границы допустимых пределов. -or- 
        /// Аутентификация не была произведена, а ответ присланный сервером зашифрован.</exception>
        /// <exception cref="CryptographicException">Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SerializationException">Во время десериализации ответа от сервера возникла ошибка.</exception>
        public async Task RegisterAsync()
        {
            if (IsRegistered)
                return;
            if (!IsAuth)
                await AuthenticateAsync();

            var content = Encoding.UTF8.GetBytes(CoreF.MyUserForm.Login);
            var result = await SendAndRecieveAsync(content, 2, true, false).ConfigureAwait(false);
            var resultOfOperation = Serializer.Deserialize<ResultOfOperation>(result, false);
            if (!resultOfOperation.OperationWasFinishedSuccessful)
                CreateException(3, resultOfOperation.ErrorMessage);
            IsRegistered = true;
        }
        /// <exception cref="InvalidOperationException">Еще не были согласованы криптографические алгоритмы. -or- 
        /// Передавать симметричный ключ, без использования асимметричных алгоритмов, бессмысленно. -or- 
        /// На запрос симметричного ключа сервер ответил ошибкой.</exception>
        public async Task GetSessionKey()
        {
            if (CryptoFactory == null)
                throw CreateException(11, 0);
            if (AsymmetricEncrypter == null)
                throw CreateException(11, 1);

            var resultAsPrivateKet = await SendAndRecieveAsync<object>(null, 10, true, true).ConfigureAwait(false);
            if (!resultAsPrivateKet.OperationWasFinishedSuccessful)
                throw CreateException(11, 2, resultAsPrivateKet.ErrorMessage);

            var currentResult = (CurrentResult<KeyDataForSymmetricAlgorithm>) resultAsPrivateKet;
            var priveteKey = currentResult.Result;
            var tupl = CryptoFactory.CreateSymmetricAlgoritm(CryptoInfoChoosen.Provider, CryptoInfoChoosen.Symmetric,
                priveteKey);
            SymmetricEncrypter = tupl.Item1;
            SymmetricDencrypter = tupl.Item2;
            PrivateKeyF = priveteKey;
        }
        /// <exception cref="InvalidOperationException">При запросе публичного ключа сервер вернул ошибку. -or- 
        /// Отсутствует необходимый криптопровайдер. -or- Длина контента, в ответе, присланным сервером, 
        /// на запрос открытого ключа, находится вне допустимых пределах. -or- Инициализация не была проведена. -or- 
        /// Сервер отклонил авторизацию. -or- Инициализация не была проведена.</exception>
        /// <exception cref="SerializationException">Во время десериализации результата, полученного от сервера, на запрос на 
        /// получение публичного ключа, возникли ошибки. -or- 
        /// Во время десериализации результата, полученного от сервера, на запрос аутентификации, возникли ошибки.</exception>
        public async Task AuthenticateAsync()
        {
            if (!ClientF.Connected)
                throw CreateException(1, 3);
            if (CryptoFactory == null)
                await SetCryptoAlgsAsync().ConfigureAwait(false);

            try
            {
                Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm> symmTupl =
                    CryptoFactory.CreateSymmetricAlgoritm(CryptoInfoChoosen.Provider, CryptoInfoChoosen.Symmetric);
                var autForm = new ClientToClientAuthForm() {Login = CoreF.MyUserForm.Login, PrivateKey = symmTupl.Item3};
                var result = await SendAndRecieveAsync(autForm, 1, true, true).ConfigureAwait(false);
                if (!result.OperationWasFinishedSuccessful)
                    throw CreateException(1, 1, result.ErrorMessage);

                IsAuth = true;
            }
            catch (SerializationException ex) when (!ex.Source.Equals(GetType().FullName))
            {
                throw CreateException(1, 0, ex);
            }
            catch (Exception ex)
            {
                throw CreateException(1, 4, ex, this);
            }

        }
        /// <exception cref="InvalidOperationException">На запрос части файла, от сервера был получен неверный ответ 
        /// и приведение типов не удалось. -or- Часть файла, возвращенная клиентом, принадлежит другому файлу.</exception>
        /// <exception cref="ArgumentException">При создании запроса на часть файла, возникла ошибка валидации входящих аргументов.</exception>
        /// <exception cref="SerializationException">Возникла ошибка сериализации объекта типа <see cref="RequestPartOfFile"/>. -or- 
        /// <see cref="CustomBinarySerializerBase.Serialize{T}(T, bool)"/></exception>
        public async Task<byte[]> GetPartOfFile(long position, int length, string nameOfFile)
        {
            try
            {
                var taskSourse = await SendRecieveAwait<byte[]>().ConfigureAwait(false);
                var request = new RequestPartOfFile() {Length = length, NameOfFile = nameOfFile, Position = position};
                var requestAsBytes = Serializer.Serialize(request, false);
                await SendContent(requestAsBytes, 4, true, false).ConfigureAwait(false);

                var resultObjAsBytes = await ReadBytesFromStream(false).ConfigureAwait(false);
                var resultOfOperation = Serializer.Deserialize<ResultOfOperation>(resultObjAsBytes, false);
                if (!resultOfOperation.OperationWasFinishedSuccessful)
                    throw CreateException(5, 3, resultOfOperation.ErrorMessage, position, length, nameOfFile);

                var filePartAsBytes = await ReadBytesFromStream(false).ConfigureAwait(false);
                taskSourse.SetResult(filePartAsBytes);
                return filePartAsBytes;


                //var partOfFile = await SendAndRecieveAsync(requestAsBytes, 4, true, false).ConfigureAwait(false);
                //return partOfFile;
                //var content = CoreF.Serializer.Serialize(request, false);
                //await SendContent(content, 4);

                //var result = (CurrentResult<ResponsePartOfFile>) await ReadResultFromStream();
                //var partOfFile = result.Result;
                //if (!partOfFile.NameOfFile.Equals(request.NameOfFile))
                //    throw CreateException(5, 3, nameOfFile, partOfFile.NameOfFile);
                //return partOfFile.PartOfFile;
            }
            catch (ArgumentException ex) when (Type.GetType(ex.Source).Equals(typeof(RequestPartOfFile)))
            {
                throw CreateException(5, 0, ex);
            }
            catch (SerializationException ex)
                when (Type.GetType(ex.Source).GetTypeInfo().IsSubclassOf(typeof(CustomBinarySerializerBase)))
            {
                throw CreateException(5, 1, typeof(RequestPartOfFile).Name, ex);
            }
            catch (InvalidCastException ex)
            {
                throw CreateException(5, 2, typeof(CurrentResult<ResponsePartOfFile>).Name, ex);
            }
        }
        //public async Task<bool> CheckPrivatekey(byte[] bytes = null)
        //{
        //    if (bytes == null)
        //        bytes = new byte[5] {5, 5, 5, 5, 5};

        //    var content = SymmetricEncrypter.TransformFinalBlock(bytes, 0, bytes.Length);
        //    await SendContent(content, 6, false);

        //    var result = (CurrentResult<byte[]>) await ReadResultFromStream();
        //    return bytes.SequenceEqual(result.Result);
        //}
        /// <exception cref="CryptographicException">При отправки контента, возникла криптографическая ошибка. -or- 
        /// Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">При отправки контента, возникла непредвиденная ошибка. -or- 
        /// При чтении ответа от сервера, возникла непредвиденная ошибка.</exception>
        public async Task<byte[]> SendAndRecieveAsync(byte[] content, ushort command,
            bool useEnc, bool useAssCrypt)
        {
            try
            {
                var source = await SendRecieveAwait<byte[]>().ConfigureAwait(false);
                await SendContent(content, command, useEnc, useAssCrypt).ConfigureAwait(false);
                var result = await ReadBytesFromStream(useAssCrypt).ConfigureAwait(false);
                source.SetResult(result);
                return result;
            }
            catch (SerializationException ex) when (!ex.Source.Equals(GetType().AssemblyQualifiedName))
            {
                throw CreateException(8, 0, ex, command, useEnc, useAssCrypt);
            }
            catch (SocketException ex)
            {
                IsAuth = false;
                IsRegistered = false;
                NetStreamF = null;
                AsymmetricEncrypter = null;
                SymmetricEncrypter = null;
                SymmetricDencrypter = null;
                ClientF = null;
                throw CreateException(8, 1, (int)ex.SocketErrorCode, RemoteEndPoint, RemouteUserForm.Login);
            }
            catch (Exception ex) when (!ex.Source.Equals(GetType().AssemblyQualifiedName))
            {
                if (GetType().Equals(Type.GetType(ex.Source, false)))
                    throw;

                throw CreateException(8, 2, ex, RemoteEndPoint, RemouteUserForm, command, useEnc, useAssCrypt,
                    typeof(byte[]).Name);
            }
        }
        /// <exception cref="CryptographicException">При отправки контента, возникла криптографическая ошибка. -or- 
        /// Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">При отправки контента, возникла непредвиденная ошибка. -or- 
        /// При чтении ответа от сервера, возникла непредвиденная ошибка.</exception>
        /// <exception cref="SerializationException">Во время сериализации или десеарелизации возникла ошибка.</exception>
        public async Task<ResultOfOperation> SendAndRecieveAsync<TConent>(TConent obj, ushort command,
            bool useEnc, bool useAssCrypt)
        {
            try
            {
                var objAsBytes = obj as byte[];
                var content = obj == null
                    ? new byte[0]
                    : objAsBytes ?? Serializer.Serialize(obj, false);
                var resultAsBytes =
                    await SendAndRecieveAsync(content, command, useEnc, useAssCrypt).ConfigureAwait(false);
                var resultOfOperation = Serializer.Deserialize<ResultOfOperation>(resultAsBytes, false);
                return resultOfOperation;
            }
            catch (SerializationException ex) when (!ex.Source.Equals(GetType().AssemblyQualifiedName))
            {
                throw CreateException(8, 0, ex, command, useEnc, useAssCrypt);
            }
        }
        /// <exception cref="CryptographicException">При отправки контента, возникла криптографическая ошибка. -or- 
        /// Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">При отправки контента, возникла непредвиденная ошибка. -or- 
        /// При чтении ответа от сервера, возникла непредвиденная ошибка.</exception>
        /// <exception cref="SerializationException">Во время сериализации или десеарелизации возникла ошибка.</exception>
        /// <exception cref="InvalidCastException">Невозможно привести к типу TResult.</exception>
        public async Task<TResult> SendAndRecieveAsync<TConent, TResult>(TConent obj, ushort command,
            bool useEnc, bool useAssCrypt)
        {
            var result = await SendAndRecieveAsync(obj, command, useEnc, useAssCrypt).ConfigureAwait(false);
            var crrentResult = result as CurrentResult<TResult>;

            if (crrentResult == null)
            {
                var str = new StringBuilder();
                str.AppendLine(
                    $"Невозможно привести тип {result.GetType().Name} к типу CurrentResult<{typeof(TResult).Name}>.");
                var type = result.GetType();
                if (type.GetTypeInfo().IsGenericType)
                    str.AppendLine(
                        $"Обобщенный тип пришедшего от сервера результата {type.GenericTypeArguments.First().Name}.");
                str.Append($"command: {command}.");

                throw new InvalidCastException(str.ToString()) {Source = GetType().AssemblyQualifiedName};
            }

            return crrentResult.Result;
        }
        public async Task<PossibleCryptoInfo> GetPossibleCryptoAlgs()
        {
            var resultPossibleCryptoInfo =
                    await
                        SendAndRecieveAsync<object, PossibleCryptoInfo>(null, 7, false, false).ConfigureAwait(false);
            return resultPossibleCryptoInfo;
        }
        public async Task SetCryptoAlgsAsync()
        {
            var possibleCrypto = await GetPossibleCryptoAlgs().ConfigureAwait(false);
            await SetCryptoAlgsAsync(possibleCrypto).ConfigureAwait(false);
        }
        /// <exception cref="InvalidOperationException">Во время создания крипто протокола, не нашлось варианта 
        /// для необходимого крипто алгоритма. -or- По согласованным криптоалгоритмам не удалось выбрать крипто-фабрику. -or- 
        /// Сервер отверг запрос на согласование криптографических алгоритмов.</exception>
        /// <exception cref="ArgumentNullException">possibleCrypto == null.</exception>
        public async Task SetCryptoAlgsAsync(PossibleCryptoInfo possibleCrypto)
        {
            if (possibleCrypto == null)
                throw new ArgumentNullException(nameof(possibleCrypto)) { Source = GetType().AssemblyQualifiedName };

            var crypto = ChooseCrypto(possibleCrypto);
            try
            {
                var result = await SendAndRecieveAsync(crypto, 8, false, false).ConfigureAwait(false);
                if (!result.OperationWasFinishedSuccessful)
                    throw CreateException(9, 1, result.ErrorMessage);

                CryptoInfoChoosen = crypto;
                CryptoFactory =
                    CoreF.Factories.First(
                        cryptoFactory =>
                                cryptoFactory.PossibleCryptoAlgs.Providers.Contains(CryptoInfoChoosen.Provider));
            }
            catch (InvalidOperationException ex) when (GetType() != Type.GetType(ex.Source))
            {
                throw CreateException(9, 0, ex, crypto.Provider);
            }
        }
        /// <exception cref="InvalidOperationException">При запросе публичного ключа сервер вернул ошибку. -or- 
        /// Отсутствует необходимый криптопровайдер. -or- Длина контента, в ответе, присланным сервером, 
        /// на запрос открытого ключа, находится вне допустимых пределах. -or- Инициализация не была проведена.</exception>
        public async Task<IAsymmetricEncrypter> GetPublicKeyAsync()
        {
            if (!ClientF.Connected)
                throw CreateException(1, 3);
            if (CryptoInfoChoosen == null)
                await SetCryptoAlgsAsync().ConfigureAwait(false);

            try
            {
                var publicKeyAsBytes = await SendAndRecieveAsync<object, byte[]>(null, 0, false, false).ConfigureAwait(false);
                var encrypter = CryptoFactory.CreateAsymmetricAlgoritm(CryptoInfoChoosen.Provider,
                    CryptoInfoChoosen.Asymmetric);
                encrypter.Import(publicKeyAsBytes);
                AsymmetricEncrypter = encrypter;
                return AsymmetricEncrypter;
            }
            catch (Exception ex) when (GetType() != Type.GetType(ex.Source))
            {
                throw CreateException(10, 0, this, ex);
            }
        }
        public async Task<IAsymmetricEncrypter> SetPublicKey()
        {
            try
            {
                var decrypter = CryptoFactory.CreateAsymmetricAlgoritm(CryptoInfoChoosen.Provider,
                CryptoInfoChoosen.Asymmetric);
                var publicKey = decrypter.Export(false);
                var result = await SendAndRecieveAsync(publicKey, 9, false, false).ConfigureAwait(false);
                var resultOfOperation = Serializer.Deserialize<ResultOfOperation>(result, false);
                if (!resultOfOperation.OperationWasFinishedSuccessful)
                    CreateException(0, 0);

                AsymmetricDecrypter = decrypter;
                return decrypter;
            }
            catch (SerializationException ex)
            {
                throw;
            }

        }


        private async Task<TaskCompletionSource<TResult>> SendRecieveAwait<TResult>()
        {
            var source = new TaskCompletionSource<TResult>();
            var currentTask = _sendRecieveAwait;
            while (Interlocked.CompareExchange(ref _sendRecieveAwait, source.Task, currentTask) != currentTask)
                await currentTask.ConfigureAwait(false);
            return source;
        }
        /// <exception cref="CryptographicException">Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="InvalidOperationException">Длина ответа присланного сервером выходит за границы допустимых пределов. -or- 
        /// Аутентификация не была произведена, а ответ присланный сервером зашифрован.</exception>
        /// <exception cref="Exception">При чтении ответа от сервера, возникла непредвиденная ошибка.</exception>
        private async Task<byte[]> ReadBytesFromStream(bool useAssCrypte)
        {
            //var isEnc = false;
            byte encType = 0;
            var length = -1;
            try
            {
                var reader = new BinaryReader(NetStreamF);
                encType = reader.ReadByte();
                //isEnc = reader.ReadBoolean();
                length = reader.ReadInt32();
                if (length < 0)
                    throw CreateException(2, 0);
                var resultAsBytes = new byte[length];
                var readedBytesNumb =
                    await NetStreamF.ReadAsync(resultAsBytes, 0, resultAsBytes.Length).ConfigureAwait(false);
                while (readedBytesNumb != resultAsBytes.Length)
                {
                    readedBytesNumb +=
                        await
                            NetStreamF.ReadAsync(resultAsBytes, readedBytesNumb,
                                resultAsBytes.Length - readedBytesNumb).ConfigureAwait(false);
                }
                switch (encType)
                {
                    case 1:
                        resultAsBytes = AsymmetricDecrypter.Decrypt(resultAsBytes);
                        break;
                    case 2:
                        resultAsBytes = SymmetricDencrypter.TransformFinalBlock(resultAsBytes, 0, resultAsBytes.Length);
                        break;
                }
                //if (encType > 0)
                //{
                //    if (!IsAuth)
                //        CreateException(2, 1);

                //    var bytesDec = useAssCrypte
                //        ? AsymmetricDecrypter.Decrypt(resultAsBytes)
                //        : SymmetricDencrypter.TransformFinalBlock(resultAsBytes, 0, resultAsBytes.Length);
                //    resultAsBytes = bytesDec;
                //}
                return resultAsBytes;
            }
            catch (CryptographicException ex)
            {
                throw CreateException(2, 2, ex);
            }
            catch (Exception ex)
            {
                throw CreateException(2, 4, ex, encType, length, this);
            }
        }
        /// <exception cref="CryptographicException">Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SerializationException">Во время десериализации ответа от сервера возникла ошибка.</exception>
        /// <exception cref="InvalidOperationException">Длина ответа присланного сервером выходит за границы допустимых пределов. -or- 
        /// Аутентификация не была произведена, а ответ присланный сервером зашифрован.</exception>
        /// <exception cref="Exception">При чтении ответа от сервера, возникла непредвиденная ошибка.</exception>
        private async Task<ResultOfOperation> ReadResultFromStream(bool useAssCrypt)
        {
            try
            {
                var bytes = await ReadBytesFromStream(useAssCrypt).ConfigureAwait(false);
                var result = Serializer.Deserialize<ResultOfOperation>(bytes, false);
                return result;
            }
            catch (SerializationException ex)
            {
                throw CreateException(2, 3, ex);
            }
        }
        /// <exception cref="CryptographicException">При отправки контента, возникла криптографическая ошибка.</exception>
        /// <exception cref="SocketException">Ошибка сокета.</exception>
        /// <exception cref="Exception">При отправки контента, возникла непредвиденная ошибка.</exception>
        private Task SendContent(byte[] content, ushort command, bool useEnc, bool useAssCrypt)
        {
            try
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                writer.Write(command);
                writer.Write(content);
                var contentForEnc = stream.ToArray();
                byte[] contentEnc;
                //var useEncryption = false;
                byte usedEncryptionType = 0;
                if (useEnc)
                {
                    if (useAssCrypt)
                    {
                        if (AsymmetricEncrypter == null)
                            contentEnc = contentForEnc;
                        else
                        {
                            contentEnc = AsymmetricEncrypter.Encrypt(contentForEnc);
                            usedEncryptionType = 1;
                        }
                    }
                    else
                    {
                        if (SymmetricEncrypter == null)
                        {
                            if (AsymmetricEncrypter == null)
                                contentEnc = contentForEnc;
                            else
                            {
                                contentEnc = AsymmetricEncrypter.Encrypt(contentForEnc);
                                usedEncryptionType = 1;
                            }
                        }
                        else
                        {
                            contentEnc = SymmetricEncrypter.TransformFinalBlock(contentForEnc, 0, contentForEnc.Length);
                            usedEncryptionType = 2;
                        }
                    }
                }
                else
                    contentEnc = contentForEnc;

                stream.SetLength(0);
                writer.Write(usedEncryptionType);
                writer.Write(BitConverter.GetBytes(contentEnc.Length));
                writer.Write(contentEnc);
                var arrayToSend = stream.ToArray();

                return NetStreamF.WriteAsync(arrayToSend, 0, (int) stream.Length);
            }
            catch (CryptographicException ex)
            {
                throw CreateException(6, 0, ex, command, useAssCrypt);
            }
            catch (SocketException ex)
            {
                throw CreateException(6, 1, ex, CoreF.MyUserForm.Login, RemoteEndPoint.Address, RemoteEndPoint.Port);
            }
            catch (Exception ex)
            {
                var typeOfEx = Type.GetType(ex.Source, false);
                if (typeOfEx != null)
                {
                    var info = typeOfEx.GetTypeInfo();
                    if (info.IsSubclassOf(typeof(MoleClientSender)) || typeOfEx.Equals(typeof(MoleClientSender)))
                        throw;
                }
                throw;
                //throw CreateException(6, 2, ex, this, command, useEnc, useAssCrypt);
            }

        }
        /// <exception cref="InvalidOperationException">Во время создания крипто протокола, не нашлось варианта 
        /// для необходимого крипто алгоритма.</exception>
        protected virtual CryptoInfo ChooseCrypto(PossibleCryptoInfo possibleCryptoInfo)
        {
            try
            {
                var provider = possibleCryptoInfo.Providers.Join(PossibleCryptoInfo.Providers, s => s, s => s, (s, s1) => s).FirstOrDefault();
                var hash = possibleCryptoInfo.Hash.Join(PossibleCryptoInfo.Hash, s => s, s => s, (s, s1) => s).First();
                var asymAlg = possibleCryptoInfo.Asymmetric.Join(PossibleCryptoInfo.Asymmetric, s => s, s => s, (s, s1) => s).First();
                var symAlg = possibleCryptoInfo.Symmetric.Join(PossibleCryptoInfo.Symmetric, s => s, s => s, (s, s1) => s).First();
                var signAlg = possibleCryptoInfo.Sign.Join(PossibleCryptoInfo.Sign, s => s, s => s, (s, s1) => s).First();

                return new CryptoInfo()
                {
                    Provider = provider,
                    Asymmetric = asymAlg,
                    Hash = hash,
                    Sign = signAlg,
                    Symmetric = symAlg
                };
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                        "Во время создания крипто протокола, не нашлось варианта для необходимого крипто алгоритма.", ex)
                    {Source = GetType().AssemblyQualifiedName};
            }
        }
        private Exception CreateException(int numb, params object[] objs)
        {
            var result = new Exception();
            var str = new StringBuilder();
            Exception innerExc;
            switch (numb)
            {
                case 0:
                    #region GetPublicKeyAsync()
                    var innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            str.AppendLine("При запросе публичного ключа сервер вернул ошибку.");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            var errorMes = (string) objs[1];
                            str.AppendLine($"Ошибка сервера: {errorMes}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 1:
                            str.AppendLine("Отсутствует необходимый криптопровайдер.");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            var nameOfCryptoProv = (string) objs[1];
                            str.AppendLine($"Имя кроптопрвайдера, полученное от сервера, при запросе публичного ключа: {nameOfCryptoProv}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 2:
                            str.AppendLine(
                                "Длина контента, в ответе, присланным сервером, на запрос открытого ключа, " +
                                "находится вне допустимых пределах. Возможно сообщение повреждено или ожидался " +
                                "неверный формат ответного сообщения.");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 3:
                            str.AppendLine(
                                "Во время десериализации результата, полученного от сервера, на запрос на получение " +
                                "публичного ключа, возникли ошибки. Возможно сообщение повреждено или ожидался " 
                                + "неверный формат ответного сообщения.");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            innerExc = (SerializationException) objs[1];
                            result = new SerializationException(str.ToString(), innerExc);
                            break;
                    }
                    #endregion
                    break;
                case 1:
                    #region AuthenticateAsync()
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            str.AppendLine(
                                "Во время десериализации результата, полученного от сервера, на запрос аутентификации, " +
                                "возникли ошибки. Возможно сообщение повреждено или ожидался неверный формат ответного сообщения.");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            innerExc = (SerializationException)objs[1];
                            result = new SerializationException(str.ToString(), innerExc);
                            break;
                        case 1:
                            str.AppendLine("Сервер отклонил авторизацию.");
                            var errorMes = (string)objs[1];
                            str.AppendLine($"Причина отклонения: {errorMes}");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 3:
                            str.AppendLine("Инициализация не была проведена.");
                            str.AppendLine($"Логин пользователя, выступающего в роли сервера: {CoreF.MyUserForm.Login}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 4:
                            //CreateException(1, 4, 1ex, 2this)
                            str.Append("Авторизация не была проведена, возникла непредвиденная ошибка.");
                            result = new Exception(str.ToString(), (Exception) objs[1]);
                            result.Data.Add("this",
                                JsonConvert.SerializeObject(this, Formatting.Indented, _jsonSettings));
                            break;
                    }
                    #endregion
                    break;
                case 2:
                    #region ReadResultFromStream(bool useAssCrypt = false)
                    innerNumb = (int) objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            str.AppendLine("Длина ответа присланного сервером выходит за границы допустимых пределов.");
                            str.AppendLine($"Логин пользователя (сервера): {CoreF.MyUserForm.Login}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 1:
                            str.AppendLine("Аутентификация не была произведена, а ответ присланный сервером зашифрован.");
                            str.AppendLine($"Логин пользователя (сервера): {CoreF.MyUserForm.Login}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 2:
                            str.AppendLine("Во время расшифровки ответа, присланного сервером, возникла ошибка.");
                            str.AppendLine($"Логин пользователя (сервера): {CoreF.MyUserForm.Login}.");
                            innerExc = (CryptographicException) objs[1];
                            result = new CryptographicException(str.ToString(), innerExc);
                            break;
                        case 3:
                            str.AppendLine("Во время десериализации ответа от сервера возникла ошибка.");
                            str.AppendLine($"Логин пользователя (сервера): {CoreF.MyUserForm.Login}.");
                            innerExc = (SerializationException)objs[1];
                            result = new SerializationException(str.ToString(), innerExc);
                            break;
                        case 4:
                            //throw CreateException(2, 4, 1ex, 2isEnc, 3length, 4this);
                            str.AppendLine("При чтении ответа от сервера, возникла непредвиденная ошибка.");
                            str.AppendLine($"useEnc нужно ли использовать шифрование: {objs[2]}.");
                            str.Append($"length Длина сообщения: {objs[3]}.");
                            result = new Exception(str.ToString(), (Exception) objs[1]);
                            result.Data.Add("this",
                                new ObjectAsDictionary(objs[4], "this", $"{objs[4].GetType().Name} this"));
                            break;
                    }
                    #endregion
                    break;
                case 3:
                    #region RegisterAsync()
                    str.AppendLine("Сервер отверг запрос на регистрацию.");
                    var desc = (string) objs[0];
                    str.AppendLine($"Причина: {desc}");
                    #endregion
                    break;
                case 4:
                    #region SendText(string str)
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            str.AppendLine("Пользователь не принял текстовое сообщение.");
                            var strOfError = (string)objs[1];
                            str.AppendLine($"Текст ошибки: {strOfError}");
                            result = new InvalidOperationException(str.ToString());
                            break;
                    }
                    #endregion
                    break;
                case 5:
                    #region GetPartOfFile(long position, int length, string nameOfFile)
                    innerNumb = (int) objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            str.Append(
                                "При создании запроса на часть файла, возникла ошибка валидации входящих аргументов.");
                            innerExc = (ArgumentException) objs[1];
                            result = new ArgumentException(str.ToString(), innerExc);
                            break;
                        case 1:
                            str.Append($"Возникла ошибка сериализации объекта типа {objs[1]}.");
                            innerExc = (SerializationException) objs[2];
                            result = new SerializationException(str.ToString(), innerExc);
                            break;
                        case 2:
                            str.AppendLine(
                                "На запрос части файла, от сервера был получен неверный ответ и приведение типов не удалось.");
                            str.Append($"Ожидаемый тип: {objs[1]}.");
                            innerExc = (InvalidCastException) objs[2];
                            result = new InvalidOperationException(str.ToString(), innerExc);
                            break;
                        case 3:
                            str.AppendLine("Сервер ответил ошибкой на запрос части файла.");
                            str.AppendLine($"ErrorMessage: {objs[1]}");
                            str.AppendLine($"position: {objs[2]}.");
                            str.AppendLine($"length: {objs[3]}.");
                            str.Append($"nameOfFile: {objs[4]}.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                    }
                    #endregion
                    break;
                case 6:
                    #region SendContent(byte[] content, ushort command, bool useEnc = true, bool useAssCrypt = false)
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            //throw CreateException(6, 0, 1ex, 2command, 3useAssCrypt);
                            str.AppendLine("При отправки контента, возникла криптографическая ошибка.");
                            str.AppendLine($"command отправляемая команда: {objs[2]}.");
                            str.Append($"useAssCrypt нужно ли использовать асимметричные шифрование: {objs[3]}.");
                            result = new CryptographicException(str.ToString(), (CryptographicException)objs[1]);
                            break;
                        case 1:
                            //throw CreateException(6, 1, 1ex, 2Login, 3Ip, 4Port);
                            result = new SocketException((int)((SocketException)objs[1]).SocketErrorCode);
                            result.Data.Add("Login логин пользователя(сервера)",
                                new ObjectAsDictionary(objs[2], "Login", $"{objs[2].GetType().Name} Login"));
                            result.Data.Add("Ip пользователя(сервера)",
                                new ObjectAsDictionary(objs[3], "Ip", $"{objs[3].GetType().Name} Ip"));
                            result.Data.Add("Port пользователя(сервера)",
                                new ObjectAsDictionary(objs[4], "Port", $"{objs[4].GetType().Name} Port"));
                            break;
                        case 2:
                            //throw CreateException(6, 2, 1ex, 2this, 3command, 4useEnc, 5useAssCrypt);
                            str.AppendLine("При отправки контента, возникла непредвиденная ошибка.");
                            str.AppendLine($"command отправляемая команда: {objs[3]}.");
                            str.AppendLine($"useEnc нужно ли использовать шифрование: {objs[4]}.");
                            str.Append($"useAssCrypt нужно ли использовать асимметричные шифрование: {objs[5]}.");
                            result = new Exception(str.ToString(), (Exception)objs[1]);
                            result.Data.Add("this", new ObjectAsDictionary(objs[2], "this", $"{objs[2].GetType().Name} this"));
                            break;

                    }
                    #endregion
                    break;
                case 7:
                    #region Task Inicialize(IPEndPoint endPoint)
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(7, 0, 1ex, 2endPoint.Address.ToString(), 3endPoint.Port);
                            result = new SocketException((int)((SocketException)objs[1]).SocketErrorCode);
                            result.Data.Add("Ip", objs[2]);
                            result.Data.Add("port", objs[3].ToString());
                            break;
                        case 1:
                            //CreateException(7, 1, 1endPoint, 2ex, 3RemouteUserForm)
                            str.AppendLine("При подключении к удаленному пользователю, возникла непредвиденная ошибка.");
                            var endPoint = (IPEndPoint)objs[1];
                            str.AppendLine($"Ip: {endPoint.Address}.");
                            str.Append($"Port: {endPoint.Port}.");
                            result = new Exception(str.ToString(), (Exception)objs[2]);
                            result.Data.Add("RemouteUserForm",
                                JsonConvert.SerializeObject(objs[3], Formatting.Indented, _jsonSettings));
                            break;
                    }
                    #endregion
                    break;
                case 8:
                    #region Task<ResultOfOperation> SendAndRecieveAsync<TConent>(TConent obj, ushort command, bool useEnc, bool useAssCrypt)
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(8, 0, 1ex, 2command, 3useEnc, 4useAssCrypt)
                            str.AppendLine("Во время сериализации контента или десеарелизации ответа от " +
                                           "удаленного пользователя возникла ошибка.");
                            str.AppendLine($"command: {objs[2]}.");
                            str.AppendLine($"useEnc: {objs[3]}.");
                            str.Append($"useAssCrypt: {objs[4]}.");
                            result = new SerializationException(str.ToString(), (SerializationException)objs[1]);
                            break;
                        case 1:
                            //CreateException(8, 1, 1(int)ex.SocketErrorCode, 2RemoteEndPoint, 3RemouteUserForm.Login)
                            result = new SocketException((int)objs[1]);
                            var endPoint = (IPEndPoint)objs[2];
                            result.Data.Add("Ip", endPoint.Address.ToString());
                            result.Data.Add("port", endPoint.Port.ToString());
                            result.Data.Add("RemouteUserForm.Login", objs[3]);
                            break;
                        case 2:
                            //CreateException(8, 2, 1ex, 2RemoteEndPoint, 3RemouteUserForm, 4command, 5useEnc, 6useAssCrypt, 7typeof(TConent).Name)
                            str.AppendLine("При отправки запроса и получения ответа от удаленного пользователя, возникла непредвиденная ошибка.");
                            endPoint = (IPEndPoint)objs[2];
                            str.AppendLine($"Ip: {endPoint.Address}.");
                            str.AppendLine($"Port: {endPoint.Port}.");
                            str.AppendLine($"command: {objs[4]}.");
                            str.AppendLine($"useEnc: {objs[5]}.");
                            str.AppendLine($"useAssCrypt: {objs[6]}.");
                            str.Append($"typeof(TConent).Name: {objs[7]}.");
                            result = new Exception(str.ToString(), (Exception)objs[1]);
                            result.Data.Add("RemouteUserForm",
                                JsonConvert.SerializeObject(objs[3], Formatting.Indented, _jsonSettings));
                            result.Data.Add("RemouteUserForm",
                                JsonConvert.SerializeObject(objs[3], Formatting.Indented, _jsonSettings));
                            break;
                    }
                    #endregion
                    break;
                case 9:
                    #region Task SetCryptoAlgsAsync(PossibleCryptoInfo possibleCrypto)
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(9, 0, 1ex, 2crypto.Provider);
                            str.AppendLine("По согласованным криптоалгоритмам не удалось выбрать крипто-фабрику.");
                            str.Append($"crypto.Provider: {objs[2]}.");
                            result = new InvalidOperationException(str.ToString(), (Exception)objs[1]);
                            break;
                        case 1:
                            //CreateException(9, 1, 1result.ErrorMessage)
                            str.AppendLine("Сервер отверг запрос на согласование криптографических алгоритмов.");
                            str.Append($"result.ErrorMessage: {objs[1]}");
                            result = new InvalidOperationException(str.ToString());
                            break;
                    }
                    #endregion
                    break;
                case 10:
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            //CreateException(10, 0, 1this, 2ex);
                            str.Append("Во время запроса, на получение открытого публичного ключа, произошла непредвиденная ошибка.");
                            result = new Exception(str.ToString(), (Exception) objs[2]);
                            result.Data.Add("this", JsonConvert.SerializeObject(objs[1], Formatting.Indented, _jsonSettings));
                            break;
                    }
                    break;
                case 11:
                    #region Task GetSessionKey()
                    innerNumb = (int)objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            str.Append("Еще не были согласованы криптографические алгоритмы.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 1:
                            str.Append(
                                "Передавать симметричный ключ, без использования асимметричных алгоритмов, бессмысленно.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                        case 2:
                            str.Append("На запрос симметричного ключа сервер ответил ошибкой.");
                            result = new InvalidOperationException(str.ToString());
                            break;
                    }
                    #endregion
                    break;
            }
            result.Source = GetType().AssemblyQualifiedName;
            return result;
        }
    }
}
