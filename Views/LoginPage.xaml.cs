namespace trovagiocatoriApp.Views;
using System.Text.Json;
using System.Text;

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

        var content = new StringContent(json, Encoding.UTF8, new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"));

        var response = await client.PostAsync("http://localhost:8080/login", content);

        if (response.IsSuccessStatusCode)
        {
            // Estrai il session_id dalla risposta (ipotizzando che venga restituito come cookie o nel corpo della risposta)
            var sessionId = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
            if (sessionId != null)
            {
                // Salva il session_id nelle preferenze
                Preferences.Set("session_id", sessionId);
            }

            await DisplayAlert("Login", "Login eseguito con successo!", "OK");
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
