using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
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
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;
        private readonly string _pythonApiBaseUrl = ApiConfig.PythonApiUrl; // o SpecificPostUrl se non hai rinominato

        // ObservableCollection per i commenti
        public ObservableCollection<Comment> Comments { get; set; } = new ObservableCollection<Comment>();

        public PostDetailPage(int postId)
        {
            InitializeComponent();
            _postId = postId;
            BindingContext = this;
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
            await LoadCommentsAsync();
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
            var response = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/posts/{_postId}");
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
                $"{_apiBaseUrl}/api/user/by-email?email={encodedEmail}");

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

            ProfileImage.Source = !string.IsNullOrEmpty(user.ProfilePic)
                ? $"{_apiBaseUrl}/images/{user.ProfilePic}"
                : "default_images.jpg";

            // Dati post
            TitoloLabel.Text = post.titolo;
            DataOraLabel.Text = $"{post.data_partita} alle {post.ora_partita}";
            PosizioneLabel.Text = $"{post.citta} ({post.provincia})";
            CommentoLabel.Text = post.commento;
        }

        private async Task LoadCommentsAsync()
        {
            try
            {
                // Aggiungi il cookie di sessione alla richiesta
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_pythonApiBaseUrl}/posts/{_postId}/comments/");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var comments = JsonSerializer.Deserialize<List<Comment>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Aggiorna la collezione dei commenti
                    Comments.Clear();
                    foreach (var comment in comments)
                    {
                        // Recupera il username per ogni commento
                        comment.autore_username = await GetUsernameByEmail(comment.autore_email);
                        Comments.Add(comment);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback silenzioso - i commenti non si caricano ma l'app non crasha
            }
        }

        private async Task<string> GetUsernameByEmail(string email)
        {
            try
            {
                var encodedEmail = Uri.EscapeDataString(email);

                // QUI! Chiamo l'auth-service Go per ottenere i dati utente
                var response = await _sharedClient.GetAsync($"{_apiBaseUrl}/api/user/by-email?email={encodedEmail}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return user?.Username ?? email; // Fallback alla mail se non trova username
                }

                return email; // Fallback alla mail
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel recupero username per {email}: {ex.Message}");
                return email; // Fallback alla mail
            }
        }

        private async void OnInviaRispostaClicked(object sender, EventArgs e)
        {
            string messaggio = RispostaEditor.Text;
            if (string.IsNullOrWhiteSpace(messaggio))
            {
                await DisplayAlert("Attenzione", "Il messaggio non può essere vuoto.", "OK");
                return;
            }

            try
            {
                // Crea l'oggetto commento
                var commentCreate = new CommentCreate
                {
                    contenuto = messaggio
                };

                // Crea la richiesta HTTP
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_pythonApiBaseUrl}/posts/{_postId}/comments/");

                // Aggiungi il cookie di sessione
                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }
                else
                {
                    await DisplayAlert("Errore", "Sessione non trovata. Effettua di nuovo il login.", "OK");
                    return;
                }

                // Serializza il contenuto
                var json = JsonSerializer.Serialize(commentCreate);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Invia la richiesta
                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Successo", "Commento inviato!", "OK");
                    RispostaEditor.Text = string.Empty; // Pulisci l'editor

                    // Ricarica i commenti per mostrare quello appena aggiunto
                    await LoadCommentsAsync();
                }
                else
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Errore", "Impossibile inviare il commento.", "OK");
                }
            }
            catch (HttpRequestException)
            {
                await DisplayAlert("Errore di Connessione", "Impossibile raggiungere il server.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
        }
    }
}