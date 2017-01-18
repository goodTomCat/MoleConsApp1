using System.Net;
using ProtoBuf;

namespace SharedMoleRes.Server.Surrogates
{
    [ProtoContract]
    public class IpAddressSurrogate
    {
        public IpAddressSurrogate() { }


        [ProtoMember(1)]
        public string IpAsString { get; set; }

        public static implicit operator IPAddress(IpAddressSurrogate sur)
        {
            if (sur == null)
                return null;

            IPAddress ip;
            if (IPAddress.TryParse(sur.IpAsString, out ip))
                return ip;
            else
                return null;
        }

        public static implicit operator IpAddressSurrogate(IPAddress ip)
        {
            if (ip == null)
                return null;

            return new IpAddressSurrogate() {IpAsString = ip.ToString()};
        }
    }
}
