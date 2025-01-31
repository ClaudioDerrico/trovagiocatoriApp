using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Controls;

namespace trovagiocatoriApp.Views
{
    public partial class PostPage : ContentPage
    {
        public ObservableCollection<Post> Posts { get; set; }

        public PostPage()
        {
            InitializeComponent();
            Posts = new ObservableCollection<Post>();
            BindingContext = this;

            // Carica i post all'avvio della pagina
            LoadPosts();
        }

        private async void LoadPosts()
        {
            // Recupera provincia e sport usando Preferences
            string provincia = Preferences.Get("provincia", string.Empty);
            string sport = Preferences.Get("opzionesport", string.Empty);

            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync($"https://example.com/forum/provincia/allpost/{provincia}/{sport}");
                    var posts = JsonSerializer.Deserialize<List<Post>>(response);

                    if (posts != null && posts.Count > 0)
                    {
                        foreach (var post in posts)
                        {
                            Posts.Add(post);
                        }
                    }
                    else
                    {
                        // Se non ci sono post, mostra un messaggio
                        var emptyPost = new Post { Titolo = "Nessun post disponibile" };
                        Posts.Add(emptyPost);
                    }
                }
            }
            catch (Exception ex)
            {
                // Gestisci eventuali errori di rete o API
                await DisplayAlert("Errore", $"Si è verificato un errore: {ex.Message}", "OK");
            }
        }



    }

    // Modello di Post
    public class Post
    {
        public string Titolo { get; set; }
        public string NomeUtente { get; set; }
        public string DataPartita { get; set; }
        public string Citta { get; set; }
        public string DataAttuale { get; set; }
    }
}
