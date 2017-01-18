namespace MoleChatApp1.PCLLibs.SharedMoleRes.Server.Result
{
    public interface IData<out TResult> : IResult
    {
        TResult Result { get; }
    }
}
