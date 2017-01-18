using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JabyLib.Other;
using MoleClientLib.RemoteFileStream;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;

namespace MoleClientLib
{

    public class MoleClientCore : MoleClientCoreBase
    {
        //protected CollectionInFile<ContactForm> ContactsF;
        protected IList<ContactForm> ContactsF;
        public event Func<object, RegisterNewContactEventArgs, Task> RegisterNewContactEvent;
        public event Func<object, RecieveOfFileTransferRequestEventArgs, Task> RecieveOfFileTransferRequestEvent;
        public event Func<object, TextMessageRecievedEventArgs, Task> TextMessageRecievedEvent;
        public event Func<object, FileRecievingPreparedEventArgs, Task> FileRecievingPreparedEvent;
        protected ConcurrentBag<RemoteFileStreamMole> PossFilesForTransfer;
        //protected string DirForFileSaving;
        protected ConcurrentDictionary<string, MoleClientSender> Senders =
            new ConcurrentDictionary<string, MoleClientSender>();

        
        //protected ConcurrentDictionary<string, StreamForTempSaving> FilesSending;


        public MoleClientCore(CustomBinarySerializerBase serializer, string dirForFileSaving, 
            ICollection<CryptoFactoryBase> factoriesBase, UserForm myUserForm) : base(serializer, factoriesBase, myUserForm)
        {
            if (dirForFileSaving == null)
                throw new ArgumentNullException(nameof(dirForFileSaving)) {Source = GetType().AssemblyQualifiedName};
            if (string.IsNullOrEmpty(dirForFileSaving))
                throw new ArgumentException("Value cannot be null or empty.", nameof(dirForFileSaving))
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (string.IsNullOrWhiteSpace(dirForFileSaving))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(dirForFileSaving))
                {
                    Source = GetType().AssemblyQualifiedName
                };
            if (!Directory.Exists(dirForFileSaving))
                throw new ArgumentOutOfRangeException(nameof(dirForFileSaving), "Указанной директории не существует.")
                {
                    Source = GetType().AssemblyQualifiedName
                };


            ContactsF = new List<ContactForm>();
            //var options = new Options()
            //{
            //    RewriteFileIfExist = true,
            //    FullFilePath = dirForFileSaving + @"\contactsDb.mole",
            //    PathToTableFile = dirForFileSaving + @"\contactsDbTable.mole",
            //    SaveRAM = true,
            //    Serializer = new ProtoBufSerializer()
            //};
            //var factory = new Factory<ContactForm>(options);
            //ContactsF = new CollectionInFile<ContactForm>(factory);
            DirForFileSaving = dirForFileSaving;
        }
        public MoleClientCore(CustomBinarySerializerBase serializer, ICollection<CryptoFactoryBase> factoriesBase,
            UserForm myUserForm, IEnumerable<ContactForm> contactsF)
            : base(serializer, factoriesBase, myUserForm)
        {
            if (contactsF == null) throw new ArgumentNullException(nameof(contactsF))
            { Source = GetType().AssemblyQualifiedName };

            ContactsF = contactsF.ToList();
        }


        public IReadOnlyCollection<ContactForm> Contacts => new ReadOnlyCollection<ContactForm>(ContactsF);
        public string DirForFileSaving { get; protected set; }


