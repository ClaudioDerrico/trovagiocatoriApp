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

        // Stato dei tab
        private TabType _activeTab = TabType.MyPosts;

        // Liste per i diversi contenuti
        public ObservableCollection<PostResponse> FavoritePosts { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<PostResponse> CalendarEvents { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<PostResponse> MyPosts { get; set; } = new ObservableCollection<PostResponse>();

        // NUOVO: Collection per gli inviti eventi
        public ObservableCollection<EventInviteInfo> EventInvites { get; set; } = new ObservableCollection<EventInviteInfo>();

        // Enum per i tipi di tab
        private enum TabType
        {
            MyPosts,
            MyEvents,
            Favorites
        }

        public ProfilePage()
        {
            InitializeComponent();

            // Imposta le CollectionView
            FavoritesCollectionView.ItemsSource = FavoritePosts;
            CalendarEventsCollectionView.ItemsSource = CalendarEvents;
            MyPostsCollectionView.ItemsSource = MyPosts;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();
            LoadMyPosts();
            LoadCalendarEvents();
            LoadFavorites();
            CheckAdminAccess();
        }

        // ========== GESTIONE TAB ==========

        private void OnMyPostsTabClicked(object sender, EventArgs e)
        {
            if (_activeTab != TabType.MyPosts)
            {
                _activeTab = TabType.MyPosts;
                UpdateTabsUI();
            }
        }

        private void OnMyEventsTabClicked(object sender, EventArgs e)
        {
            if (_activeTab != TabType.MyEvents)
            {
                _activeTab = TabType.MyEvents;
                UpdateTabsUI();
            }
        }

        private void OnFavoritesTabClicked(object sender, EventArgs e)
        {
            if (_activeTab != TabType.Favorites)
            {
                _activeTab = TabType.Favorites;
                UpdateTabsUI();
            }
        }

        private void UpdateTabsUI()
        {
            // Reset tutti i tab
            MyPostsTabButton.Style = (Style)Resources["TabButtonStyle"];
            MyEventsTabButton.Style = (Style)Resources["TabButtonStyle"];
            FavoritesTabButton.Style = (Style)Resources["TabButtonStyle"];

            // Nascondi tutti i contenuti
            MyPostsContent.IsVisible = false;
            MyEventsContent.IsVisible = false;
            FavoritesContent.IsVisible = false;

            // Attiva il tab selezionato
            switch (_activeTab)
            {
                case TabType.MyPosts:
                    MyPostsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                    MyPostsContent.IsVisible = true;
                    Grid.SetColumn(TabIndicator, 0);
                    break;

                case TabType.MyEvents:
                    MyEventsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                    MyEventsContent.IsVisible = true;
                    Grid.SetColumn(TabIndicator, 1);
                    break;

                case TabType.Favorites:
                    FavoritesTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                    FavoritesContent.IsVisible = true;
                    Grid.SetColumn(TabIndicator, 2);
                    break;
            }
        }

        // ========== CARICAMENTO DATI ==========

        private async void LoadProfile()
        {
            Debug.WriteLine("LoadProfile called");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/profile");

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

        // MODIFICATO: Carica eventi calendario E inviti
        private async void LoadCalendarEvents()
        {
            try
            {
                Debug.WriteLine("[CALENDAR] Inizio caricamento eventi calendario e inviti");

                // Carica eventi normali (partecipazioni)
                await LoadUserParticipations();

                // NUOVO: Carica inviti ricevuti
                await LoadEventInvites();

                Debug.WriteLine($"[CALENDAR] Caricati {CalendarEvents.Count} eventi totali nel calendario");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Eccezione durante il caricamento eventi: {ex.Message}");
                Debug.WriteLine($"[CALENDAR] Stack trace: {ex.StackTrace}");
            }
        }

        // NUOVO: Carica le partecipazioni esistenti
        private async Task LoadUserParticipations()
        {
            try
            {
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
                Debug.WriteLine($"[CALENDAR] Errore caricamento partecipazioni: {ex.Message}");
            }
        }

        // NUOVO: Carica gli inviti eventi ricevuti
        private async Task LoadEventInvites()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/events/invites");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[INVITES] Risposta inviti: {jsonResponse}");

                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result.ContainsKey("invites") && result["invites"] is JsonElement invitesElement)
                    {
                        EventInvites.Clear();

                        foreach (var inviteElement in invitesElement.EnumerateArray())
                        {
                            var invite = new EventInviteInfo
                            {
                                InviteID = GetLongProperty(inviteElement, "invite_id"),
                                PostID = GetIntProperty(inviteElement, "post_id"),
                                Message = GetStringProperty(inviteElement, "message"),
                                CreatedAt = GetStringProperty(inviteElement, "created_at"),
                                Status = GetStringProperty(inviteElement, "status"),
                                SenderUsername = GetStringProperty(inviteElement, "sender_username"),
                                SenderNome = GetStringProperty(inviteElement, "sender_nome"),
                                SenderCognome = GetStringProperty(inviteElement, "sender_cognome"),
                                SenderEmail = GetStringProperty(inviteElement, "sender_email"),
                                SenderProfilePicture = GetStringProperty(inviteElement, "sender_profile_picture")
                            };

                            EventInvites.Add(invite);

                            // Carica anche i dettagli del post per l'invito
                            await LoadInviteEventDetails(invite);
                        }

                        Debug.WriteLine($"[INVITES] Caricati {EventInvites.Count} inviti eventi");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore caricamento inviti: {ex.Message}");
            }
        }

        // NUOVO: Carica i dettagli dell'evento per un invito
        private async Task LoadInviteEventDetails(EventInviteInfo invite)
        {
            try
            {
                var response = await _client.GetAsync($"{pythonApiBaseUrl}/posts/{invite.PostID}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        // Crea un post "speciale" per gli inviti
                        var invitePost = new PostResponse
                        {
                            id = post.id,
                            titolo = $"📩 INVITO: {post.titolo}",
                            provincia = post.provincia,
                            citta = post.citta,
                            sport = post.sport,
                            data_partita = post.data_partita,
                            ora_partita = post.ora_partita,
                            commento = $"Invito da {invite.SenderUsername}: {invite.Message}",
                            autore_email = post.autore_email,
                            campo_id = post.campo_id,
                            campo = post.campo,
                            livello = post.livello,
                            numero_giocatori = post.numero_giocatori,
                            // Aggiungi proprietà per identificare che è un invito
                            IsInvite = true,
                            InviteID = invite.InviteID
                        };

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CalendarEvents.Insert(0, invitePost); // Metti gli inviti in cima
                        });

                        Debug.WriteLine($"[INVITES] Aggiunto invito evento: {post.titolo} da {invite.SenderUsername}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore caricamento dettagli invito evento {invite.PostID}: {ex.Message}");
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

        // ========== METODI HELPER ==========

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

        // NUOVO: Helper per long
        private long GetLongProperty(JsonElement element, string propertyName, long defaultValue = 0)
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetInt64()
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
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

        // ========== GESTIONE SELEZIONI ==========

        private async void OnFavoriteSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailMainPage(selectedPost.id));
            }
        }

        private async void OnMyPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailMainPage(selectedPost.id));
            }
        }

        // MODIFICATO: Gestisce la selezione di eventi normali e inviti
        private async void OnCalendarEventSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedEvent)
            {
                ((CollectionView)sender).SelectedItem = null;

                // Se è un invito, mostra opzioni di accettazione/rifiuto
                if (selectedEvent.IsInvite)
                {
                    await HandleEventInviteSelection(selectedEvent);
                }
                else
                {
                    // Evento normale, apri i dettagli
                    await Navigation.PushAsync(new PostDetailMainPage(selectedEvent.id));
                }
            }
        }

        // NUOVO: Gestisce la selezione di un invito evento
        private async Task HandleEventInviteSelection(PostResponse inviteEvent)
        {
            try
            {
                var action = await DisplayActionSheet(
                    $"Invito da {inviteEvent.autore_email}",
                    "Annulla",
                    null,
                    "✅ Accetta Invito",
                    "❌ Rifiuta Invito",
                    "👀 Visualizza Dettagli Evento"
                );

                switch (action)
                {
                    case "✅ Accetta Invito":
                        await AcceptEventInvite(inviteEvent);
                        break;

                    case "❌ Rifiuta Invito":
                        await RejectEventInvite(inviteEvent);
                        break;

                    case "👀 Visualizza Dettagli Evento":
                        await Navigation.PushAsync(new PostDetailMainPage(inviteEvent.id));
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore gestione selezione invito: {ex.Message}");
                await DisplayAlert("Errore", "Errore nella gestione dell'invito", "OK");
            }
        }

        // NUOVO: Accetta un invito evento
        private async Task AcceptEventInvite(PostResponse inviteEvent)
        {
            try
            {
                var confirm = await DisplayAlert(
                    "Accetta Invito",
                    $"Vuoi accettare l'invito per '{inviteEvent.titolo.Replace("📩 INVITO: ", "")}'? Sarai automaticamente iscritto all'evento.",
                    "Accetta",
                    "Annulla"
                );

                if (!confirm) return;

                var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/events/invite/accept?invite_id={inviteEvent.InviteID}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Rimuovi l'invito dalla lista
                    var inviteToRemove = CalendarEvents.FirstOrDefault(e => e.IsInvite && e.InviteID == inviteEvent.InviteID);
                    if (inviteToRemove != null)
                    {
                        CalendarEvents.Remove(inviteToRemove);
                    }

                    await DisplayAlert("Invito Accettato", "Sei ora iscritto all'evento! Puoi vedere i dettagli nella sezione eventi.", "OK");

                    // Ricarica gli eventi per mostrare il nuovo evento accettato
                    LoadCalendarEvents();

                    Debug.WriteLine($"[INVITES] ✅ Invito accettato per evento {inviteEvent.id}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[INVITES] Errore accettazione invito: {errorContent}");
                    await DisplayAlert("Errore", "Impossibile accettare l'invito. Riprova più tardi.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore accettazione invito: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'accettazione dell'invito", "OK");
            }
        }

        // NUOVO: Rifiuta un invito evento
        private async Task RejectEventInvite(PostResponse inviteEvent)
        {
            try
            {
                var confirm = await DisplayAlert(
                    "Rifiuta Invito",
                    $"Vuoi rifiutare l'invito per '{inviteEvent.titolo.Replace("📩 INVITO: ", "")}'?",
                    "Rifiuta",
                    "Annulla"
                );

                if (!confirm) return;

                var request = new HttpRequestMessage(HttpMethod.Post, $"{apiBaseUrl}/events/invite/reject?invite_id={inviteEvent.InviteID}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Rimuovi l'invito dalla lista
                    var inviteToRemove = CalendarEvents.FirstOrDefault(e => e.IsInvite && e.InviteID == inviteEvent.InviteID);
                    if (inviteToRemove != null)
                    {
                        CalendarEvents.Remove(inviteToRemove);
                    }

                    await DisplayAlert("Invito Rifiutato", "Invito rifiutato con successo.", "OK");

                    Debug.WriteLine($"[INVITES] ❌ Invito rifiutato per evento {inviteEvent.id}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[INVITES] Errore rifiuto invito: {errorContent}");
                    await DisplayAlert("Errore", "Impossibile rifiutare l'invito. Riprova più tardi.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore rifiuto invito: {ex.Message}");
                await DisplayAlert("Errore", "Errore nel rifiuto dell'invito", "OK");
            }
        }
        private async void OnAcceptInviteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PostResponse inviteEvent)
            {
                await AcceptEventInvite(inviteEvent);
            }
        }

        private async void OnRejectInviteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PostResponse inviteEvent)
            {
                await RejectEventInvite(inviteEvent);
            }
        }

        private async void OnNavigateToChangePassword(object sender, EventArgs e)
        {
            try
            {
                // Naviga alla pagina di cambio password
                await Navigation.PushAsync(new ChangePasswordPage());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore navigazione cambio password: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire la pagina di cambio password", "OK");
            }
        }
        private async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var confirm = await DisplayAlert(
                    "Logout",
                    "Sei sicuro di voler effettuare il logout?",
                    "Sì",
                    "No"
                );

                if (!confirm) return;

                // Pulisci i dati di sessione
                Preferences.Clear();

                Debug.WriteLine($"[LOGOUT] Session cleared from ProfilePage");

                // Naviga alla pagina di login
                Application.Current.MainPage = new NavigationPage(new LoginPage());

                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante il logout: {ex.Message}");
                await DisplayAlert("Errore", "Errore durante il logout", "OK");
            }
        }

        private async Task CheckAdminAccess()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/profile");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<Models.User>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    // Se è admin, mostra il pulsante
                    if (user.IsAdmin)
                    {
                        ShowAdminButton();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore controllo admin: {ex.Message}");
            }
        }

        private void ShowAdminButton()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Crea il pulsante admin
                    var adminButton = new Button
                    {
                        Text = "🔧 PANNELLO AMMINISTRATORE",
                        BackgroundColor = Color.FromArgb("#DC2626"),
                        TextColor = Colors.White,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 16,
                        Margin = new Thickness(16, 10),
                        CornerRadius = 12,
                        HeightRequest = 50
                    };

                    adminButton.Clicked += async (s, e) =>
                    {
                        try
                        {
                            await Navigation.PushAsync(new AdminPage());
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"Errore apertura AdminPage: {ex.Message}");
                            await DisplayAlert("Errore", "Impossibile aprire il pannello amministratore", "OK");
                        }
                    };

                    // Trova il Grid principale e aggiungi il pulsante
                    if (Content is Grid mainGrid)
                    {
                        // Il pulsante va nella riga 3 (Bottom Buttons), modificando la struttura esistente
                        var bottomGrid = mainGrid.Children.OfType<Grid>().LastOrDefault();
                        if (bottomGrid != null)
                        {
                            // Crea un nuovo StackLayout per contenere sia i pulsanti esistenti che quello admin
                            var buttonStack = new VerticalStackLayout
                            {
                                Spacing = 8,
                                Padding = new Thickness(16)
                            };

                            // Aggiungi prima il pulsante admin
                            buttonStack.Children.Add(adminButton);

                            // Crea un container per i pulsanti esistenti
                            var existingButtonsGrid = new Grid
                            {
                                ColumnDefinitions =
                        {
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                            new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                        },
                                ColumnSpacing = 12
                            };

                            // Crea i pulsanti esistenti
                            var changePasswordButton = new Button
                            {
                                Text = "🔒 Cambia Password",
                                BackgroundColor = Color.FromArgb("#5B9CFD"),
                                TextColor = Colors.White,
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 14,
                                CornerRadius = 12,
                                HeightRequest = 48
                            };
                            changePasswordButton.Clicked += OnNavigateToChangePassword;

                            var logoutButton = new Button
                            {
                                Text = "🚪 Logout",
                                BackgroundColor = Color.FromArgb("#FF5A5F"),
                                TextColor = Colors.White,
                                FontAttributes = FontAttributes.Bold,
                                FontSize = 14,
                                CornerRadius = 12,
                                HeightRequest = 48
                            };
                            logoutButton.Clicked += OnLogoutButtonClicked;

                            Grid.SetColumn(changePasswordButton, 0);
                            Grid.SetColumn(logoutButton, 1);

                            existingButtonsGrid.Children.Add(changePasswordButton);
                            existingButtonsGrid.Children.Add(logoutButton);

                            buttonStack.Children.Add(existingButtonsGrid);

                            // Sostituisci il contenuto della riga dei pulsanti
                            bottomGrid.Children.Clear();
                            bottomGrid.Children.Add(buttonStack);
                        }
                    }

                    Debug.WriteLine("[ADMIN] Pulsante amministratore aggiunto al profilo");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ADMIN] Errore nell'aggiunta del pulsante admin: {ex.Message}");

                    // Fallback: mostra un alert
                    DisplayAlert("Amministratore", "Hai privilegi di amministratore! Il pannello admin sarà disponibile a breve.", "OK");
                }
            });
        }

    }
}
    