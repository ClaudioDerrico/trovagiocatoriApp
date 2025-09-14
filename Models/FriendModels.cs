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
        public int RequestId { get; set; } // ID specifico della richiesta
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

    // Modelli per le richieste API
    public class SendFriendRequestModel
    {
        public string target_email { get; set; }
    }

    public class FriendActionModel
    {
        public int request_id { get; set; }
    }

    public class RemoveFriendModel
    {
        public string target_email { get; set; }
    }

    // Modelli per le risposte API
    public class FriendsListResponse
    {
        public bool success { get; set; }
        public List<FriendInfo> friends { get; set; } = new List<FriendInfo>();
        public int count { get; set; }
    }

    public class FriendRequestResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public string error { get; set; }
    }

    public class SearchUsersResponse
    {
        public bool success { get; set; }
        public List<UserSearchResult> users { get; set; } = new List<UserSearchResult>();
        public int count { get; set; }
    }
}