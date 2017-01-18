namespace MoleChatApp1.PCLLibs.SharedMoleRes.Server.Result
{
    public class Result : IResult
    {
        public Result(bool isSuccessful)
        {
            IsSuccessful = isSuccessful;
        }


        public bool IsSuccessful { get; }
    }
}
