using trovagiocatoriApp.ViewModels;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Views;
using System.Text.Json;
using System.Net.Http;
using System.Diagnostics;

namespace trovagiocatoriApp.Views;

public partial class HomePage : ContentPage
{
    private readonly HttpClient _client = new HttpClient();
    private bool _hasShownAdminWelcome = false; // Flag per mostrare il messaggio di benvenuto admin una sola volta

    public HomePage()
    {
        InitializeComponent();
        BindingContext = HasValidSession() ? new HomePageViewModel() : new NavigationPage(new LoginPage());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNotificationsSummary();

        if (!_hasShownAdminWelcome)
        {
            await CheckAdminAccess();
        }
    }

    // Verifica se esiste una sessione valida
    private bool HasValidSession()
    {
        return Preferences.ContainsKey("session_id") && !string.IsNullOrEmpty(Preferences.Get("session_id", ""));
    }

    // Carica il riassunto delle notifiche non lette
    private async Task LoadNotificationsSummary()
    {
        try
        {
            var request = CreateAuthenticatedRequest(HttpMethod.Get, "/notifications/summary");
            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (result.ContainsKey("data") && result["data"] is JsonElement dataElement)
                {
                    var unreadCount = dataElement.TryGetProperty("unread_count", out var countElement)
                        ? countElement.GetInt32() : 0;

                    MainThread.BeginInvokeOnMainThread(() => UpdateNotificationBadge(unreadCount));
                    Debug.WriteLine($"[NOTIFICATIONS] {unreadCount} notifiche non lette");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NOTIFICATIONS] Errore: {ex.Message}");
        }
    }

    // Aggiorna il badge delle notifiche
    private void UpdateNotificationBadge(int unreadCount)
    {
        if (NotificationBadge?.IsVisible != null && NotificationCountLabel != null)
        {
            if (unreadCount > 0)
            {
                NotificationBadge.IsVisible = true;
                NotificationCountLabel.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
            }
            else
            {
                NotificationBadge.IsVisible = false;
            }
        }
    }

    // Controlla se l'utente è admin e mostra il messaggio di benvenuto
    private async Task CheckAdminAccess()
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

                if (user.IsAdmin && !_hasShownAdminWelcome)
                {
                    await ShowAdminWelcome();
                    _hasShownAdminWelcome = true;
                    Preferences.Set("admin_welcome_shown", true);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Errore controllo admin: {ex.Message}");
        }
    }

    // Mostra il messaggio di benvenuto per amministratori
    private async Task ShowAdminWelcome()
    {
        await DisplayAlert("Benvenuto Amministratore",
            "Hai effettuato l'accesso come amministratore.\n\nPuoi accedere al pannello di gestione dal tuo profilo.",
            "Capito");
        Debug.WriteLine("[ADMIN] Welcome message mostrato");
    }

    // Crea una richiesta HTTP autenticata con cookie di sessione
    private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string endpoint)
    {
        var request = new HttpRequestMessage(method, $"{ApiConfig.BaseUrl}{endpoint}");

        if (Preferences.ContainsKey("session_id"))
        {
            string sessionId = Preferences.Get("session_id", "");
            request.Headers.Add("Cookie", $"session_id={sessionId}");
        }

        return request;
    }

    // Forza l'aggiornamento delle notifiche (metodo pubblico per altre pagine)
    public async Task RefreshNotifications()
    {
        await LoadNotificationsSummary();
    }

    // Resetta il flag di benvenuto admin (da chiamare al logout)
    public static void ResetAdminWelcome()
    {
        Preferences.Remove("admin_welcome_shown");
        Debug.WriteLine("[ADMIN] Flag welcome resettato");
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _hasShownAdminWelcome = Preferences.Get("admin_welcome_shown", false);
    }

    // ========== EVENT HANDLERS ==========

    private async void OnNotificationsPageTapped(object sender, EventArgs e)
    {
        await SafeNavigate(() => new NotificationsPage());
    }

    private async void PostPageClicked(object sender, EventArgs e)
    {
        var region = RegionPicker.SelectedItem?.ToString();
        var province = ProvincePicker.SelectedItem?.ToString();
        var sport = SportPicker.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(sport) || string.IsNullOrEmpty(region))
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = "Compilare Regione, Provincia e Sport.";
            return;
        }

        ErrorLabel.IsVisible = false;
        await SafeNavigate(() => new PostPage(province, sport));
    }

    private async void OnCreatePostButtonClicked(object sender, EventArgs e)
    {
        await SafeNavigate(() => new CreatePostPage());
    }

    private async void OnAboutAppTapped(object sender, EventArgs e)
    {
        await SafeNavigate(() => new AboutAppPage());
    }

    private async void OnFriendsPageTapped(object sender, EventArgs e)
    {
        await SafeNavigate(() => new FriendsPage());
    }

    private async void ProfilePageTapped(object sender, EventArgs e)
    {
        await SafeNavigate(() => new ProfilePage());
    }

    // Metodo helper per navigazione sicura con gestione errori
    private async Task SafeNavigate(Func<ContentPage> pageFactory)
    {
        try
        {
            await Navigation.PushAsync(pageFactory());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire la pagina richiesta", "OK");
        }
    }
}