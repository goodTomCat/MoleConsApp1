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
using Newtonsoft.Json;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;
//using jaby

namespace SharedMoleRes.Client
{
    public class MolePushServerSender
    {
        protected NetworkStream NetStream;
        protected CustomBinarySerializerBase Serializer;
        //protected CustomBinarySerializerBase Ser = new ProtoBufSerializer();
        protected ICollection<CryptoFactoryBase> FactoriesF;
        protected PossibleCryptoInfo CryptoInfoF;
        protected TcpClient ClientF;
        private JsonSerializerSettings _jsonSettings;
        private Task _sendRecieveAwait = Task.CompletedTask;


        public MolePushServerSender(ICollection<CryptoFactoryBase> factories, PossibleCryptoInfo info)
        {
            if (factories == null || factories.Count == 0)
                throw new ArgumentException("factories == null || factories.Count == 0", nameof(factories))
                    {Source = GetType().AssemblyQualifiedName};
            if (info == null)
                throw new ArgumentNullException(nameof(info)) {Source = GetType().AssemblyQualifiedName};

            FactoriesF = factories;
            CryptoInfoF = info;
            ClientF = new TcpClient();
            Serializer = new ProtoBufSerializer();
            _jsonSettings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All
            };
        }
        public MolePushServerSender(ICollection<CryptoFactoryBase> factories, PossibleCryptoInfo info, bool isReg)
            : this(factories, info)
        {
            IsReg = isReg;
        }


        public bool IsAuth { get; protected set; }
        public bool IsReg { get; protected set; }
        public ICryptoTransform SymEncrypter { get; protected set; }
        public ICryptoTransform SymDecrypter { get; protected set; }
        public IAsymmetricEncrypter AssEncrypter { get; protected set; }
        public IAsymmetricEncrypter AssDecrypter { get; protected set; }
        public CryptoInfo CryptoInfoChoosen { get; protected set; }
        public bool IsConnected => ClientF.Connected;
        public CryptoFactoryBase FactoryChoosen { get; protected set; }



