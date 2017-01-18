using System;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using SharedMoleRes.Server;

namespace SharedMoleRes.Client
{
    public class ContactForm
    {
        private string _login;
        //private string _pass;
        //private byte[] _signPublicKeyParamsBlob;
        private int _portClientToClient1;
        private int _portClientToClient2;
        private int _portClientToClient3;
        private int _portServerToClient;
        //private byte[] _publicKeyParamsBlob;
        private string _ip;
        //private byte[] _signBlob;
        //private KeyDataForSymmetricAlgorithm _keyDataForSymmetricAlgorithm;
        private SignForm _signForm;
        private UserInfo _userInfo;


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
        ///// <exception cref="ArgumentNullException">value == null.</exception>
        ///// <exception cref="ArgumentOutOfRangeException">Значение длины пароля превышает 16 символов или меньше 6 символов.</exception>
        //public string Password
        //{
        //    get { return _pass; }
        //    set
        //    {
        //        if (value == null)
        //            throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
        //        if (value.Length < 6 || value.Length > 16)
        //            throw new ArgumentOutOfRangeException(nameof(value),
        //                "Значение длины пароля превышает 16 символов или меньше 6 символов.")
        //            {
        //                Source = GetType().AssemblyQualifiedName
        //            };

        //        _pass = value;
        //    }
        //}
        //public CryptoProvider CryptoProvider { get; set; }
        ///// <summary>Открытый ключ для цифровой подписи.</summary>
        ///// <exception cref="ArgumentNullException">value == null.</exception>
        ///// <exception cref="ArgumentOutOfRangeException">Длина массива равна нулю.</exception>
        //public byte[] SignPublicKeyBlob
        //{
        //    get { return _signPublicKeyParamsBlob; }
        //    set
        //    {
        //        if (value == null)
        //            throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
        //        if (value.Length == 0)
        //            throw new ArgumentOutOfRangeException(nameof(value), "Длина массива равна нулю.")
        //            {
        //                Source = GetType().AssemblyQualifiedName
        //            };

