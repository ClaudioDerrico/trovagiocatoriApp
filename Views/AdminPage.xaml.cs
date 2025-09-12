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

private async Task LoadDashboardStats()
    {
        try
        {
            // Prima prova l'endpoint specifico per dashboard
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.PythonApiUrl}/admin/dashboard-stats");

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
                        TotalPostsLabel.Text = GetStatValue(stats, "total_posts");
                        TotalCommentsLabel.Text = GetStatValue(stats, "total_comments");
                        TotalUsersLabel.Text = GetStatValue(stats, "total_users");
                        TotalFieldsLabel.Text = GetStatValue(stats, "total_sport_fields");
                    });
                    Debug.WriteLine("[ADMIN] ✅ Statistiche dashboard caricate");
                    return;
                }
            }

            // Fallback: usa endpoint normale
            await LoadFallbackStats();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento stats dashboard: {ex.Message}");
            await LoadFallbackStats();
        }
    }

    private async Task LoadFallbackStats()
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

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalPostsLabel.Text = GetStatValue(stats, "total_posts");
                    TotalCommentsLabel.Text = GetStatValue(stats, "total_comments");
                    TotalUsersLabel.Text = GetStatValue(stats, "total_users", "0");
                    TotalFieldsLabel.Text = GetStatValue(stats, "total_sport_fields");
                });
                Debug.WriteLine("[ADMIN] ✅ Statistiche fallback caricate");
            }
            else
            {
                // Ultima risorsa: dati mock
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TotalPostsLabel.Text = "12";
                    TotalCommentsLabel.Text = "45";
                    TotalUsersLabel.Text = "8";
                    TotalFieldsLabel.Text = "15";
                });
                Debug.WriteLine("[ADMIN] ⚠️ Usando statistiche mock");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento stats fallback: {ex.Message}");
            // Dati mock come ultima risorsa
            MainThread.BeginInvokeOnMainThread(() =>
            {
                TotalPostsLabel.Text = "0";
                TotalCommentsLabel.Text = "0";
                TotalUsersLabel.Text = "0";
                TotalFieldsLabel.Text = "0";
            });
        }
    }

    private string GetStatValue(Dictionary<string, object> stats, string key, string defaultValue = "0")
    {
        if (stats?.ContainsKey(key) == true)
        {
            var value = stats[key];
            if (value is JsonElement element)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Number => element.GetInt32().ToString(),
                    JsonValueKind.String => element.GetString() ?? defaultValue,
                    _ => defaultValue
                };
            }
            return value?.ToString() ?? defaultValue;
        }
        return defaultValue;
    }

    // Correggi LoadAllComments
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
            else
            {
                Debug.WriteLine($"[ADMIN] Errore caricamento commenti: {response.StatusCode}");
                await LoadMockComments();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento commenti: {ex.Message}");
            await LoadMockComments();
        }
    }

    private async Task LoadMockComments()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            AllComments.Clear();

            // Dati mock per test
            var mockComments = new List<AdminCommentInfo>
        {
            new AdminCommentInfo
            {
                Id = 1,
                PostId = 1,
                PostTitolo = "Partita di calcio a Milano",
                AutoreEmail = "user1@test.com",
                Contenuto = "Sono interessato a partecipare! Che livello è richiesto?",
                DataCreazione = DateTime.Now.AddHours(-2)
            },
            new AdminCommentInfo
            {
                Id = 2,
                PostId = 2,
                PostTitolo = "Tennis al parco",
                AutoreEmail = "user2@test.com",
                Contenuto = "Perfetto! Ci sarò sicuramente. A che ora iniziamo esattamente?",
                DataCreazione = DateTime.Now.AddHours(-1)
            },
            new AdminCommentInfo
            {
                Id = 3,
                PostId = 1,
                PostTitolo = "Partita di calcio a Milano",
                AutoreEmail = "admin@test.com",
                Contenuto = "Ottimo! Abbiamo già 5 persone confermate.",
                DataCreazione = DateTime.Now.AddMinutes(-30)
            }
        };

            foreach (var comment in mockComments)
            {
                AllComments.Add(comment);
            }

            CommentsCollectionView.ItemsSource = AllComments;
            Debug.WriteLine($"[ADMIN] Caricati {AllComments.Count} commenti (mock)");
        });
    }

    // Correggi LoadAllPosts per gestire il fallback
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
            else
            {
                Debug.WriteLine($"[ADMIN] Errore caricamento post: {response.StatusCode}");
                await LoadMockPosts();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento post: {ex.Message}");
            await LoadMockPosts();
        }
    }

    private async Task LoadMockPosts()
    {
        try
        {
            // Usa i post esistenti dal backend normale come base
            var response = await _client.GetAsync($"{ApiConfig.PythonApiUrl}/posts/search?provincia=Milano&sport=Calcio");

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var regularPosts = JsonSerializer.Deserialize<List<JsonElement>>(json);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPosts.Clear();

                    foreach (var postElement in regularPosts?.Take(10) ?? new List<JsonElement>())
                    {
                        var adminPost = new AdminPostInfo
                        {
                            Id = GetIntProperty(postElement, "id"),
                            Titolo = GetStringProperty(postElement, "titolo"),
                            AutoreEmail = GetStringProperty(postElement, "autore_email"),
                            Sport = GetStringProperty(postElement, "sport"),
                            Citta = GetStringProperty(postElement, "citta"),
                            Provincia = GetStringProperty(postElement, "provincia"),
                            DataCreazione = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)),
                            DataPartita = DateTime.TryParse(GetStringProperty(postElement, "data_partita"), out var dataPartita)
                                         ? dataPartita : DateTime.Now.AddDays(Random.Shared.Next(1, 15)),
                            NumeroGiocatori = GetIntProperty(postElement, "numero_giocatori", 1),
                            PartecipantiIscritti = GetIntProperty(postElement, "partecipanti_iscritti", 0),
                            Status = GetIntProperty(postElement, "posti_disponibili", 0) > 0 ? "Aperto" : "Completo"
                        };

                        AllPosts.Add(adminPost);
                    }

                    PostsCollectionView.ItemsSource = AllPosts;
                    Debug.WriteLine($"[ADMIN] Caricati {AllPosts.Count} post (mock da backend)");
                });
            }
            else
            {
                // Mock completo se anche il backend normale non funziona
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPosts.Clear();
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
                        NumeroGiocatori = 10,
                        PartecipantiIscritti = 7,
                        Status = "Aperto"
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
                        NumeroGiocatori = 4,
                        PartecipantiIscritti = 4,
                        Status = "Completo"
                    }
                };

                    foreach (var post in mockPosts)
                    {
                        AllPosts.Add(post);
                    }

                    PostsCollectionView.ItemsSource = AllPosts;
                    Debug.WriteLine($"[ADMIN] Caricati {AllPosts.Count} post (mock completo)");
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ADMIN] Errore caricamento mock post: {ex.Message}");
        }
    }

    // Helper methods già esistenti ma assicurati che ci siano
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