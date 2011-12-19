using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Client
{
    [Serializable()]
    class ContentConversation
    {
        //string allMessages="";
        public List<MessageRecord> mRec = new List<MessageRecord>();

        public ContentConversation()
        {
          
        }

        public void AppendMessage(string message, string username, DateTime time)
        {
            //allMessages += message;
            MessageRecord mR = new MessageRecord(message, username, time);
            mRec.Add(mR);
        }
        
        /*public string GetAllMessages()
        {
            return allMessages;
        }*/
        
       
    }
}
