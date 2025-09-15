using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private bool isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
            ClearPreviousSessions();
        }

        // Pulisce sessioni precedenti e flag al caricamento della pagina di login
        private void ClearPreviousSessions()
        {
            try
            {
                Preferences.Clear();
                Debug.WriteLine("[LOGIN] Sessioni precedenti pulite");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore pulizia sessioni: {ex.Message}");
            }
        }

        // Attiva/disattiva la visibilità della password
        private void OnTogglePasswordVisibility(object sender, EventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;
            PasswordEntry.IsPassword = !isPasswordVisible;
            var button = sender as ImageButton;
            button.Source = isPasswordVisible ? "eye_close.png" : "eye_open.png";
        }

        // Gestisce il processo di login
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var loginButton = sender as Button;
            SetButtonLoadingState(loginButton, true);

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
                var responseJson = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[LOGIN] Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    await HandleSuccessfulLogin(response);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    await HandleBannedUser(responseJson);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    await DisplayAlert("Errore Login", "Credenziali non valide. Controlla email/username e password.", "OK");
                }
                else
                {
                    await HandleLoginError(responseJson);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore: {ex}");
                await DisplayAlert("Errore", "Errore durante il login. Controlla la connessione e riprova.", "OK");
            }
            finally
            {
                SetButtonLoadingState(loginButton, false);
            }
        }

        // Imposta lo stato di caricamento del pulsante
        private void SetButtonLoadingState(Button button, bool isLoading)
        {
            if (button != null)
            {
                button.IsEnabled = !isLoading;
                button.Text = isLoading ? "Accesso in corso..." : "ACCEDI";
            }
        }

        // Gestisce il login riuscito salvando la sessione
        private async Task HandleSuccessfulLogin(HttpResponseMessage response)
        {
            try
            {
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

                // Configura la nuova Shell
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    var appShell = new AppShell();
                    Application.Current.MainPage = appShell;
                    await Task.Delay(500);
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore handling successful login: {ex.Message}");
                await DisplayAlert("Errore", "Errore nella configurazione della sessione", "OK");
            }
        }

        // Gestisce il caso di utente bannato
        private async Task HandleBannedUser(string responseJson)
        {
            try
            {
                var loginResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (loginResponse?.BanInfo != null)
                {
                    await ShowBanDialog(loginResponse.BanInfo, loginResponse.Error);
                }
                else
                {
                    await DisplayAlert("Account Sospeso",
                        "Il tuo account è stato sospeso. Contatta l'amministratore per maggiori informazioni.",
                        "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore parsing ban info: {ex.Message}");
                await DisplayAlert("Account Sospeso",
                    "Il tuo account è stato sospeso. Contatta l'amministratore per maggiori informazioni.",
                    "OK");
            }
        }

        // Gestisce errori generici di login
        private async Task HandleLoginError(string responseJson)
        {
            var errorMessage = "Errore durante il login. Riprova più tardi.";
            try
            {
                var errorResponse = JsonSerializer.Deserialize<LoginResponse>(responseJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (!string.IsNullOrEmpty(errorResponse?.Error))
                {
                    errorMessage = errorResponse.Error;
                }
            }
            catch
            {
                // Usa il messaggio di default se la deserializzazione fallisce
            }

            await DisplayAlert("Errore Login", errorMessage, "OK");
        }

        // Mostra il dialog dettagliato per utenti bannati
        private async Task ShowBanDialog(BanInfo banInfo, string errorMessage)
        {
            try
            {
                var title = "🚫 Account Sospeso Permanentemente";

                var message = new StringBuilder();
                message.AppendLine("Il tuo account è stato sospeso permanentemente.");
                message.AppendLine();

                if (!string.IsNullOrEmpty(banInfo.Reason))
                {
                    message.AppendLine($"📋 Motivo: {banInfo.Reason}");
                    message.AppendLine();
                }

                message.AppendLine($"📅 Data sospensione: {banInfo.BannedAt:dd/MM/yyyy HH:mm}");
                message.AppendLine("⏰ Durata: Permanente");
                message.AppendLine();
                message.AppendLine("❌ Questo ban è permanente e non ha scadenza.");
                message.AppendLine("Solo un amministratore può revocare questa sospensione.");
                message.AppendLine();
                message.AppendLine("📧 Per richiedere la revoca del ban, contatta l'amministratore del sito.");

                bool contactAdmin = await DisplayAlert(
                    title,
                    message.ToString(),
                    "Contatta Admin",
                    "OK"
                );

                if (contactAdmin)
                {
                    await HandleContactAdmin();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore showing ban dialog: {ex.Message}");
                await DisplayAlert("Account Sospeso",
                    "Il tuo account è stato sospeso permanentemente. Contatta l'amministratore per la revoca.",
                    "OK");
            }
        }

        // Gestisce le opzioni per contattare l'amministratore
        private async Task HandleContactAdmin()
        {
            try
            {
                var options = new string[] { "Invia Email", "Copia Email Admin", "Annulla" };

                var choice = await DisplayActionSheet(
                    "Come vuoi contattare l'amministratore?",
                    "Annulla",
                    null,
                    options
                );

                switch (choice)
                {
                    case "Invia Email":
                        await OpenEmailApp();
                        break;
                    case "Copia Email Admin":
                        await CopyAdminEmail();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore contact admin: {ex.Message}");
            }
        }

        // Apre l'app email con messaggio precompilato
        private async Task OpenEmailApp()
        {
            try
            {
                var message = new EmailMessage
                {
                    Subject = "Richiesta Revoca Ban - Account Sospeso Permanentemente",
                    Body = "Buongiorno,\n\nIl mio account è stato sospeso permanentemente e vorrei richiedere la revoca del ban.\n\nSpiego i motivi della mia richiesta:\n[Inserisci qui la tua spiegazione]\n\nGrazie per l'attenzione e spero in una vostra risposta.",
                    To = new List<string> { "admin@trovagiocatori.com" }
                };

                await Email.Default.ComposeAsync(message);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore opening email: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire l'app email. Prova a copiare l'email dell'admin.", "OK");
            }
        }

        // Copia l'email dell'amministratore negli appunti
        private async Task CopyAdminEmail()
        {
            try
            {
                await Clipboard.Default.SetTextAsync("admin@trovagiocatori.com");
                await DisplayAlert("Email Copiata", "Email dell'amministratore copiata negli appunti!", "OK");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore copying email: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile copiare l'email. Email admin: admin@trovagiocatori.com", "OK");
            }
        }

        // Naviga alla pagina di registrazione
        private async void OnRegisterNowClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}