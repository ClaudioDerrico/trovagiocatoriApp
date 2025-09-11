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
            // NUOVO: Disabilita il pulsante durante il login per evitare doppi click
            var loginButton = sender as Button;
            if (loginButton != null)
            {
                loginButton.IsEnabled = false;
                loginButton.Text = "Accesso in corso...";
            }

            try
            {
                // Raccogli i dati del login
                var loginData = new
                {
                    email_or_username = EmailEntry.Text?.Trim(),
                    password = PasswordEntry.Text
                };

                // Validazione input
                if (string.IsNullOrEmpty(loginData.email_or_username) || string.IsNullOrEmpty(loginData.password))
                {
                    await DisplayAlert("Errore", "Inserisci email/username e password", "OK");
                    return;
                }

                string json = JsonSerializer.Serialize(loginData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var baseUrl = ApiConfig.BaseUrl;

                using var client = new HttpClient();

                //  Timeout più breve per evitare hang
                client.Timeout = TimeSpan.FromSeconds(30);

                var response = await client.PostAsync($"{baseUrl}/login", content);

                if (response.IsSuccessStatusCode)
                {
                    //Pulisci sessioni precedenti prima di salvare la nuova
                    Preferences.Clear();

                    // Salva il cookie di sessione
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                    {
                        var sessionCookie = cookies.FirstOrDefault(c => c.StartsWith("session_id="));
                        if (sessionCookie != null)
                        {
                            var sessionId = sessionCookie.Split(';')[0].Split('=')[1];
                            Preferences.Set("session_id", sessionId);

                            // NUOVO: Salva timestamp login per debug
                            Preferences.Set("login_timestamp", DateTime.Now.ToString());

                            Debug.WriteLine($"[LOGIN] ✅ Session salvata: {sessionId}");
                        }
                    }

                    await DisplayAlert("Login", "Login eseguito con successo!", "OK");

                    // NUOVO: Naviga con MainThread per evitare problemi
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        Application.Current.MainPage = new AppShell();
                    });
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[LOGIN] Errore login: {response.StatusCode} - {errorMsg}");
                    await DisplayAlert("Errore Login", errorMsg, "OK");
                }
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"[LOGIN] Errore HTTP: {httpEx}");
                await DisplayAlert("Errore di connessione",
                    "Impossibile raggiungere il server. Controlla la connessione di rete.", "OK");
            }
            catch (TaskCanceledException timeoutEx)
            {
                Debug.WriteLine($"[LOGIN] Timeout: {timeoutEx}");
                await DisplayAlert("Timeout",
                    "Il server non risponde. Riprova tra qualche momento.", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore generico: {ex}");
                await DisplayAlert("Errore",
                    "Si è verificato un errore imprevisto. Riprova.", "OK");
            }
            finally
            {
                // NUOVO: Riabilita sempre il pulsante
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