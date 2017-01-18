using System;
using SharedMoleRes.Client;

namespace MoleClientLib
{
    public class TextMessageRecievedEventArgs : EventArgs
    {
        public string Message { get; set; }
        public ContactForm Contact { get; set; }
    }
}
