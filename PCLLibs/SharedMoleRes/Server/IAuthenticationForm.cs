namespace SharedMoleRes.Server
{
    public enum AuthenticationMethod : byte { Sign, Classic }

    public interface IAuthenticationForm
    {
        AuthenticationMethod AuthenticationMethod { get; }
        string CryptoProvider { get; }
        string Login { get; }
    }
}
