namespace trovagiocatoriApp.Models;

public static class RegionsData
{
    public static readonly Dictionary<string, List<string>> Regions = new()
    {
        { "Abruzzo", new List<string> { "Chieti", "L'Aquila", "Pescara", "Teramo" } },
        { "Basilicata", new List<string> { "Potenza", "Matera" } },
        { "Campania", new List<string> { "Avellino", "Benevento", "Caserta", "Napoli", "Salerno" } },
        { "Lombardia", new List<string> { "Bergamo", "Brescia", "Milano", "Monza e Brianza", "Pavia", "Varese" } },
        // Altre regioni...
    };

    public static readonly List<string> Sports = new()
    {
        "Calcio", "Padel", "Basket", "Pallavolo", "Beach Volley", "Tennis"
    };
}
