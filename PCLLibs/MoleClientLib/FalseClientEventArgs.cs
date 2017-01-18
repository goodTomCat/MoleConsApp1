using System.Net;

namespace MoleClientLib
{
    public class FalseClientEventArgs
    {
        public FalseClientEventArgs(string login, IPEndPoint endPoint)
        {
            Login = login;
            EndPoint = endPoint;
        }


        public string Login { get; }
        public IPEndPoint EndPoint { get; }
    }
}
