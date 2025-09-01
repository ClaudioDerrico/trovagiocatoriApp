using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class FootballField
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

        // Proprietà computed per il display
        public string DisplayName => $"{Nome} - {Indirizzo}";
    }
}