        //        _signPublicKeyParamsBlob = value;
        //    }
        //}
        ///// <exception cref="ArgumentNullException">value == null.</exception>
        public SignForm SignForm
        {
            get
            {
                return _signForm;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                _signForm = value;
            }
        }
        /// <exception cref="ArgumentOutOfRangeException">Номер порта равен нулю.</exception>
        public int PortClientToClient1
        {
            get { return _portClientToClient1; }
            set
            {
                if (value == 0)
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

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
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

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
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

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
                    throw new ArgumentOutOfRangeException(nameof(value), "Номер парта равен нулю.")
                    {
                        Source = GetType().AssemblyQualifiedName
                    };

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
        //            throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
        //        if (value.Length == 0)
        //            throw new ArgumentOutOfRangeException(nameof(value), "Длина массива равна нулю.")
        //            {
        //                Source = GetType().AssemblyQualifiedName
        //            };

        //        _publicKeyParamsBlob = value;
        //    }
        //}
        /// <exception cref="ArgumentNullException">value == null.</exception>
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
        public UserInfo SecondaryUserInfo
        {
            get { return _userInfo; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};

                _userInfo = value;
            }
        }
        ///// <exception cref="ArgumentNullException">value == null.</exception>
        ///// <exception cref="ArgumentOutOfRangeException">Длина массива равна нулю.</exception>
        //public byte[] SignantureBlob
        //{
        //    get { return _signBlob; }
        //    set
        //    {
        //        if (value == null)
        //            throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
        //        if (value.Length == 0)
        //            throw new ArgumentOutOfRangeException(nameof(value), "Длина массива равна нулю.")
        //            {
        //                Source = GetType().AssemblyQualifiedName
        //            };

        //        _signBlob = value;
        //    }
        //}
        ///// <exception cref="ArgumentNullException">DataForSymmetricAlgorithm == null. -или- 
        ///// DataForSymmetricAlgorithm.SymmetricKeyBlob == null. -или- 
        ///// DataForSymmetricAlgorithm.SymmetricIvBlob == null.</exception>
        //public KeyDataForSymmetricAlgorithm DataForSymmetricAlgorithm
        //{
        //    get { return _keyDataForSymmetricAlgorithm; }
        //    set
        //    {
        //        if (value == null)
        //            throw new ArgumentNullException(nameof(value)) {Source = GetType().AssemblyQualifiedName};
        //        if (value.SymmetricKeyBlob == null)
        //            throw new ArgumentNullException(nameof(value.SymmetricKeyBlob)) {Source = GetType().AssemblyQualifiedName};
        //        if (value.SymmetricIvBlob == null)
        //            throw new ArgumentNullException(nameof(value.SymmetricIvBlob)) {Source = GetType().AssemblyQualifiedName};

        //        _keyDataForSymmetricAlgorithm = value;
        //    }
        //}


        public static implicit operator ContactForm(UserForm userForm)
        {
            if (userForm == null)
                return null;
            try
            {
                var contact = new ContactForm();
                contact._login = userForm.Login;
                contact._portClientToClient1 = userForm.PortClientToClient1;
                contact._portClientToClient2 = userForm.PortClientToClient2;
                contact._portClientToClient3 = userForm.PortClientToClient3;
                contact._portServerToClient = userForm.PortServerToClient;
                contact._ip = userForm.Ip?.ToString();
                return contact;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException(
                    $"Не удалось привести тип {typeof(UserForm).FullName} к типу {typeof(ContactForm).FullName}.", ex);
            }

        }
        
        /// <summary>
        /// Обновляет текущую форму заданной. Если свойство заданной формы не равно null, оно будет присвоено свойству текущей формы.
        /// </summary>
        /// <exception cref="ArgumentNullException">formNew == null</exception>
        public void Update(ContactForm formNew)
        {
            if (formNew == null)
                throw new ArgumentNullException(nameof(formNew)) {Source = GetType().AssemblyQualifiedName};

            if (formNew.Login != null)
                Login = formNew.Login;
            if (formNew.SignForm != null)
                SignForm = formNew.SignForm;
            PortClientToClient1 = formNew.PortClientToClient1;
            PortClientToClient2 = formNew.PortClientToClient2;
            PortClientToClient3 = formNew.PortClientToClient3;
            PortServerToClient = formNew.PortServerToClient;
            if (formNew.Ip != null)
                Ip = formNew.Ip;
            if (SecondaryUserInfo != null)
                SecondaryUserInfo = formNew.SecondaryUserInfo;
        }
        ///// <summary>
        ///// Возвращает форму с скопированными данными, а именно: Ip, CryptoProvider, SignPublicKeyBlob, PublicKeyParamsBlob, 
        ///// Login, PortClientToClient1, PortClientToClient2, PortClientToClient3, PortServerToClient. Если тип ссылочный, 
        ///// то будет скопирована ссылка.
        ///// </summary>
        //public ContactForm GetUserPublicData()
        //{
        //    var clone = new ContactForm()
        //    {
        //        Ip = Ip,
        //        CryptoProvider = CryptoProvider,
        //        SignPublicKeyBlob = SignPublicKeyBlob,
        //        PublicKeyParamsBlob = PublicKeyParamsBlob,
        //        Login = Login,
        //        PortClientToClient1 = PortClientToClient1,
        //        PortClientToClient2 = PortClientToClient2,
        //        PortClientToClient3 = PortClientToClient3,
        //        PortServerToClient = PortServerToClient
        //    };
        //    return clone;
        //}
        /// <exception cref="PlatformNotSupportedException">Классы криптографии следующего поколения (CNG) не поддерживаются данной системой.</exception>
        /// <exception cref="CryptographicException">Все остальные ошибки.</exception>
        /// <exception cref="InvalidOperationException">Свойство <see cref="SignPublicKeyBlob"/> не задано.</exception>
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
        /// <exception cref="ArgumentOutOfRangeException">Значение numbOfPortsForValidation не попадает в диапазон от 1 до 3.</exception>
        /// <exception cref="ArgumentNullException">resultOfOperation == null. -или- 
        /// Ip == null.</exception>
        /// <exception cref="ArgumentException">Не задан какой-либо из портов.</exception>
        /// <exception cref="SocketException">Ошибка на сокете.</exception>
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
        //                    throw new ArgumentException("Не задан какой-либо из портов.") {Source = GetType().AssemblyQualifiedName};

        //                resultOfOperation.ErrorMessage = "Не задан какой-либо из портов.";
        //                resultOfOperation.OperationWasFinishedSuccessful = false;
        //                return false;
        //            }
        //            var client = new TcpClient();
        //            await client.ConnectAsync(Ip, ports[i]);
        //            client.Close();
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