        /// <exception cref="ArgumentNullException">ip == null.</exception>
        /// <exception cref="SocketException">An error occurred when accessing the socket. See the Remarks section for more information.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Значение выходит за границы диапазона: от 1 до 65535.</exception>
        public async Task InitializeConnectionAsync(IPAddress ip, int port)
        {
            if (ip == null) throw new ArgumentNullException(nameof(ip)) {Source = GetType().AssemblyQualifiedName};
            if (port <= 0 || port >= 65535)
                throw new ArgumentOutOfRangeException(nameof(port),
                    "Значение выходит за границы диапазона: от 1 до 65535.") {Source = GetType().AssemblyQualifiedName};
            if (ClientF.Connected)
                return;

            try
            {
                await ClientF.ConnectAsync(ip, port).ConfigureAwait(false);
                NetStream = ClientF.GetStream();
            }
            catch (SocketException ex)
            {
                throw CreateException(0, ex);
            }
        }
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="InvalidOperationException">Не удалось согласовать с сервером криптографические алгоритмы.</exception>
        /// <exception cref="Exception">Во время согласования с сервером криптографических алгоритмов, 
        /// возникла непредвиденная ошибка.</exception>
        public async Task SetCryptoAlgs()
        {
            try
            {
                await SendContent(new byte[0], 8, false, false).ConfigureAwait(false);
                PossibleCryptoInfo cryptoAlgsPossible = Serializer.Deserialize<PossibleCryptoInfo>(NetStream, true);
                var cryptoChoosen = new CryptoInfo()
                {
                    Asymmetric =
                        cryptoAlgsPossible.Asymmetric.Join(CryptoInfoF.Asymmetric, s => s, s => s, (s, s1) => s).First(),
                    Hash = cryptoAlgsPossible.Hash.Join(CryptoInfoF.Hash, s => s, s => s, (s, s1) => s).First(),
                    Provider =
                        cryptoAlgsPossible.Providers.Join(CryptoInfoF.Providers, s => s, s => s, (s, s1) => s).First(),
                    Sign = cryptoAlgsPossible.Sign.Join(CryptoInfoF.Sign, s => s, s => s, (s, s1) => s).First(),
                    Symmetric =
                        cryptoAlgsPossible.Symmetric.Join(CryptoInfoF.Symmetric, s => s, s => s, (s, s1) => s).First()
                };
                var result = await SendAndRecieveAsync(cryptoChoosen, 3, false, false);
                if (!result.OperationWasFinishedSuccessful)
                    throw CreateException(8, 0, result.ErrorMessage, result.ErrorCode, cryptoAlgsPossible, cryptoChoosen);

                CryptoInfoChoosen = cryptoChoosen;
                FactoryChoosen =
                    FactoriesF.First(
                        basee => basee.PossibleCryptoAlgs.Providers.Any(s => s.Equals(cryptoChoosen.Provider)));
            }
            catch (SocketException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw CreateException(8, 1, ex);
            }

        }
        /// <exception cref="InvalidOperationException">Инициализация не была произведена.</exception>
        /// <exception cref="CryptographicException">Во время импорта ключа в криптографический объект возникла ошибка.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><see cref="IAsymmetricEncrypter.Import(byte[])"/></exception>
        public virtual async Task<IAsymmetricEncrypter> GetPublicKeAsync()
        {
            if (NetStream == null || !ClientF.Connected)
                throw new InvalidOperationException(
                    "Инициализация не была произведена. -or- Подключение не установлено.")
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (CryptoInfoChoosen == null)
                throw CreateException(1, 2);


            try
            {
                var publicKey = await SendAndRecieveAsync<object, byte[]>(null, 0, false, false).ConfigureAwait(false);
                AssEncrypter = FactoryChoosen.CreateAsymmetricAlgoritm(CryptoInfoChoosen.Provider,
                    CryptoInfoChoosen.Asymmetric);
                AssEncrypter.Import(publicKey);

                return AssEncrypter;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw;
            }
            catch (CryptographicException ex)
            {
                var type = Type.GetType(ex.Source, false);
                if (type != null && type.GetTypeInfo().IsAssignableFrom(typeof(IAsymmetricEncrypter).GetTypeInfo()))
                    throw CreateException(1, 1, ex);
                else
                    throw CreateException(1, 3, ex, CryptoInfoChoosen.Asymmetric, ClientF);
            }
            catch (Exception ex)
            {
                throw CreateException(1, 3, ex, CryptoInfoChoosen.Asymmetric, ClientF);
            }
        }
        /// <exception cref="InvalidOperationException">Инициализация не была произведена. -or- 
        /// Подключение не установлено. -or- Сервер не принял открытый ключ. -or- 
        /// Криптографические алгоритмы еще не были согласованы с сервером.</exception>
        /// <exception cref="Exception">При отправки открытого ключа возникла непредвиденная ошибка.</exception>
        public virtual async Task<IAsymmetricEncrypter> SetPublicKeyAsync()
        {
            if (NetStream == null || !ClientF.Connected)
                throw new InvalidOperationException(
                    "Инициализация не была произведена. -or- Подключение не установлено.")
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (CryptoInfoChoosen == null)
                throw CreateException(1, 2);

            try
            {
                var decrypter = FactoryChoosen.CreateAsymmetricAlgoritm(CryptoInfoChoosen.Provider,
                    CryptoInfoChoosen.Asymmetric);
                var publicKey = decrypter.Export(false);
                var result = await SendAndRecieveAsync(publicKey, 9, false, false).ConfigureAwait(false);
                if (!result.OperationWasFinishedSuccessful)
                    throw CreateException(12, 0, result.ErrorMessage);

                AssDecrypter = decrypter;
                return AssDecrypter;
            }
            catch (Exception ex)
            {
                throw CreateException(12, 1, ex);
            }

        }
        /// <exception cref="InvalidOperationException">Инициализация не была произведена. -or- 
        /// Не удалось согласовать с сервером криптографические алгоритмы.</exception>
        /// <exception cref="CryptographicException">Во время импорта ключа в криптографический объект возникла ошибка. -or- 
        /// Во время шифрования контента сообщения для отправки на push сервер, возникла криптографическая ошибка. 
        /// -or- Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        public virtual async Task<Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm>> GetSessionKey
            ()
        {
            if (NetStream == null || !ClientF.Connected)
                throw new InvalidOperationException(
                    "Инициализация не была произведена. -or- Подключение не установлено.")
                {
                    Source = GetType().AssemblyQualifiedName
                };

            if (CryptoInfoChoosen == null)
                await SetCryptoAlgs().ConfigureAwait(false);
            if (AssEncrypter == null)
                await GetPublicKeAsync().ConfigureAwait(false);
            if (AssDecrypter == null)
                await SetPublicKeyAsync().ConfigureAwait(false);

            var privateKeyDataResult =
                await
                    SendAndRecieveAsync<object, KeyDataForSymmetricAlgorithm>(null, 7, true, true)
                        .ConfigureAwait(false);
            var symTupl = FactoryChoosen.CreateSymmetricAlgoritm(CryptoInfoChoosen.Provider,
                CryptoInfoChoosen.Symmetric, privateKeyDataResult);
            SymEncrypter = symTupl.Item1;
            SymDecrypter = symTupl.Item2;
            return new Tuple<ICryptoTransform, ICryptoTransform, KeyDataForSymmetricAlgorithm>(symTupl.Item1,
                symTupl.Item2, privateKeyDataResult);
        }
        /// <exception cref="InvalidOperationException">Инициализация не была произведена. -or- 
        /// <see cref="MolePushServerSender.SetCryptoAlgs()"/>.</exception>
        /// <exception cref="ArgumentNullException">userForm == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Форма userForm не верно заполнена.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception"><see cref="MolePushServerSender.SetCryptoAlgs()"/>. -or- 
        /// Во время отправки сообщения на push сервер, возникла непредвиденная ошибка.</exception>
        public virtual async Task RegisterNewUserAsync(UserForm userForm)
        {
            if (NetStream == null || !ClientF.Connected)
                throw new InvalidOperationException("Инициализация не была произведена.")
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (IsReg)
                return;
            if (userForm == null)
                throw new ArgumentNullException(nameof(userForm))
                    {Source = GetType().AssemblyQualifiedName};
            var errorMes = "";
            if (!IsTrueForm(userForm, ref errorMes))
                throw new ArgumentOutOfRangeException(nameof(userForm), errorMes)
                    {Source = GetType().AssemblyQualifiedName};

            try
            {
                if (CryptoInfoChoosen == null)
                    await SetCryptoAlgs().ConfigureAwait(false);
                if (AssEncrypter == null)
                    await GetPublicKeAsync().ConfigureAwait(false);
                if (SymEncrypter == null)
                    await GetSessionKey().ConfigureAwait(false);

                var result = await SendAndRecieveAsync(userForm, 1, true, false).ConfigureAwait(false);
                if (!result.OperationWasFinishedSuccessful)
                    throw new InvalidOperationException($"Сервер не зарегистрировал нового пользователя. " +
                                                        $"Ошибка сервера: {result.ErrorMessage}.");
                IsAuth = true;
                IsReg = true;

            }
            catch (SocketException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                var type = Type.GetType(ex.Source, false);
                if (GetType().Equals(type))
                    throw;

                CreateException(3, ex);
            }
        }
        /// <exception cref="InvalidOperationException">Инициализация не была произведена. -or- 
        /// Авторизация не была произведена.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">Не удалось согласовать с сервером криптографические алгоритмы.</exception>
        public virtual async Task<IEnumerable<UserForm>> GetUsersPublicData(string[] logins)
        {
            if (NetStream == null || !ClientF.Connected)
                throw new InvalidOperationException("Инициализация не была произведена.")
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (!IsAuth)
                throw new InvalidOperationException("Авторизация не была произведена.")
                {
                    Source = GetType().AssemblyQualifiedName
                };

            try
            {
                var forms =
                    await
                        SendAndRecieveAsync<string[], ICollection<UserForm>>(logins, 3, AssEncrypter != null,
                                SymEncrypter == null)
                            .ConfigureAwait(false);
                return forms;
            }
            catch (SocketException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (GetType().Equals(Type.GetType(ex.Source, false)))
                    throw;

                throw CreateException(9, 0, ex, logins);
            }

        }
        /// <exception cref="InvalidOperationException">Инициализация не была произведена. -or- 
        /// Регистрация не была произведена. -or- При попытке авторизации возникла непредвиденная ошибка.</exception>
        /// <exception cref="ArgumentNullException">authenticationForm == null.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">При попытке авторизации возникла непредвиденная ошибка.</exception>
        public virtual async Task<OfflineMessagesConcurentSur> AuthenticateUserAsync(
            IAuthenticationForm authenticationForm)
        {
            if (IsAuth)
                return new OfflineMessagesConcurentSur();
                //return new Dictionary<string, IList<byte[]>>();
            if (NetStream == null || !ClientF.Connected)
                throw new InvalidOperationException("Инициализация не была произведена.")
                    {Source = GetType().AssemblyQualifiedName};
            if (!IsReg)
                throw new InvalidOperationException("Регистрация не была произведена.")
                    {Source = GetType().AssemblyQualifiedName};
            if (authenticationForm == null)
                throw new ArgumentNullException(nameof(authenticationForm)) {Source = GetType().AssemblyQualifiedName};

            try
            {
                var result =
                    await SendAndRecieveAsync(authenticationForm, 2, AssEncrypter != null, SymEncrypter == null);
                if (!result.OperationWasFinishedSuccessful)
                    throw CreateException(10, 0, result);

                var currentResult = (CurrentResult<OfflineMessagesConcurentSur>) result;
                IsAuth = true;
                return currentResult.Result;

            }
            catch (SocketException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (GetType().Equals(Type.GetType(ex.Source, false)))
                    throw;

                throw CreateException(10, 1, authenticationForm, ex);
            }

        }
        /// <exception cref="InvalidOperationException">Авторизация не была произведена.</exception>
        /// <exception cref="ArgumentNullException">login == null</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">При попытке поиска пользователя возникла непредвиденная ошибка.</exception>
        public virtual async Task<ICollection<UserFormSurrogate>> FinedUserAsync(string login)
        {
            if (!IsAuth)
                throw new InvalidOperationException("Авторизация не была произведена.")
                    {Source = GetType().AssemblyQualifiedName};
            if (login == null)
                throw new ArgumentNullException(nameof(login)) {Source = GetType().AssemblyQualifiedName};

            try
            {
                var forms =
                    await
                        SendAndRecieveAsync<string, ICollection<UserFormSurrogate>>(login, 4, AssEncrypter != null,
                            SymEncrypter == null).ConfigureAwait(false);
                return forms;
            }
            catch (SocketException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (GetType().Equals(Type.GetType(ex.Source, false)))
                    throw;

                throw CreateException(11, 0, ex, login);
            }

        }
        /// <exception cref="CryptographicException">Во время шифрования контента сообщения для отправки на push сервер, 
        /// возникла криптографическая ошибка. -or- Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">Во время отправки сообщения на push сервер, 
        /// возникла непредвиденная ошибка.</exception>
        /// <exception cref="SerializationException">Во время десериализации ответа от сервера возникла ошибка.</exception>
        /// <exception cref="InvalidOperationException">Длина ответа присланного сервером выходит за границы допустимых пределов. -or- 
        /// Аутентификация не была произведена, а ответ присланный сервером зашифрован.</exception>
        public async Task<ResultOfOperation> SendAndRecieveAsync<TConent>(TConent obj, ushort command,
            bool useEnc, bool useAssCrypt)
        {
            try
            {
                var source = await SendRecieveAwait<ResultOfOperation>().ConfigureAwait(false);
                var objAsBytes = obj as byte[];
                var content = obj == null
                    ? new byte[0]
                    : objAsBytes ?? Serializer.Serialize(obj, false);
                await SendContent(content, command, useEnc, useAssCrypt).ConfigureAwait(false);
                var result = await ReadResultFromStream(useAssCrypt).ConfigureAwait(false);
                source.SetResult(result);
                return result;
            }
            catch (SerializationException ex) when (!ex.Source.Equals(GetType().AssemblyQualifiedName))
            {
                throw CreateException(7, 0, ex);
            }
            catch (SocketException ex)
            {
                IsAuth = false;
                IsReg = false;
                NetStream = null;
                AssEncrypter = null;
                SymEncrypter = null;
                SymDecrypter = null;
                ClientF = null;
                if (!ex.Source.Equals(GetType().AssemblyQualifiedName))
                    ex.Source = GetType().AssemblyQualifiedName;
                throw;
            }
            catch (Exception ex) when (!ex.Source.Equals(GetType().AssemblyQualifiedName))
            {
                throw CreateException(7, 1, ex);
            }
        }
        /// <exception cref="CryptographicException">Во время шифрования контента сообщения для отправки на push сервер, 
        /// возникла криптографическая ошибка. -or- Во время расшифровки ответа, присланного сервером, возникла ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">Во время отправки сообщения на push сервер, 
        /// возникла непредвиденная ошибка.</exception>
        /// <exception cref="SerializationException">Во время десериализации ответа от сервера возникла ошибка.</exception>
        /// <exception cref="InvalidOperationException">Длина ответа присланного сервером выходит за границы допустимых пределов. -or- 
        /// Аутентификация не была произведена, а ответ присланный сервером зашифрован.</exception>
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


