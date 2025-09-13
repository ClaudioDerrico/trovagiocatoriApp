using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Text;
using System.Diagnostics;
using trovagiocatoriApp.Models;
using trovagiocatoriApp.Services;

namespace trovagiocatoriApp.Views;

public partial class AdminPage : ContentPage
{
    private readonly IAdminService _adminService;
    private readonly IBanService _banService;

    // Collections per i dati utilizzando i nuovi Models
    public ObservableCollection<AdminPostInfo> AllPosts { get; set; } = new ObservableCollection<AdminPostInfo>();
    public ObservableCollection<AdminCommentInfo> AllComments { get; set; } = new ObservableCollection<AdminCommentInfo>();
    public ObservableCollection<AdminUserInfo> AllUsers { get; set; } = new ObservableCollection<AdminUserInfo>();

    // Filtri
    private string _postSearchFilter = "";
    private string _commentSearchFilter = "";
    private string _userSearchFilter = "";

    // Tab corrente
    private AdminTab _currentTab = AdminTab.Dashboard;

    private enum AdminTab
    {
        Dashboard,
        Posts,
        Comments,
        Users
    }

    public AdminPage()
    {
        InitializeComponent();
        BindingContext = this;

        // Inizializza il service (normalmente dovrebbe essere iniettato via DI)
        _adminService = new AdminService();
        _banService = new BanService();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAdminInfo();
        await LoadDashboardStats();
        await LoadAllData(); // Questo ora include anche LoadAllBans()
        await LoadBanStats(); // Carica statistiche ban
    }

    // ========== CARICAMENTO DATI ==========

