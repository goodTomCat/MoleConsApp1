using System;
using MoleChatApp1.PCLLibs.SharedMoleRes.Server;
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    public class AuthenticationFormClassicSur : AuthenticationFormSur
    {
        [ProtoMember(1)]
        public string HashAlgotitm { get; set; }
        [ProtoMember(2, OverwriteList = true)]
        public byte[] HashOfPassword { get; set; }
        //[ProtoMember(3)]
        //public AuthenticationMethod AuthenticationMethod { get; set; }
        //[ProtoMember(4)]
        //public CryptoProvider CryptoProvider { get; set; }
        //[ProtoMember(5)]
        //public string Login { get; set; }


        public static implicit operator AuthenticationFormClassic(AuthenticationFormClassicSur sur)
        {
            if (sur == null) return null;

            var formNew = new AuthenticationFormClassic();
            formNew.CryptoProvider = sur.CryptoProvider;
            formNew.HashAlgotitm = sur.HashAlgotitm;
            if (sur.HashOfPassword != null && sur.HashOfPassword.Length != 0)
                formNew.HashOfPassword = sur.HashOfPassword;
            if (sur.Login != null)
                formNew.Login = sur.Login;
            return formNew;
        }

        public static implicit operator AuthenticationFormClassicSur(AuthenticationFormClassic formClassic)
        {
            if (formClassic == null) return null;

            var surNew = new AuthenticationFormClassicSur
            {
                HashOfPassword = formClassic.HashOfPassword,
                HashAlgotitm = formClassic.HashAlgotitm,
                CryptoProvider = formClassic.CryptoProvider,
                Login = formClassic.Login,
                AuthenticationMethod = formClassic.AuthenticationMethod
            };
            return surNew;
        }
        [ProtoConverter]
        public static IAuthenticationFormClassic To(AuthenticationFormClassicSur sur)
        {
            return (AuthenticationFormClassic)sur;
        }
        [ProtoConverter]
        public static AuthenticationFormClassicSur From(IAuthenticationFormClassic formClassic)
        {
            if (formClassic == null)
                return null;

            return new AuthenticationFormClassicSur()
            {
                AuthenticationMethod = formClassic.AuthenticationMethod,
                HashAlgotitm = formClassic.HashAlgotitm,
                HashOfPassword = formClassic.HashOfPassword,
                CryptoProvider = formClassic.CryptoProvider,
                Login = formClassic.Login
            };
        }

        public bool VerifyPassword(string password, ResultOfOperation resultOfOperation)
        {
            throw new NotImplementedException();
        }
    }
}
