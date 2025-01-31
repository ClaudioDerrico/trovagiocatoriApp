using System.Text.Json;
using System.Text;
using Microsoft.Maui.Storage; // Per Preferences
using trovagiocatoriApp;     // Per AppShell

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
                if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
                {
                    var sessionCookie = cookieValues.FirstOrDefault(c => c.StartsWith("session_id="));
                    if (sessionCookie != null)
                    {
                        var sessionId = sessionCookie.Split(';')[0].Split('=')[1];
                        Preferences.Set("session_id", sessionId);
                    }
                }

                await DisplayAlert("Login", "Login eseguito con successo!", "OK");

                if (Application.Current.MainPage is AppShell shell)
                {
                    shell.RefreshMenu();
                }

                await Shell.Current.GoToAsync("//ProfilePage");
            }
            else
            {
                string errorMsg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore Login", errorMsg, "OK");
            }
        }

        private async void OnRegisterNowClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//RegisterPage");
        }
    }
}
