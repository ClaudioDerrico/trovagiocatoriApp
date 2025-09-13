using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trovagiocatoriApp.Models
{
    // Classi di supporto rimangono invariate
    public class NotificationItem
    {
        public long Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
        public SenderInfo SenderInfo { get; set; }
    }

    public class SenderInfo
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string ProfilePic { get; set; }
    }
}
