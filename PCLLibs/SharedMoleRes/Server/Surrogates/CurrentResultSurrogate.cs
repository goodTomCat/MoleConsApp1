
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    public class CurrentResultSurrogate<T> : ResultOfOperation
    {
        [ProtoMember(1)]
        public T Result { get; set; }

        public static implicit operator CurrentResultSurrogate<T>(CurrentResult<T> result)
        {
            return result == null ? null : new CurrentResultSurrogate<T>() {Result = result.Result};
        }

        public static implicit operator CurrentResult<T>(CurrentResultSurrogate<T> sur)
        {
            return sur == null ? null : new CurrentResult<T>(sur) {Result = sur.Result};
        }
    }
}
