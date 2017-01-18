namespace MoleChatApp1.PCLLibs.SharedMoleRes.Server.Result
{
    public class ResultAsData<TResult> : IData<TResult>
    {
        public ResultAsData(TResult result)
        {
            Result = result;
        }


        public bool IsSuccessful { get; } = true;
        public TResult Result { get; }
    }
}
