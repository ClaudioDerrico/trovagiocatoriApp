using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace trovagiocatoriApp.Models
{
    public class PostResponse
    {
        public int id { get; set; }
        public string titolo { get; set; }
        public string provincia { get; set; }
        public string citta { get; set; }
        public string sport { get; set; }
        public string data_partita { get; set; }
        public string ora_partita { get; set; }
        public string commento { get; set; }
        public string autore_email { get; set; }
        public int? campo_id { get; set; }
        public CampoInfo campo { get; set; }
        public string livello { get; set; } = "Intermedio";
        public int numero_giocatori { get; set; } = 1;

        // Proprietà per i partecipanti
        public int partecipanti_iscritti { get; set; } = 0;
        public int posti_disponibili { get; set; } = 1;

        // NUOVO: Proprietà per lo stato "completo/aperto"
        public bool is_full { get; set; } = false;

        // Proprietà computed per gestire le date nel calendario
        public DateTime DataPartitaDateTime
        {
            get
            {
                if (DateTime.TryParse(data_partita, out DateTime result))
                {
                    return result;
                }
                return DateTime.MinValue;
            }
        }

        public TimeSpan OraPartitaTimeSpan
        {
            get
            {
                if (TimeSpan.TryParse(ora_partita, out TimeSpan result))
                {
                    return result;
                }
                return TimeSpan.Zero;
            }
        }

        // Proprietà computed per il display nel calendario
        public string DataPartitaFormatted
        {
            get
            {
                var data = DataPartitaDateTime;
                if (data != DateTime.MinValue)
                {
                    return data.ToString("dd MMM");
                }
                return data_partita;
            }
        }

        public string DataPartitaLong
        {
            get
            {
                var data = DataPartitaDateTime;
                if (data != DateTime.MinValue)
                {
                    return data.ToString("dd/MM/yyyy");
                }
                return data_partita;
            }
        }

        public string OraPartitaFormatted
        {
            get
            {
                var ora = OraPartitaTimeSpan;
                if (ora != TimeSpan.Zero)
                {
                    return ora.ToString(@"hh\:mm");
                }
                return ora_partita;
            }
        }

        // Proprietà per determinare se l'evento è futuro o passato
        public bool IsFutureEvent
        {
            get
            {
                var data = DataPartitaDateTime;
                return data != DateTime.MinValue && data.Date >= DateTime.Today;
            }
        }

        public bool IsTodayEvent
        {
            get
            {
                var data = DataPartitaDateTime;
                return data != DateTime.MinValue && data.Date == DateTime.Today;
            }
        }

        // Proprietà esistenti per la UI
        public string LivelloDisplayText => livello switch
        {
            "Principiante" => "🟢 Principiante",
            "Intermedio" => "🟡 Intermedio",
            "Avanzato" => "🔴 Avanzato",
            _ => "🟡 Intermedio"
        };

        public string LivelloColor => livello switch
        {
            "Principiante" => "#4CAF50",   // Verde
            "Intermedio" => "#FF9800",     // Arancione
            "Avanzato" => "#F44336",       // Rosso
            _ => "#FF9800"
        };

        public string NumeroGiocatoriText => numero_giocatori == 1
            ? "Cerco 1 giocatore"
            : $"Cerco {numero_giocatori} giocatori";
    }

    public class CampoInfo
    {
        public string nome { get; set; }
        public string indirizzo { get; set; }
    }
}