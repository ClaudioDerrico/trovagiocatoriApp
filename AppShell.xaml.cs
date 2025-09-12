using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp;

public partial class AppShell : Shell
{
    private readonly HttpClient _client = new HttpClient();

    public AppShell()
    {
        InitializeComponent();
        _ = Task.Run(ConfigureShellAsync); // Configura in background
    }

    private async Task ConfigureShellAsync()
    {
        try
        {
            var isAdmin = await CheckIfUserIsAdminAsync();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (isAdmin)
                {
                    // Mostra solo tab admin
                    AdminTabBar.IsVisible = true;
                    UserTabBar.IsVisible = false;
                    CurrentItem = AdminTabBar;
                    Debug.WriteLine("[SHELL] ✅ Configurazione ADMIN attiva");
                }
                else
                {
                    // Mostra tab utente normale
                    AdminTabBar.IsVisible = false;
                    UserTabBar.IsVisible = true;
                    CurrentItem = UserTabBar;
                    Debug.WriteLine("[SHELL] ✅ Configurazione USER attiva");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SHELL] Errore configurazione: {ex.Message}");
            // Default: utente normale
            MainThread.BeginInvokeOnMainThread(() =>
            {
                AdminTabBar.IsVisible = false;
                UserTabBar.IsVisible = true;
                CurrentItem = UserTabBar;
            });
        }
    }

    private async Task<bool> CheckIfUserIsAdminAsync()
    {
        try
        {
            if (!Preferences.ContainsKey("session_id"))
                return false;

            var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/profile");
            string sessionId = Preferences.Get("session_id", "");
            request.Headers.Add("Cookie", $"session_id={sessionId}");

            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var user = JsonSerializer.Deserialize<User>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return user?.IsAdmin ?? false;
            }

            return false;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SHELL] Errore controllo admin: {ex.Message}");
            return false;
        }
    }

    // Metodo pubblico per riconfigurare dopo login
    public async Task ReconfigureAsync()
    {
        await ConfigureShellAsync();
    }
}