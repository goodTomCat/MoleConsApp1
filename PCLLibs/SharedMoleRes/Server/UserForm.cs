using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using SharedMoleRes.Client;


//using MoleClientNetCoreLib;

namespace SharedMoleRes.Server
{
    public enum CryptoProvider : byte { CngMicrosoft = 0, BouncyCastle = 1 }

    public class UserForm
    {
        private string _login;
        private string _pass;
        private byte[] _keyParamsBlob;
        private int _portClientToClient1;
        private int _portClientToClient2;
        private int _portClientToClient3;
        private int _portServerToClient;
        private string _ip;
        private IAuthenticationForm _authenticationForm;
        private AccessibilityInfo _accessibility;


        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Длина логина превышает 15 символов или равна нулю.</exception>
        public string Login
        {
            get { return _login; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
                if (value.Length > 15 || value.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Длина логина превышает 15 символов или равна нулю.") {Source = GetType().AssemblyQualifiedName};

                _login = value;
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Значение длины пароля превышает 16 символов или меньше 6 символов.</exception>
        public string Password
        {
            get { return _pass; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
                if (value.Length < 6 || value.Length > 16)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Значение длины пароля превышает 16 символов или меньше 6 символов.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

                _pass = value;
            }
        }
        /// <summary>Открытый ключ для цифровой подписи.</summary>
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Длина массива равна нулю.</exception>
        public byte[] KeyParametrsBlob
        {
            get { return _keyParamsBlob; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
                if (value.Length == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Длина массива равна нулю.") { Source = GetType().AssemblyQualifiedName };

                _keyParamsBlob = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Номер порта равен нулю.</exception>
        public int PortClientToClient1
        {
            get { return _portClientToClient1; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.") { Source = GetType().AssemblyQualifiedName };

                _portClientToClient1 = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Номер порта равен нулю.</exception>
        public int PortClientToClient2
        {
            get { return _portClientToClient2; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.") { Source = GetType().AssemblyQualifiedName };

                _portClientToClient2 = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Номер порта равен нулю.</exception>
        public int PortClientToClient3
        {
            get { return _portClientToClient3; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.") { Source = GetType().AssemblyQualifiedName };

                _portClientToClient3 = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Номер порта равен нулю.</exception>
        public int PortServerToClient
        {
            get { return _portServerToClient; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.") { Source = GetType().AssemblyQualifiedName };

                _portServerToClient = value;
            }
        }
        ///// <summary>Открытый ключ RSA.</summary>
        ///// <exception cref="ArgumentNullException">value == null.</exception>
        ///// <exception cref="ArgumentOutOfRangeException">Длина массива равна нулю.</exception>
        //public byte[] PublicKeyParamsBlob
        //{
        //    get { return _publicKeyParamsBlob; }
        //    set
        //    {
        //        if (value == null)
        //            throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
        //        if (value.Length == 0)
        //            throw new ArgumentOutOfRangeException(nameof(value), "Длина массива равна нулю.") { Source = GetType().AssemblyQualifiedName };

        //        _publicKeyParamsBlob = value;
        //    }
        //}
        ///// <exception cref="ArgumentNullException">value == null.</exception>
        public IPAddress Ip
        {
            get
            {
                if (_ip == null)
                    return null;

                return IPAddress.Parse(_ip);
            }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                _ip = value.ToString();
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Не задано имя хеш алгоритма. -or- 
        /// Не задано имя алгоритма цифровой подписи.</exception>
        public IAuthenticationForm AuthenticationForm
        {
            get { return _authenticationForm; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
                var classicForm = value as IAuthenticationFormClassic;
                if (classicForm != null)
                {
                    if (classicForm.HashAlgotitm == null)
                        throw new ArgumentOutOfRangeException(nameof(value), "Не задано имя хеш алгоритма.")
                        { Source = GetType().AssemblyQualifiedName};
                }
                var signForm = value as IAuthenticationFormSign;
                if (signForm != null)
                {
                    if (signForm.HashAlgotitmName == null)
                        throw new ArgumentOutOfRangeException(nameof(value), "Не задано имя хеш алгоритма.")
                        { Source = GetType().AssemblyQualifiedName };
                    if (signForm.SignantureAlgoritmName == null)
                        throw new ArgumentOutOfRangeException(nameof(value), "Не задано имя алгоритма цифровой подписи.")
                        { Source = GetType().AssemblyQualifiedName };
                }
                _authenticationForm = value;
            }
        }
        /// <exception cref="ArgumentNullException">value == null.</exception>
        public AccessibilityInfo Accessibility
        {
            get { return _accessibility; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                _accessibility = value;
            }
        }


        public static implicit operator UserForm(ContactForm contact)
        {
            if (contact == null)
                return null;
            try
            {
                var userForm = new UserForm();
                if (contact.Login != null)
                    userForm.Login = contact.Login;
                if (contact.PortClientToClient1 != 0)
                    userForm.PortClientToClient1 = contact.PortClientToClient1;
                if (contact.PortClientToClient2 != 0)
                    userForm.PortClientToClient2 = contact.PortClientToClient2;
                if (contact.PortClientToClient3 != 0)
                    userForm.PortClientToClient3 = contact.PortClientToClient3;
                if (contact.PortServerToClient != 0)
                    userForm.PortServerToClient = contact.PortServerToClient;
                if (contact.Ip != null)
                    userForm.Ip = contact.Ip;

                return userForm;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Не удалось привести тип {typeof(ContactForm).FullName} к типу {typeof(UserForm).FullName}.", ex);
            }

        }
        public void Update(UserForm formNew)
        {
            if (formNew == null)
                return;

            if (formNew.Password != null)
                Password = formNew.Password;
            if (formNew.KeyParametrsBlob != null)
                KeyParametrsBlob = formNew.KeyParametrsBlob;
            if (formNew.PortClientToClient1 != 0)
                PortClientToClient1 = formNew.PortClientToClient1;
            if (formNew.PortClientToClient2 != 0)
                PortClientToClient2 = formNew.PortClientToClient2;
            if (formNew.PortClientToClient3 != 0)
                PortClientToClient3 = formNew.PortClientToClient3;
            if (formNew.PortServerToClient != 0)
                PortServerToClient = formNew.PortServerToClient;
            if (formNew.Ip != null)
                Ip = formNew.Ip;
            if (formNew.AuthenticationForm != null)
                AuthenticationForm = formNew.AuthenticationForm;
            if (formNew.Accessibility != null)
                Accessibility = formNew.Accessibility;
        }
        /// <summary>
        /// Возвращает форму с скопированными данными, а именно: Ip, CryptoProvider, SignPublicKeyBlob, PublicKeyParamsBlob, 
        /// Login, PortClientToClient1, PortClientToClient2, PortClientToClient3, PortServerToClient. Если тип ссылочный, 
        /// то будет скопирована ссылка.
        /// </summary>
        public UserForm GetUserPublicData()
        {
            var clone = new UserForm()
            {
                Ip = Ip,
                //CryptoProvider = CryptoProvider,
                KeyParametrsBlob = KeyParametrsBlob,
                //PublicKeyParamsBlob = PublicKeyParamsBlob,
                Login = Login,
                PortClientToClient1 = PortClientToClient1,
                PortClientToClient2 = PortClientToClient2,
                PortClientToClient3 = PortClientToClient3,
                PortServerToClient = PortServerToClient
            };
            return clone;
        }
        /// <exception cref="PlatformNotSupportedException">Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.</exception>
        /// <exception cref="CryptographicException">Все остальные ошибки.</exception>
        /// <exception cref="InvalidOperationException">Свойство <see cref="KeyParametrsBlob"/> не задано.</exception>
        public CngKey CreateCngKey()
        {
            if (KeyParametrsBlob == null)
                throw new InvalidOperationException("Свойство SignPublicKeyBlob не задано.") { Source = GetType().AssemblyQualifiedName };

            try
            {
                return CngKey.Import(KeyParametrsBlob, CngKeyBlobFormat.GenericPublicBlob);
            }
            catch (PlatformNotSupportedException ex)
            {
                throw new PlatformNotSupportedException(ex.Message, ex) {Source = GetType().AssemblyQualifiedName};
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException(ex.Message, ex) { Source = GetType().AssemblyQualifiedName };
            }
            
        }
        /////// <exception cref="ArgumentException">Одно из свойств: <see cref="Login"/> или <see cref="Password"/> или <see cref="SignPublicKeyBlob"/>, не задано. ИЛИ 
        /////// При проверке свойства <see cref="SignPublicKeyBlob"/>, свойство <see cref="CngKey.AlgorithmGroup"/> не равно <see cref="CngAlgorithmGroup.ECDsa"/>. ИЛИ 
        /////// Одно из свойств: <see cref="Ip"/> или <see cref="PortClientToClient1"/> или <see cref="PortClientToClient2"/> или <see cref="PortClientToClient3"/>, не задано. ИЛИ 
        /////// Одно из свойств: <see cref="Ip"/> или <see cref="PortServerToClient"/>, не задано. ИЛИ 
        /////// При проверке свойства <see cref="PublicKeyParamsBlob"/>, свойство <see cref="CngKey.AlgorithmGroup"/> не равно <see cref="CngAlgorithmGroup.ECDsa"/>.</exception>
        /////// <exception cref="PlatformNotSupportedException">Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.</exception>
        /////// <exception cref="CryptographicException">Все остальные ошибки.</exception>
        /////// <exception cref="SocketException">An error occurred when accessing the socket.See the Remarks section for more information.</exception>
        ////public bool Validate(bool throwExc, ushort numbOfClientToClientPortCheck, bool checkPublicKey, 
        ////    ResultOfOperation resultOfOperation, bool checkSign)
        ////{
        ////    resultOfOperation.ErrorCode = -1;
        ////    if (Login == null || Password == null || SignPublicKeyBlob == null)
        ////    {
        ////        if (throwExc)
        ////            throw new ArgumentException("Одно из свойств: Login или Password или SignPublicKeyBlob, не задано.")
        ////            {
        ////                Source = GetType().AssemblyQualifiedName
        ////            };

        ////        resultOfOperation.ErrorMessage =
        ////                "Одно из полей: Login или Password или SignPublicKeyBlob, не задано.";
        ////        resultOfOperation.OperationWasFinishedSuccessful = false;
        ////        return false;
        ////    }

        ////    if (!CheckKeyBlobFormat(CryptoProvider, SignPublicKeyBlob, resultOfOperation, throwExc, true))
        ////        return false;
            

        ////    SocketException ex;
        ////    switch (numbOfClientToClientPortCheck)
        ////    {
        ////        case 0:
        ////            break;
        ////        case 1:
        ////            if (!CheckIpAdressesAndPorts(new[] {Ip}, new[] {PortClientToClient1}, false, resultOfOperation, out ex))
        ////                return false;
        ////            break;
        ////        case 2:
        ////            if (!CheckIpAdressesAndPorts(new[] { Ip, Ip }, 
        ////                new[] { PortClientToClient1, PortClientToClient2 }, false, resultOfOperation, out ex))
        ////                return false;
        ////            break;
        ////        case 3:
        ////            if (!CheckIpAdressesAndPorts(new[] { Ip, Ip, Ip }, 
        ////                new[] { PortClientToClient1, PortClientToClient2, PortClientToClient3 }, false, resultOfOperation, out ex))
        ////                return false;
        ////            break;
        ////    }
            
        ////    if (CheckIpAdressesAndPorts(new[] { Ip }, new[] { PortServerToClient }, throwExc, resultOfOperation, out ex))
        ////        return false;

        ////    if (checkPublicKey)
        ////    {
        ////        if (CheckKeyBlobFormat(CryptoProvider, PublicKeyParamsBlob, resultOfOperation, throwExc, false))
        ////            return false;
        ////    }

        ////    if (checkSign)
        ////    {
        ////        if (!CheckSign(CryptoProvider, SignPublicKeyBlob, Login, SignantureBlob, throwExc, resultOfOperation))
        ////            return false;
        ////    }

        ////    return true;
        ////}
        ///// <exception cref="PlatformNotSupportedException">Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.</exception>
        ///// <exception cref="CryptographicException">В параметре parameters заданы не все поля. -или- 
        ///// Все остальные ошибки.</exception>
        ///// <exception cref="SerializationException">Во время десиарелизации объекта произошла ошибка. Source: <see cref="CustomBinarySerializerBase.Deserialize{T}(byte[], bool)"/></exception>
        ///// <exception cref="InvalidOperationException">Данный криптопровайдер не поддерживается.</exception>
        //public bool ValidateBlobFormat(ResultOfOperation result, bool throwExc, bool isSign)
        //{
        //    if (CryptoProvider == CryptoProvider.CngMicrosoft)
        //    {
        //        try
        //        {
        //            if (isSign)
        //            {
        //                if (KeyParametrsBlob == null)
        //                {
        //                    if (throwExc)
        //                        throw new ArgumentNullException(nameof(KeyParametrsBlob)) {Source = GetType().AssemblyQualifiedName};

        //                    result.ErrorMessage = "Поле SignPublicKeyBlob не задано.";
        //                    result.OperationWasFinishedSuccessful = false;
        //                    return false;
        //                }

        //                CngKey.Import(KeyParametrsBlob, CngKeyBlobFormat.GenericPublicBlob);
        //            }
        //            else
        //            {
        //                if (PublicKeyParamsBlob == null)
        //                {
        //                    if (throwExc)
        //                        throw new ArgumentNullException(nameof(PublicKeyParamsBlob)) { Source = GetType().AssemblyQualifiedName };

        //                    result.ErrorMessage = "Поле PublicKeyParamsBlob не задано.";
        //                    result.OperationWasFinishedSuccessful = false;
        //                    return false;
        //                }

        //                var ser = new ProtoBufSerializer();
        //                var key = CngKey.Import(PublicKeyParamsBlob, CngKeyBlobFormat.GenericPublicBlob);
        //                var rsaCng = new RSACng(key);
        //            }
        //            return true;
        //        }
        //        catch (PlatformNotSupportedException ex)
        //        {
        //            if (throwExc)
        //                throw new PlatformNotSupportedException(ex.Message, ex) { Source = GetType().AssemblyQualifiedName };

        //            result.ErrorMessage =
        //                "Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.";
        //            result.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }
        //        catch (CryptographicException ex)
        //        {
        //            if (throwExc)
        //                throw new CryptographicException(ex.Message, ex) { Source = GetType().AssemblyQualifiedName };

        //            result.ErrorMessage = isSign
        //                ? "Ключ цифровой подписи имеет не верный формат."
        //                : "Открытый ключ шифрования RSA имеет не верный формат.";
        //            result.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }
        //    }
        //    if (throwExc)
        //        throw new InvalidOperationException(
        //            $"Данный криптопровайдер {Enum.GetName(typeof (CryptoProvider), CryptoProvider)} не поддерживается.");

        //    result.ErrorMessage = $"Данный криптопровайдер {Enum.GetName(typeof(CryptoProvider), CryptoProvider)} не поддерживается.";
        //    result.OperationWasFinishedSuccessful = false;
        //    return false;
        //}
        ///// <exception cref="PlatformNotSupportedException">Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.</exception>
        ///// <exception cref="CryptographicException">В параметре parameters заданы не все поля. -или- 
        ///// Все остальные ошибки.</exception>
        ///// <exception cref="SerializationException">Во время десиарелизации объекта произошла ошибка. Source: <see cref="CustomBinarySerializerBase.Deserialize{T}(byte[], bool)"/></exception>
        ///// <exception cref="ArgumentNullException"><see cref="KeyParametrsBlob"/> == null. -или- 
        ///// <see cref="Login"/> == null. -или- <see cref="SignantureBlob"/> == null.</exception>
        ///// /// <exception cref="InvalidOperationException">Данный криптопровайдер не поддерживается.</exception>
        //public bool ValidateSign(bool throwExc, ResultOfOperation resultOfOperation)
        //{
        //    if (CryptoProvider == CryptoProvider.CngMicrosoft)
        //    {
        //        if (KeyParametrsBlob == null)
        //        {
        //            if (throwExc)
        //                throw new ArgumentNullException(nameof(KeyParametrsBlob)) { Source = GetType().AssemblyQualifiedName };

        //            resultOfOperation.ErrorMessage = "Поле SignPublicKeyBlob не задано.";
        //            resultOfOperation.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }

        //        if (Login == null)
        //        {
        //            if (throwExc)
        //                throw new ArgumentNullException(nameof(Login)) { Source = GetType().AssemblyQualifiedName };

        //            resultOfOperation.ErrorMessage = "Поле Login не задано.";
        //            resultOfOperation.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }

        //        if (SignantureBlob == null)
        //        {
        //            if (throwExc)
        //                throw new ArgumentNullException(nameof(SignantureBlob)) { Source = GetType().AssemblyQualifiedName };

        //            resultOfOperation.ErrorMessage = "Поле SignantureBlob не задано.";
        //            resultOfOperation.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }

        //        try
        //        {
        //            var cngKey = CngKey.Import(KeyParametrsBlob, CngKeyBlobFormat.EccPublicBlob);
        //            var ec = new ECDsaCng(cngKey);
        //            var ser = new ProtoBufSerializer();
        //            var signData = ser.Serialize(Login, false);
        //            if (!ec.VerifyData(signData, SignantureBlob, HashAlgorithmName.SHA1))
        //            {
        //                resultOfOperation.ErrorMessage = "Аутентификация не удалась.";
        //                resultOfOperation.OperationWasFinishedSuccessful = false;
        //                return false;
        //            }
        //            return true;
        //        }
        //        catch (PlatformNotSupportedException ex)
        //        {
        //            if (throwExc)
        //                throw new PlatformNotSupportedException(ex.Message, ex) { Source = GetType().AssemblyQualifiedName };

        //            resultOfOperation.ErrorMessage =
        //                "Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.";
        //            resultOfOperation.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }
        //        catch (CryptographicException ex)
        //        {
        //            if (throwExc)
        //                throw new CryptographicException(ex.Message, ex) { Source = GetType().AssemblyQualifiedName };

        //            resultOfOperation.ErrorMessage = "Ключ цифровой подписи имеет не верный формат.";
        //            resultOfOperation.OperationWasFinishedSuccessful = false;
        //            return false;
        //        }

        //    }

        //    if (throwExc)
        //        throw new InvalidOperationException($"Данный криптопровайдер {Enum.GetName(typeof(CryptoProvider), CryptoProvider)} не поддерживается.");

        //    resultOfOperation.ErrorMessage = $"Данный криптопровайдер {Enum.GetName(typeof(CryptoProvider), CryptoProvider)} не поддерживается.";
        //    resultOfOperation.OperationWasFinishedSuccessful = false;
        //    return false;
        //}
        /// <exception cref="ArgumentOutOfRangeException">Значение numbOfPortsForValidation не попадает в диапазон от 1 до 3.</exception>
        /// <exception cref="ArgumentNullException">resultOfOperation == null. -или- 
        /// Ip == null.</exception>
        /// <exception cref="ArgumentException">Не задан какой-либо из портов.</exception>
        /// <exception cref="SocketException">Ошибка на сокете.</exception>
        public bool ValidateIpAdressesAndPorts(ushort numbOfPortsForValidation, bool throwExc, 
            ResultOfOperation resultOfOperation)
        {
            if (numbOfPortsForValidation > 3)
                throw new ArgumentOutOfRangeException(nameof(numbOfPortsForValidation),
                    "Значение не попадает в диапазон от 1 до 3.") {Source = GetType().AssemblyQualifiedName};
            if (resultOfOperation == null)
                throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };

            if (Ip == null)
            {
                if (throwExc)
                    throw new ArgumentNullException(nameof(Ip)) {Source = GetType().AssemblyQualifiedName};

                resultOfOperation.ErrorMessage = "Не задан ip адрес.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }

            var ports = new int[0];
            switch (numbOfPortsForValidation)
            {
                case 1:
                    ports = new[] {PortClientToClient1};
                    break;
                case 2:
                    ports = new[] { PortClientToClient1, PortClientToClient2 };
                    break;
                case 3:
                    ports = new[] {PortClientToClient1, PortClientToClient2, PortClientToClient3};
                    break;
            }

            ushort i = 0;
            try
            {
                for (i = 0; i < numbOfPortsForValidation; i++)
                {
                    if (ports[i] == 0)
                    {
                        if (throwExc)
                            throw new ArgumentException("Не задан какой-либо из портов.") {Source = GetType().AssemblyQualifiedName};

                        resultOfOperation.ErrorMessage = "Не задан какой-либо из портов.";
                        resultOfOperation.OperationWasFinishedSuccessful = false;
                        return false;
                    }
                    var endPoint = new IPEndPoint(Ip, ports[i]);
                    using (var client = new TcpClient())
                    {
                        var connectTask = client.ConnectAsync(Ip, ports[i]);
                        connectTask.Wait();
                    }
                }

                return true;
            }
            catch (AggregateException ex) when (ex.InnerException is SocketException)
            {
                if (throwExc)
                    throw CreateException(0, i, ex);

                resultOfOperation.ErrorMessage = $"Порт {i} не отвечает.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            catch (SocketException ex)
            {
                if (throwExc)
                    throw CreateException(0, i, ex);

                resultOfOperation.ErrorMessage = $"Порт {i} не отвечает.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            
        }
        ///// <exception cref="ArgumentOutOfRangeException">Значение numbOfPortsForValidation не попадает в диапазон от 1 до 3.</exception>
        ///// <exception cref="ArgumentNullException">resultOfOperation == null. -или- 
        ///// Ip == null.</exception>
        ///// <exception cref="ArgumentException">Не задан какой-либо из портов.</exception>
        ///// <exception cref="SocketException">Ошибка на сокете.</exception>
        //public async Task<bool> ValidateIpAdressesAndPortsAsync(ushort numbOfPortsForValidation, bool throwExc,
        //    ResultOfOperation resultOfOperation)
        //{
        //    if (numbOfPortsForValidation > 3)
        //        throw new ArgumentOutOfRangeException(nameof(numbOfPortsForValidation),
        //            "Значение не попадает в диапазон от 1 до 3.")
        //        { Source = GetType().AssemblyQualifiedName };
        //    if (resultOfOperation == null)
        //        throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };

        //    if (Ip == null)
        //    {
        //        if (throwExc)
        //            throw new ArgumentNullException(nameof(Ip)) { Source = GetType().AssemblyQualifiedName };

        //        resultOfOperation.ErrorMessage = "Не задан ip адрес.";
        //        resultOfOperation.OperationWasFinishedSuccessful = false;
        //        return false;
        //    }

        //    var ports = new ushort[0];
        //    switch (numbOfPortsForValidation)
        //    {
        //        case 1:
        //            ports = new[] { PortClientToClient1 };
        //            break;
        //        case 2:
        //            ports = new[] { PortClientToClient1, PortClientToClient2 };
        //            break;
        //        case 3:
        //            ports = new[] { PortClientToClient1, PortClientToClient2, PortClientToClient3 };
        //            break;
        //    }

        //    ushort i = 0;
        //    try
        //    {
        //        var connectTasks = new Task[numbOfPortsForValidation];
        //        for (i = 0; i < numbOfPortsForValidation; i++)
        //        {
        //            if (ports[i] == 0)
        //            {
        //                if (throwExc)
        //                    throw new ArgumentException("Не задан какой-либо из портов.") { Source = GetType().AssemblyQualifiedName };

        //                resultOfOperation.ErrorMessage = "Не задан какой-либо из портов.";
        //                resultOfOperation.OperationWasFinishedSuccessful = false;
        //                return false;
        //            }
        //            var client = new TcpClient();
        //            await client.ConnectAsync(Ip, ports[i]);

        //        }
        //        return true;
        //    }
        //    catch (AggregateException ex) when (ex.InnerException is SocketException)
        //    {
        //        if (throwExc)
        //            throw CreateException(0, i, ex);

        //        resultOfOperation.ErrorMessage = $"Порт {i} не отвечает.";
        //        resultOfOperation.OperationWasFinishedSuccessful = false;
        //        return false;
        //    }
        //    catch (SocketException ex)
        //    {
        //        if (throwExc)
        //            throw CreateException(0, i, ex);

        //        resultOfOperation.ErrorMessage = $"Порт {i} не отвечает.";
        //        resultOfOperation.OperationWasFinishedSuccessful = false;
        //        return false;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}


        private Exception CreateException(int numb, params object[] objs)
        {
            //var strBuilder = new StringBuilder();
            Exception result = null;
            switch (numb)
            {
                case 0:
                    var innerSocketExcept = (SocketException) objs[1];
                    result = new SocketException((int)innerSocketExcept.SocketErrorCode) {Source = GetType().AssemblyQualifiedName};
                    var portNumb = (ushort)objs[0];
                    result.Data.Add("port number", portNumb);
                    break;
            }
            return result;
        }
        
    }
}
