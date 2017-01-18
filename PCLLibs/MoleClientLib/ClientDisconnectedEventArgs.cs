using System.Net;
using SharedMoleRes.Client;

namespace MoleClientLib
{
    public class ClientDisconnectedEventArgs
    {
        public ClientDisconnectedEventArgs(ContactForm form, IPEndPoint remoteEndPoint)
        {
            Form = form;
            RemoteEndPoint = remoteEndPoint;
        }


        public ContactForm Form { get; }
        public IPEndPoint RemoteEndPoint { get; }
    }
}
