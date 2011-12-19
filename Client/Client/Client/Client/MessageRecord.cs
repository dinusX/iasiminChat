using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{   [Serializable()]
    class MessageRecord
    {
        string messageFromUser = "";
        string user="";
        DateTime timeReceive;

        public MessageRecord()
        {
 
        }

        public MessageRecord(string _messageFromUser, string _user, DateTime _timeReceive)
        {
            messageFromUser = _messageFromUser;
            user = _user;
            timeReceive = _timeReceive;
        }

        public string GetMessage()
        {
            return messageFromUser;
        }
        public string GetUsername()
        {
            return user;
        }
        public DateTime GetReceivedTime()
        {
            return timeReceive;
        }

    }
}
