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
        private TabType _activeTab = TabType.MyPosts;
        private bool _isAdmin = false;

        // Collections per utenti normali
        public ObservableCollection<PostResponse> FavoritePosts { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<PostResponse> CalendarEvents { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<PostResponse> MyPosts { get; set; } = new ObservableCollection<PostResponse>();
        public ObservableCollection<EventInviteInfo> EventInvites { get; set; } = new ObservableCollection<EventInviteInfo>();

        private enum TabType { MyPosts, MyEvents, Favorites }

        public ProfilePage()
        {
            InitializeComponent();
            SetupCollectionViews();
        }

        // Configura le ItemsSource delle CollectionView
        private void SetupCollectionViews()
        {
            FavoritesCollectionView.ItemsSource = FavoritePosts;
            CalendarEventsCollectionView.ItemsSource = CalendarEvents;
            MyPostsCollectionView.ItemsSource = MyPosts;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();
            await CheckAndSetupUserType();
        }

        // ========== SETUP TIPO UTENTE ==========

        // Verifica se l'utente è admin e configura l'interfaccia appropriata
        private async Task CheckAndSetupUserType()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/profile");
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
                    await SetupUserProfile();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROFILE] Errore setup tipo utente: {ex.Message}");
                await SetupUserProfile();
            }
        }

        // Configura il profilo per utenti normali
        private async Task SetupUserProfile()
        {
            await Task.WhenAll(
                LoadMyPosts(),
                LoadCalendarEvents(),
                LoadFavorites()
            );
            Debug.WriteLine("[PROFILE] Setup utente normale completato");
        }

        // Configura il profilo per amministratori
        private async Task SetupAdminProfile()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    // Nascondi le sezioni utente normale
                    HideUserSections();

                    // Sostituisci il contenuto con interfaccia admin
                    if (Content is Grid mainGrid)
                    {
                        ReplaceContentWithAdminInterface(mainGrid);
                    }

                    Debug.WriteLine("[PROFILE] ✅ Interfaccia admin configurata");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[PROFILE] Errore setup interfaccia admin: {ex.Message}");
                }
            });
        }

        // Nasconde le sezioni specifiche per utenti normali
        private void HideUserSections()
        {
            // Nascondi i tab utente normali se esistono
            if (MyPostsTabButton != null) MyPostsTabButton.IsVisible = false;
            if (MyEventsTabButton != null) MyEventsTabButton.IsVisible = false;
            if (FavoritesTabButton != null) FavoritesTabButton.IsVisible = false;
            if (TabIndicator != null) TabIndicator.IsVisible = false;
        }

        // Sostituisce il contenuto con l'interfaccia admin
        private void ReplaceContentWithAdminInterface(Grid mainGrid)
        {
            var contentScrollView = mainGrid.Children.OfType<ScrollView>().FirstOrDefault(s => Grid.GetRow(s) == 2);
            if (contentScrollView != null)
            {
                contentScrollView.Content = CreateAdminContent();
            }

            var bottomGrid = mainGrid.Children.OfType<Grid>().LastOrDefault();
            if (bottomGrid != null)
            {
                SetupAdminButtons(bottomGrid);
            }
        }

        // ========== INTERFACCIA ADMIN ==========

        // Crea il contenuto dell'interfaccia amministratore
        private View CreateAdminContent()
        {
            var adminStack = new VerticalStackLayout
            {
                Spacing = 20,
                Padding = new Thickness(16)
            };

            adminStack.Children.Add(CreateWelcomeCard());
            adminStack.Children.Add(CreateQuickAccessCard());

            return adminStack;
        }

        // Crea la card di benvenuto per l'admin
        private Frame CreateWelcomeCard()
        {
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

            var adminPanelButton = CreateMainAdminButton();
            welcomeContent.Children.Add(adminPanelButton);

            welcomeFrame.Content = welcomeContent;
            return welcomeFrame;
        }

        // Crea il pulsante principale per il pannello admin
        private Button CreateMainAdminButton()
        {
            var button = new Button
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

        // Crea la card con accessi rapidi
        private Frame CreateQuickAccessCard()
        {
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

            var quickButtonsGrid = CreateQuickButtonsGrid();
            quickAccessContent.Children.Add(quickButtonsGrid);

            quickAccessFrame.Content = quickAccessContent;
            return quickAccessFrame;
        }

        // Crea la griglia con i pulsanti di accesso rapido
        private Grid CreateQuickButtonsGrid()
        {
            var grid = new Grid
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

            var buttons = new[]
            {
                CreateQuickButton("📝", "Gestisci Post", "#6366F1"),
                CreateQuickButton("💬", "Commenti", "#10B981"),
                CreateQuickButton("👥", "Utenti", "#F59E0B"),
                CreateQuickButton("📊", "Statistiche", "#DC2626")
            };

            for (int i = 0; i < buttons.Length; i++)
            {
                Grid.SetColumn(buttons[i], i % 2);
                Grid.SetRow(buttons[i], i / 2);
                grid.Children.Add(buttons[i]);
            }

            return grid;
        }

        // Crea un pulsante di accesso rapido
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

        // Configura i pulsanti per l'admin (solo logout)
        private void SetupAdminButtons(Grid bottomGrid)
        {
            bottomGrid.Children.Clear();
            bottomGrid.ColumnDefinitions.Clear();
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

        // ========== CARICAMENTO PROFILO ==========

        // Carica i dati del profilo utente
        private async void LoadProfile()
        {
            Debug.WriteLine("LoadProfile called");
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/profile");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var userProfile = JsonSerializer.Deserialize<User>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (userProfile != null)
                    {
                        UpdateProfileUI(userProfile);
                        Debug.WriteLine($"Profilo Utente Caricato: {userProfile.Username}");
                    }
                    else
                    {
                        await DisplayAlert("Errore", "Profilo utente non valido.", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile caricare il profilo.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante il caricamento del profilo: {ex.Message}");
                await DisplayAlert("Errore", $"Errore durante il caricamento: {ex.Message}", "OK");
            }
        }

        // Aggiorna l'interfaccia con i dati del profilo
        private void UpdateProfileUI(User userProfile)
        {
            UsernameLabel.Text = userProfile.Username;
            NameLabel.Text = userProfile.Nome;
            SurnameLabel.Text = userProfile.Cognome;
            EmailLabel.Text = userProfile.Email;

            ProfileImage.Source = !string.IsNullOrEmpty(userProfile.ProfilePic)
                ? $"{ApiConfig.BaseUrl}/images/{userProfile.ProfilePic}"
                : "default_images.jpg";
        }

        // ========== CARICAMENTO DATI UTENTE NORMALE ==========

        // Carica i post creati dall'utente
        private async Task LoadMyPosts()
        {
            if (_isAdmin) return;

            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/posts/by-user", ApiConfig.PythonApiUrl);
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(jsonResponse);

                    MyPosts.Clear();
                    foreach (var element in jsonElements ?? new List<JsonElement>())
                    {
                        var post = ParsePostFromJson(element);
                        MyPosts.Add(post);
                    }

                    Debug.WriteLine($"[MY_POSTS] Caricati {MyPosts.Count} post dell'utente");
                }
                else
                {
                    MyPosts.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MY_POSTS] Errore: {ex.Message}");
                MyPosts.Clear();
            }
        }

        // Carica gli eventi del calendario (partecipazioni + inviti)
        private async Task LoadCalendarEvents()
        {
            if (_isAdmin) return;

            try
            {
                await LoadUserParticipations();
                await LoadEventInvites();
                Debug.WriteLine($"[CALENDAR] Caricati {CalendarEvents.Count} eventi totali");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Errore: {ex.Message}");
            }
        }

        // Carica le partecipazioni dell'utente
        private async Task LoadUserParticipations()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/user/participations");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result.ContainsKey("participations") && result["participations"] is JsonElement participationsElement)
                    {
                        var participationIds = participationsElement.EnumerateArray()
                            .Select(x => x.GetInt32())
                            .ToList();

                        CalendarEvents.Clear();
                        var loadTasks = participationIds.Select(LoadCalendarEventDetails);
                        await Task.WhenAll(loadTasks);

                        // Ordina eventi: futuri prima, poi passati
                        var orderedEvents = CalendarEvents
                            .OrderBy(e => GetEventDateTime(e))
                            .ToList();

                        CalendarEvents.Clear();
                        foreach (var eventItem in orderedEvents)
                        {
                            CalendarEvents.Add(eventItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Errore partecipazioni: {ex.Message}");
            }
        }

        // Carica gli inviti agli eventi
        private async Task LoadEventInvites()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/events/invites");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result.ContainsKey("invites") && result["invites"] is JsonElement invitesElement)
                    {
                        EventInvites.Clear();
                        foreach (var inviteElement in invitesElement.EnumerateArray())
                        {
                            var invite = ParseEventInviteFromJson(inviteElement);
                            EventInvites.Add(invite);
                            await LoadInviteEventDetails(invite);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore: {ex.Message}");
            }
        }

        // Carica i dettagli di un evento dal calendario
        private async Task LoadCalendarEventDetails(int postId)
        {
            try
            {
                var response = await _client.GetAsync($"{ApiConfig.PythonApiUrl}/posts/{postId}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => CalendarEvents.Add(post));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CALENDAR] Errore dettagli evento {postId}: {ex.Message}");
            }
        }

        // Carica i dettagli di un evento da invito
        private async Task LoadInviteEventDetails(EventInviteInfo invite)
        {
            try
            {
                var response = await _client.GetAsync($"{ApiConfig.PythonApiUrl}/posts/{invite.PostID}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        var invitePost = CreateInvitePost(post, invite);
                        MainThread.BeginInvokeOnMainThread(() => CalendarEvents.Insert(0, invitePost));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore dettagli invito {invite.PostID}: {ex.Message}");
            }
        }

        // Carica i post preferiti
        private async Task LoadFavorites()
        {
            if (_isAdmin) return;

            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/favorites");
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

                        FavoritePosts.Clear();
                        foreach (var postId in favoriteIds)
                        {
                            await LoadFavoritePostDetails(postId);
                        }

                        Debug.WriteLine($"Caricati {favoriteIds.Count} preferiti");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore caricamento preferiti: {ex.Message}");
            }
        }

        // Carica i dettagli di un post preferito
        private async Task LoadFavoritePostDetails(int postId)
        {
            try
            {
                var response = await _client.GetAsync($"{ApiConfig.PythonApiUrl}/posts/{postId}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        MainThread.BeginInvokeOnMainThread(() => FavoritePosts.Add(post));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore dettagli preferito {postId}: {ex.Message}");
            }
        }

        // ========== GESTIONE TAB (SOLO UTENTI NORMALI) ==========

        private void OnMyPostsTabClicked(object sender, EventArgs e)
        {
            if (_isAdmin) return;
            SwitchToTab(TabType.MyPosts);
        }

        private void OnMyEventsTabClicked(object sender, EventArgs e)
        {
            if (_isAdmin) return;
            SwitchToTab(TabType.MyEvents);
        }

        private void OnFavoritesTabClicked(object sender, EventArgs e)
        {
            if (_isAdmin) return;
            SwitchToTab(TabType.Favorites);
        }

        // Cambia il tab attivo e aggiorna l'UI
        private void SwitchToTab(TabType newTab)
        {
            if (_activeTab == newTab) return;

            _activeTab = newTab;
            UpdateTabsUI();
        }

        // Aggiorna l'interfaccia dei tab
        private void UpdateTabsUI()
        {
            if (_isAdmin) return;

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

        // ========== EVENT HANDLERS ==========

        private async void OnMyPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                await Navigation.PushAsync(new PostDetailMainPage(selectedPost.id));
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async void OnCalendarEventSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedEvent)
            {
                await Navigation.PushAsync(new PostDetailMainPage(selectedEvent.id));
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async void OnFavoriteSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedFavorite)
            {
                await Navigation.PushAsync(new PostDetailMainPage(selectedFavorite.id));
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async void OnAcceptInviteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PostResponse invitePost)
            {
                await HandleInviteAction(invitePost.InviteID, "accept", "Invito accettato! Sei ora iscritto all'evento.");
                CalendarEvents.Remove(invitePost);
                await LoadCalendarEvents();
            }
        }

        private async void OnRejectInviteClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is PostResponse invitePost)
            {
                await HandleInviteAction(invitePost.InviteID, "reject", "L'invito è stato rifiutato.");
                CalendarEvents.Remove(invitePost);
            }
        }

        private async void OnNavigateToChangePassword(object sender, EventArgs e)
        {
            try
            {
                await Navigation.PushAsync(new ChangePasswordPage());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROFILE] Errore navigazione cambio password: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire la pagina di cambio password", "OK");
            }
        }

        private async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            await HandleLogout("Logout", "Sei sicuro di voler effettuare il logout?");
        }

        private async void OnAdminLogoutClicked(object sender, EventArgs e)
        {
            await HandleLogout("Logout Amministratore", "Sei sicuro di voler effettuare il logout dal pannello amministratore?");
        }

        // ========== HELPER METHODS ==========

        // Gestisce le azioni sugli inviti (accetta/rifiuta)
        private async Task HandleInviteAction(long inviteId, string action, string successMessage)
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"/events/invite/{action}?invite_id={inviteId}");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Successo", successMessage, "OK");
                }
                else
                {
                    await DisplayAlert("Errore", $"Impossibile {action} l'invito.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITES] Errore {action} invito: {ex.Message}");
                await DisplayAlert("Errore", $"Errore durante {action} dell'invito.", "OK");
            }
        }

        // Gestisce il processo di logout
        private async Task HandleLogout(string title, string message)
        {
            try
            {
                var confirm = await DisplayAlert(title, message, "Logout", "Annulla");
                if (!confirm) return;

                var request = CreateAuthenticatedRequest(HttpMethod.Post, "/logout");
                await _client.SendAsync(request);

                Preferences.Clear();
                HomePage.ResetAdminWelcome();

                Application.Current.MainPage = new NavigationPage(new LoginPage());
                await DisplayAlert("Logout Completato", "Sei stato disconnesso con successo.", "OK");

                Debug.WriteLine($"[PROFILE] Logout completato");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PROFILE] Errore durante logout: {ex.Message}");
                await DisplayAlert("Errore", "Errore durante il logout", "OK");
            }
        }

        // Crea una richiesta HTTP autenticata
        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string endpoint, string baseUrl = null)
        {
            var url = baseUrl ?? ApiConfig.BaseUrl;
            var request = new HttpRequestMessage(method, $"{url}{endpoint}");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            return request;
        }

        // Parsing helper methods
        private PostResponse ParsePostFromJson(JsonElement element)
        {
            return new PostResponse
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
        }

        private EventInviteInfo ParseEventInviteFromJson(JsonElement inviteElement)
        {
            return new EventInviteInfo
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
        }

        private PostResponse CreateInvitePost(PostResponse post, EventInviteInfo invite)
        {
            return new PostResponse
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
        }

        private DateTime GetEventDateTime(PostResponse eventPost)
        {
            if (DateTime.TryParse(eventPost.data_partita, out DateTime dataPartita))
            {
                if (TimeSpan.TryParse(eventPost.ora_partita, out TimeSpan oraPartita))
                {
                    return dataPartita.Add(oraPartita);
                }
                return dataPartita;
            }
            return DateTime.MinValue;
        }

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

        private int? GetNullableIntProperty(JsonElement element, string propertyName)
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetInt32()
                    : null;
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
                if (element.TryGetProperty("campo", out var campoElement) && campoElement.ValueKind == JsonValueKind.Object)
                {
                    return new CampoInfo
                    {
                        nome = GetStringProperty(campoElement, "nome"),
                        indirizzo = GetStringProperty(campoElement, "indirizzo")
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}