using System;
using System.Net;
using SharedMoleRes.Client;

namespace SharedMoleRes.Server.Surrogates
{
    
    public class UserFormSurrogate
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public PublicKeyForm KeyParametrsBlob { get; set; }
        public int PortClientToClient1 { get; set; }
        public int PortClientToClient2 { get; set; }
        public int PortClientToClient3 { get; set; }
        public int PortServerToClient { get; set; }
        public string Ip { get; set; }
        public AuthenticationFormSur AuthenticationForm { get; set; }
        public AccessibilityInfoSur Accessibility { get; set; }


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
                Ip = formNew.Ip.ToString();
            if (formNew.AuthenticationForm != null)
                AuthenticationForm = AuthenticationFormSur.From(formNew.AuthenticationForm);
            AuthenticationForm.UserForm = this;
            if (formNew.Accessibility != null)
                Accessibility = formNew.Accessibility;
            Accessibility.UserForm = this;
        }
        public static implicit operator UserFormSurrogate(UserForm form)
        {
            var sur = form == null
                ? null
                : new UserFormSurrogate()
                {
                    Login = form.Login,
                    Password = form.Password,
                    KeyParametrsBlob = form.KeyParametrsBlob,
                    PortClientToClient1 = form.PortClientToClient1,
                    PortClientToClient2 = form.PortClientToClient2,
                    PortClientToClient3 = form.PortClientToClient3,
                    PortServerToClient = form.PortServerToClient,
                    Ip = form.Ip?.ToString(),
                    AuthenticationForm = AuthenticationFormSur.From(form.AuthenticationForm),
                    Accessibility = form.Accessibility
                };
            if (sur?.Accessibility != null)
                sur.Accessibility.UserForm = sur;
            if (sur?.AuthenticationForm != null)
                sur.AuthenticationForm.UserForm = sur;
            return sur;
        }
        public static implicit operator UserForm(UserFormSurrogate sur)
        {
            if (sur == null)
                return null;

            try
            {
                var formNew = new UserForm();
                if (sur.Login != null)
                    formNew.Login = sur.Login;
                if (sur.Password != null)
                    formNew.Password = sur.Password;
                if (sur.KeyParametrsBlob != null)
                    formNew.KeyParametrsBlob = sur.KeyParametrsBlob;
                if (sur.PortClientToClient1 != 0)
                    formNew.PortClientToClient1 = sur.PortClientToClient1;
                if (sur.PortClientToClient2 != 0)
                    formNew.PortClientToClient2 = sur.PortClientToClient2;
                if (sur.PortClientToClient3 != 0)
                    formNew.PortClientToClient3 = sur.PortClientToClient3;
                if (sur.PortServerToClient != 0)
                    formNew.PortServerToClient = sur.PortServerToClient;
                if (sur.Ip != null)
                    formNew.Ip = IPAddress.Parse(sur.Ip);
                if (sur.AuthenticationForm != null)
                    formNew.AuthenticationForm = AuthenticationFormSur.To(sur.AuthenticationForm);
                if (sur.Accessibility != null)
                    formNew.Accessibility = sur.Accessibility;
                return formNew;
            }
            catch (Exception ex)
            {
                throw new InvalidCastException("При приведении типа UserFormSurrogate в тип UserForm возникла ошибка.",
                    ex) {Source = typeof(UserFormSurrogate).AssemblyQualifiedName};
            }
        }
        public static implicit operator UserFormSurrogate(ContactForm contact)
        {
            return (UserFormSurrogate) (UserForm) contact;
        }
        public static implicit operator ContactForm(UserFormSurrogate form)
        {
            return (ContactForm) (UserForm) form;
        }
    }
}