        private bool IsTrueForm(UserForm form, ref string errorMes)
        {
            if (form.Login == null)
            {
                errorMes = "Не задан логин.";
                return false;
            }
            if (form.Password == null)
            {
                errorMes = "Не задан паспорт.";
                return false;
            }
            if (form.Accessibility == null)
            {
                errorMes = "Не задана форма аутентификации.";
                return false;
            }
            return true;
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
        /// <exception cref="SerializationException">Во время десериализации ответа от сервера возникла ошибка.</exception>
        /// <exception cref="InvalidOperationException">Длина ответа присланного сервером выходит за границы допустимых пределов. -or- 
        /// Аутентификация не была произведена, а ответ присланный сервером зашифрован.</exception>
        private async Task<ResultOfOperation> ReadResultFromStream(bool useAssCrypt = false)
        {
            try
            {
                var encByte = new byte[1];
                await NetStream.ReadAsync(encByte, 0, encByte.Length).ConfigureAwait(false);
                var isEnc = Convert.ToBoolean(encByte[0]);
                var lengthAsBytes = new byte[4];
                await NetStream.ReadAsync(lengthAsBytes, 0, lengthAsBytes.Length);
                var length = BitConverter.ToInt32(lengthAsBytes, 0);
                if (length < 0 || length > 4096)
                    throw CreateException(4, 0);
                var resultAsBytes = new byte[length];
                await NetStream.ReadAsync(resultAsBytes, 0, resultAsBytes.Length);
                if (isEnc)
                {
                    if (!useAssCrypt && SymDecrypter == null)
                        throw CreateException(4, 1);

                    var bytesDec = useAssCrypt
                        ? AssDecrypter.Decrypt(resultAsBytes)
                        : SymDecrypter.TransformFinalBlock(resultAsBytes, 0, resultAsBytes.Length);
                    resultAsBytes = bytesDec;
                }
                var result = Serializer.Deserialize<ResultOfOperation>(resultAsBytes, false);
                return result;
            }
            catch (CryptographicException ex)
            {
                throw CreateException(4, 2, ex);
            }
            catch (SerializationException ex)
            {
                //var model = RuntimeTypeModel.Default;
                throw CreateException(4, 3, ex);
            }

        }
        private void TempFunc()
        {
            //var rsa = new RSACng(4096);
            //var keyTrue = (rsa).ExportParameters(false);
            //var publicKeyInForm = new UserForm()
            //{
            //    PublicKeyParamsBlob = Serializer.Serialize(keyTrue, false),
            //    CryptoProvider = CryptoProvider.CngMicrosoft
            //};
            //var resultForSer = new CurrentResult<UserForm>() {Result = publicKeyInForm};

            //var stream = new MemoryStream();
            //var writer = new BinaryWriter(stream);
            //var contentForEnc = Serializer.Serialize(resultForSer, false);
            //writer.Write(false);
            //var contentEnc = contentForEnc;
            //writer.Write(contentEnc.Length);
            //writer.Write(contentEnc);

            //stream.Seek(0, SeekOrigin.Begin);
            //var reader = new BinaryReader(stream);
            //var isEnc = reader.ReadBoolean();
            //var length = reader.ReadInt32();
            //var resultAsBytes = new byte[length];
            //stream.Read(resultAsBytes, 0, resultAsBytes.Length);
            //var currentResult = Serializer.Deserialize<SharedMoleResources.Server.ResultOfOperation>(
            //    resultAsBytes, false);

            //var resultForSer2 =
            //    (SharedMoleResources.Server.CurrentResult<SharedMoleResources.Server.UserForm>) currentResult;
            //var key1 = Serializer.Deserialize<RSAParameters>(resultForSer2.Result.PublicKeyParamsBlob, false);
            //bool isEq = keyTrue.Modulus.SequenceEqual(key1.Modulus);



            //var resultAsBytes = Core.Serializer.Serialize(resultForSer, false);
            //var resultForSer2 =
            //    Core.Serializer
            //        .Deserialize<SharedMoleResources.Server.CurrentResult<SharedMoleResources.Server.UserForm>>(
            //            resultAsBytes, false);
            //var key1 = Core.Serializer.Deserialize<RSAParameters>(resultForSer2.Result.PublicKeyParamsBlob, false);
            //bool isEq = keyTrue.Modulus.SequenceEqual(key1.Modulus);

        }
        /// <exception cref="ArgumentNullException">content.</exception>
        /// <exception cref="CryptographicException">Во время шифрования контента сообщения для отправки на push сервер, 
        /// возникла криптографическая ошибка.</exception>
        /// <exception cref="SocketException">Socket error.</exception>
        /// <exception cref="Exception">Во время отправки сообщения на push сервер, 
        /// возникла непредвиденная ошибка.</exception>
        private Task SendContent(byte[] content, ushort command, bool useEnc = true, bool useAssCrypt = false)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            var contentForEnc = new byte[0];
            try
            {
                var stream = new MemoryStream();
                var writer = new BinaryWriter(stream);
                writer.Write(command);
                writer.Write(content);
                contentForEnc = stream.ToArray();
                stream.SetLength(0);
                writer.Write(useEnc);

                //Action func = () =>
                //{
                //    if (!useEnc || !useAssCrypt)
                //        return;
                //    try
                //    {
                //        var bytes = new byte[500];
                //        for (int i = 0; i < bytes.Length; i++)
                //            bytes[i] = 1;
                //        //var err = AssEncrypter.Encrypt(bytes);
                //        bytes = new byte[30];
                //        for (int i = 0; i < bytes.Length; i++)
                //            bytes[i] = 1;
                //        var err = AssEncrypter.Encrypt(bytes);
                //    }
                //    catch (Exception ex)
                //    {
                        
                //        throw;
                //    }
                    
                //};
                //func.Invoke();

                var contentEnc = useEnc
                    ? (useAssCrypt
                        ? AssEncrypter.Encrypt(contentForEnc)
                        : SymEncrypter.TransformFinalBlock(contentForEnc, 0, contentForEnc.Length))
                    : contentForEnc;
                writer.Write(contentEnc.Length);
                writer.Write(contentEnc);

                return NetStream.WriteAsync(stream.ToArray(), 0, (int) stream.Length);
            }
            catch (CryptographicException ex)
            {
                throw CreateException(5, 0, ex, contentForEnc.Length, useAssCrypt);
            }
            catch (SocketException ex)
            {
                throw CreateException(5, 1, ex);
            }
            catch (Exception ex)
            {
                throw CreateException(5, 2, ex, content.Length, command, useAssCrypt);
            }
            
        }
        ////private async Task<ResultOfOperation> ReadEncResultAsync(CustomBinarySerializerBase ser = null)
        ////{
        ////    if (ser == null)
        ////        ser = new ProtoBufSerializer();

