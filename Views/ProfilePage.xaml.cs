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

        // NUOVO: Flag per utente admin
        private bool _isAdmin = false;

        // Enum per i tipi di tab
        private enum TabType
        {
            MyPosts,
            MyEvents,
            Favorites,
            AdminPanel // NUOVO: Tab per pannello admin
        }

        public ProfilePage()
        {
            InitializeComponent();

            // Imposta le CollectionView
            FavoritesCollectionView.ItemsSource = FavoritePosts;
            CalendarEventsCollectionView.ItemsSource = CalendarEvents;
            MyPostsCollectionView.ItemsSource = MyPosts;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();

            // MODIFICATO: Carica contenuti solo se non è admin
            if (!_isAdmin)
            {
                await LoadMyPosts();
                await LoadCalendarEvents();
                await LoadFavorites();
            }

            await ConfigureAdminInterface();
        }

        // NUOVO: Configura l'interfaccia per admin
        private async Task ConfigureAdminInterface()
        {
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
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<Models.User>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _isAdmin = user.IsAdmin;

                    if (_isAdmin)
                    {
                        await SetupAdminProfile();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore configurazione admin: {ex.Message}");
            }
        }

        // NUOVO: Configura il profilo per amministratori
        private async Task SetupAdminProfile()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Nascondi i tab normali per admin
                    if (Content is Grid mainGrid)
                    {
                        // Trova il frame dei tab (Grid.Row="1")
                        var tabFrame = mainGrid.Children.OfType<Frame>().FirstOrDefault(f => Grid.GetRow(f) == 1);
                        if (tabFrame?.Content is Grid tabGrid)
                        {
                            // Nascondi tutti i tab normali
                            MyPostsTabButton.IsVisible = false;
                            MyEventsTabButton.IsVisible = false;
                            FavoritesTabButton.IsVisible = false;
                            TabIndicator.IsVisible = false;

                            // Sostituisci con un messaggio admin
                            tabGrid.Children.Clear();
                            var adminLabel = new Label
                            {
                                Text = "👑 PANNELLO AMMINISTRATORE",
                                FontSize = 16,
                                FontAttributes = FontAttributes.Bold,
                                TextColor = Color.FromArgb("#DC2626"),
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            };
                            tabGrid.Children.Add(adminLabel);
                        }

                        // Trova l'area del contenuto (Grid.Row="2") e sostituiscila
                        var contentScrollView = mainGrid.Children.OfType<ScrollView>().FirstOrDefault(s => Grid.GetRow(s) == 2);
                        if (contentScrollView != null)
                        {
                            contentScrollView.Content = CreateAdminContent();
                        }

                        // Modifica i pulsanti in basso
                        var bottomGrid = mainGrid.Children.OfType<Grid>().LastOrDefault();
                        if (bottomGrid != null)
                        {
                            SetupAdminButtons(bottomGrid);
                        }
                    }

                    Debug.WriteLine("[ADMIN] Interfaccia admin configurata");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[ADMIN] Errore setup interfaccia admin: {ex.Message}");
                }
            });
        }

        // NUOVO: Crea il contenuto specifico per admin
        private View CreateAdminContent()
        {
            var adminStack = new VerticalStackLayout
            {
                Spacing = 20,
                Padding = new Thickness(16)
            };

            // Card di benvenuto admin
            var welcomeFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                CornerRadius = 16,
                Padding = new Thickness(20),
                HasShadow = true,
                BorderColor = Color.FromArgb("#DC2626")
            };

            var welcomeContent = new VerticalStackLayout
            {
                Spacing = 16
            };

            welcomeContent.Children.Add(new Label
            {
                Text = "🔧 Pannello Amministratore",
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#DC2626"),
                HorizontalOptions = LayoutOptions.Center
            });

            welcomeContent.Children.Add(new Label
            {
                Text = "Benvenuto nel pannello di amministrazione. Da qui puoi gestire l'intera piattaforma.",
                FontSize = 14,
                TextColor = Color.FromArgb("#64748B"),
                HorizontalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.Center
            });

            // Pulsante per aprire admin panel
            var adminPanelButton = new Button
            {
                Text = "🚀 APRI PANNELLO GESTIONE",
                BackgroundColor = Color.FromArgb("#DC2626"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                CornerRadius = 12,
                HeightRequest = 50,
                Margin = new Thickness(0, 16, 0, 0)
            };

            adminPanelButton.Clicked += async (s, e) =>
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

            welcomeContent.Children.Add(adminPanelButton);
            welcomeFrame.Content = welcomeContent;
            adminStack.Children.Add(welcomeFrame);

            // Card statistiche rapide
            var statsFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                CornerRadius = 16,
                Padding = new Thickness(20),
                HasShadow = true
            };

            var statsContent = new VerticalStackLayout
            {
                Spacing = 16
            };

        
            return adminStack;
        }

        // NUOVO: Crea pulsanti di accesso rapido
        private Button CreateQuickAccessButton(string icon, string text, string colorHex)
        {
            var button = new Button
            {
                Text = $"{icon}\n{text}",
                BackgroundColor = Color.FromArgb(colorHex),
                TextColor = Colors.White,
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 8,
                HeightRequest = 60
            };

            button.Clicked += async (s, e) =>
            {
                // Apri direttamente AdminPage per tutti i pulsanti
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

            return button;
        }

        // NUOVO: Configura i pulsanti per admin
        private void SetupAdminButtons(Grid bottomGrid)
        {
            bottomGrid.Children.Clear();
            bottomGrid.ColumnDefinitions.Clear();

            // Solo un pulsante per admin: Logout
            bottomGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var logoutButton = new Button
            {
                Text = "🚪 LOGOUT AMMINISTRATORE",
                BackgroundColor = Color.FromArgb("#DC2626"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                FontSize = 16,
                CornerRadius = 12,
                HeightRequest = 48,
                Margin = new Thickness(16)
            };

            logoutButton.Clicked += OnAdminLogoutClicked;

            Grid.SetColumn(logoutButton, 0);
            bottomGrid.Children.Add(logoutButton);
        }

        // NUOVO: Logout specifico per admin
        private async void OnAdminLogoutClicked(object sender, EventArgs e)
        {
            try
            {
                var confirm = await DisplayAlert(
                    "Logout Amministratore",
                    "Sei sicuro di voler effettuare il logout dal pannello amministratore?",
                    "Logout",
                    "Annulla"
                );

                if (!confirm) return;

                // Pulisci i dati di sessione
                Preferences.Clear();

                // IMPORTANTE: Resetta anche il flag admin welcome
                HomePage.ResetAdminWelcome();

                Debug.WriteLine($"[ADMIN] Session cleared e flag resettato");

                // Naviga alla pagina di login
                Application.Current.MainPage = new NavigationPage(new LoginPage());

                await DisplayAlert("Logout Completato", "Sei stato disconnesso con successo dal pannello amministratore.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante il logout admin: {ex.Message}");
                await DisplayAlert("Errore", "Errore durante il logout", "OK");
            }
        }

        // ========== METODI ORIGINALI (usati solo per utenti normali) ==========

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

        // ========== GESTIONE TAB (solo per utenti normali) ==========

        private void OnMyPostsTabClicked(object sender, EventArgs e)
        {
            if (_isAdmin) return; // Admin non usa i tab

            if (_activeTab != TabType.MyPosts)
            {
                _activeTab = TabType.MyPosts;
                UpdateTabsUI();
            }
        }

        private void OnMyEventsTabClicked(object sender, EventArgs e)
        {
            if (_isAdmin) return; // Admin non usa i tab

            if (_activeTab != TabType.MyEvents)
            {
                _activeTab = TabType.MyEvents;
                UpdateTabsUI();
            }
        }

        private void OnFavoritesTabClicked(object sender, EventArgs e)
        {
            if (_isAdmin) return; // Admin non usa i tab

            if (_activeTab != TabType.Favorites)
            {
                _activeTab = TabType.Favorites;
                UpdateTabsUI();
            }
        }

        private void UpdateTabsUI()
        {
            if (_isAdmin) return; // Admin non usa i tab

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

        // ========== METODI DI CARICAMENTO (solo per utenti normali) ==========
        // [Resto dei metodi LoadMyPosts, LoadCalendarEvents, etc. rimangono identici]
        // Li ometto per brevità ma vanno mantenuti per gli utenti normali

        private async Task LoadMyPosts()
        {
            if (_isAdmin) return; // Admin non ha "i miei post" 

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

        private async Task LoadCalendarEvents()
        {
            if (_isAdmin) return; // Admin non ha eventi calendario

            try
            {
                Debug.WriteLine("[CALENDAR] Inizio caricamento eventi calendario e inviti");
                await LoadUserParticipations();
                await LoadEventInvites();
                Debug.WriteLine($"[CALENDAR] Caricati {CalendarEvents.Count} eventi totali nel calendario");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Eccezione durante il caricamento eventi: {ex.Message}");
            }
        }

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
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Errore caricamento partecipazioni: {ex.Message}");
            }
        }

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
                            IsInvite = true,
                            InviteID = invite.InviteID
                        };

                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            CalendarEvents.Insert(0, invitePost);
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

        private async Task LoadFavorites()
        {
            if (_isAdmin) return; // Admin non ha preferiti

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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante il caricamento dei preferiti: {ex.Message}");
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

        // ========== METODI HELPER ==========

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

        // ========== GESTIONE SELEZIONI (solo per utenti normali) ==========

        private async void OnFavoriteSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_isAdmin || e.CurrentSelection.FirstOrDefault() is not PostResponse selectedPost) return;

            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new PostDetailMainPage(selectedPost.id));
        }

        private async void OnMyPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_isAdmin || e.CurrentSelection.FirstOrDefault() is not PostResponse selectedPost) return;

            ((CollectionView)sender).SelectedItem = null;
            await Navigation.PushAsync(new PostDetailMainPage(selectedPost.id));
        }

        private async void OnCalendarEventSelected(object sender, SelectionChangedEventArgs e)
        {
            if (_isAdmin || e.CurrentSelection.FirstOrDefault() is not PostResponse selectedEvent) return;

            ((CollectionView)sender).SelectedItem = null;

            if (selectedEvent.IsInvite)
            {
                await HandleEventInviteSelection(selectedEvent);
            }
            else
            {
                await Navigation.PushAsync(new PostDetailMainPage(selectedEvent.id));
            }
        }

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
                    var inviteToRemove = CalendarEvents.FirstOrDefault(e => e.IsInvite && e.InviteID == inviteEvent.InviteID);
                    if (inviteToRemove != null)
                    {
                        CalendarEvents.Remove(inviteToRemove);
                    }

                    await DisplayAlert("Invito Accettato", "Sei ora iscritto all'evento! Puoi vedere i dettagli nella sezione eventi.", "OK");
                    await LoadCalendarEvents();
                    Debug.WriteLine($"[INVITES] ✅ Invito accettato per evento {inviteEvent.id}");
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile accettare l'invito. Riprova più tardi.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore accettazione invito: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'accettazione dell'invito", "OK");
            }
        }

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

        // ========== METODI DI NAVIGAZIONE (solo per utenti normali) ==========

        private async void OnNavigateToChangePassword(object sender, EventArgs e)
        {
            if (_isAdmin) return; // Admin non cambia password da qui

            try
            {
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
            if (_isAdmin) return; // Admin usa il logout specifico

            try
            {
                var confirm = await DisplayAlert(
                    "Logout",
                    "Sei sicuro di voler effettuare il logout?",
                    "Sì",
                    "No"
                );

                if (!confirm) return;

                Preferences.Clear();
                HomePage.ResetAdminWelcome(); // Resetta sempre il flag
                Debug.WriteLine($"[LOGOUT] Session cleared from ProfilePage");

                Application.Current.MainPage = new NavigationPage(new LoginPage());
                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore durante il logout: {ex.Message}");
                await DisplayAlert("Errore", "Errore durante il logout", "OK");
            }
        }
    }
}