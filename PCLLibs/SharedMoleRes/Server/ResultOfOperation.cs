namespace SharedMoleRes.Server
{
    //[ProtoContract]
    //[ProtoInclude(300, typeof(CurrentResult<>))]
    public class ResultOfOperation
    {
        public ResultOfOperation() {}

        
        public int ErrorCode { get; set; } = -1;
       
        public string ErrorMessage { get; set; }
        
        public bool OperationWasFinishedSuccessful { get; set; }
    }
}
