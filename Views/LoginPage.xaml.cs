using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Microsoft.Maui.Devices;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private bool isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();

            // Pulisci sessioni residue al caricamento della pagina di login
            ClearPreviousSessions();
        }

        // Pulisce sessioni precedenti e flag
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
                var responseJson = await response.Content.ReadAsStringAsync();

                Debug.WriteLine($"[LOGIN] Response status: {response.StatusCode}");
                Debug.WriteLine($"[LOGIN] Response body: {responseJson}");

                if (response.IsSuccessStatusCode)
                {
                    // Login riuscito
                    await HandleSuccessfulLogin(response);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    // Utente bannato (403 Forbidden)
                    await HandleBannedUser(responseJson);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    // Credenziali errate
                    await DisplayAlert("Errore Login", "Credenziali non valide. Controlla email/username e password.", "OK");
                }
                else
                {
                    // Altri errori
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore: {ex}");
                await DisplayAlert("Errore", "Errore durante il login. Controlla la connessione e riprova.", "OK");
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

        private async Task HandleSuccessfulLogin(HttpResponseMessage response)
        {
            try
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
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LOGIN] Errore handling successful login: {ex.Message}");
                await DisplayAlert("Errore", "Errore nella configurazione della sessione", "OK");
            }
        }

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
                    // Fallback se non ci sono info sul ban
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

        private async Task ShowBanDialog(BanInfo banInfo, string errorMessage)
        {
            try
            {
                var title = "🚫 Account Sospeso";

                var message = new StringBuilder();
                message.AppendLine($"Il tuo account è stato {banInfo.BanTypeDisplay.ToLower()}.");
                message.AppendLine();

                if (!string.IsNullOrEmpty(banInfo.Reason))
                {
                    message.AppendLine($"📋 Motivo: {banInfo.Reason}");
                    message.AppendLine();
                }

                message.AppendLine($"📅 Data sospensione: {banInfo.BannedAt:dd/MM/yyyy HH:mm}");

                if (banInfo.IsPermanent)
                {
                    message.AppendLine("⏰ Durata: Permanente");
                    message.AppendLine();
                    message.AppendLine("❌ Questo ban non ha scadenza.");
                }
                else if (banInfo.ExpiresAt.HasValue)
                {
                    message.AppendLine($"⏰ Scadenza: {banInfo.ExpiresAt.Value:dd/MM/yyyy HH:mm}");

                    var timeRemaining = banInfo.ExpiresAt.Value - DateTime.Now;
                    if (timeRemaining.TotalMinutes > 0)
                    {
                        var remainingText = FormatTimeRemaining(timeRemaining);
                        message.AppendLine($"⏳ Tempo rimanente: {remainingText}");
                    }
                    else
                    {
                        message.AppendLine("✅ Il ban dovrebbe essere scaduto. Contatta l'amministratore.");
                    }
                }

                message.AppendLine();
                message.AppendLine("📧 Per assistenza, contatta l'amministratore del sito.");

                // Mostra un dialog personalizzato invece del semplice DisplayAlert
                bool contactAdmin = await DisplayAlert(
                    title,
                    message.ToString(),
                    "Contatta Admin",  // Pulsante per contattare admin
                    "OK"              // Pulsante per chiudere
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
                    "Il tuo account è stato sospeso. Contatta l'amministratore.",
                    "OK");
            }
        }

        private string FormatTimeRemaining(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
            {
                var days = (int)timeSpan.TotalDays;
                var hours = timeSpan.Hours;
                return days == 1 ? "1 giorno" : $"{days} giorni" + (hours > 0 ? $" e {hours} ore" : "");
            }
            else if (timeSpan.TotalHours >= 1)
            {
                var hours = (int)timeSpan.TotalHours;
                var minutes = timeSpan.Minutes;
                return hours == 1 ? "1 ora" : $"{hours} ore" + (minutes > 0 ? $" e {minutes} minuti" : "");
            }
            else if (timeSpan.TotalMinutes >= 1)
            {
                var minutes = (int)timeSpan.TotalMinutes;
                return minutes == 1 ? "1 minuto" : $"{minutes} minuti";
            }
            else
            {
                return "meno di un minuto";
            }
        }

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

        private async Task OpenEmailApp()
        {
            try
            {
                var message = new EmailMessage
                {
                    Subject = "Richiesta Assistenza - Account Sospeso",
                    Body = "Buongiorno,\n\nIl mio account è stato sospeso e vorrei richiedere assistenza.\n\nGrazie per l'attenzione.",
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

        private async void OnRegisterNowClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}