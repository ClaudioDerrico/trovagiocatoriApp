using System;

namespace trovagiocatoriApp.Models
{
    public class FriendInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime FriendsSince { get; set; }
    }

    public class FriendRequest
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime RequestSent { get; set; }
        public DateTime RequestReceived { get; set; }
        public FriendRequestStatus Status { get; set; }
    }

    public class UserSearchResult
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
    }

    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled
    }
}