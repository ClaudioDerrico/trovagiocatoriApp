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

        // Flag per tipo utente
        private bool _isAdmin = false;

        // Liste per i diversi contenuti (solo per utenti normali)
        public ObservableCollection<PostResponse> FavoritePosts { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<PostResponse> CalendarEvents { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<PostResponse> MyPosts { get; set; } = new ObservableCollection<PostResponse>();

        // Collection per gli inviti eventi
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

            // Imposta le CollectionView per utenti normali
            FavoritesCollectionView.ItemsSource = FavoritePosts;
            CalendarEventsCollectionView.ItemsSource = CalendarEvents;
            MyPostsCollectionView.ItemsSource = MyPosts;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadProfile();
            await CheckAndSetupUserType();
        }

        // ========== SETUP TIPO UTENTE ==========

        private async Task CheckAndSetupUserType()
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
                        Debug.WriteLine("[PROFILE] ✅ Configurato profilo ADMIN");
                    }
                    else
                    {
                        await SetupUserProfile();
                        Debug.WriteLine("[PROFILE] ✅ Configurato profilo USER");
                    }
                }
                else
                {
                    // Default: tratta come utente normale
                    await SetupUserProfile();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROFILE] Errore setup tipo utente: {ex.Message}");
                // Fallback: setup utente normale
                await SetupUserProfile();
            }
        }

        private async Task SetupUserProfile()
        {
            // Carica tutti i dati per utenti normali
            await LoadMyPosts();
            await LoadCalendarEvents();
            await LoadFavorites();

            // Mantieni l'interfaccia tab normale (è già così di default)
            Debug.WriteLine("[PROFILE] Setup utente normale completato");
        }

        private async Task SetupAdminProfile()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // 1. NASCONDI I TAB UTENTE NORMALI
                    if (MyPostsTabButton != null) MyPostsTabButton.IsVisible = false;
                    if (MyEventsTabButton != null) MyEventsTabButton.IsVisible = false;
                    if (FavoritesTabButton != null) FavoritesTabButton.IsVisible = false;
                    if (TabIndicator != null) TabIndicator.IsVisible = false;

                    // 2. SOSTITUISCI L'AREA TAB CON MESSAGGIO ADMIN
                    if (Content is Grid mainGrid)
                    {
                        var tabFrame = mainGrid.Children.OfType<Frame>().FirstOrDefault(f => Grid.GetRow(f) == 1);
                        if (tabFrame?.Content is Grid tabGrid)
                        {
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

                        // 3. SOSTITUISCI IL CONTENUTO CON INTERFACCIA ADMIN
                        var contentScrollView = mainGrid.Children.OfType<ScrollView>().FirstOrDefault(s => Grid.GetRow(s) == 2);
                        if (contentScrollView != null)
                        {
                            contentScrollView.Content = CreateAdminContent();
                        }

                        // 4. MODIFICA I PULSANTI IN BASSO
                        var bottomGrid = mainGrid.Children.OfType<Grid>().LastOrDefault();
                        if (bottomGrid != null)
                        {
                            SetupAdminButtons(bottomGrid);
                        }
                    }

                    Debug.WriteLine("[PROFILE] ✅ Interfaccia admin configurata");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PROFILE] Errore setup interfaccia admin: {ex.Message}");
                }
            });
        }

        // ========== INTERFACCIA ADMIN ==========

        private View CreateAdminContent()
        {
            var adminStack = new VerticalStackLayout
            {
                Spacing = 20,
                Padding = new Thickness(16)
            };

            // CARD BENVENUTO ADMIN
            var welcomeFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                CornerRadius = 16,
                Padding = new Thickness(20),
                HasShadow = true,
                BorderColor = Color.FromArgb("#DC2626")
            };

            var welcomeContent = new VerticalStackLayout { Spacing = 16 };

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

            // PULSANTE PRINCIPALE ADMIN
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
                    Debug.WriteLine($"[PROFILE] Errore apertura AdminPage: {ex.Message}");
                    await DisplayAlert("Errore", "Impossibile aprire il pannello amministratore", "OK");
                }
            };

            welcomeContent.Children.Add(adminPanelButton);
            welcomeFrame.Content = welcomeContent;
            adminStack.Children.Add(welcomeFrame);

            // CARD ACCESSI RAPIDI
            var quickAccessFrame = new Frame
            {
                BackgroundColor = Color.FromArgb("#FFFFFF"),
                CornerRadius = 16,
                Padding = new Thickness(20),
                HasShadow = true
            };

            var quickAccessContent = new VerticalStackLayout { Spacing = 16 };

            quickAccessContent.Children.Add(new Label
            {
                Text = "⚡ Accesso Rapido",
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1E293B"),
                HorizontalOptions = LayoutOptions.Start
            });

            // GRIGLIA PULSANTI RAPIDI
            var quickButtonsGrid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(60, GridUnitType.Absolute) },
                    new RowDefinition { Height = new GridLength(60, GridUnitType.Absolute) }
                },
                ColumnSpacing = 12,
                RowSpacing = 12
            };

            // Pulsanti di accesso rapido
            var postsButton = CreateQuickButton("📝", "Gestisci Post", "#6366F1");
            var commentsButton = CreateQuickButton("💬", "Commenti", "#10B981");
            var usersButton = CreateQuickButton("👥", "Utenti", "#F59E0B");
            var statsButton = CreateQuickButton("📊", "Statistiche", "#DC2626");

            Grid.SetColumn(postsButton, 0);
            Grid.SetRow(postsButton, 0);
            Grid.SetColumn(commentsButton, 1);
            Grid.SetRow(commentsButton, 0);
            Grid.SetColumn(usersButton, 0);
            Grid.SetRow(usersButton, 1);
            Grid.SetColumn(statsButton, 1);
            Grid.SetRow(statsButton, 1);

            quickButtonsGrid.Children.Add(postsButton);
            quickButtonsGrid.Children.Add(commentsButton);
            quickButtonsGrid.Children.Add(usersButton);
            quickButtonsGrid.Children.Add(statsButton);

            quickAccessContent.Children.Add(quickButtonsGrid);
            quickAccessFrame.Content = quickAccessContent;
            adminStack.Children.Add(quickAccessFrame);

            return adminStack;
        }

        private Button CreateQuickButton(string icon, string text, string colorHex)
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
                try
                {
                    await Navigation.PushAsync(new AdminPage());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PROFILE] Errore apertura AdminPage: {ex.Message}");
                    await DisplayAlert("Errore", "Impossibile aprire il pannello amministratore", "OK");
                }
            };

            return button;
        }

        private void SetupAdminButtons(Grid bottomGrid)
        {
            bottomGrid.Children.Clear();
            bottomGrid.ColumnDefinitions.Clear();

            // Solo logout per admin
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
                Margin = new Thickness(0, 16)
            };

            logoutButton.Clicked += OnAdminLogoutClicked;

            Grid.SetColumn(logoutButton, 0);
            bottomGrid.Children.Add(logoutButton);
        }

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
                HomePage.ResetAdminWelcome();

                Debug.WriteLine($"[PROFILE] Admin logout completato");

                // Torna alla pagina di login
                Application.Current.MainPage = new NavigationPage(new LoginPage());

                await DisplayAlert("Logout Completato", "Sei stato disconnesso con successo.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROFILE] Errore durante logout admin: {ex.Message}");
                await DisplayAlert("Errore", "Errore durante il logout", "OK");
            }
        }

        // ========== CARICAMENTO PROFILO (COMUNE) ==========

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

        // ========== GESTIONE TAB (SOLO PER UTENTI NORMALI) ==========

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

        // ========== CARICAMENTO DATI (SOLO UTENTI NORMALI) ==========

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
                        foreach (var postI