    private async Task LoadAdminInfo()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/profile");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var httpClient = new HttpClient();
            var response = await httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<Models.User>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AdminInfoLabel.Text = $"Amministratore: {user.Username} ({user.Email})";
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento info: {ex.Message}");
        }
    }

    private async Task LoadBanStats()
    {
        try
        {
            Debug.WriteLine("[ADMIN] Caricamento statistiche ban...");
            var stats = await _banService.GetBanStatsAsync();
            Debug.WriteLine($"[ADMIN] ✅ Ban Stats: {stats.ActiveBans} attivi, {stats.TotalBans} totali");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento ban stats: {ex.Message}");
        }
    }

    private async Task LoadAllBans()
    {
        try
        {
            Debug.WriteLine("[ADMIN] Caricamento ban attivi...");
            var bans = await _banService.GetActiveBansAsync();
            Debug.WriteLine($"[ADMIN] ✅ Caricati {bans.Count} ban attivi");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento ban: {ex.Message}");
        }
    }

    private async Task LoadDashboardStats()
    {
        try
        {
            Debug.WriteLine("[ADMIN] Caricamento statistiche dashboard...");

            var stats = await _adminService.GetStatsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalPostsLabel.Text = stats.TotalPosts.ToString();
                TotalCommentsLabel.Text = stats.TotalComments.ToString();
                TotalUsersLabel.Text = stats.TotalUsers.ToString();
                TotalFieldsLabel.Text = stats.TotalSportFields.ToString();
            });

            Debug.WriteLine($"[ADMIN] ✅ Statistiche caricate: {stats.TotalPosts} post, {stats.TotalComments} commenti, {stats.TotalUsers} utenti");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento stats: {ex.Message}");

            // Fallback con statistiche di default
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalPostsLabel.Text = "0";
                TotalCommentsLabel.Text = "0";
                TotalUsersLabel.Text = "0";
                TotalFieldsLabel.Text = "0";
            });
        }
    }

    private async Task LoadAllData()
    {
        await LoadAllPosts();
        await LoadAllComments();
        await LoadAllUsers();
        await LoadAllBans();
    }

    private async Task LoadUserBanStatuses()
    {
        try
        {
            // Per ogni utente, controlla se è bannato
            foreach (var user in AllUsers)
            {
                var isBanned = await _banService.CheckUserBanStatusAsync(user.Id);
                if (isBanned)
                {
                    user.IsActive = false; // L'utente bannato viene mostrato come non attivo
                }
            }

            FilterUsers(); // Riapplica i filtri con i nuovi stati
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento ban status utenti: {ex.Message}");
        }
    }

    private async Task LoadAllPosts()
    {
        try
        {
            Debug.WriteLine("[ADMIN] Caricamento tutti i post...");

            var posts = await _adminService.GetAllPostsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllPosts.Clear();
                foreach (var post in posts)
                {
                    AllPosts.Add(post);
                }

                PostsCollectionView.ItemsSource = AllPosts;
                Debug.WriteLine($"[ADMIN] Caricati {AllPosts.Count} post");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento post: {ex.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllPosts.Clear();
                PostsCollectionView.ItemsSource = AllPosts;
            });
        }
    }

    private async Task LoadAllComments()
    {
        try
        {
            Debug.WriteLine("[ADMIN] Caricamento tutti i commenti...");

            var comments = await _adminService.GetAllCommentsAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllComments.Clear();
                foreach (var comment in comments)
                {
                    AllComments.Add(comment);
                }

                CommentsCollectionView.ItemsSource = AllComments;
                Debug.WriteLine($"[ADMIN] Caricati {AllComments.Count} commenti");
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento commenti: {ex.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllComments.Clear();
                CommentsCollectionView.ItemsSource = AllComments;
            });
        }
    }



    private async Task LoadAllUsers()
    {
        try
        {
            Debug.WriteLine("[ADMIN] Caricamento tutti gli utenti...");

            var users = await _adminService.GetAllUsersAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllUsers.Clear();
                foreach (var user in users)
                {
                    AllUsers.Add(user);
                }

                UsersCollectionView.ItemsSource = AllUsers;
                Debug.WriteLine($"[ADMIN] Caricati {AllUsers.Count} utenti");
            });

            // Carica anche lo stato dei ban per gli utenti
            await LoadUserBanStatuses();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento utenti: {ex.Message}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AllUsers.Clear();
                UsersCollectionView.ItemsSource = AllUsers;
            });
        }
    }

    // ========== GESTIONE TAB ==========

    private void OnDashboardTabClicked(object sender, EventArgs e)
    {
        SwitchToTab(AdminTab.Dashboard);
    }

    private void OnPostsTabClicked(object sender, EventArgs e)
    {
        SwitchToTab(AdminTab.Posts);
    }

    private void OnCommentsTabClicked(object sender, EventArgs e)
    {
        SwitchToTab(AdminTab.Comments);
    }

    private void OnUsersTabClicked(object sender, EventArgs e)
    {
        SwitchToTab(AdminTab.Users);
    }

    private void SwitchToTab(AdminTab tab)
    {
        _currentTab = tab;

        // Reset tutti i tab
        DashboardTabButton.Style = (Style)Resources["TabButtonStyle"];
        PostsTabButton.Style = (Style)Resources["TabButtonStyle"];
        CommentsTabButton.Style = (Style)Resources["TabButtonStyle"];
        UsersTabButton.Style = (Style)Resources["TabButtonStyle"];

        // Nascondi tutti i contenuti
        DashboardContent.IsVisible = false;
        PostsContent.IsVisible = false;
        CommentsContent.IsVisible = false;
        UsersContent.IsVisible = false;

        // Attiva il tab selezionato
        switch (tab)
        {
            case AdminTab.Dashboard:
                DashboardTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                DashboardContent.IsVisible = true;
                Grid.SetColumn(TabIndicator, 0);
                break;

            case AdminTab.Posts:
                PostsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                PostsContent.IsVisible = true;
                Grid.SetColumn(TabIndicator, 1);
                break;

            case AdminTab.Comments:
                CommentsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                CommentsContent.IsVisible = true;
                Grid.SetColumn(TabIndicator, 2);
                break;

            case AdminTab.Users:
                UsersTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                UsersContent.IsVisible = true;
                Grid.SetColumn(TabIndicator, 3);
                break;
        }
    }

    // ========== RICERCA E FILTRI ==========

    private void OnPostSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _postSearchFilter = e.NewTextValue?.ToLower() ?? "";
        FilterPosts();
    }

    private void OnCommentSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _commentSearchFilter = e.NewTextValue?.ToLower() ?? "";
        FilterComments();
    }

    private void OnUserSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        _userSearchFilter = e.NewTextValue?.ToLower() ?? "";
        FilterUsers();
    }

    private void FilterPosts()
    {
        var filtered = AllPosts.Where(p =>
            string.IsNullOrEmpty(_postSearchFilter) ||
            p.Titolo.ToLower().Contains(_postSearchFilter) ||
            p.AutoreEmail.ToLower().Contains(_postSearchFilter) ||
            p.Sport.ToLower().Contains(_postSearchFilter) ||
            p.Citta.ToLower().Contains(_postSearchFilter)
        ).ToList();

        PostsCollectionView.ItemsSource = filtered;
    }

    private void FilterComments()
    {
        var filtered = AllComments.Where(c =>
            string.IsNullOrEmpty(_commentSearchFilter) ||
            c.Contenuto.ToLower().Contains(_commentSearchFilter) ||
            c.AutoreEmail.ToLower().Contains(_commentSearchFilter) ||
            c.PostTitolo.ToLower().Contains(_commentSearchFilter)
        ).ToList();

        CommentsCollectionView.ItemsSource = filtered;
    }

    private void FilterUsers()
    {
        var filtered = AllUsers.Where(u =>
            string.IsNullOrEmpty(_userSearchFilter) ||
            u.Username.ToLower().Contains(_userSearchFilter) ||
            u.Email.ToLower().Contains(_userSearchFilter) ||
            u.Nome.ToLower().Contains(_userSearchFilter) ||
            u.Cognome.ToLower().Contains(_userSearchFilter)
        ).ToList();

        UsersCollectionView.ItemsSource = filtered;
    }

    // ========== AZIONI ADMIN ==========

    private async void OnDeletePostClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminPostInfo post)
        {
            await DeletePost(post);
        }
    }

    private async void OnDeleteCommentClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminCommentInfo comment)
        {
            await DeleteComment(comment);
        }
    }

    private async void OnToggleUserStatusClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminUserInfo user)
        {
            await ToggleUserStatus(user);
        }
    }


    private async Task DeletePost(AdminPostInfo post)
    {
        try
        {
            bool confirm = await DisplayAlert(
                "Elimina Post",
                $"Sei sicuro di voler eliminare il post:\n\n" +
                $"📝 {post.Titolo}\n" +
                $"👤 di {post.AutoreEmail}\n" +
                $"📅 {post.DataCreazione:dd/MM/yyyy}\n\n" +
                $"⚠️ Questa azione è irreversibile!",
                "Elimina",
                "Annulla"
            );

            if (!confirm) return;

            Debug.WriteLine($"[ADMIN] Eliminazione post {post.Id}...");

            bool success = await _adminService.DeletePostAsync(post.Id);

            if (success)
            {
                AllPosts.Remove(post);
                FilterPosts();
                await DisplayAlert("Successo", $"Post '{post.Titolo}' eliminato con successo!", "OK");
                await LoadDashboardStats(); // Aggiorna le statistiche

                Debug.WriteLine($"[ADMIN] ✅ Post {post.Id} eliminato con successo");
            }
            else
            {
                await DisplayAlert("Errore", "Impossibile eliminare il post. Riprova più tardi.", "OK");
                Debug.WriteLine($"[ADMIN] ❌ Errore eliminazione post {post.Id}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore eliminazione post: {ex.Message}");
            await DisplayAlert("Errore", "Errore nell'eliminazione del post", "OK");
        }
    }

    private async Task DeleteComment(AdminCommentInfo comment)
    {
        try
        {
            bool confirm = await DisplayAlert(
                "Elimina Commento",
                $"Sei sicuro di voler eliminare il commento:\n\n" +
                $"💬 {comment.Contenuto.Substring(0, Math.Min(100, comment.Contenuto.Length))}...\n" +
                $"👤 di {comment.AutoreEmail}\n" +
                $"📝 nel post: {comment.PostTitolo}\n" +
                $"📅 {comment.DataCreazione:dd/MM/yyyy}\n\n" +
                $"⚠️ Questa azione è irreversibile!",
                "Elimina",
                "Annulla"
            );

            if (!confirm) return;

            Debug.WriteLine($"[ADMIN] Eliminazione commento {comment.Id}...");

            bool success = await _adminService.DeleteCommentAsync(comment.Id);

            if (success)
            {
                AllComments.Remove(comment);
                FilterComments();
                await DisplayAlert("Successo", "Commento eliminato con successo!", "OK");
                await LoadDashboardStats(); // Aggiorna le statistiche

                Debug.WriteLine($"[ADMIN] ✅ Commento {comment.Id} eliminato con successo");
            }
            else
            {
                await DisplayAlert("Errore", "Impossibile eliminare il commento. Riprova più tardi.", "OK");
                Debug.WriteLine($"[ADMIN] ❌ Errore eliminazione commento {comment.Id}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore eliminazione commento: {ex.Message}");
            await DisplayAlert("Errore", "Errore nell'eliminazione del commento", "OK");
        }
    }

    private async Task ToggleUserStatus(AdminUserInfo user)
    {
        try
        {
            Debug.WriteLine($"[ADMIN] Toggle status per utente {user.Username} - Attivo: {user.IsActive}");

            if (user.IsActive)
            {
                // L'utente è attivo -> BANNA
                bool confirm = await DisplayAlert(
                    "Ban Utente",
                    $"Sei sicuro di voler bannare {user.Username}?\n\n" +
                    $"👤 {user.NomeCompleto}\n" +
                    $"📧 {user.Email}\n\n" +
                    $"⚠️ L'utente non potrà più accedere alla piattaforma!",
                    "Banna",
                    "Annulla"
                );

                if (!confirm) return;

                Debug.WriteLine($"[ADMIN] Bannando utente {user.Username}...");

                // Crea richiesta di ban semplice
                var banRequest = new BanUserRequest
                {
                    UserId = user.Id,
                    Reason = "Ban amministrativo",
                    BanType = "permanent", // Ban permanente per semplicità
                    Notes = "Utente bannato dall'amministratore"
                };

                var response = await _banService.BanUserAsync(banRequest);

                if (response.Success)
                {
                    // Aggiorna lo stato dell'utente nella lista
                    user.IsActive = false;
                    FilterUsers();

                    await DisplayAlert("Successo", $"Utente {user.Username} bannato con successo!\n\nNon potrà più accedere alla piattaforma.", "OK");
                    Debug.WriteLine($"[ADMIN] ✅ Utente {user.Username} bannato con successo");
                }
                else
                {
                    await DisplayAlert("Errore", response.Error ?? "Errore durante il ban dell'utente", "OK");
                    Debug.WriteLine($"[ADMIN] ❌ Errore ban {user.Username}: {response.Error}");
                }
            }
            else
            {
                // L'utente non è attivo (bannato) -> SBANNA
                bool confirm = await DisplayAlert(
                    "Sbanna Utente",
                    $"Vuoi sbannare {user.Username}?\n\n" +
                    $"👤 {user.NomeCompleto}\n" +
                    $"📧 {user.Email}\n\n" +
                    $"L'utente potrà nuovamente accedere alla piattaforma.",
                    "Sbanna",
                    "Annulla"
                );

                if (!confirm) return;

                Debug.WriteLine($"[ADMIN] Sbannando utente {user.Username}...");

                var response = await _banService.UnbanUserAsync(user.Id);

                if (response.Success)
                {
                    // Aggiorna lo stato dell'utente nella lista
                    user.IsActive = true;
                    FilterUsers();

                    await DisplayAlert("Successo", $"Utente {user.Username} sbannato con successo!\n\nPuò nuovamente accedere alla piattaforma.", "OK");
                    Debug.WriteLine($"[ADMIN] ✅ Utente {user.Username} sbannato con successo");
                }
                else
                {
                    await DisplayAlert("Errore", response.Error ?? "Errore durante lo sbannamento", "OK");
                    Debug.WriteLine($"[ADMIN] ❌ Errore sbannamento {user.Username}: {response.Error}");
                }
            }

            // Aggiorna le statistiche
            await LoadDashboardStats();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore toggle user status: {ex.Message}");
            await DisplayAlert("Errore", "Errore nella gestione dello stato utente", "OK");
        }
    }

   


   

    // ========== AZIONI RAPIDE ==========

    private async void OnRefreshStatsClicked(object sender, EventArgs e)
    {
        try
        {
            Debug.WriteLine("[ADMIN] Avvio refresh completo dati...");

            // Mostra indicatore di caricamento
            var button = sender as Button;
            var originalText = button?.Text;
            if (button != null)
            {
                button.Text = "🔄 Aggiornamento...";
                button.IsEnabled = false;
            }

            // Carica tutto
            await LoadDashboardStats();
            await LoadBanStats(); // Carica statistiche ban
            await LoadAllData(); // Include tutti i dati compreso ban

            if (button != null)
            {
                button.Text = originalText;
                button.IsEnabled = true;
            }

            await DisplayAlert("Aggiornato", "Tutti i dati sono stati aggiornati con successo!", "OK");
            Debug.WriteLine("[ADMIN] ✅ Refresh completo completato");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore durante refresh completo: {ex.Message}");
            await DisplayAlert("Errore", "Errore durante l'aggiornamento dei dati", "OK");
        }
    }


    private async void OnViewPostDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminPostInfo post)
        {
            try
            {
                await Navigation.PushAsync(new PostDetailMainPage(post.Id));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN] Errore apertura dettagli post {post.Id}: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire i dettagli del post", "OK");
            }
        }
    }

    private async void OnViewUserPostsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminUserInfo user)
        {
            try
            {
                var userPosts = AllPosts.Where(p => p.AutoreEmail == user.Email).ToList();

                if (userPosts.Any())
                {
                    var postTitles = string.Join("\n• ", userPosts.Select(p => $"{p.Titolo} ({p.DataCreazione:dd/MM/yyyy})"));

                    await DisplayAlert(
                        $"Post di {user.Username}",
                        $"Post creati ({userPosts.Count}):\n\n• {postTitles}",
                        "OK"
                    );
                }
                else
                {
                    await DisplayAlert("Info", $"{user.Username} non ha ancora creato post.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN] Errore visualizzazione post utente {user.Username}: {ex.Message}");
                await DisplayAlert("Errore", "Errore nel caricamento dei post dell'utente", "OK");
            }
        }
    }

    // ========== GESTIONE ERRORI E FALLBACK ==========

    private AdminStats GetFallbackStats()
    {
        return new AdminStats
        {
            TotalPosts = AllPosts.Count,
            TotalComments = AllComments.Count,
            TotalUsers = AllUsers.Count,
            TotalSportFields = 0,
            PostsThisWeek = 0,
            CommentsToday = 0,
            GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };
    }

    private void LoadMockDataIfNeeded()
    {
        // Carica dati mock solo se le collections sono vuote e in modalità debug
#if DEBUG
        if (!AllPosts.Any())
        {
            LoadMockPosts();
        }

        if (!AllComments.Any())
        {
            LoadMockComments();
        }

        if (!AllUsers.Any())
        {
            LoadMockUsers();
        }
#endif
    }

