using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using JabyLib.Other;
using MoleClientLib.RemoteFileStream;
using SharedMoleRes.Client;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;

namespace MoleClientLib
{
    //using CryptoProviderTupl = Tuple<IAsymmetricEncrypter, ISign, ICryptoTransform,//encrypter
    //    ICryptoTransform,//decryptor
    //    KeyDataForSymmetricAlgorithm>;

    public abstract class MoleClientCoreBase
    {
        protected CustomBinarySerializerBase Ser;
        protected ISign SignAlg;
        protected IAsymmetricEncrypter AsEnc;
        protected string CryptProv;
        //protected Dictionary<string, CryptoProviderTupl> CryptoProviders;


        /// <exception cref="ArgumentNullException">nameOfCryptoProvider == null. -or- signAlg == null. 
        /// -or- asEnc == null. -or- serializer == null. -or- nameOfCryptoProvider == null.</exception>
        protected MoleClientCoreBase(CustomBinarySerializerBase serializer, ICollection<CryptoFactoryBase> factoriesBase, 
            UserForm myUserForm, ISign signAlgImpl)
        {
            if (serializer == null) throw new ArgumentNullException(nameof(serializer)) {Source = GetType().AssemblyQualifiedName};
            if (factoriesBase == null) throw new ArgumentNullException(nameof(factoriesBase)) { Source = GetType().AssemblyQualifiedName };
            if (!factoriesBase.Any())
                throw new ArgumentException("Value cannot be an empty collection.", nameof(factoriesBase))
                { Source = GetType().AssemblyQualifiedName };
            if (myUserForm == null)
                throw new ArgumentNullException(nameof(myUserForm))
                { Source = GetType().AssemblyQualifiedName };
            if (signAlgImpl == null)
                throw new ArgumentNullException(nameof(signAlgImpl))
                { Source = GetType().AssemblyQualifiedName };

            Ser = serializer;
            Factories = new ReadOnlyCollection<CryptoFactoryBase>(factoriesBase.ToArray());
            MyUserForm = myUserForm;
            SignAlgImpl = signAlgImpl;
        }


        /// <exception cref="ArgumentNullException">Serializer == null</exception>
        public virtual CustomBinarySerializerBase Serializer
        {
            get { return Ser; }
            protected set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                Interlocked.Exchange(ref Ser, value);
            }
        }
        /// <exception cref="ArgumentNullException">value == null</exception>
        //public virtual IAsymmetricEncrypter AsymmetricEncrypter
        //{
        //    get { return AsEnc; }
        //    protected set
        //    {
        //        if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

        //        AsEnc = value;
        //    }
        //}
        ///// <exception cref="ArgumentNullException">value == null</exception>
        //public virtual ISign SignatureAlgorithm
        //{
        //    get { return SignAlg; }
        //    set
        //    {
        //        if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };

        //        SignAlg = value;
        //    }
        //}
        //protected string CryptoProvider
        //{
        //    get { return CryptProv; }
        //    set
        //    {
        //        if (value == null) throw new ArgumentNullException(nameof(value)) { Source = GetType().AssemblyQualifiedName };
        //        if (value.Length <= 3)
        //            throw new ArgumentOutOfRangeException(nameof(value), "В строке должно быть больше чем 3 символа.");

        //        CryptProv = value;
        //    }
        //}
        public IReadOnlyCollection<CryptoFactoryBase> Factories { get; protected set; }
        //public string Login { get; }
        public UserForm MyUserForm { get; protected set; }
        public ConcurrentDictionary<string, FileStream> FilesSending { get; } =
            new ConcurrentDictionary<string, FileStream>();
        public PossibleCryptoInfo PossibleAlgs
        {
            get
            {
                var possibleAlgs = Factories.Select(factory => factory.PossibleCryptoAlgs).ToArray();
                var posNew = new PossibleCryptoInfo(possibleAlgs.SelectMany(info => info.Providers).Distinct(),
                    possibleAlgs.SelectMany(info => info.Hash).Distinct(),
                    possibleAlgs.SelectMany(info => info.Asymmetric).Distinct(),
                    possibleAlgs.SelectMany(info => info.Symmetric).Distinct(),
                    possibleAlgs.SelectMany(info => info.Sign).Distinct());
                return posNew;
            }
        }
        public ISign SignAlgImpl { get; protected set; }


        public abstract Task TextMessageRecieved(string login, string message, ResultOfOperation result, ContactForm form = null,
            bool userIsAuth = true);
        public abstract Tuple<bool, ContactForm> AuthenticateContacnt(ClientToClientAuthForm authForm, CryptoFactoryBase factory, IPEndPoint endPoint, IEnumerable<ContactForm> publicForms,
            ResultOfOperation resultOfOperation);
        //public abstract Task<Tuple<bool, ContactForm>> RegisterNewContactAsync(string login, IPEndPoint endPoint, IEnumerable<ContactForm> formsPublic,
        //    ResultOfOperation resultOfOperation);
        public abstract Task<bool> RegisterNewContactAsync(ContactForm form, IPEndPoint endPoint,
            ResultOfOperation resultOfOperation);
        public abstract Task<bool> RecieveOfFileTransferRequest(FileRecieveRequest request, IPEndPoint endPoint, ContactForm contact,
            ResultOfOperation result);

        protected abstract Task OnFileRecievingPrepared(FileRecievingPreparedEventArgs args);
        protected abstract Task OnTextMessageRecieved(TextMessageRecievedEventArgs args);
        protected abstract Task OnRegisterNewContact(RegisterNewContactEventArgs args);
        protected abstract Task OnFileReciveBegining(RecieveOfFileTransferRequestEventArgs args);
        //public abstract byte[] GetPublicKey();

        //public abstract Task<byte[]> GetPublicKeyAsync();
    }
}
