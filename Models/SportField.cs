using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class SportField
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; }

        [JsonPropertyName("indirizzo")]
        public string Indirizzo { get; set; }

        [JsonPropertyName("provincia")]
        public string Provincia { get; set; }

        [JsonPropertyName("citta")]
        public string Citta { get; set; }

        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }

        [JsonPropertyName("tipo")]
        public string Tipo { get; set; }

        [JsonPropertyName("descrizione")]
        public string Descrizione { get; set; }

        [JsonPropertyName("sports_disponibili")]
        public List<string> SportsDisponibili { get; set; } = new List<string>();

        // Proprietà computed per il display
        public string DisplayName => $"{Nome} - {Indirizzo}";

        // Proprietà computed per mostrare gli sport supportati
        public string SportsText => SportsDisponibili != null && SportsDisponibili.Any()
            ? string.Join(", ", SportsDisponibili)
            : "Non specificato";

        // Metodo per verificare se il campo supporta uno sport specifico
        public bool SupportsSport(string sport)
        {
            return SportsDisponibili != null && SportsDisponibili.Contains(sport, StringComparer.OrdinalIgnoreCase);
        }
    }
}