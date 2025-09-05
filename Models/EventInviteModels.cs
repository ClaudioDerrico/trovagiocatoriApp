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

        // Proprietà computed
        public string SenderDisplayName =>
            !string.IsNullOrEmpty(SenderUsername) ? SenderUsername :
            !string.IsNullOrEmpty(SenderNome) && !string.IsNullOrEmpty(SenderCognome) ?
                $"{SenderNome} {SenderCognome}" : SenderEmail;

        public DateTime CreatedAtDateTime
        {
            get
            {
                if (DateTime.TryParse(CreatedAt, out DateTime result))
                {
                    return result;
                }
                return DateTime.MinValue;
            }
        }

        public string CreatedAtFormatted => CreatedAtDateTime != DateTime.MinValue
            ? CreatedAtDateTime.ToString("dd/MM/yyyy HH:mm")
            : CreatedAt;
    }

    public class EventInviteRequest
    {
        public int PostID { get; set; }
        public string FriendEmail { get; set; }
        public string Message { get; set; }
    }

    public class EventInviteResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}