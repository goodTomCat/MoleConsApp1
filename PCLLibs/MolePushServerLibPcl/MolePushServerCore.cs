using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;

namespace MolePushServerLibPcl
{
    public class MolePushServerCore : MolePushServerCoreBase
    {
        protected UserFormContext UsersF;
        //protected ConcurrentDictionary<string, List<OfflineMessagesConcurent>> OfflineMessagesF =
        //    new ConcurrentDictionary<string, List<OfflineMessagesConcurent>>();
        protected ConcurrentDictionary<string, OfflineMessagesConcurent> OfflineMessagesF =
            new ConcurrentDictionary<string, OfflineMessagesConcurent>();


        public MolePushServerCore(PossibleCryptoInfo possibleCryptoInfo, 
            IList<CryptoFactoryBase> cryptoFactories) : base(possibleCryptoInfo, cryptoFactories)
        { }

        public MolePushServerCore(PossibleCryptoInfo possibleCryptoInfo,
            IList<CryptoFactoryBase> cryptoFactories, UserFormContext bdContext) : base(possibleCryptoInfo, cryptoFactories)
        {
            UsersF = bdContext;
        }


        //public ICollection<UserForm> Users => ;
        public IReadOnlyDictionary<string, OfflineMessagesConcurent> OfflineMessages
            => new ReadOnlyDictionary<string, OfflineMessagesConcurent>(OfflineMessagesF);


        public override Task Initialize()
        {
            //var options = new Options()
            //{
            //    RewriteFileIfExist = true,
            //    FullFilePath = @"C:\Users\jaby\Desktop\Новая папка\MoleTest\usersCollection.mole",
            //    PathToTableFile = @"C:\Users\jaby\Desktop\Новая папка\MoleTest\usersCollectionTable.mole",
            //    SaveRAM = true,
            //    Serializer = new ProtoBufSerializer()
            //};
            //var factory = new Factory<UserForm>(options);
            //UsersF = new CollectionInFile<UserForm>(factory);
            //IsInitialized = true;
            return Task.CompletedTask;
        }
        ///// <exception cref="ArgumentNullException">form == null. -или- ip == null. -или- resultOfOperation == null.</exception>
        ///// <exception cref="ArgumentOutOfRangeException">Строка не является ip адресом.</exception>
        //public override bool RegisterNewUser(UserForm form, IPAddress ip, ResultOfOperation resultOfOperation)
        //{
        //    if (form == null)
        //        throw new ArgumentNullException(nameof(form)) {Source = GetType().AssemblyQualifiedName};
        //    if (form.Login == null)
        //        throw new ArgumentNullException(nameof(form.Login));
        //    if (ip == null)
        //        throw new ArgumentNullException(nameof(ip)) {Source = GetType().AssemblyQualifiedName};
        //    if (resultOfOperation == null)
        //        throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };
        //    //IPAddress address;
        //    //if (!IPAddress.TryParse(ip, out address))
        //    //    throw new ArgumentOutOfRangeException(nameof(ip), "Строка не является ip адресом.")
        //    //{
        //    //    Source = GetType().AssemblyQualifiedName
        //    //};

        //    if (UsersF.Where((info => info.Login == form.Login)).Any())
        //        return CreateResultWithError(0, resultOfOperation, form.Login).OperationWasFinishedSuccessful;

        //    form.Ip = ip;
        //    //if (!form.ValidateBlobFormat(resultOfOperation, false, false))
        //    //    return false;
        //    if (! await Task.Run(() => form.ValidateIpAdressesAndPorts(3, false, resultOfOperation)).ConfigureAwait(false))
        //        return false;
        //    //if (form.DataForSymmetricAlgorithm == null)
        //    //    return CreateResultWithError(1, resultOfOperation).OperationWasFinishedSuccessful;
        //    //else
        //    //{
        //    //    if (form.DataForSymmetricAlgorithm.SymmetricIvBlob == null ||
        //    //        form.DataForSymmetricAlgorithm.SymmetricIvBlob.Length == 0 ||
        //    //        form.DataForSymmetricAlgorithm.SymmetricKeyBlob == null ||
        //    //        form.DataForSymmetricAlgorithm.SymmetricKeyBlob.Length == 0)
        //    //        return CreateResultWithError(2, resultOfOperation).OperationWasFinishedSuccessful;
        //    //}

            
        //    resultOfOperation.OperationWasFinishedSuccessful = true;
        //    UsersF.TryAdd(form);