#if DEBUG
    private void LoadMockPosts()
    {
        var mockPosts = new List<AdminPostInfo>
        {
            new AdminPostInfo
            {
                Id = 1,
                Titolo = "Partita di calcio a Milano",
                AutoreEmail = "utente1@email.com",
                Sport = "Calcio",
                Citta = "Milano",
                Provincia = "Milano",
                DataCreazione = DateTime.Now.AddDays(-2),
                DataPartita = DateTime.Now.AddDays(3),
                OraPartita = "18:00",
                NumeroGiocatori = 10,
                PartecipantiIscritti = 7,
                Status = "Aperto",
                Livello = "Intermedio",
                Commento = "Partita amichevole"
            },
            new AdminPostInfo
            {
                Id = 2,
                Titolo = "Tennis al parco",
                AutoreEmail = "utente2@email.com",
                Sport = "Tennis",
                Citta = "Roma",
                Provincia = "Roma",
                DataCreazione = DateTime.Now.AddDays(-1),
                DataPartita = DateTime.Now.AddDays(5),
                OraPartita = "16:30",
                NumeroGiocatori = 4,
                PartecipantiIscritti = 4,
                Status = "Completo",
                Livello = "Avanzato",
                Commento = "Doppio misto"
            }
        };

        foreach (var post in mockPosts)
        {
            AllPosts.Add(post);
        }

        Debug.WriteLine($"[ADMIN] Caricati {mockPosts.Count} post mock");
    }

    private void LoadMockComments()
    {
        var mockComments = new List<AdminCommentInfo>
        {
            new AdminCommentInfo
            {
                Id = 1,
                PostId = 1,
                PostTitolo = "Partita di calcio a Milano",
                AutoreEmail = "commentatore1@email.com",
                Contenuto = "Sono interessato a partecipare! Che livello è richiesto?",
                DataCreazione = DateTime.Now.AddHours(-2)
            },
            new AdminCommentInfo
            {
                Id = 2,
                PostId = 2,
                PostTitolo = "Tennis al parco",
                AutoreEmail = "commentatore2@email.com",
                Contenuto = "Perfetto! Ci sarò sicuramente. A che ora iniziamo esattamente?",
                DataCreazione = DateTime.Now.AddHours(-1)
            }
        };

        foreach (var comment in mockComments)
        {
            AllComments.Add(comment);
        }

        Debug.WriteLine($"[ADMIN] Caricati {mockComments.Count} commenti mock");
    }

    private void LoadMockUsers()
    {
        var mockUsers = new List<AdminUserInfo>
        {
            new AdminUserInfo
            {
                Id = 1,
                Username = "mario_rossi",
                Nome = "Mario",
                Cognome = "Rossi",
                Email = "mario.rossi@email.com",
                DataRegistrazione = DateTime.Now.AddDays(-30),
                IsActive = true,
                IsAdmin = false,
                PostCreati = 3,
                CommentiScritti = 12
            },
            new AdminUserInfo
            {
                Id = 2,
                Username = "giulia_verdi",
                Nome = "Giulia",
                Cognome = "Verdi",
                Email = "giulia.verdi@email.com",
                DataRegistrazione = DateTime.Now.AddDays(-15),
                IsActive = true,
                IsAdmin = false,
                PostCreati = 1,
                CommentiScritti = 5
            },
            new AdminUserInfo
            {
                Id = 3,
                Username = "luca_bianchi",
                Nome = "Luca",
                Cognome = "Bianchi",
                Email = "luca.bianchi@email.com",
                DataRegistrazione = DateTime.Now.AddDays(-45),
                IsActive = false,
                IsAdmin = false,
                PostCreati = 0,
                CommentiScritti = 2
            }
        };

        foreach (var user in mockUsers)
        {
            AllUsers.Add(user);
        }

        Debug.WriteLine($"[ADMIN] Caricati {mockUsers.Count} utenti mock");
    }
