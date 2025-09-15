using System.Collections.ObjectModel;
using System.Text.Json;
using System.Diagnostics;
using trovagiocatoriApp.Models;
using trovagiocatoriApp.Services;

namespace trovagiocatoriApp.Views;

public partial class AdminPage : ContentPage
{
    private readonly IAdminService _adminService;
    private readonly IBanService _banService;
    private string _currentAdminEmail = "";

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

                // NUOVO: Salva l'email dell'admin corrente
                _currentAdminEmail = user.Email;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AdminInfoLabel.Text = $"Amministratore: {user.Username} ({user.Email})";
                });

                Debug.WriteLine($"[ADMIN] Admin corrente: {_currentAdminEmail}");
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
            Debug.WriteLine("[ADMIN] Aggiornamento statistiche dashboard con dati locali...");

            // Usa i dati già caricati nelle collezioni locali
            int totalPosts = AllPosts.Count;
            int totalComments = AllComments.Count;
            int totalUsers = AllUsers.Count;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalPostsLabel.Text = totalPosts.ToString();
                TotalCommentsLabel.Text = totalComments.ToString();
                TotalUsersLabel.Text = totalUsers.ToString();
                // Rimossa la riga TotalFieldsLabel
            });

            Debug.WriteLine($"[ADMIN] ✅ Statistiche aggiornate: {totalPosts} post, {totalComments} commenti, {totalUsers} utenti");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore aggiornamento stats: {ex.Message}");

            // Fallback con statistiche di default
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalPostsLabel.Text = "0";
                TotalCommentsLabel.Text = "0";
                TotalUsersLabel.Text = "0";
                // Rimossa la riga TotalFieldsLabel
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
            // Per ogni utente, controlla se è bannato (senza controlli di scadenza)
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
            // NUOVO: Escludi l'admin corrente dalla lista
            !u.Email.Equals(_currentAdminEmail, StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrEmpty(_userSearchFilter) ||
            u.Username.ToLower().Contains(_userSearchFilter) ||
            u.Email.ToLower().Contains(_userSearchFilter) ||
            u.Nome.ToLower().Contains(_userSearchFilter) ||
            u.Cognome.ToLower().Contains(_userSearchFilter))
        ).ToList();

        UsersCollectionView.ItemsSource = filtered;

        Debug.WriteLine($"[ADMIN] Filtrati {filtered.Count} utenti (escluso admin corrente: {_currentAdminEmail})");
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

    private async void OnViewCommentPostClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminCommentInfo comment)
        {
            try
            {
                // Naviga al dettaglio del post associato al commento
                await Navigation.PushAsync(new PostDetailMainPage(comment.PostId));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN] Errore apertura post del commento {comment.Id}: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire il post associato", "OK");
            }
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
                $"⚠️ Questa azione è irreversibile e eliminerà anche tutti i commenti associati!",
                "Elimina",
                "Annulla"
            );

            if (!confirm) return;

            Debug.WriteLine($"[ADMIN] Eliminazione post {post.Id}...");

            bool success = await _adminService.DeletePostAsync(post.Id);

            if (success)
            {
                // Rimuovi il post dalla collezione
                AllPosts.Remove(post);

                // Rimuovi tutti i commenti associati al post eliminato
                var commentsToRemove = AllComments.Where(c => c.PostId == post.Id).ToList();
                foreach (var comment in commentsToRemove)
                {
                    AllComments.Remove(comment);
                    Debug.WriteLine($"[ADMIN] Rimosso commento {comment.Id} associato al post {post.Id}");
                }

                // Riapplica i filtri per aggiornare entrambe le viste
                FilterPosts();
                FilterComments();

                // AGGIORNA IMMEDIATAMENTE LE STATISTICHE
                LoadDashboardStats();

                await DisplayAlert("Successo",
                    $"Post '{post.Titolo}' eliminato con successo!\n" +
                    $"Eliminati anche {commentsToRemove.Count} commenti associati.",
                    "OK");

                Debug.WriteLine($"[ADMIN] ✅ Post {post.Id} eliminato con successo insieme a {commentsToRemove.Count} commenti");
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
                $"💬 {comment.ContenutoPreview}\n" +
                $"👤 di {comment.AutoreEmail}\n" +
                $"📝 nel post: {comment.PostTitolo}\n" +
                $"📅 {comment.DataCreazione:dd/MM/yyyy HH:mm}\n\n" +
                $"⚠️ Questa azione è irreversibile!",
                "Elimina",
                "Annulla"
            );

            if (!confirm) return;

            Debug.WriteLine($"[ADMIN] Eliminazione commento {comment.Id}...");

            bool success = await _adminService.DeleteCommentAsync(comment.Id);

            if (success)
            {
                // Rimuovi il commento dalla collezione
                AllComments.Remove(comment);

                // Riapplica i filtri per aggiornare la vista
                FilterComments();

                // AGGIORNA IMMEDIATAMENTE LE STATISTICHE
                LoadDashboardStats();

                await DisplayAlert("Successo", "Commento eliminato con successo!", "OK");

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
                // L'utente è attivo -> BANNA PERMANENTEMENTE
                bool confirm = await DisplayAlert(
                    "Ban Permanente",
                    $"Sei sicuro di voler bannare permanentemente {user.Username}?\n\n" +
                    $"👤 {user.NomeCompleto}\n" +
                    $"📧 {user.Email}\n\n" +
                    $"⚠️ Il ban sarà PERMANENTE e l'utente non potrà più accedere alla piattaforma!\n\n" +
                    $"Questa azione può essere revocata solo manualmente dall'amministratore.",
                    "Ban Permanente",
                    "Annulla"
                );

                if (!confirm) return;

                Debug.WriteLine($"[ADMIN] Bannando permanentemente utente {user.Username}...");

                // Crea richiesta di ban permanente
                var banRequest = new BanUserRequest
                {
                    UserId = user.Id,
                    Reason = "Ban amministrativo permanente",
                    BanType = "permanent",
                    Notes = "Utente bannato permanentemente dall'amministratore"
                };

                var response = await _banService.BanUserAsync(banRequest);

                if (response.Success)
                {
                    // Aggiorna lo stato dell'utente nella lista
                    user.IsActive = false;
                    FilterUsers();

                    await DisplayAlert("Ban Applicato",
                        $"Utente {user.Username} bannato permanentemente!\n\n" +
                        $"Non potrà più accedere alla piattaforma fino a revoca manuale del ban.",
                        "OK");
                    Debug.WriteLine($"[ADMIN] ✅ Utente {user.Username} bannato permanentemente");
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
                    "Revoca Ban",
                    $"Vuoi revocare il ban per {user.Username}?\n\n" +
                    $"👤 {user.NomeCompleto}\n" +
                    $"📧 {user.Email}\n\n" +
                    $"L'utente potrà nuovamente accedere alla piattaforma.",
                    "Revoca Ban",
                    "Annulla"
                );

                if (!confirm) return;

                Debug.WriteLine($"[ADMIN] Revocando ban per utente {user.Username}...");

                var response = await _banService.UnbanUserAsync(user.Id);

                if (response.Success)
                {
                    // Aggiorna lo stato dell'utente nella lista
                    user.IsActive = true;
                    FilterUsers();

                    await DisplayAlert("Ban Revocato",
                        $"Ban revocato per {user.Username}!\n\n" +
                        $"L'utente può nuovamente accedere alla piattaforma.",
                        "OK");
                    Debug.WriteLine($"[ADMIN] ✅ Ban revocato per {user.Username}");
                }
                else
                {
                    await DisplayAlert("Errore", response.Error ?? "Errore durante la revoca del ban", "OK");
                    Debug.WriteLine($"[ADMIN] ❌ Errore revoca ban {user.Username}: {response.Error}");
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
}