        public override Task TextMessageRecieved(string login, string message, ResultOfOperation result,
            ContactForm form = null,
            bool userIsAuth = true)
        {
            try
            {
                if (!userIsAuth)
                {
                    result.OperationWasFinishedSuccessful = false;
                    result.ErrorMessage = "Аутентификация не была проведена.";
                    return Task.CompletedTask;
                }
                if (form == null)
                    form = ContactsF.First(userForm => userForm.Login.Equals(login));

                var args = new TextMessageRecievedEventArgs() {Message = message, Contact = form};
                result.OperationWasFinishedSuccessful = true;
                return OnTextMessageRecieved(args);
            }
            catch (InvalidOperationException ex)
            {
                result.OperationWasFinishedSuccessful = false;
                result.ErrorMessage = "Пользователь не зарегистрирован.";
                return Task.CompletedTask;
            }
        }
        public override Tuple<bool, ContactForm> AuthenticateContacnt(string login, IPEndPoint endPoint,
            IEnumerable<ContactForm> publicForms,
            ResultOfOperation resultOfOperation)
        {
            var neededUsers = publicForms.Where(form => form.Login.Equals(login)).ToArray();
            if (neededUsers.Length != 1)
            {
                resultOfOperation.ErrorMessage = "Пользователь с таким именем не зарегистрирован.";
                resultOfOperation.OperationWasFinishedSuccessful = true;
                return new Tuple<bool, ContactForm>(resultOfOperation.OperationWasFinishedSuccessful, null);
            }

            var neededUser = neededUsers[0];
            if (!endPoint.Address.Equals(neededUser.Ip))
            {
                resultOfOperation.ErrorMessage = "Аутентификация не удалась.";
                resultOfOperation.OperationWasFinishedSuccessful = false;
                return new Tuple<bool, ContactForm>(resultOfOperation.OperationWasFinishedSuccessful, null);
            }

            resultOfOperation.OperationWasFinishedSuccessful = true;
            return new Tuple<bool, ContactForm>(resultOfOperation.OperationWasFinishedSuccessful, neededUser);
        }
        public override async Task<Tuple<bool, ContactForm>> RegisterNewContactAsync(string login, IPEndPoint endPoint,
            IEnumerable<ContactForm> formsPublic,
            ResultOfOperation resultOfOperation)
        {
            try
            {
                var forms = formsPublic as ContactForm[] ?? formsPublic.ToArray();
                Tuple<bool, ContactForm> authResult = AuthenticateContacnt(login, endPoint, forms, resultOfOperation);
                if (authResult.Item1)
                {
                    var form = forms.First(userForm => userForm.Login.Equals(login));
                    var args = new RegisterNewContactEventArgs {Ip = endPoint.Address, Login = login};
                    await OnRegisterNewContact(args).ConfigureAwait(false);
                    if (args.IsAllow)
                        ContactsF.Add(form);
                    resultOfOperation.OperationWasFinishedSuccessful = true;
                }
                return authResult;
            }
            catch (InvalidOperationException ex)
            {
                resultOfOperation.OperationWasFinishedSuccessful = false;
                resultOfOperation.ErrorMessage = "Пользователь не зарегистрирован на внешнем сервере.";
                return new Tuple<bool, ContactForm>(false, null);
            }

        }
        public override async Task<bool> RegisterNewContactAsync(ContactForm form, IPEndPoint endPoint,
            ResultOfOperation resultOfOperation)
        {
            try
            {
                var args = new RegisterNewContactEventArgs { Ip = endPoint.Address, Login = form.Login };
                await OnRegisterNewContact(args).ConfigureAwait(false);
                if (args.IsAllow)
                {
                    ContactsF.Add(form);
                    resultOfOperation.OperationWasFinishedSuccessful = true;
                }
                else
                {
                    resultOfOperation.OperationWasFinishedSuccessful = false;
                    resultOfOperation.ErrorMessage = "Пользователь отверг запрос на авторизацию.";
                }
                return resultOfOperation.OperationWasFinishedSuccessful;
            }
            catch (Exception ex)
            {
                
                throw;
            }
            
        }
        public override async Task<bool> RecieveOfFileTransferRequest(FileRecieveRequest request, IPEndPoint endPoint, ContactForm contact, 
            ResultOfOperation result)
        {
            var args = new RecieveOfFileTransferRequestEventArgs()
            {
                FileName = request.Name,
                Login = contact.Login,
                Ip = endPoint.Address,
                Length = request.Length
            };
            await OnFileReciveBegining(args).ConfigureAwait(false);
            var remoteFile = args.RemoteFileStream;
            if (args.IsAllow)
            {
                if (remoteFile == null)
                {
                    var dirFull = DirForFileSaving + @"\" + request.Name;
                    var stream = new FileStream(dirFull, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite);
                    await Task.Run(() => stream.SetLength(request.Length)).ConfigureAwait(false);
                    MoleClientSender sender;
                    if (!Senders.TryGetValue(contact.Login, out sender))
                    {
                        sender = new MoleClientSender(contact, PossibleAlgs, this, true);
                        Senders.TryAdd(contact.Login, sender);
                    }
                    await sender.Inicialize(endPoint).ConfigureAwait(false);
                    await sender.GetSessionKey().ConfigureAwait(false);
                    await sender.AuthenticateAsync().ConfigureAwait(false);
                    
                    remoteFile = new RemoteFileStreamMole(sender, stream, request.Name);
                    await remoteFile.InitializeAsync().ConfigureAwait(false);
                }
                
                await
                    OnFileRecievingPrepared(new FileRecievingPreparedEventArgs(request.Name, request.Length, remoteFile))
                        .ConfigureAwait(false);
                result.OperationWasFinishedSuccessful = true;
                return true;
            }
            result.ErrorMessage = "Пользователь отказался принять файл.";
            result.OperationWasFinishedSuccessful = false;
            return false;
        }


