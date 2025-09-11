using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Text;
using System.Diagnostics;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views;

public partial class AdminPage : ContentPage
{
    private readonly HttpClient _client = new HttpClient();

    // Collections per i dati
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
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAdminInfo();
        await LoadDashboardStats();
        await LoadAllData();
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

            var response = await _client.SendAsync(request);
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

    private async Task LoadDashboardStats()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.PythonApiUrl}/admin/stats");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var stats = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (stats != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        TotalPostsLabel.Text = stats.TryGetValue("total_posts", out var posts) ? posts.ToString() : "0";
                        TotalCommentsLabel.Text = stats.TryGetValue("total_comments", out var comments) ? comments.ToString() : "0";
                        TotalUsersLabel.Text = stats.TryGetValue("total_users", out var users) ? users.ToString() : "0";
                        TotalFieldsLabel.Text = stats.TryGetValue("total_sport_fields", out var fields) ? fields.ToString() : "0";
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento stats: {ex.Message}");
        }
    }

    private async Task LoadAllData()
    {
        await LoadAllPosts();
        await LoadAllComments();
        await LoadAllUsers();
    }

    private async Task LoadAllPosts()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.PythonApiUrl}/admin/posts");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var posts = JsonSerializer.Deserialize<List<AdminPostInfo>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPosts.Clear();
                    foreach (var post in posts ?? new List<AdminPostInfo>())
                    {
                        AllPosts.Add(post);
                    }

                    PostsCollectionView.ItemsSource = AllPosts;
                    Debug.WriteLine($"[ADMIN] Caricati {AllPosts.Count} post");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento post: {ex.Message}");
        }
    }

    private async Task LoadAllComments()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.PythonApiUrl}/admin/comments");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var comments = JsonSerializer.Deserialize<List<AdminCommentInfo>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllComments.Clear();
                    foreach (var comment in comments ?? new List<AdminCommentInfo>())
                    {
                        AllComments.Add(comment);
                    }

                    CommentsCollectionView.ItemsSource = AllComments;
                    Debug.WriteLine($"[ADMIN] Caricati {AllComments.Count} commenti");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento commenti: {ex.Message}");
        }
    }

    private async Task LoadAllUsers()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/admin/users");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var users = JsonSerializer.Deserialize<List<AdminUserInfo>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllUsers.Clear();
                    foreach (var user in users ?? new List<AdminUserInfo>())
                    {
                        AllUsers.Add(user);
                    }

                    UsersCollectionView.ItemsSource = AllUsers;
                    Debug.WriteLine($"[ADMIN] Caricati {AllUsers.Count} utenti");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento utenti: {ex.Message}");
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

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}/admin/posts/{post.Id}");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                AllPosts.Remove(post);
                FilterPosts();
                await DisplayAlert("Successo", $"Post '{post.Titolo}' eliminato con successo!", "OK");
                await LoadDashboardStats(); // Aggiorna le statistiche
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore", $"Impossibile eliminare il post: {error}", "OK");
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
                $"💬 {comment.Contenuto}\n" +
                $"👤 di {comment.AutoreEmail}\n" +
                $"📝 nel post: {comment.PostTitolo}\n" +
                $"📅 {comment.DataCreazione:dd/MM/yyyy}\n\n" +
                $"⚠️ Questa azione è irreversibile!",
                "Elimina",
                "Annulla"
            );

            if (!confirm) return;

            var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}/admin/comments/{comment.Id}");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                AllComments.Remove(comment);
                FilterComments();
                await DisplayAlert("Successo", "Commento eliminato con successo!", "OK");
                await LoadDashboardStats(); // Aggiorna le statistiche
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore", $"Impossibile eliminare il commento: {error}", "OK");
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
            string action = user.IsActive ? "disattivare" : "riattivare";
            string actionCaps = user.IsActive ? "Disattiva" : "Riattiva";

            bool confirm = await DisplayAlert(
                $"{actionCaps} Utente",
                $"Sei sicuro di voler {action} l'utente:\n\n" +
                $"👤 {user.Username} ({user.Nome} {user.Cognome})\n" +
                $"📧 {user.Email}\n" +
                $"📅 Registrato: {user.DataRegistrazione:dd/MM/yyyy}",
                actionCaps,
                "Annulla"
            );

            if (!confirm) return;

            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiConfig.BaseUrl}/admin/users/{user.Id}/toggle-status");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                user.IsActive = !user.IsActive;
                FilterUsers();
                await DisplayAlert("Successo", $"Utente {(user.IsActive ? "riattivato" : "disattivato")} con successo!", "OK");
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore", $"Impossibile modificare lo stato dell'utente: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore toggle utente: {ex.Message}");
            await DisplayAlert("Errore", "Errore nella modifica dello stato utente", "OK");
        }
    }

    // ========== AZIONI RAPIDE ==========

    private async void OnRefreshStatsClicked(object sender, EventArgs e)
    {
        await LoadDashboardStats();
        await LoadAllData();
        await DisplayAlert("Aggiornato", "Dati aggiornati con successo!", "OK");
    }

    private async void OnViewPostDetailsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminPostInfo post)
        {
            await Navigation.PushAsync(new PostDetailMainPage(post.Id));
        }
    }

    private async void OnViewUserPostsClicked(object sender, EventArgs e)
    {
        if (sender is Button button && button.CommandParameter is AdminUserInfo user)
        {
            var userPosts = AllPosts.Where(p => p.AutoreEmail == user.Email).ToList();

            if (userPosts.Any())
            {
                var postTitles = string.Join("\n• ", userPosts.Select(p => p.Titolo));
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
    }
}

// ========== MODELLI DATI ADMIN ==========

public class AdminPostInfo
{
    public int Id { get; set; }
    public string Titolo { get; set; }
    public string AutoreEmail { get; set; }
    public string Sport { get; set; }
    public string Citta { get; set; }
    public string Provincia { get; set; }
    public DateTime DataCreazione { get; set; }
    public DateTime DataPartita { get; set; }
    public int NumeroGiocatori { get; set; }
    public int PartecipantiIscritti { get; set; }
    public string Status { get; set; }

    public string DataCreazioneFormatted => DataCreazione.ToString("dd/MM/yyyy HH:mm");
    public string DataPartitaFormatted => DataPartita.ToString("dd/MM/yyyy HH:mm");
    public string PostiLiberi => $"{Math.Max(0, NumeroGiocatori - PartecipantiIscritti)}";
    public Color StatusColor => Status == "Completo" ? Colors.Red : Colors.Green;
}

public class AdminCommentInfo
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string PostTitolo { get; set; }
    public string AutoreEmail { get; set; }
    public string Contenuto { get; set; }
    public DateTime DataCreazione { get; set; }

    public string DataCreazioneFormatted => DataCreazione.ToString("dd/MM/yyyy HH:mm");
    public string ContenutoPreview => Contenuto.Length > 100 ? $"{Contenuto.Substring(0, 100)}..." : Contenuto;
}

public class AdminUserInfo
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Nome { get; set; }
    public string Cognome { get; set; }
    public string Email { get; set; }
    public DateTime DataRegistrazione { get; set; }
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
    public int PostCreati { get; set; }
    public int CommentiScritti { get; set; }

    public string DataRegistrazioneFormatted => DataRegistrazione.ToString("dd/MM/yyyy");
    public string NomeCompleto => $"{Nome} {Cognome}";
    public string StatusText => IsActive ? "Attivo" : "Disattivato";
    public Color StatusColor => IsActive ? Colors.Green : Colors.Red;
    public string AdminBadge => IsAdmin ? "👑 ADMIN" : "";
    public string AttivitaText => $"{PostCreati} post • {CommentiScritti} commenti";
}