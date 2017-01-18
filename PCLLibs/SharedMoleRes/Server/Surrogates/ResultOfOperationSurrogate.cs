
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    [ProtoInclude(100, typeof(CurrentResultSurrogate<>))]
    public class ResultOfOperationSurrogate
    {
        [ProtoMember(1)]
        public int ErrorCode { get; set; } = -1;
        [ProtoMember(2)]
        public string ErrorMessage { get; set; }
        [ProtoMember(3)]
        public bool OperationWasFinishedSuccessful { get; set; }


        public static implicit operator ResultOfOperationSurrogate(ResultOfOperation result)
        {
            return result == null
                ? null
                : new ResultOfOperationSurrogate()
                {
                    ErrorCode = result.ErrorCode,
                    ErrorMessage = result.ErrorMessage,
                    OperationWasFinishedSuccessful = result.OperationWasFinishedSuccessful
                };
        }

        public static implicit operator ResultOfOperation(ResultOfOperationSurrogate sur)
        {
            return sur == null
                ? null
                : new ResultOfOperation()
                {
                    ErrorCode = sur.ErrorCode,
                    ErrorMessage = sur.ErrorMessage,
                    OperationWasFinishedSuccessful = sur.OperationWasFinishedSuccessful
                };
        }
    }
}
