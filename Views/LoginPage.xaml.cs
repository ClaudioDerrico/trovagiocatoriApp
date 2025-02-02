using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage; // Per Preferences
using trovagiocatoriApp;
using System.Diagnostics;     // Per AppShell

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
            // Raccogli i dati del login (ad esempio email/username e password)
            var loginData = new
            {
                email_or_username = EmailEntry.Text,
                password = PasswordEntry.Text
            };

            string json = JsonSerializer.Serialize(loginData);

            using var client = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("http://localhost:8080/login", content);

            if (response.IsSuccessStatusCode)
            {
                // Legge il cookie dalla risposta e lo salva nelle Preferences
                if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                {
                    var sessionCookie = cookieValues.FirstOrDefault(c => c.StartsWith("session_id="));
                    if (sessionCookie != null)
                    {
                        var sessionId = sessionCookie.Split(';')[0].Split('=')[1];
                        Preferences.Set("session_id", sessionId);
                        Debug.WriteLine($"Session id salvata: {sessionId}");
                    }
                }

                await DisplayAlert("Login", "Login eseguito con successo!", "OK");

                // Dopo il login riuscito, imposta l'AppShell come nuova MainPage
                Application.Current.MainPage = new AppShell();
            }
            else
            {
                string errorMsg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore Login", errorMsg, "OK");
            }
        }

        private async void OnRegisterNowClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}
