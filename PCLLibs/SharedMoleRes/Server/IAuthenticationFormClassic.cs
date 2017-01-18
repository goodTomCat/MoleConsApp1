namespace SharedMoleRes.Server
{
    //public enum HashAlgotitm : byte { Md5, Sha1, Sha256, Sha384, Sha512 }

    public interface IAuthenticationFormClassic : IAuthenticationForm
    {
        string HashAlgotitm { get; }
        byte[] HashOfPassword { get; }

    }
}
