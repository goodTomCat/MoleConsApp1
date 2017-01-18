namespace MoleChatApp1.PCLLibs.SharedMoleRes.Server.Result
{
    public interface IError : IResult
    {
        string Message { get; }
        int Code { get; }
    }
}
