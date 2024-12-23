namespace trovagiocatoriApp.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public string Sender { get; set; }
    public string Content { get; set; }
    public DateTime Timestamp { get; set; }
}
