using System;

namespace MoleChatApp1.PCLLibs.SharedMoleRes.Server.Result
{
    public class ResultAsError : IError
    {
        public ResultAsError(string mes, int code)
        {
            Code = code;
        }
        public ResultAsError(string mes)
        {
            if (mes == null) throw new ArgumentNullException(nameof(mes)) { Source = GetType().AssemblyQualifiedName };

            Message = mes;
        }
        public ResultAsError(int code)
        {
            Code = code;
        }


        public string Message { get; }
        public int Code { get; }
        public bool IsSuccessful { get; } = false;
    }
}
