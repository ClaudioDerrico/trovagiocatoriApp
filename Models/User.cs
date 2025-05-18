using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models;

public class User
{
    public int Id { get; set; }
    public string Nome { get; set; }
    public string Cognome { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    [JsonPropertyName("profile_picture")]
    public string ProfilePic { get; set; }
   
}
