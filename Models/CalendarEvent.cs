using System;
using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class CalendarEvent
    {
        public int Id { get; set; }
        public string Titolo { get; set; }
        public string Sport { get; set; }
        public DateTime DataPartita { get; set; }
        public TimeSpan OraPartita { get; set; }
        public string Citta { get; set; }
        public string Provincia { get; set; }
        public string Livello { get; set; }
        public CampoInfo Campo { get; set; }
        public string Commento { get; set; }
        public DateTime RegisteredAt { get; set; }

        // Proprietà computed per il display
        public string SportDisplayText => $"Partita di {Sport}";

        public string DateDisplayText => DataPartita.ToString("dd MMM yyyy");

        public string TimeDisplayText => OraPartita.ToString(@"hh\:mm");

        public string LocationDisplayText => $"{Citta}, {Provincia}";

        public string LivelloDisplayText => Livello switch
        {
            "Principiante" => "🟢 Principiante",
            "Intermedio" => "🟡 Intermedio",
            "Avanzato" => "🔴 Avanzato",
            _ => "🟡 Intermedio"
        };

        public string CampoDisplayText => Campo?.nome ?? "Campo da definire";

        // Proprietà per determinare se l'evento è oggi
        public bool IsToday => DataPartita.Date == DateTime.Today;

        // Proprietà per determinare se l'evento è nel futuro
        public bool IsFutureEvent => DataPartita.Date >= DateTime.Today;

        // Proprietà per determinare se l'evento è passato
        public bool IsPastEvent => DataPartita.Date < DateTime.Today;

        // Colore basato sulla vicinanza dell'evento
        public string EventColor
        {
            get
            {
                if (IsToday) return "#FF5722"; // Arancione per oggi
                if (DataPartita.Date == DateTime.Today.AddDays(1)) return "#FF9800"; // Arancione chiaro per domani
                if (IsFutureEvent) return "#2196F3"; // Blu per eventi futuri
                return "#9E9E9E"; // Grigio per eventi passati
            }
        }

        // Testo descrittivo per quando è l'evento
        public string WhenText
        {
            get
            {
                var daysDiff = (DataPartita.Date - DateTime.Today).Days;

                return daysDiff switch
                {
                    0 => "Oggi",
                    1 => "Domani",
                    -1 => "Ieri",
                    var d when d > 1 && d <= 7 => $"Tra {d} giorni",
                    var d when d > 7 => DataPartita.ToString("dd/MM/yyyy"),
                    var d when d < -1 && d >= -7 => $"{Math.Abs(d)} giorni fa",
                    _ => DataPartita.ToString("dd/MM/yyyy")
                };
            }
        }
    }

    // Response model per l'API
    public class UserParticipationsResponse
    {
        public bool Success { get; set; }
        public List<int> Participations { get; set; } = new List<int>();
        public int Count { get; set; }
    }
}