using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedService.Models
{
        public enum ConversationType
        {
            Direct = 1,
            Group = 2
        }

        public enum GroupRole
        {
            Member = 1,
            Admin = 2,
            Owner = 3
        }

        public enum MessageType
        {
            Text = 1,
            Image = 2,
            Video = 3,
            Audio = 4,
            File = 5,
            System = 6
        }

        public enum FriendRequestStatus
        {
            Pending = 0,
            Accepted = 1,
            Rejected = 2,
            UnFollowed = 3,
            Blocked = 4
        }

    public enum CallType
    {
        Audio = 1,
        Video = 2
    }

    public enum CallStatus
    {
        Ringing = 1,
        Ongoing = 2,
        Ended = 3,
        Missed = 4,
        Rejected = 5
    }

    public enum ParticiapantCallStatus
    {
        Invited= 1,
        Joined= 2,
        Left= 3,
        Missed= 4,
        Rejected= 5
    }
}
