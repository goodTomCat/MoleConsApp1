//using System.ComponentModel.DataAnnotations.Schema;

//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;


using MoleChatApp1.PCLLibs.SharedMoleRes.Server;
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    [ProtoInclude(10, typeof(AuthenticationFormClassicSur))]
    [ProtoInclude(20, typeof(AuthenticationFormSignSur))]
    public class AuthenticationFormSur
    {
        public UserFormSurrogate UserForm { get; set; }
        public int UserFormId { get; set; }
        public int Id { get; set; }


        [ProtoMember(1)]
        public AuthenticationMethod AuthenticationMethod { get; set; }
        [ProtoMember(2)]
        public string CryptoProvider { get; set; }
        [ProtoMember(3)]
        public string Login { get; set; }

        [ProtoConverter]
        public static IAuthenticationForm To(AuthenticationFormSur sur)
        {
            var surOfClassForm = sur as AuthenticationFormClassicSur;
            if (surOfClassForm != null)
                return (AuthenticationFormClassic)surOfClassForm;

            var surOfSignForm = sur as AuthenticationFormSignSur;
            if (surOfSignForm != null)
                return (AuthenticationFormSign) surOfSignForm;

            return null;
        }
        [ProtoConverter]
        public static AuthenticationFormSur From(IAuthenticationForm form)
        {
            if (form == null)
                return null;

            var formAsclassic = form as AuthenticationFormClassic;
            if (formAsclassic != null)
                return (AuthenticationFormClassicSur) formAsclassic;
                //return new AuthenticationFormClassicSur()
                //{
                //    AuthenticationMethod = formAsclassic.AuthenticationMethod,
                //    CryptoProvider = formAsclassic.CryptoProvider,
                //    Login = formAsclassic.Login,
                //    HashOfPassword = formAsclassic.HashOfPassword,
                //    HashAlgotitm = formAsclassic.HashAlgotitm
                //};

            var formAsSign = form as AuthenticationFormSign;
            if (formAsSign != null)
                return (AuthenticationFormSignSur) formAsSign;
                //return new AuthenticationFormSignSur()
                //{
                //    AuthenticationMethod = formAsSign.AuthenticationMethod,
                //    CryptoProvider = formAsSign.CryptoProvider,
                //    Login = formAsSign.Login,
                //    Sign = formAsSign.Sign,
                //    Hash = formAsSign.Hash,
                //    SignantureAlgoritmName = formAsSign.SignantureAlgoritmName
                //};

            return new AuthenticationFormSur()
            {
                AuthenticationMethod = form.AuthenticationMethod,
                CryptoProvider = form.CryptoProvider,
                Login = form.Login
            };
        }
        
    }
}
