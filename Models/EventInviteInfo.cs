using System;

namespace trovagiocatoriApp.Models
{
    public class EventInviteInfo
    {
        public long InviteID { get; set; }
        public int PostID { get; set; }
        public string Message { get; set; }
        public string CreatedAt { get; set; }
        public string Status { get; set; }
        public string SenderUsername { get; set; }
        public string SenderNome { get; set; }
        public string SenderCognome { get; set; }
        public string SenderEmail { get; set; }
        public string SenderProfilePicture { get; set; }
    }
}