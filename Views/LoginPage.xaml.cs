using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Microsoft.Maui.Devices;

namespace trovagiocatoriApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private bool isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();

            // NUOVO: Pulisci sessioni residue al caricamento della pagina di login
            ClearPreviousSessions();
        }

        // NUOVO: Pulisce sessioni precedenti e flag
        private void ClearPreviousSessions()
        {
            try
            {
                // Pulisci tutte le preferences
                Preferences.Clear();

                Debug.WriteLine("[LOGIN] Sessioni precedenti pulite");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore pulizia sessioni: {ex.Message}");
            }
        }

        private void OnTogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            PasswordEntry.IsPassword = !isPasswordVisible;
            var button = sender as ImageButton;
            button.Source = isPasswordVisible ? "eye_close.png" : "eye_open.png";
        }

        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var loginButton = sender as Button;
            if (loginButton != null)
            {
                loginButton.IsEnabled = false;
                loginButton.Text = "Accesso in corso...";
            }

            try
            {
                var loginData = new
                {
                    email_or_username = EmailEntry.Text?.Trim(),
                    password = PasswordEntry.Text
                };

                if (string.IsNullOrEmpty(loginData.email_or_username) || string.IsNullOrEmpty(loginData.password))
                {
                    await DisplayAlert("Errore", "Inserisci email/username e password", "OK");
                    return;
                }

                string json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(30);

                var response = await client.PostAsync($"{ApiConfig.BaseUrl}/login", content);

                if (response.IsSuccessStatusCode)
                {
                    // Pulisci sessioni precedenti
                    Preferences.Clear();

                    // Salva il cookie di sessione
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                    {
                        var sessionCookie = cookies.FirstOrDefault(c => c.StartsWith("session_id="));
                        if (sessionCookie != null)
                        {
                            var sessionId = sessionCookie.Split(';')[0].Split('=')[1];
                            Preferences.Set("session_id", sessionId);
                            Preferences.Set("login_timestamp", DateTime.Now.ToString());

                            Debug.WriteLine($"[LOGIN] ✅ Session salvata: {sessionId}");
                        }
                    }

                    await DisplayAlert("Login", "Login eseguito con successo!", "OK");

                    // Crea la nuova Shell e riconfigura
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var appShell = new AppShell();
                        Application.Current.MainPage = appShell;

                        // Aspetta un attimo per la configurazione
                        await Task.Delay(500);
                        await appShell.ReconfigureAsync();
                    });
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Errore Login", errorMsg, "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore: {ex}");
                await DisplayAlert("Errore", "Errore durante il login. Riprova.", "OK");
            }
            finally
            {
                if (loginButton != null)
                {
                    loginButton.IsEnabled = true;
                    loginButton.Text = "ACCEDI";
                }
            }
        }

        private async void OnRegisterNowClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}