using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat
{
    enum ServerOperation
    {
        SignIn = 1,
        SignUp = 3,
        FriendRequest =4,
        ChangeStatus =5,


        AddOfflineMessage = 9
    }

    enum NotifyOperation
    {
        LogIn = 1,
        ChangeStatus = 2,
        LogOut = 3,
        FriendRequest = 4,
        AddNewFriend = 5
    }

    enum ClientOperation
    {
        ReceiveMessage = 101,
        ReceiveFile = 102
    }


}

