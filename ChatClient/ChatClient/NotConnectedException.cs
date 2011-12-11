using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat
{
    class NotConnectedException : Exception
    {
        public NotConnectedException(string message) : base(message)
        {
            
        }
    }
}