        ////    var reader = new BinaryReader(NetStream);
        ////    var numbOfBytesToRead = reader.ReadInt32();
        ////    var readedBytes = new byte[numbOfBytesToRead];
        ////    await NetStream.ReadAsync(readedBytes, 0, readedBytes.Length);
        ////    var bytesDec = AesDecrypter.TransformFinalBlock(readedBytes, 0, readedBytes.Length);
        ////    var resultFromServer = ser.Deserialize<ResultOfOperation>(bytesDec, false);
        ////    return resultFromServer;
        ////}

        ////private async Task<CurrentResult<TResult>> ReadEncCurrentResultAsync<TResult>(CustomBinarySerializerBase ser = null)
        ////{
        ////    if (ser == null)
        ////        ser = new ProtoBufSerializer();

        ////    var reader = new BinaryReader(NetStream);
        ////    var numbOfBytesToRead = reader.ReadInt32();
        ////    var readedBytes = new byte[numbOfBytesToRead];
        ////    await NetStream.ReadAsync(readedBytes, 0, readedBytes.Length);
        ////    var bytesDec = AesDecrypter.TransformFinalBlock(readedBytes, 0, readedBytes.Length);
        ////    var resultFromServer = ser.Deserialize<CurrentResult<TResult>>(bytesDec, false);
        ////    return resultFromServer;
        ////}
        //private async Task<Tuple<IEnumerable<TcpListener>, CngKey, RSACng, UserForm>> CreateFormAsync(bool listenPorts,
        //    Random random = null)
        //{
        //    var keySign = CngKey.Create(CngAlgorithm.ECDsaP521);
        //    var rsa = new RSACng(4096);
        //    var rand = random ?? new Random();
        //    var form = new UserForm()
        //    {
        //        KeyParametrsBlob = keySign.Export(CngKeyBlobFormat.EccPublicBlob),
        //        Login = $"Frodo {rand.Next(0, 555)}",
        //        Password = "123456",
        //        PortClientToClient1 = (ushort)rand.Next(20000, 60000),
        //        PortClientToClient2 = (ushort)rand.Next(20000, 60000),
        //        PortClientToClient3 = (ushort)rand.Next(20000, 60000),
        //        PortServerToClient = (ushort)rand.Next(20000, 60000)
        //    };

