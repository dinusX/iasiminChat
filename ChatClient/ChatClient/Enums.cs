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
        AcceptFriend = 6,
        ChangeLogo = 7,
        GetLogo = 8,
        AddOfflineMessage = 9
    }

    enum NotifyOperation
    {
        LogIn = 1,
        ChangeStatus = 2,
        LogOut = 3,
        FriendRequest = 4,
        AddNewFriend = 5,
        ChangedLogo = 6
    }

    enum ClientOperation
    {
        ReceiveMessage = 101,
        ReceiveFile = 102
    }


}