#endif

    // ========== GESTIONE ERRORI PERSONALIZZATA ==========

    private async Task HandleServiceError(Exception ex, string operation)
    {
        Debug.WriteLine($"[ADMIN] Errore durante {operation}: {ex.Message}");

        string userMessage = operation switch
        {
            "caricamento statistiche" => "Impossibile caricare le statistiche del dashboard",
            "caricamento post" => "Impossibile caricare la lista dei post",
            "caricamento commenti" => "Impossibile caricare la lista dei commenti",
            "caricamento utenti" => "Impossibile caricare la lista degli utenti",
            "eliminazione post" => "Impossibile eliminare il post selezionato",
            "eliminazione commento" => "Impossibile eliminare il commento selezionato",
            "modifica utente" => "Impossibile modificare lo stato dell'utente",
            _ => "Si è verificato un errore imprevisto"
        };

        await DisplayAlert("Errore", $"{userMessage}. Riprova più tardi.", "OK");
    }

    // ========== METODI DI UTILITÀ ==========

    private async Task<bool> ConfirmAction(string title, string message, string acceptText = "Conferma", string cancelText = "Annulla")
    {
        return await DisplayAlert(title, message, acceptText, cancelText);
    }

    private void ShowLoadingState(Button button, bool isLoading)
    {
        if (button == null) return;

        if (isLoading)
        {
            button.Text = "⏳ Caricamento...";
            button.IsEnabled = false;
        }
        else
        {
            // Il testo originale dovrebbe essere ripristinato dal chiamante
            button.IsEnabled = true;
        }
    }

    private string FormatUserActivity(AdminUserInfo user)
    {
        var activities = new List<string>();

        if (user.PostCreati > 0)
            activities.Add($"{user.PostCreati} post");

        if (user.CommentiScritti > 0)
            activities.Add($"{user.CommentiScritti} commenti");

        return activities.Any() ? string.Join(" • ", activities) : "Nessuna attività";
    }

    // ========== GESTIONE NAVIGAZIONE ==========

    protected override bool OnBackButtonPressed()
    {
        // Gestione personalizzata del pulsante indietro se necessario
        return base.OnBackButtonPressed();
    }

    private async void OnBackToMainClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore navigazione indietro: {ex.Message}");
        }
    }

    // ========== GESTIONE CICLO DI VITA ==========

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        Debug.WriteLine("[ADMIN] AdminPage OnDisappearing");
    }

    // ========== METODI DIAGNOSTICI ==========

    private void LogCurrentState()
    {
        Debug.WriteLine($"[ADMIN] Current State:");
        Debug.WriteLine($"  - Tab attivo: {_currentTab}");
        Debug.WriteLine($"  - Post caricati: {AllPosts.Count}");
        Debug.WriteLine($"  - Commenti caricati: {AllComments.Count}");
        Debug.WriteLine($"  - Utenti caricati: {AllUsers.Count}");
        Debug.WriteLine($"  - Filtro post: '{_postSearchFilter}'");
        Debug.WriteLine($"  - Filtro commenti: '{_commentSearchFilter}'");
        Debug.WriteLine($"  - Filtro utenti: '{_userSearchFilter}'");
    }

    private async Task ValidateDataIntegrity()
    {
        try
        {
            // Verifica integrità dei dati
            var invalidPosts = AllPosts.Where(p => string.IsNullOrEmpty(p.Titolo) || string.IsNullOrEmpty(p.AutoreEmail)).Count();
            var invalidComments = AllComments.Where(c => string.IsNullOrEmpty(c.Contenuto) || string.IsNullOrEmpty(c.AutoreEmail)).Count();
            var invalidUsers = AllUsers.Where(u => string.IsNullOrEmpty(u.Username) || string.IsNullOrEmpty(u.Email)).Count();

            if (invalidPosts > 0 || invalidComments > 0 || invalidUsers > 0)
            {
                Debug.WriteLine($"[ADMIN] ⚠️ Dati non validi trovati: {invalidPosts} post, {invalidComments} commenti, {invalidUsers} utenti");
            }
            else
            {
                Debug.WriteLine("[ADMIN] ✅ Tutti i dati sono validi");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore validazione dati: {ex.Message}");
        }
    }
}