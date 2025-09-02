using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class Comment
    {
        public int id { get; set; }
        public int post_id { get; set; }
        public string autore_email { get; set; }
        public string contenuto { get; set; }
        public DateTime created_at { get; set; }
        public string autore_username { get; set; }
        public string autore_nome { get; set; }
        public string autore_cognome { get; set; }

        public bool IsAuthorComment { get; set; }

        // Proprietà computed per il display name
        public string DisplayName =>
            !string.IsNullOrEmpty(autore_username) ? autore_username :
            !string.IsNullOrEmpty(autore_nome) && !string.IsNullOrEmpty(autore_cognome) ? $"{autore_nome} {autore_cognome}" :
            autore_email;
    }

    public class CommentCreate
    {
        public string contenuto { get; set; }
    }
}