        //    return true;
        //}
        /// <exception cref="ArgumentNullException">form == null. -или- ip == null. -или- resultOfOperation == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Строка не является ip адресом.</exception>
        public override async Task<bool> RegisterNewUserAsync(UserForm form, IPAddress ip,
            ResultOfOperation resultOfOperation)
        {
            var validateTask = Task.Run(() =>
            {
                if (form == null)
                    throw new ArgumentNullException(nameof(form)) {Source = GetType().AssemblyQualifiedName};
                if (ip == null)
                    throw new ArgumentNullException(nameof(ip)) {Source = GetType().AssemblyQualifiedName};
                if (resultOfOperation == null)
                    throw new ArgumentNullException(nameof(resultOfOperation)) {Source = GetType().AssemblyQualifiedName};

                var isExist = UsersF.Users.Any(surrogate => surrogate.Login == form.Login);
                if (isExist)
                    return CreateResultWithError(0, resultOfOperation, 0, form.Login).OperationWasFinishedSuccessful;
                if (!ValidateUserFormForRegistration(form, resultOfOperation))
                    return false;
                if (!form.ValidateIpAdressesAndPorts(3, false, resultOfOperation))
                    return false;
                return true;
            });

            if (!await validateTask.ConfigureAwait(false))
                return false;
            try
            {
                await UsersF.Users.AddAsync(form).ConfigureAwait(false);
                await UsersF.SaveChangesAsync().ConfigureAwait(false);
                resultOfOperation.OperationWasFinishedSuccessful = true;
                return resultOfOperation.OperationWasFinishedSuccessful;

            }
            //catch (SerializationException ex)
            //    when (Type.GetType(ex.Source).GetTypeInfo().GetInterface("IFileEngine<UserForm>") != null)
            //{
            //    return CreateResultWithError(0, resultOfOperation, 5).OperationWasFinishedSuccessful;
            //}
            catch (SerializationException ex)
                when (
                    Type.GetType(ex.Source)
                        .GetTypeInfo()
                        .ImplementedInterfaces.FirstOrDefault(type => type.FullName.Contains("IFileEngine<UserForm>")) !=
                    null)
            {
                return CreateResultWithError(0, resultOfOperation, 5).OperationWasFinishedSuccessful;
            }
            //catch (IOException ex)
            //    when (Type.GetType(ex.Source).GetTypeInfo().GetInterface("IFileEngine<UserForm>") != null)
            //{
            //    return CreateResultWithError(0, resultOfOperation, 4).OperationWasFinishedSuccessful;
            //}
            catch (IOException ex)
                when (Type.GetType(ex.Source)
                          .GetTypeInfo()
                          .ImplementedInterfaces.FirstOrDefault(type => type.FullName.Contains("IFileEngine<UserForm>")) !=
                      null)
            {
                return CreateResultWithError(0, resultOfOperation, 4).OperationWasFinishedSuccessful;
            }
            catch (Exception ex)
            {
                return CreateResultWithError(0, resultOfOperation, 6).OperationWasFinishedSuccessful;
            }
        }

        //public override UserForm GetPublicKey()
        //{
        //    var formNew = new UserForm();
        //    if (CryptoProvider == CryptoProvider.CngMicrosoft)
        //    {
        //        if (AsymmetricAlgorithm is RSACng)
        //        {
        //            var rsa = (RSACng) AsymmetricAlgorithm;
        //            //var cngKeyBlob = rsa.Key.Export(CngKeyBlobFormat.GenericPublicBlob);
        //            var key = rsa.ExportParameters(false);
        //            var cngKeyBlob = Serializer.Serialize(key, false);
        //            formNew = new UserForm()
        //            {
        //                CryptoProvider = CryptoProvider.CngMicrosoft,
        //                PublicKeyParamsBlob = cngKeyBlob
        //            };
        //        }
        //    }
        //    return formNew;

        //}
        ///// <exception cref="ArgumentNullException">form == null. -или- resultOfOperation == null.</exception>
        //public override UserForm AuthenticateUser(UserForm form, out IEnumerable<byte[]> messages, 
        //    ResultOfOperation resultOfOperation)
        //{
        //    if (form == null)
        //        throw new ArgumentNullException(nameof(form)) { Source = GetType().AssemblyQualifiedName };
        //    if (resultOfOperation == null)
        //        throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };

        //    messages = new byte[0][];
        //    var formWasFinded = UsersF.FirstOrDefault((registrationForm => registrationForm.Login.Equals(form.Login)));
        //    var KeyBlob = formWasFinded.SignPublicKeyBlob;
        //    if (KeyBlob == null)
        //    {
        //        resultOfOperation.ErrorMessage = "Пользователь не зарегистрирован.";
        //        resultOfOperation.OperationWasFinishedSuccessful = false;
        //        return null;
        //    }
        //    form.SignPublicKeyBlob = KeyBlob;

