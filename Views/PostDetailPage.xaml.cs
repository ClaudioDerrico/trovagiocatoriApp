using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostDetailPage : ContentPage
    {
        private readonly int _postId;
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = "http://localhost:8080";

        public PostDetailPage(int postId)
        {
            InitializeComponent();
            _postId = postId;
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true  // Abilita la gestione automatica dei cookie
            };
            return new HttpClient(handler);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPostDetailAsync();
        }

        private async Task LoadPostDetailAsync()
        {
            try
            {
                // 1. Carica i dati del post
                var post = await LoadPostDataAsync();

                // 2. Carica i dati dell'utente
                var user = await LoadUserDataAsync(post.autore_email);

                // 3. Aggiorna la UI
                UpdateUI(post, user);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", ex.Message, "OK");
            }
        }

        private async Task<PostResponse> LoadPostDataAsync()
        {
            var response = await _sharedClient.GetAsync($"http://localhost:8000/posts/{_postId}");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Post non trovato");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PostResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }



        private async Task<User> LoadUserDataAsync(string email)
        {
            var encodedEmail = Uri.EscapeDataString(email);

            var response = await _sharedClient.GetAsync(
                $"http://localhost:8080/api/user/by-email?email={encodedEmail}");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Utente non trovato");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private void UpdateUI(PostResponse post, User user)
        {
            // Dati utente

            AutoreLabel.Text = $"{user.Username}";

            // Stampa tutte le info contenute in user come JSON
            string userJson = JsonSerializer.Serialize(user, new JsonSerializerOptions { WriteIndented = true });
            Debug.WriteLine("[DEBUG] USER INFO:\n" + userJson);

            ProfileImage.Source = !string.IsNullOrEmpty(user.ProfilePic)
                ? $"{_apiBaseUrl}/images/{user.ProfilePic}"
                : "default_images.jpg";

            // Dati post
            TitoloLabel.Text = post.titolo;
            DataOraLabel.Text = $"{post.data_partita} alle {post.ora_partita}";
            PosizioneLabel.Text = $"{post.citta} ({post.provincia})";
            CommentoLabel.Text = post.commento;
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            if (Navigation.NavigationStack.Count > 1)
            {
                await Navigation.PopAsync();
            }
            // Se questa è la prima pagina dello stack (improbabile per una pagina di dettaglio),
            // potresti voler gestire diversamente la chiusura o la navigazione.
        }

        private void OnInviaRispostaClicked(object sender, EventArgs e)
        {
            string messaggio = RispostaEditor.Text;
            if (string.IsNullOrWhiteSpace(messaggio))
            {
                DisplayAlert("Attenzione", "Il messaggio non può essere vuoto.", "OK");
                return;
            }
            // Qui logica per inviare il messaggio/risposta
            DisplayAlert("Successo", "Messaggio inviato!", "OK"); // Esempio
            RispostaEditor.Text = string.Empty; // Pulisci l'editor
        }

        // Qui potresti avere il costruttore che accetta un oggetto Post
        // public PostDetailPage(MyPostObject post)
        // {
        //     InitializeComponent();
        //     BindingContext = post; // Se usi il binding
        //     // o assegna manualmente i valori come sopra
        // }
   
    }
}