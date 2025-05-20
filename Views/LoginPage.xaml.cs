using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage; // Per Preferences
using System.Diagnostics;     // Per Debug
using Microsoft.Maui.Devices;  // Per DeviceInfo

namespace trovagiocatoriApp.Views
{
    public partial class LoginPage : ContentPage
    {
        private bool isPasswordVisible = false;

        public LoginPage()
        {
            InitializeComponent();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            Application.Current.MainPage.Window.Height = 800;
            Application.Current.MainPage.Window.Width = 500;
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
            // Raccogli i dati del login
            var loginData = new
            {
                email_or_username = EmailEntry.Text,
                password = PasswordEntry.Text
            };

            string json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var baseUrl = ApiConfig.BaseUrl;

            try
            {
                using var client = new HttpClient();
                var response = await client.PostAsync($"{baseUrl}/login", content);

                if (response.IsSuccessStatusCode)
                {
                    // Salva il cookie di sessione
                    if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
                    {
                        var sessionCookie = cookies.FirstOrDefault(c => c.StartsWith("session_id="));
                        if (sessionCookie != null)
                        {
                            var sessionId = sessionCookie.Split(';')[0].Split('=')[1];
                            Preferences.Set("session_id", sessionId);
                            Debug.WriteLine($"Session id salvata: {sessionId}");
                        }
                    }

                    await DisplayAlert("Login", "Login eseguito con successo!", "OK");
                    Application.Current.MainPage = new AppShell();
                }
                else
                {
                    var errorMsg = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Errore Login", errorMsg, "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore HTTP: {ex}");
                await DisplayAlert("Errore di connessione",
                    "Impossibile raggiungere il server. Controlla le impostazioni di rete.", "OK");
            }
        }

        private async void OnRegisterNowClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}