        //    if (!form.ValidateSign(false, resultOfOperation))
        //        return null;

        //    if (OfflineMessagesF.ContainsKey(form.Login))
        //        messages = OfflineMessagesF[form.Login].MessagesAsBytes.ToArray();
            
        //    return form;
        //}
        /// <exception cref="ArgumentNullException">form == null. -или- resultOfOperation == null.</exception>
        public override async Task<Tuple<UserFormSurrogate, OfflineMessagesConcurent>> AuthenticateUserAsync(IAuthenticationForm formAuth, 
            CryptoFactoryBase cryptoFactory, CryptoInfo choosenCrypto, ResultOfOperation resultOfOperation)
        {
            if (formAuth == null)
                throw new ArgumentNullException(nameof(formAuth)) { Source = GetType().AssemblyQualifiedName };
            if (resultOfOperation == null)
                throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };

            //messages = null;
            try
            {
                //var formWasFinded = UsersF.First(registrationForm => registrationForm.Login.Equals(formAuth.Login));
                var formWasFinded =
                    await
                        UsersF.Users.FirstAsync(registrationForm => registrationForm.Login == formAuth.Login)
                            .ConfigureAwait(false);
                switch (formAuth.AuthenticationMethod)
                {
                    case AuthenticationMethod.Classic:
                        var formClassicAuth = formAuth as IAuthenticationFormClassic;
                        if (formClassicAuth == null)
                            throw new InvalidCastException("Преобразование из типа IAuthenticationForm в тип " +
                                                           "IAuthenticationFormClassic завершилось с ошибкой.")
                            {
                                Source = GetType().AssemblyQualifiedName
                            };
                        var hashAlg = cryptoFactory.CreateHashAlgorithm(choosenCrypto.Provider, formClassicAuth.HashAlgotitm);
                        var hashOfTruePass = hashAlg.ComputeHash(Encoding.UTF8.GetBytes(formWasFinded.Password));
                        if (hashOfTruePass.SequenceEqual(formClassicAuth.HashOfPassword))
                            resultOfOperation.OperationWasFinishedSuccessful = true;
                        else
                        {
                            resultOfOperation.ErrorMessage = "Правильность пароля под сомнением?";
                            resultOfOperation.OperationWasFinishedSuccessful = false;
                        }
                        break;
                    case AuthenticationMethod.Sign:
                        var formSignAuth = formAuth as IAuthenticationFormSign;
                        if (formSignAuth == null)
                            throw new InvalidCastException("Преобразование из типа IAuthenticationForm в тип " +
                                                           "IAuthenticationFormSign завершилось с ошибкой.")
                            {
                                Source = GetType().AssemblyQualifiedName
                            };
                        var signAlg = cryptoFactory.CreateSignAlgoritm(choosenCrypto.Provider, formSignAuth.SignantureAlgoritmName);
                        signAlg.Import(formWasFinded.KeyParametrsBlob);
                        if (signAlg.VerifySign(formSignAuth.Hash, formSignAuth.Sign))
                            resultOfOperation.OperationWasFinishedSuccessful = true;
                        else
                        {
                            resultOfOperation.ErrorMessage = "Достоверность цифровой подписи под сомнением?";
                            resultOfOperation.OperationWasFinishedSuccessful = false;
                        }
                        break;
                }
                OfflineMessagesConcurent messages;
                OfflineMessagesF.TryRemove(formWasFinded.Login, out messages);
                if (messages == null)
                    messages = new OfflineMessagesConcurent(formWasFinded.Login);
                return new Tuple<UserFormSurrogate, OfflineMessagesConcurent>(formWasFinded, messages);
            }
            catch (InvalidOperationException ex)
            {
                CreateResultWithError(3, resultOfOperation, formAuth.Login);
                return null;
            }
            
        }
        /// <exception cref="ArgumentNullException">form == null. -или- names == null.</exception>
        public override async Task<ICollection<UserFormSurrogate>> GetUsersPublicData(IEnumerable<string> names, UserForm form)
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names)) { Source = GetType().AssemblyQualifiedName };
            if (form == null)
                throw new ArgumentNullException(nameof(form)) { Source = GetType().AssemblyQualifiedName };

            //var resultSurs =
            //    await UsersF.AccesForms.Where(
            //            accesForm => accesForm.TempUsers.Contains(form.Login) || accesForm.ConstUsers.Contains(form.Login))
            //        .ToArrayAsync().ConfigureAwait(false);
            //var resultForms = resultSurs.Select(sur => ((UserForm) sur.UserForm).GetUserPublicData()).ToArray();
            //var resultSurs = await UsersF.Users.Select(surrogate => surrogate.Login).Join(names, s => s, s => s, (s, s1) => s).
            var resultSursFromDb = await
                UsersF.Users.Include(surrogate => surrogate.Accessibility)
                    .Where(
                        surrogate => names.Contains(surrogate.Login))
                    .ToArrayAsync().ConfigureAwait(false);
            var resultSurs = resultSursFromDb.Where(
                        surrogate =>
                            surrogate.Accessibility.IsPublicProfile ||
                            surrogate.Accessibility.ConstUsers.Contains(form.Login) ||
                            surrogate.Accessibility.TempUsers.Contains(form.Login)).ToArray();
            //var resultSurs =
            //    await
            //        UsersF.Users.Join(names, surrogate => surrogate.Login, s => s, (surrogate, s) => surrogate)
            //            .Where(
            //                surrogate =>
            //                    surrogate.Accessibility.IsPublicProfile ||
            //                    surrogate.Accessibility.ConstUsers.Contains(form.Login) ||
            //                    surrogate.Accessibility.TempUsers.Contains(form.Login))
            //            .ToArrayAsync().ConfigureAwait(false);
            return resultSurs;
        }
        /// <exception cref="ArgumentNullException">form == null. -или- resultOfOperation == null.</exception>
        public override bool UpdateUserData(ref UserForm form, ResultOfOperation resultOfOperation)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form)) { Source = GetType().AssemblyQualifiedName };
            if (resultOfOperation == null)
                throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };

            if (!ValidateUserFormForRegistration(form, resultOfOperation))
                return false;

            try
            {
                var form1 = form;
                var oldForm = UsersF.Users.First(registrationForm => registrationForm.Login.Equals(form1.Login));
                oldForm.Update(form1);
                UsersF.SaveChanges();
                form = form1;
                resultOfOperation.OperationWasFinishedSuccessful = true;
                return true;
            }
            catch (InvalidOperationException)
            {
                resultOfOperation.ErrorMessage = "Пользователь с таким именем не найден.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
        }
        /// <exception cref="ArgumentNullException">strToFined == null.</exception>
        public override async Task<ICollection<UserFormSurrogate>> FinedUserAsync(string strToFined, string senderLogin)
        {
            if (strToFined == null)
                throw new ArgumentNullException(nameof(strToFined)) {Source = GetType().AssemblyQualifiedName};

            var result = await
                UsersF.Users.Include(sur => sur.Accessibility)
                    .Where(
                        surrogate =>
                                surrogate.Login.Contains(strToFined)).Take(7)
                    .ToArrayAsync().ConfigureAwait(false);
            result = result.TakeWhile(surrogate => surrogate.Accessibility.IsPublicProfile ||
                                                   surrogate.Accessibility.ConstUsers.Contains(senderLogin) ||
                                                   surrogate.Accessibility.TempUsers.Contains(senderLogin)).ToArray();
            return result;
        }
        /// <exception cref="ArgumentNullException">formOfSender == null. -или- OfflineMessageForm == null. -или- 
        /// resultOfOperation == null.</exception>
        public override bool SendOfflineMessage(OfflineMessageForm offlineMessage, UserForm formOfSender, 
            ResultOfOperation resultOfOperation)
        {
            if (formOfSender == null)
                throw new ArgumentNullException(nameof(formOfSender)) { Source = GetType().AssemblyQualifiedName };
            if (offlineMessage == null)
                throw new ArgumentNullException(nameof(offlineMessage)) { Source = GetType().AssemblyQualifiedName };
            if (resultOfOperation == null)
                throw new ArgumentNullException(nameof(resultOfOperation)) { Source = GetType().AssemblyQualifiedName };

            try
            {
                var receiver = (UserForm)UsersF.Users.First((form => form.Login == offlineMessage.LoginOfReciever));
                while (true)
                {
                    OfflineMessagesConcurent messages;
                    if (!receiver.ValidateIpAdressesAndPorts(1, false, resultOfOperation))
                    {
                        //OfflineMessagesConcurent messagesConcurentOffline;
                        if (OfflineMessagesF.TryGetValue(receiver.Login, out messages))
                        {
                            if (messages.Count > 50)
                            {
                                resultOfOperation.ErrorMessage = "Кол-во сообщений для офлайн пользователей не может превышать 50.";
                                resultOfOperation.OperationWasFinishedSuccessful = false;
                                return false;
                            }
                            //messagesConcurentOffline =
                            //    messages.Where(
                            //            offlineMessages => offlineMessages.FormOfSender.Login.Equals(formOfSender.Login))
                            //        .ToArray()[0];

                            //if (messagesConcurentOffline == null)
                            //{
                            //    messagesConcurentOffline = new OfflineMessagesConcurent(formOfSender);
                            //    messages.Add(messagesConcurentOffline);
                            //}
                            messages.Add(formOfSender.Login, offlineMessage.Message);
                                
                            //messagesConcurentOffline.Messages.Add(offlineMessage.Message);
                            resultOfOperation.OperationWasFinishedSuccessful = true;
                            return true;
                        }
                        else
                        {
                            while (true)
                            {
                                messages = new OfflineMessagesConcurent(offlineMessage.LoginOfReciever);
                                if (OfflineMessagesF.TryAdd(offlineMessage.LoginOfReciever, messages))
                                {
                                    messages.Add(formOfSender.Login, offlineMessage.Message);
                                    break;
                                }
                                else
                                {
                                    if (OfflineMessagesF.TryGetValue(receiver.Login, out messages))
                                    {
                                        messages.Add(formOfSender.Login, offlineMessage.Message);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        resultOfOperation.ErrorMessage = "Пользователь находится в сети.";
                        resultOfOperation.OperationWasFinishedSuccessful = false;
                        return false;
                    }

                }
            }
            catch (InvalidOperationException)
            {
                resultOfOperation.ErrorMessage = "Пользователь получатель не зарегистрирован.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            
        }


        protected virtual bool ValidateUserFormForRegistration(UserForm form, ResultOfOperation resultOfOperation)
        {
            if (string.IsNullOrEmpty(form.Login))
            {
                resultOfOperation.ErrorMessage = "Поле Login является пустой строкой или равно null.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            if (form.KeyParametrsBlob == null || form.KeyParametrsBlob.Length == 0)
            {
                resultOfOperation.ErrorMessage = "Поле KeyParametrsBlob является пустым массивом байтов или равно null.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            if (string.IsNullOrEmpty(form.Password))
            {
                resultOfOperation.ErrorMessage = "Поле Password является пустой строкой или равно null.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            if (form.PortClientToClient1 < 50)
            {
                resultOfOperation.ErrorMessage = "Значение поля PortClientToClient1 является недопустимым.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            if (form.PortClientToClient2 < 50)
            {
                resultOfOperation.ErrorMessage = "Значение поля PortClientToClient2 является недопустимым.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            if (form.PortClientToClient3 < 50)
            {
                resultOfOperation.ErrorMessage = "Значение поля PortClientToClient3 является недопустимым.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            if (form.PortServerToClient < 50)
            {
                resultOfOperation.ErrorMessage = "Значение поля PortServerToClient является недопустимым.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return false;
            }
            return true;
        }
        private ResultOfOperation CreateResultWithError(uint numb, ResultOfOperation result, params object[] objs)
        {
            result.OperationWasFinishedSuccessful = false;
            var strBuilder = new StringBuilder();
            var innerNumb = 0;
            switch (numb)
            {
                case 0:
                    #region RegisterNewUserAsync(UserForm form, string ip, ResultOfOperation resultOfOperation)
                    innerNumb = (int) objs[0];
                    switch (innerNumb)
                    {
                        case 0:
                            strBuilder.Append($"Пользователь с именем {objs[1]} уже существует.");
                            break;
                        case 1:
                            strBuilder.Append("Информация для осуществления симметричного шифрования не была задана.");
                            break;
                        case 2:
                            strBuilder.Append(
                                "Информация для осуществления симметричного шифрования задана не полностью.");
                            break;
                        case 3:
                            strBuilder.Append(
                                "Не удалось осуществить запись в базу дынных. Возможно стоит повторить запрос.");
                            break;
                        case 4:
                            strBuilder.Append(
                                "Не удалось осуществить запись в базу дынных. Ошибка ввода/вывода.");
                            break;
                        case 5:
                            strBuilder.Append(
                                "Не удалось осуществить запись в базу дынных. Ошибка сериализации.");
                            break;
                        case 6:
                            strBuilder.Append(
                                "Во время регистрации клиента возникла непредвиденная ошибка.");
                            break;
                    }
                    #endregion
                    break;
            }
            result.ErrorMessage = strBuilder.ToString();
            return result;
        }
    }
}
