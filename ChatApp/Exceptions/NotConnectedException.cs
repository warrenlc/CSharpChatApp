using System;

namespace ChatApp.Exceptions
{
    public class NotConnectedException : Exception
    {
        public string Msg { get; }  
        
        public NotConnectedException(string msg) : base("Not connected to a session")
        {
            Msg = msg;
        }   
    }
}
