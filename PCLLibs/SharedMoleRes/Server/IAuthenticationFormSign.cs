namespace SharedMoleRes.Server
{
    //public enum SignantureAlgoritmName : byte { Rsa, EcDsa }

    public interface IAuthenticationFormSign : IAuthenticationForm
    {
        string SignantureAlgoritmName { get; }
        string HashAlgotitmName { get; }
        byte[] Sign { get; }
        byte[] Hash { get; }
    }
}
