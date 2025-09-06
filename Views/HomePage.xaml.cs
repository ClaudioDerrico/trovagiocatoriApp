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

    public HomePage()
    {
        InitializeComponent();

        bool hasSession = Preferences.ContainsKey("session_id") &&
                          !string.IsNullOrEmpty(Preferences.Get("session_id", ""));

        if (hasSession)
        {
            BindingContext = new HomePageViewModel();
        }
        else
        {
            BindingContext = new NavigationPage(new LoginPage());
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadNotificationsSummary();
    }

    // NUOVO: Carica il riassunto delle notifiche
    private async Task LoadNotificationsSummary()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/notifications/summary");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (result.ContainsKey("data") && result["data"] is JsonElement dataElement)
                {
                    var unreadCount = 0;
                    if (dataElement.TryGetProperty("unread_count", out var countElement))
                    {
                        unreadCount = countElement.GetInt32();
                    }

                    // Aggiorna UI badge notifiche
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        UpdateNotificationBadge(unreadCount);
                    });

                    Debug.WriteLine($"[NOTIFICATIONS] Caricato riassunto: {unreadCount} notifiche non lette");
                }
            }
            else
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore caricamento summary: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NOTIFICATIONS] Errore caricamento notifiche: {ex.Message}");
            // Non mostrare errori all'utente per le notifiche, è un'operazione in background
        }
    }

    // NUOVO: Aggiorna il badge delle notifiche
    private void UpdateNotificationBadge(int unreadCount)
    {
        try
        {
            if (NotificationBadge != null && NotificationCountLabel != null)
            {
                if (unreadCount > 0)
                {
                    NotificationBadge.IsVisible = true;
                    NotificationCountLabel.Text = unreadCount > 99 ? "99+" : unreadCount.ToString();
                    Debug.WriteLine($"[NOTIFICATIONS] Badge aggiornato: {unreadCount} notifiche");
                }
                else
                {
                    NotificationBadge.IsVisible = false;
                    Debug.WriteLine("[NOTIFICATIONS] Badge nascosto: nessuna notifica non letta");
                }
            }
            else
            {
                Debug.WriteLine("[NOTIFICATIONS] WARNING: NotificationBadge o NotificationCountLabel è null");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NOTIFICATIONS] Errore aggiornamento badge: {ex.Message}");
        }
    }

    // NUOVO: Navigazione alla pagina notifiche
    private async void OnNotificationsPageTapped(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new NotificationsPage());
            Debug.WriteLine("[NAVIGATION] Navigazione a NotificationsPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore navigazione notifiche: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire le notifiche", "OK");
        }
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

        try
        {
            // USA NAVIGATION TRADIZIONALE
            await Navigation.PushAsync(new PostPage(province, sport));
            Debug.WriteLine($"[NAVIGATION] Navigazione a PostPage: {province}, {sport}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore navigazione PostPage: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire la ricerca", "OK");
        }
    }

    private async void OnCreatePostButtonClicked(object sender, EventArgs e)
    {
        try
        {
            // USA NAVIGATION TRADIZIONALE
            await Navigation.PushAsync(new CreatePostPage());
            Debug.WriteLine("[NAVIGATION] Navigazione a CreatePostPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore navigazione CreatePostPage: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire la creazione post", "OK");
        }
    }

    private async void OnAboutAppTapped(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new AboutAppPage());
            Debug.WriteLine("[NAVIGATION] Navigazione a AboutAppPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore navigazione AboutAppPage: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire le informazioni", "OK");
        }
    }

    // Gestione navigazione alla pagina Amici
    private async void OnFriendsPageTapped(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new FriendsPage());
            Debug.WriteLine("[NAVIGATION] Navigazione a FriendsPage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore navigazione FriendsPage: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire la pagina amici", "OK");
        }
    }

    private async void ProfilePageTapped(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushAsync(new ProfilePage());
            Debug.WriteLine("[NAVIGATION] Navigazione a ProfilePage");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NAVIGATION] Errore navigazione ProfilePage: {ex.Message}");
            await DisplayAlert("Errore", "Impossibile aprire il profilo", "OK");
        }
    }

    //  Metodo per forzare l'aggiornamento delle notifiche (può essere chiamato da altre pagine)
    public async Task RefreshNotifications()
    {
        await LoadNotificationsSummary();
    }

    //  Metodo per gestire il cleanup quando la pagina viene nascosta
    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        // Eventuale cleanup se necessario
    }
}