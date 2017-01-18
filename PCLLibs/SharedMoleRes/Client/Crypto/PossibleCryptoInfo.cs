using System.Collections.Generic;
using System.Linq;
using SharedMoleRes.Client.Surrogates;

namespace SharedMoleRes.Client.Crypto
{
    public class PossibleCryptoInfo
    {
        public PossibleCryptoInfo(IEnumerable<string> providers, IEnumerable<string> hashAlgs,
            IEnumerable<string> asymmetricAlgs,
            IEnumerable<string> symmetricAlgs, IEnumerable<string> signAlgs)
        {
            if (providers != null)
                Providers = providers.ToArray();
            if (hashAlgs != null)
                Hash = hashAlgs.ToArray();
            if (asymmetricAlgs != null)
                Asymmetric = asymmetricAlgs.ToArray();
            if (symmetricAlgs != null)
                Symmetric = symmetricAlgs.ToArray();
            if (signAlgs != null)
                Sign = signAlgs.ToArray();
        }


        public string[] Providers { get; } = new string[0];
        public string[] Hash { get; } = new string[0];
        public string[] Asymmetric { get; } = new string[0];
        public string[] Symmetric { get; } = new string[0];
        public string[] Sign { get; } = new string[0];


        public static implicit operator PossibleCryptoInfo(PossibleCryptoInfoSur sur)
        {
            if (sur == null)
                return null;

            return new PossibleCryptoInfo(sur.Providers, sur.Hash, sur.Asymmetric, sur.Symmetric, sur.Sign);
        }
        public static implicit operator PossibleCryptoInfoSur(PossibleCryptoInfo cryptoInfo)
        {
            if (cryptoInfo == null)
                return null;

            return new PossibleCryptoInfoSur()
            {
                Asymmetric = cryptoInfo.Asymmetric,
                Providers = cryptoInfo.Providers,
                Sign = cryptoInfo.Sign,
                Hash = cryptoInfo.Hash,
                Symmetric = cryptoInfo.Symmetric
            };
        }
    }
}
