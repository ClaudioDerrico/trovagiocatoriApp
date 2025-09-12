using System;
using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class AdminPostInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("titolo")]
        public string Titolo { get; set; }

        [JsonPropertyName("autore_email")]
        public string AutoreEmail { get; set; }

        [JsonPropertyName("sport")]
        public string Sport { get; set; }

        [JsonPropertyName("citta")]
        public string Citta { get; set; }

        [JsonPropertyName("provincia")]
        public string Provincia { get; set; }

        [JsonPropertyName("data_creazione")]
        public DateTime DataCreazione { get; set; }

        [JsonPropertyName("data_partita")]
        public DateTime DataPartita { get; set; }

        [JsonPropertyName("ora_partita")]
        public string OraPartita { get; set; }

        [JsonPropertyName("numero_giocatori")]
        public int NumeroGiocatori { get; set; }

        [JsonPropertyName("partecipanti_iscritti")]
        public int PartecipantiIscritti { get; set; }

        [JsonPropertyName("posti_liberi")]
        public int PostiLiberi { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("livello")]
        public string Livello { get; set; }

        [JsonPropertyName("commento")]
        public string Commento { get; set; }

        // Proprietà computed per il display
        public string DataCreazioneFormatted => DataCreazione.ToString("dd/MM/yyyy HH:mm");
        public string DataPartitaFormatted => DataPartita.ToString("dd/MM/yyyy");
        public Color StatusColor => Status == "Completo" ? Colors.Red : Colors.Green;
    }

    public class AdminCommentInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("post_id")]
        public int PostId { get; set; }

        [JsonPropertyName("post_titolo")]
        public string PostTitolo { get; set; }

        [JsonPropertyName("autore_email")]
        public string AutoreEmail { get; set; }

        [JsonPropertyName("contenuto")]
        public string Contenuto { get; set; }

        [JsonPropertyName("data_creazione")]
        public DateTime DataCreazione { get; set; }

        [JsonPropertyName("contenuto_preview")]
        public string ContenutoPreview { get; set; }

        // Proprietà computed per il display
        public string DataCreazioneFormatted => DataCreazione.ToString("dd/MM/yyyy HH:mm");
    }

    public class AdminUserInfo
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("nome")]
        public string Nome { get; set; }

        [JsonPropertyName("cognome")]
        public string Cognome { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("dataRegistrazione")]
        public DateTime DataRegistrazione { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("isAdmin")]
        public bool IsAdmin { get; set; }

        [JsonPropertyName("postCreati")]
        public int PostCreati { get; set; }

        [JsonPropertyName("commentiScritti")]
        public int CommentiScritti { get; set; }

        // Proprietà computed per il display
        public string DataRegistrazioneFormatted => DataRegistrazione.ToString("dd/MM/yyyy");
        public string NomeCompleto => $"{Nome} {Cognome}";
        public string StatusText => IsActive ? "Attivo" : "Disattivato";
        public Color StatusColor => IsActive ? Colors.Green : Colors.Red;
        public string AdminBadge => IsAdmin ? "👑 ADMIN" : "";
        public string AttivitaText => $"{PostCreati} post • {CommentiScritti} commenti";
    }

    public class AdminStats
    {
        [JsonPropertyName("total_posts")]
        public int TotalPosts { get; set; }

        [JsonPropertyName("total_comments")]
        public int TotalComments { get; set; }

        [JsonPropertyName("total_users")]
        public int TotalUsers { get; set; }

        [JsonPropertyName("total_sport_fields")]
        public int TotalSportFields { get; set; }

        [JsonPropertyName("posts_this_week")]
        public int PostsThisWeek { get; set; }

        [JsonPropertyName("comments_today")]
        public int CommentsToday { get; set; }

        [JsonPropertyName("generated_at")]
        public string GeneratedAt { get; set; }
    }

    public class AdminResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public string Error { get; set; }
    }
}