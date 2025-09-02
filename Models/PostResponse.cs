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
        public SportField campo { get; set; }
        public string livello { get; set; } = "Intermedio"; // NUOVO CAMPO LIVELLO
        public int numero_giocatori { get; set; } = 1; // NUOVO CAMPO NUMERO GIOCATORI

        // Proprietà computed per visualizzare il livello con emoji
        public string LivelloDisplayText => livello switch
        {
            "Principiante" => "🟢 Principiante",
            "Intermedio" => "🟡 Intermedio",
            "Avanzato" => "🔴 Avanzato",
            _ => "🟡 Intermedio"
        };

        // Colore per il livello
        public string LivelloColor => livello switch
        {
            "Principiante" => "#4CAF50",   // Verde
            "Intermedio" => "#FF9800",   // Arancione
            "Avanzato" => "#F44336",    // Rosso
            _ => "#FF9800"
        };

        // Testo per numero giocatori
        public string NumeroGiocatoriText => numero_giocatori == 1
            ? "Cerco 1 giocatore"
            : $"Cerco {numero_giocatori} giocatori";
    }
}