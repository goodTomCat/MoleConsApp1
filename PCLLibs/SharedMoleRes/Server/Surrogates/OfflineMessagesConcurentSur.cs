using System.Collections.Generic;

namespace SharedMoleRes.Server.Surrogates
{
    public class OfflineMessagesConcurentSur
    {
        public string ReceiverLogin { get; set; }
        public Dictionary<string, List<byte[]>> Messages { get; set; }
    }
}
