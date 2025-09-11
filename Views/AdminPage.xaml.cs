using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Text;

namespace trovagiocatoriApp.Views;

public partial class AdminPage : ContentPage
{
    private readonly HttpClient _client = new HttpClient();

    public AdminPage()
    {
        InitializeComponent();
        LoadAdminInfo();
        LoadStats();
    }

    private async void LoadAdminInfo()
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

                AdminInfoLabel.Text = $"Amministratore: {user.Username} ({user.Email})";
            }
        }
        catch (Exception ex)
        {
            AdminInfoLabel.Text = $"Errore caricamento info: {ex.Message}";
        }
    }

    private async void LoadStats()
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
                    TotalPostsLabel.Text = stats.TryGetValue("total_posts", out var posts) ? posts.ToString() : "0";
                    TotalCommentsLabel.Text = stats.TryGetValue("total_comments", out var comments) ? comments.ToString() : "0";
                    TotalFieldsLabel.Text = stats.TryGetValue("total_sport_fields", out var fields) ? fields.ToString() : "0";
                }
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Impossibile caricare le statistiche: {ex.Message}", "OK");
        }
    }

    private async void OnRefreshStatsClicked(object sender, EventArgs e)
    {
        LoadStats();
        await DisplayAlert("Aggiornato", "Statistiche aggiornate!", "OK");
    }

    private async void OnDeletePostClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PostIdEntry.Text))
        {
            await DisplayAlert("Errore", "Inserisci un ID post valido", "OK");
            return;
        }

        if (!int.TryParse(PostIdEntry.Text, out int postId))
        {
            await DisplayAlert("Errore", "ID post deve essere un numero", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Conferma", $"Sei sicuro di voler eliminare il post {postId}?", "Elimina", "Annulla");
        if (!confirm) return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}/admin/posts/{postId}");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Successo", $"Post {postId} eliminato con successo!", "OK");
                PostIdEntry.Text = "";
                LoadStats(); // Aggiorna le statistiche
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore", $"Impossibile eliminare il post: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Errore nell'eliminazione: {ex.Message}", "OK");
        }
    }

    private async void OnDeleteCommentClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommentIdEntry.Text))
        {
            await DisplayAlert("Errore", "Inserisci un ID commento valido", "OK");
            return;
        }

        if (!int.TryParse(CommentIdEntry.Text, out int commentId))
        {
            await DisplayAlert("Errore", "ID commento deve essere un numero", "OK");
            return;
        }

        bool confirm = await DisplayAlert("Conferma", $"Sei sicuro di voler eliminare il commento {commentId}?", "Elimina", "Annulla");
        if (!confirm) return;

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Delete, $"{ApiConfig.BaseUrl}/admin/comments/{commentId}");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                await DisplayAlert("Successo", $"Commento {commentId} eliminato con successo!", "OK");
                CommentIdEntry.Text = "";
                LoadStats(); // Aggiorna le statistiche
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore", $"Impossibile eliminare il commento: {error}", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Errore nell'eliminazione: {ex.Message}", "OK");
        }
    }
}