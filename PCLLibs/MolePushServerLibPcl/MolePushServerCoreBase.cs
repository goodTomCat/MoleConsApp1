using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Threading.Tasks;
using SharedMoleRes.Client.Crypto;
using SharedMoleRes.Server;
using SharedMoleRes.Server.Surrogates;

namespace MolePushServerLibPcl
{
    public abstract class MolePushServerCoreBase
    {
        protected IList<CryptoFactoryBase> CryptoFactoriesF;


        /// <exception cref="ArgumentNullException">cryptoFactories == null. -or- possibleCryptoInfo == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">cryptoFactories.Count == 0.</exception>
        protected MolePushServerCoreBase(PossibleCryptoInfo possibleCryptoInfo, IList<CryptoFactoryBase> cryptoFactories)
        {
            if (cryptoFactories == null) throw new ArgumentNullException(nameof(cryptoFactories)) { Source = GetType().AssemblyQualifiedName };
            if (possibleCryptoInfo == null)
                throw new ArgumentNullException(nameof(possibleCryptoInfo)) { Source = GetType().AssemblyQualifiedName };
            if (cryptoFactories.Count == 0)
                throw new ArgumentOutOfRangeException(nameof(cryptoFactories.Count))
                { Source = GetType().AssemblyQualifiedName };

            CryptoFactoriesF = cryptoFactories;
            PossibleCryptoInfo = possibleCryptoInfo;
        }


        public virtual IReadOnlyCollection<CryptoFactoryBase> CryptoFactories
            => new ReadOnlyCollection<CryptoFactoryBase>(CryptoFactoriesF);
        public virtual PossibleCryptoInfo PossibleCryptoInfo { get; }
        public bool IsInitialized { get; protected set; }


        public abstract Task Initialize();
        ///// <exception cref="ArgumentNullException">form == null. -или- ip == null. -или- resultOfOperation == null.</exception>
        ///// <exception cref="ArgumentOutOfRangeException">Строка не является ip адресом.</exception>
        //public abstract bool RegisterNewUser(UserForm form, IPAddress ip, ResultOfOperation resultOfOperation);
        /// <exception cref="ArgumentNullException">form == null. -или- ip == null. -или- resultOfOperation == null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Строка не является ip адресом.</exception>
        public abstract Task<bool> RegisterNewUserAsync(UserForm form, IPAddress ip,
            ResultOfOperation resultOfOperation);
        //public abstract UserForm GetPublicKey();
        /// <exception cref="ArgumentNullException">form == null. -или- resultOfOperation == null.</exception>
        public abstract Task<Tuple<UserFormSurrogate, OfflineMessagesConcurent>> AuthenticateUserAsync(
            IAuthenticationForm formAuth,
            CryptoFactoryBase cryptoFactory, CryptoInfo choosenCrypto, ResultOfOperation resultOfOperation);

        /// <exception cref="ArgumentNullException">form == null. -или- names == null.</exception>
        public abstract Task<ICollection<UserFormSurrogate>> GetUsersPublicData(IEnumerable<string> names, UserForm form);

        /// <exception cref="ArgumentNullException">form == null. -или- resultOfOperation == null.</exception>
        public abstract bool UpdateUserData(ref UserForm form, ResultOfOperation resultOfOperation);

        /// <exception cref="ArgumentNullException">strToFined == null.</exception>
        public abstract Task<ICollection<UserFormSurrogate>> FinedUserAsync(string strToFined, string senderLogin);

        /// <exception cref="ArgumentNullException">formOfSender == null. -или- OfflineMessageForm == null. -или- 
        /// resultOfOperation == null.</exception>
        public abstract bool SendOfflineMessage(OfflineMessageForm offlineMessage, UserForm formOfSender,
            ResultOfOperation resultOfOperation);
    }
}
