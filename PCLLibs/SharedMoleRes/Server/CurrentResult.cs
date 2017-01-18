namespace SharedMoleRes.Server
{
    //[ProtoContract]
    public class CurrentResult<T> : ResultOfOperation
    {
        public CurrentResult() { }

        public CurrentResult(ResultOfOperation result)
        {
            ErrorMessage = result.ErrorMessage;
            ErrorCode = result.ErrorCode;
            OperationWasFinishedSuccessful = result.OperationWasFinishedSuccessful;
        }


        public T Result { get; set; }
    }
}
