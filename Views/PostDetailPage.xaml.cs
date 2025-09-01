using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostDetailPage : ContentPage, INotifyPropertyChanged
    {
        private readonly int _postId;
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;
        private readonly string _pythonApiBaseUrl = ApiConfig.PythonApiUrl;

        // Stato del preferito
        private bool _isFavorite = false;

        // ObservableCollection per i commenti
        public ObservableCollection<Comment> Comments { get; set; } = new ObservableCollection<Comment>();

        // Proprietà per il campo da calcio
        private SportField _campo;
        public SportField Campo
        {
            get => _campo;
            set
            {
                _campo = value;
                OnPropertyChanged();
            }
        }

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
            await CheckFavoriteStatusAsync();
        }

        private async Task LoadPostDetailAsync()
        {
            try
            {
                // 1. Carica i dati del post
                var post = await LoadPostDataAsync();

                // 2. Carica i dati dell'utente
                var user = await LoadUserDataAsync(post.autore_email);

                // 3. Carica le informazioni del campo se presente
                if (post.campo_id.HasValue)
                {
                    Campo = await LoadSportFieldAsync(post.campo_id.Value);
                }

                // 4. Aggiorna la UI
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

        private async Task<SportField> LoadSportFieldAsync(int fieldId)
        {
            try
            {
                var response = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/fields/{fieldId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SportField>(
                        json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel caricamento del campo: {ex.Message}");
            }

            return null;
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
            CommentoLabel.Text = post.commento;
        }

        // NUOVE FUNZIONI PER I PREFERITI

        private async Task CheckFavoriteStatusAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/favorites/check/{_postId}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (result.ContainsKey("is_favorite") && result["is_favorite"] is JsonElement element)
                    {
                        _isFavorite = element.GetBoolean();
                        UpdateFavoriteIcon();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel controllo preferiti: {ex.Message}");
            }
        }

        private async void OnFavoriteButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _isFavorite ? $"{_apiBaseUrl}/favorites/remove" : $"{_apiBaseUrl}/favorites/add"
                );

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var payload = new { post_id = _postId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _isFavorite = !_isFavorite;
                    UpdateFavoriteIcon();

                    var message = _isFavorite ? "Aggiunto ai preferiti!" : "Rimosso dai preferiti!";
                    await DisplayAlert("Preferiti", message, "OK");
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile aggiornare i preferiti", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
        }

        private void UpdateFavoriteIcon()
        {
            FavoriteButton.Source = _isFavorite ? "heart_filled.png" : "heart_empty.png";
        }

        // Handler per visualizzare il campo sulla mappa
        private async void OnViewOnMapClicked(object sender, EventArgs e)
        {
            if (Campo == null)
            {
                await DisplayAlert("Info", "Coordinate o indirizzo del campo non disponibili.", "OK");
                return;
            }

            // Preferisci usare le coordinate se presenti
            bool hasCoords = Campo.Lat != 0 && Campo.Lng != 0; // o altra condizione valida per te
            string nameEscaped = Uri.EscapeDataString(Campo.Nome ?? "Posizione");


            try
            {
                string uriString;

                if (hasCoords)
                {
                    // Apri Google Maps con coordinate (funziona sempre)
                    string latStr = Campo.Lat.ToString(CultureInfo.InvariantCulture);
                    string lngStr = Campo.Lng.ToString(CultureInfo.InvariantCulture);
                    uriString = $"https://www.google.com/maps/search/?api=1&query={latStr},{lngStr}";
                }
                else
                {
                    // Se non hai coordinate, usa l'indirizzo
                    string address = Campo.Indirizzo ?? Campo.Nome ?? "";
                    if (string.IsNullOrWhiteSpace(address))
                    {
                        await DisplayAlert("Info", "Impossibile ottenere posizione o indirizzo del campo.", "OK");
                        return;
                    }

                    uriString = $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString(address)}";
                }

                await Launcher.OpenAsync(new Uri(uriString));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fallback map open failed: {ex.Message}");
                await DisplayAlert("Errore", $"Impossibile aprire la mappa: {ex.Message}", "OK");
            }
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
                    foreach (var comment in comments ?? new List<Comment>())
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

                var response = await _sharedClient.GetAsync($"{_apiBaseUrl}/api/user/by-email?email={encodedEmail}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return user?.Username ?? email;
                }

                return email;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel recupero username per {email}: {ex.Message}");
                return email;
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

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Implementazione INotifyPropertyChanged
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}