using MoleChatApp1.PCLLibs.SharedMoleRes.Server;
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    public class AuthenticationFormSignSur : AuthenticationFormSur
    {
        [ProtoMember(1, OverwriteList = true)]
        public byte[] Sign { get; set; }
        [ProtoMember(2)]
        public string SignantureAlgoritmName { get; set; }
        [ProtoMember(3, OverwriteList = true)]
        public byte[] Hash { get; set; }
        [ProtoMember(4)]
        public string HashAlgotitmName { get; set; }
        //[ProtoMember(4)]
        //public AuthenticationMethod AuthenticationMethod { get; set; }
        //[ProtoMember(5)]
        //public CryptoProvider CryptoProvider { get; set; }
        //[ProtoMember(6)]
        //public string Login { get; set; }


        public static implicit operator AuthenticationFormSign(AuthenticationFormSignSur sur)
        {
            if (sur == null)
                return null;

            var formNew = new AuthenticationFormSign();
            formNew.CryptoProvider = sur.CryptoProvider;
            if (sur.Hash != null && sur.Hash.Length != 0)
                formNew.Hash = sur.Hash;
            if (sur.Login != null)
                formNew.Login = sur.Login;
            if (sur.Sign != null && sur.Sign.Length != 0)
                formNew.Sign = sur.Sign;
            formNew.SignantureAlgoritmName = sur.SignantureAlgoritmName;
            formNew.HashAlgotitmName = sur.HashAlgotitmName;
            return formNew;
        }

        public static implicit operator AuthenticationFormSignSur(AuthenticationFormSign formSign)
        {
            if (formSign == null) return null;

            var surNew = new AuthenticationFormSignSur()
            {
                CryptoProvider = formSign.CryptoProvider,
                Hash = formSign.Hash,
                Login = formSign.Login,
                Sign = formSign.Sign,
                SignantureAlgoritmName = formSign.SignantureAlgoritmName,
                AuthenticationMethod = formSign.AuthenticationMethod,
                HashAlgotitmName = formSign.HashAlgotitmName
            };
            return surNew;
        }
        [ProtoConverter]
        public static IAuthenticationFormSign To(AuthenticationFormSignSur sur)
        {
            return (AuthenticationFormSign) sur;
        }
        [ProtoConverter]
        public static AuthenticationFormSignSur From(IAuthenticationFormSign formSign)
        {
            if (formSign == null)
                return null;

            return new AuthenticationFormSignSur()
            {
                AuthenticationMethod = formSign.AuthenticationMethod,
                Hash = formSign.Hash,
                Sign = formSign.Sign,
                SignantureAlgoritmName = formSign.SignantureAlgoritmName,
                CryptoProvider = formSign.CryptoProvider,
                Login = formSign.Login
            };
        }
    }
}
