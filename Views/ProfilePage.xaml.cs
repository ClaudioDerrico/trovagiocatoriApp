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

        // NUOVO: Lista per gli eventi del calendario
        public ObservableCollection<PostResponse> CalendarEvents { get; set; } = new ObservableCollection<PostResponse>();

        public ProfilePage()
        {
            InitializeComponent();
            FavoritesCollectionView.ItemsSource = FavoritePosts;
            CalendarEventsCollectionView.ItemsSource = CalendarEvents; // NUOVO
        }

        // Ricarica il profilo ogni volta che la pagina diventa visibile
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();
            LoadFavorites();
            LoadCalendarEvents(); // NUOVO
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

        // NUOVO: Metodo per caricare gli eventi del calendario - VERSIONE CORRETTA
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

                        // CORRETTO: Ordina gli eventi per data (eventi futuri per primi, poi passati)
                        // Usa DateTime.TryParse per convertire le stringhe in DateTime prima del confronto
                        var futureEvents = CalendarEvents.Where(e =>
                        {
                            if (DateTime.TryParse(e.data_partita, out DateTime dataPartita))
                            {
                                return dataPartita >= DateTime.Today;
                            }
                            return false; // Se non riesce a fare il parse, consideralo passato
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
                            return true; // Se non riesce a fare il parse, consideralo passato
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

                        // Se ci sono eventi, mostra un messaggio di debug
                        if (CalendarEvents.Count > 0)
                        {
                            var nextEvent = futureEvents.FirstOrDefault();
                            if (nextEvent != null)
                            {
                                if (DateTime.TryParse(nextEvent.data_partita, out DateTime nextEventDate))
                                {
                                    Debug.WriteLine($"[CALENDAR] Prossimo evento: {nextEvent.titolo} il {nextEventDate:dd/MM/yyyy}");
                                }
                            }
                        }
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

        // NUOVO: Metodo per caricare i dettagli di un evento del calendario - VERSIONE CORRETTA
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
                        // CORRETTO: Non modifichiamo le stringhe, le usiamo così come sono
                        // Il binding nel XAML gestirà la visualizzazione attraverso le proprietà computed

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

        // NUOVO: Gestione selezione mio post
        private async void OnMyPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailPage(selectedPost.id));
            }
        }

        // NUOVO: Gestione selezione evento calendario
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