        //    var listners = new List<TcpListener>(4);
        //    if (listenPorts)
        //    {
        //        var listnerStartTasks = new Task[4];
        //        var listner1 = new TcpListener(IPAddress.Parse("192.168.65.129"), form.PortClientToClient1);
        //        listnerStartTasks[0] = Task.Run(() => listner1.Start());
        //        listners.Add(listner1);

        //        var listner2 = new TcpListener(IPAddress.Parse("192.168.65.129"), form.PortClientToClient2);
        //        listnerStartTasks[1] = Task.Run(() => listner2.Start());
        //        listners.Add(listner2);

        //        var listner3 = new TcpListener(IPAddress.Parse("192.168.65.129"), form.PortClientToClient3);
        //        listnerStartTasks[2] = Task.Run(() => listner3.Start());
        //        listners.Add(listner3);

        //        var listner4 = new TcpListener(IPAddress.Parse("192.168.65.129"), form.PortServerToClient);
        //        listnerStartTasks[3] = Task.Run(() => listner4.Start());
        //        listners.Add(listner4);

        //        await Task.WhenAll(listnerStartTasks);
                
        //    }
        //    return new Tuple<IEnumerable<TcpListener>, CngKey, RSACng, UserForm>(listners, keySign, rsa, form);
        //}
        private Exception CreateException(int n, params object[] objs)
        {
            try
            {
                Exception result = new Exception("Не удалось сгенерировать ошибку.");// = null;
                Exception innerException;
                var innerNumb = 0;
                var str = new StringBuilder();
                switch (n)
                {
                    case 0:
                        var innerSockExc = (SocketException)objs[0];
                        result = new SocketException((int)innerSockExc.SocketErrorCode);
                        break;
                    case 1:
                        var addNumb = (int) objs[0];
                        switch (addNumb)
                        {
                            case 0:
                                var innerSerExc = (SerializationException)objs[0];
                                result =
                                    new SerializationException(
                                        "Возникла ошибка во время десиарелизации в объект RSAParameters.",
                                        innerSerExc);
                                break;
                            case 1:
                                innerException = (Exception) objs[1];
                                var mes = "Во время импорта ключа в криптографический объект возникла ошибка.";
                                if (innerException is ArgumentException)
                                    result = new ArgumentException(mes, innerException);
                                else
                                    result = new CryptographicException(mes, innerException);
                                break;
                            case 2:
                                result = new InvalidOperationException("Криптографические алгоритмы еще не были согласованы с сервером.");
                                break;
                            case 3:
                                //throw CreateException(1, 3, 0ex, 1CryptoInfoChoosenF.Asymmetric, 2ClientF);
                                str.AppendLine("Во время запроса публичного ключа произошла непредвиденная ошибка.");
                                str.Append($"AsymmetricEncrypter: {objs[1]}.");
                                result = new Exception(str.ToString(), (Exception) objs[0]);
                                result.Data.Add("ClientF",
                                    JsonConvert.SerializeObject(objs[2], Formatting.Indented, _jsonSettings));
                                break;
                            default:
                                throw new Exception();
                        }
                        
                        break;
                    case 2:
                        innerException = (SerializationException) objs[0];
                        result =
                            new SerializationException(
                                "Произошла ошибка десеарилизации ответа от сервера, при отправки приватного ключа.", innerException);
                        break;
                    case 3:
                        //Task RegisterNewUserAsync(UserForm userForm)
                        result = new Exception("При попытки регистрации пользователя возникла непредвиденная ошибка.",
                            (Exception) objs[0]);
                        break;
                    case 4:
                        #region ReadResultFromStream(bool useAssCrypt = false)
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                str.AppendLine("Длина ответа присланного push сервером выходит за границы допустимых пределов.");
                                result = new InvalidOperationException(str.ToString());
                                break;
                            case 1:
                                str.AppendLine("На сервер не был отправлен приватный ключ.");
                                result = new InvalidOperationException(str.ToString());
                                break;
                            case 2:
                                str.AppendLine("Во время расшифровки ответа, присланного сервером, возникла ошибка.");
                                innerException = (CryptographicException)objs[1];
                                result = new CryptographicException(str.ToString(), innerException);
                                break;
                            case 3:
                                str.AppendLine("Во время десериализации ответа от сервера возникла ошибка.");
                                innerException = (SerializationException)objs[1];
                                result = new SerializationException(str.ToString(), innerException);
                                break;
                        }
                        #endregion
                        break;
                    case 5:
                        #region SendContent(byte[] content, ushort command, bool useEnc = true, bool useAssCrypt = false)
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                str.AppendLine("Во время шифрования контента сообщения для отправки на push сервер, " +
                                           "возникла криптографическая ошибка.");
                                str.AppendLine($"Длина контента вместе с командой: {objs[2]}");
                                str.AppendLine($"Использовалось ли асимметричное шифрование: {objs[3]}");
                                innerException = (CryptographicException)objs[1];
                                result = new CryptographicException(str.ToString(), innerException);
                                break;
                            case 1:
                                innerException = (SocketException)objs[1];
                                result = new SocketException((int)((SocketException)innerException).SocketErrorCode);
                                break;
                            case 2:
                                str.AppendLine("Во время отправки сообщения на push сервер, " +
                                           "возникла непредвиденная ошибка.");
                                str.AppendLine($"Длина контента вместе с командой: {objs[2]}");
                                str.AppendLine($"Использовалось ли асимметричное шифрование: {objs[4]}");
                                str.AppendLine($"Команда для push сервера: {objs[3]}");
                                innerException = (Exception)objs[1];
                                result = new Exception(str.ToString(), innerException);
                                break;
                        }
                        #endregion
                        break;
                    case 6:
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                str.AppendLine("Сервер ответил отказом на запрос вернуть публичные " +
                                               "данные пользователей с похожим логином.");
                                str.AppendLine($"Ошибка сервера: {objs[2]}.");
                                result = new InvalidOperationException(str.ToString());
                                break;
                            case 1:
                                str.AppendLine("При десеарелизации коллекции с публичными данными пользователей возникла ошибка.");
                                innerException = (SerializationException) objs[2];
                                result = new SerializationException(str.ToString(), innerException);
                                break;
                            case 2:
                                str.AppendLine("Возникла ошибка преобразования типов.");
                                str.AppendLine($"Ожидался тип: {objs[3]}.");
                                innerException = (InvalidCastException)objs[2];
                                result = new InvalidCastException(str.ToString(), innerException);
                                break;
                        }
                        break;
                    case 7:
                        #region SendAndRecieveContentAsync<TConent, TResult>(TConent obj, ushort command,bool useEnc, bool useAssCrypt)
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                str.Append("Во время сериализации объекта, для отправки команды push серверу, возникла ошибка.");
                                innerException = (SerializationException)objs[1];
                                result = new SerializationException(str.ToString(), innerException);
                                break;
                            case 1:
                                str.Append("Во время обмена сообщениями с сервером возникла ошибка.");
                                innerException = (Exception)objs[1];
                                result = new Exception(str.ToString(), innerException);
                                break;
                        }
                        #endregion
                        break;
                    case 8:
                        #region SetCryptoAlgs()
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            //throw CreateException(08, 0, 0result.ErrorMessage, 1result.ErrorCode, 2cryptoAlgsPossible, 3cryptoChoosen);
                            case 0:
                                str.AppendLine("Не удалось согласовать с сервером криптографические алгоритмы.");
                                str.AppendLine($"ResultOfOperation.ErrorMessage: {objs[0]}.");
                                str.Append($"ResultOfOperation.ErrorCode: {objs[1]}.");
                                result = new InvalidOperationException(str.ToString());
                                result.Data.Add("cryptoAlgsPossible",
                                    JsonConvert.SerializeObject(objs[2], Formatting.Indented, _jsonSettings));
                                result.Data.Add("cryptoChoosen",
                                    JsonConvert.SerializeObject(objs[3], Formatting.Indented, _jsonSettings));
                                break;
                            case 1:
                                result =
                                    new Exception("Во время согласования с сервером криптографических алгоритмов," +
                                                  " возникла непредвиденная ошибка.", (Exception) objs[0]);
                                break;
                        }
                        #endregion
                        break;
                    case 9:
                        #region Task<IEnumerable<UserForm>> GetUsersPublicData(string[] logins)
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                //CreateException(9, 0, 1ex, 2logins);
                                str.AppendLine(
                                    "При запросе публичных данных пользователей по их логинам возникла непредвиденная ошибка.");
                                var logins = (string[])objs[2];
                                str.Append($"Логины: {logins.First()}");
                                for (int i = 1; i < logins.Length; i++)
                                {
                                    str.Append($" ,{logins[i]}");
                                }
                                str.Append(".");
                                result = new Exception(str.ToString(), (Exception)objs[1]);
                                break;
                        }
                        #endregion
                        break;
                    case 10:
                        #region Task AuthenticateUserAsync(IAuthenticationForm authenticationForm)
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                //CreateException(10, 0, 1result);
                                var resultOfOperation = (ResultOfOperation)objs[1];
                                str.AppendLine("Попытка авторизации провалилась.");
                                str.Append($"Ошибка присланная сервером: {resultOfOperation.ErrorMessage}");
                                result = new InvalidOperationException(str.ToString());
                                break;
                            case 1:
                                //CreateException(10, 1, 1authenticationForm, 2ex);
                                str.AppendLine("При попытке авторизации возникла непредвиденная ошибка.");
                                var auth = (IAuthenticationForm) objs[1];
                                var authClassic = auth as AuthenticationFormClassic;
                                if (authClassic != null)
                                    str.AppendLine($"HashAlgotitm: {authClassic.HashAlgotitm}");
                                var authSign = auth as IAuthenticationFormSign;
                                if (authSign != null)
                                {
                                    str.AppendLine($"HashAlgotitm: {authSign.HashAlgotitmName}");
                                    str.AppendLine($"SignantureAlgoritm: {authSign.SignantureAlgoritmName}");
                                }
                                result = new Exception(str.ToString(), (Exception) objs[2]);
                                result.Data.Add("authenticationForm", JsonConvert.SerializeObject(auth, Formatting.Indented, _jsonSettings));
                                break;
                        }
                        #endregion
                        break;
                    case 11:
                        #region Task<ICollection<UserForm>> FinedUserAsync(string login)
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                //CreateException(11, 0, 1ex, 2login);
                                str.AppendLine("При попытке поиска пользователя возникла непредвиденная ошибка.");
                                str.Append($"Логин либо его часть: {objs[2]}");
                                result = new Exception(str.ToString(), (Exception)objs[1]);
                                break;
                        }
                        #endregion
                        break;
                    case 12:
                        #region Task<IAsymmetricEncrypter> SetPublicKey()
                        innerNumb = (int)objs[0];
                        switch (innerNumb)
                        {
                            case 0:
                                //CreateException(12, 0, 1result.ErrorMessage);
                                str.AppendLine("Сервер не принял открытый ключ.");
                                str.Append($"Ошибка присланная сервером: {objs[1]}.");
                                result = new InvalidOperationException(str.ToString());
                                break;
                            case 1:
                                //CreateException(12, 1, 1ex);
                                result = new Exception("При отправки открытого ключа возникла непредвиденная ошибка.",
                                    (Exception)objs[1]);
                                break;
                        }
                        #endregion
                        break;
                    default:
                        throw new Exception("Не удалось сгенерировать ошибку.");
                }
                result.Source = GetType().AssemblyQualifiedName;
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось сгенерировать ошибку. Код: {n}.", ex);
            }
            
        }
    }
}
