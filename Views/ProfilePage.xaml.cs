using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string apiBaseUrl = ApiConfig.BaseUrl;
        private readonly string pythonApiBaseUrl = ApiConfig.PythonApiUrl;

        // Lista per i preferiti
        public ObservableCollection<PostResponse> FavoritePosts { get; set; } = new ObservableCollection<PostResponse>();

        // Lista per gli eventi del calendario
        public ObservableCollection<PostResponse> CalendarEvents { get; set; } = new ObservableCollection<PostResponse>();

        // NUOVO: Lista per i miei post
        public ObservableCollection<PostResponse> MyPosts { get; set; } = new ObservableCollection<PostResponse>();

        public ProfilePage()
        {
            InitializeComponent();
            FavoritesCollectionView.ItemsSource = FavoritePosts;
            CalendarEventsCollectionView.ItemsSource = CalendarEvents;
            MyPostsCollectionView.ItemsSource = MyPosts; // NUOVO
        }

        // Ricarica il profilo ogni volta che la pagina diventa visibile
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();
            LoadFavorites();
            LoadCalendarEvents();
            LoadMyPosts(); // NUOVO: Carica i miei post
        }

        private async void LoadProfile()
        {
            Debug.WriteLine("LoadProfile called");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/profile");

                // Recupera il cookie di sessione salvato nelle Preferences
                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"jsonResponse: {jsonResponse}");
                    var userProfile = JsonSerializer.Deserialize<User>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (userProfile != null)
                    {
                        Debug.WriteLine($"Profilo Utente Caricato: {userProfile.Username}");
                        UsernameLabel.Text = userProfile.Username;
                        NameLabel.Text = userProfile.Nome;
                        SurnameLabel.Text = userProfile.Cognome;
                        EmailLabel.Text = userProfile.Email;

                        // Se il profilo contiene un'immagine, la carichiamo; altrimenti, usiamo un'immagine predefinita
                        ProfileImage.Source = !string.IsNullOrEmpty(userProfile.ProfilePic)
                            ? $"{apiBaseUrl}/images/{userProfile.ProfilePic}"
                            : "default_images.jpg";
                    }
                    else
                    {
                        Debug.WriteLine("Errore: userProfile è null.");
                        await DisplayAlert("Errore", "Profilo utente non valido.", "OK");
                    }
                }
                else
                {
                    Debug.WriteLine($"Errore: Stato della risposta {response.StatusCode}");
                    await DisplayAlert("Errore", "Impossibile caricare il profilo.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante il caricamento del profilo: {ex.Message}");
                await DisplayAlert("Errore", $"Errore durante il caricamento: {ex.Message}", "OK");
            }
        }

        // NUOVO: Metodo per caricare i miei post
        private async void LoadMyPosts()
        {
            try
            {
                Debug.WriteLine("[MY_POSTS] Inizio caricamento i miei post");

                var request = new HttpRequestMessage(HttpMethod.Get, $"{pythonApiBaseUrl}/posts/by-user");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[MY_POSTS] Risposta API: {jsonResponse}");

                    // Deserializza come lista di JsonElement per gestire le proprietà aggiuntive
                    var jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(jsonResponse);

                    MyPosts.Clear();

                    foreach (var element in jsonElements ?? new List<JsonElement>())
                    {
                        var post = new PostResponse
                        {
                            id = GetIntProperty(element, "id"),
                            titolo = GetStringProperty(element, "titolo"),
                            provincia = GetStringProperty(element, "provincia"),
                            citta = GetStringProperty(element, "citta"),
                            sport = GetStringProperty(element, "sport"),
                            data_partita = GetStringProperty(element, "data_partita"),
                            ora_partita = GetStringProperty(element, "ora_partita"),
                            commento = GetStringProperty(element, "commento"),
                            autore_email = GetStringProperty(element, "autore_email"),
                            campo_id = GetNullableIntProperty(element, "campo_id"),
                            campo = GetCampoProperty(element),
                            livello = GetStringProperty(element, "livello", "Intermedio"),
                            numero_giocatori = GetIntProperty(element, "numero_giocatori", 1),
                            partecipanti_iscritti = GetIntProperty(element, "partecipanti_iscritti", 0),
                            posti_disponibili = GetIntProperty(element, "posti_disponibili", 1)
                        };

                        MyPosts.Add(post);
                    }

                    Debug.WriteLine($"[MY_POSTS] Caricati {MyPosts.Count} post dell'utente");
                }
                else
                {
                    Debug.WriteLine($"[MY_POSTS] Errore nel caricamento post: {response.StatusCode}");
                    MyPosts.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MY_POSTS] Eccezione durante il caricamento post: {ex.Message}");
                MyPosts.Clear();
            }
        }

        // Metodi helper per estrarre proprietà dal JsonElement
        private string GetStringProperty(JsonElement element, string propertyName, string defaultValue = "")
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetString() ?? defaultValue
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetInt32()
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private int? GetNullableIntProperty(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Null)
                        return null;
                    return prop.GetInt32();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private CampoInfo GetCampoProperty(JsonElement element)
        {
            try
            {
                if (element.TryGetProperty("campo", out var campoProp) && campoProp.ValueKind != JsonValueKind.Null)
                {
                    return new CampoInfo
                    {
                        nome = GetStringProperty(campoProp, "nome"),
                        indirizzo = GetStringProperty(campoProp, "indirizzo")
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private async void LoadFavorites()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/favorites");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result.ContainsKey("favorites") && result["favorites"] is JsonElement favoritesElement)
                    {
                        var favoriteIds = favoritesElement.EnumerateArray()
                            .Select(x => x.GetInt32())
                            .ToList();

                        Debug.WriteLine($"Caricati {favoriteIds.Count} preferiti");

                        // Carica i dettagli di ogni post preferito
                        FavoritePosts.Clear();
                        foreach (var postId in favoriteIds)
                        {
                            await LoadFavoritePostDetails(postId);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Errore nel caricamento preferiti: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante il caricamento dei preferiti: {ex.Message}");
            }
        }

        // Metodo per caricare gli eventi del calendario
        private async void LoadCalendarEvents()
        {
            try
            {
                Debug.WriteLine("[CALENDAR] Inizio caricamento eventi calendario");

                // Ottieni l'elenco degli eventi a cui l'utente partecipa dall'auth-service
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/user/participations");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[CALENDAR] Risposta partecipazioni: {jsonResponse}");

                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result.ContainsKey("participations") && result["participations"] is JsonElement participationsElement)
                    {
                        var participationIds = participationsElement.EnumerateArray()
                            .Select(x => x.GetInt32())
                            .ToList();

                        Debug.WriteLine($"[CALENDAR] Trovate {participationIds.Count} partecipazioni");

                        // Carica i dettagli di ogni evento a cui l'utente partecipa
                        CalendarEvents.Clear();

                        var loadTasks = participationIds.Select(LoadCalendarEventDetails);
                        await Task.WhenAll(loadTasks);

                        // Ordina gli eventi per data
                        var futureEvents = CalendarEvents.Where(e =>
                        {
                            if (DateTime.TryParse(e.data_partita, out DateTime dataPartita))
                            {
                                return dataPartita >= DateTime.Today;
                            }
                            return false;
                        })
                        .OrderBy(e => DateTime.TryParse(e.data_partita, out DateTime d1) ? d1 : DateTime.MinValue)
                        .ThenBy(e => TimeSpan.TryParse(e.ora_partita, out TimeSpan t1) ? t1 : TimeSpan.Zero)
                        .ToList();

                        var pastEvents = CalendarEvents.Where(e =>
                        {
                            if (DateTime.TryParse(e.data_partita, out DateTime dataPartita))
                            {
                                return dataPartita < DateTime.Today;
                            }
                            return true;
                        })
                        .OrderByDescending(e => DateTime.TryParse(e.data_partita, out DateTime d2) ? d2 : DateTime.MinValue)
                        .ThenByDescending(e => TimeSpan.TryParse(e.ora_partita, out TimeSpan t2) ? t2 : TimeSpan.Zero)
                        .ToList();

                        CalendarEvents.Clear();

                        // Aggiungi prima gli eventi futuri, poi quelli passati
                        foreach (var eventItem in futureEvents.Concat(pastEvents))
                        {
                            CalendarEvents.Add(eventItem);
                        }

                        Debug.WriteLine($"[CALENDAR] Caricati e ordinati {CalendarEvents.Count} eventi nel calendario");
                    }
                    else
                    {
                        Debug.WriteLine("[CALENDAR] Nessuna partecipazione trovata nel JSON");
                        CalendarEvents.Clear();
                    }
                }
                else
                {
                    Debug.WriteLine($"[CALENDAR] Errore nel caricamento partecipazioni: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[CALENDAR] Contenuto errore: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Eccezione durante il caricamento eventi: {ex.Message}");
                Debug.WriteLine($"[CALENDAR] Stack trace: {ex.StackTrace}");
            }
        }

        private async Task LoadFavoritePostDetails(int postId)
        {
            try
            {
                var response = await _client.GetAsync($"{pythonApiBaseUrl}/posts/{postId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        FavoritePosts.Add(post);
                        Debug.WriteLine($"Aggiunto post preferito: {post.titolo}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel caricamento dettagli post {postId}: {ex.Message}");
            }
        }

        private async Task LoadCalendarEventDetails(int postId)
        {
            try
            {
                var response = await _client.GetAsync($"{pythonApiBaseUrl}/posts/{postId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CalendarEvents.Add(post);
                        });

                        Debug.WriteLine($"[CALENDAR] Aggiunto evento calendario: {post.titolo} - {post.data_partita} {post.ora_partita}");
                    }
                }
                else
                {
                    Debug.WriteLine($"[CALENDAR] Errore nel caricamento post {postId}: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Errore nel caricamento dettagli evento {postId}: {ex.Message}");
            }
        }

        // GESTIONE SELEZIONE PREFERITO
        private async void OnFavoriteSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailPage(selectedPost.id));
            }
        }

        // Gestione selezione mio post
        private async void OnMyPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailPage(selectedPost.id));
            }
        }

        // Gestione selezione evento calendario
        private async void OnCalendarEventSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedEvent)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailPage(selectedEvent.id));
            }
        }

        private async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Sei sicuro di voler effettuare il logout?", "Sì", "No");
            if (confirm)
            {
                if (Preferences.ContainsKey("session_id"))
                {
                    Preferences.Remove("session_id");
                }

                Application.Current.MainPage = new NavigationPage(new LoginPage());

                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }

        private async void OnNavigateToChangePassword(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ChangePasswordPage());
        }
    }
}