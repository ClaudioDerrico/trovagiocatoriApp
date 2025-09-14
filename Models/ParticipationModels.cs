// Models/ParticipationModels.cs
using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class ParticipationRequest
    {
        public int post_id { get; set; }
    }

    public class ParticipationResponse
    {
        public bool success { get; set; }
        public bool is_participant { get; set; }
        public string message { get; set; }
    }

    public class ParticipantInfo
    {
        public long user_id { get; set; }
        public string username { get; set; }
        public string nome { get; set; }
        public string cognome { get; set; }
        public string email { get; set; }
        public string profile_picture { get; set; }
        public DateTime registered_at { get; set; }
        public bool IsOrganizer { get; set; } // AGGIUNTO

        // Proprietà computed per il display name
        public string DisplayName =>
            !string.IsNullOrEmpty(username) ? username :
            !string.IsNullOrEmpty(nome) && !string.IsNullOrEmpty(cognome) ? $"{nome} {cognome}" :
            email;
    }

    public class EventParticipantsResponse
    {
        public bool success { get; set; }
        public List<ParticipantInfo> participants { get; set; } = new List<ParticipantInfo>();
        public int count { get; set; }
    }

    public class PostAvailabilityResponse
    {
        public bool success { get; set; }
        public int post_id { get; set; }
        public int numero_giocatori_richiesti { get; set; }
        public int partecipanti_iscritti { get; set; }
        public int posti_disponibili { get; set; }
        public bool is_full { get; set; }
    }


}