        protected override Task OnFileRecievingPrepared(FileRecievingPreparedEventArgs args)
        {
            Func<object, FileRecievingPreparedEventArgs, Task> handlerEvent = FileRecievingPreparedEvent;
            if (handlerEvent == null)
                return Task.FromResult(false);

            try
            {
                Delegate[] invocationList = handlerEvent.GetInvocationList();
                var handlerTasks =
                    invocationList.Select(
                        delegatee => ((Func<object, FileRecievingPreparedEventArgs, Task>)delegatee)(this, args));
                return Task.WhenAll(handlerTasks);
            }
            catch (Exception ex)
            {
                return Task.CompletedTask;
            }
        }
        protected override Task OnTextMessageRecieved(TextMessageRecievedEventArgs args)
        {
            Func<object, TextMessageRecievedEventArgs, Task> handlerEvent = TextMessageRecievedEvent;
            if (handlerEvent == null)
                return Task.FromResult(false);

            try
            {
                Delegate[] invocationList = handlerEvent.GetInvocationList();
                var handlerTasks =
                    invocationList.Select(
                        delegatee => ((Func<object, TextMessageRecievedEventArgs, Task>) delegatee)(this, args));
                return Task.WhenAll(handlerTasks);
            }
            catch (Exception ex)
            {
                return Task.CompletedTask;
            }
        }
        protected override Task OnRegisterNewContact(RegisterNewContactEventArgs args)
        {
            Func<object, RegisterNewContactEventArgs, Task> handlerEvent = RegisterNewContactEvent;
            if (handlerEvent == null)
                return Task.FromResult(false);

            try
            {
                Delegate[] invocationList = handlerEvent.GetInvocationList();
                var handlerTasks = new Task[invocationList.Length];
                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ((Func<object, RegisterNewContactEventArgs, Task>)invocationList[i])(this, args);
                }
                return Task.WhenAll(handlerTasks);
            }
            catch (Exception ex)
            {
                args.TryAllowContinueRegistration(false);
                return Task.CompletedTask;
            }
        }
        protected override Task OnFileReciveBegining(RecieveOfFileTransferRequestEventArgs args)
        {
            Func<object, RecieveOfFileTransferRequestEventArgs, Task> handlerEvent = RecieveOfFileTransferRequestEvent;
            if (handlerEvent == null)
                return Task.FromResult(false);

            try
            {
                Delegate[] invocationList = handlerEvent.GetInvocationList();
                var handlerTasks = new Task[invocationList.Length];
                for (int i = 0; i < invocationList.Length; i++)
                {
                    handlerTasks[i] = ((Func<object, RecieveOfFileTransferRequestEventArgs, Task>)invocationList[i])(this, args);
                }
                return Task.WhenAll(handlerTasks);
            }
            catch (Exception ex)
            {
                args.TryAllowContinueRegistration(false, null);
                return Task.CompletedTask;
            }
        }


        //public override byte[] GetPublicKey()
        //{
        //    throw new NotImplementedException();
        //}

        //public override async Task<byte[]> GetPublicKeyAsync()
        //{
        //    throw new NotImplementedException();
        //}

    }
}
