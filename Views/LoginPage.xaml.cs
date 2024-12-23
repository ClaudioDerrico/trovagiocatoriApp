namespace trovagiocatoriApp.Views;

public partial class LoginPage : ContentPage
{
    // Variabile per tracciare lo stato della visibilità della password
    private bool isPasswordVisible = false;

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Impostare la dimensione per la pagina di login (1000x800)

        Application.Current.MainPage.Window.Height = 800;
        Application.Current.MainPage.Window.Width = 500;

    }

    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        // Alterna la visibilità della password
        isPasswordVisible = !isPasswordVisible;
        PasswordEntry.IsPassword = !isPasswordVisible;

        // Cambia l'icona in base allo stato della visibilità
        var button = sender as ImageButton;
        button.Source = isPasswordVisible ? "eye_close.png" : "eye_open.png";
    }

    private void OnLoginClicked(object sender, EventArgs e)
    {
        // Reset messaggi di errore
        ErrorMessage.IsVisible = false;
        EmailError.IsVisible = false;
        PasswordError.IsVisible = false;

        // Validazione
        bool isValid = true;

        if (string.IsNullOrWhiteSpace(EmailEntry.Text) || !EmailEntry.Text.Contains("@"))
        {
            EmailError.IsVisible = true;
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(PasswordEntry.Text) || PasswordEntry.Text.Length < 8)
        {
            PasswordError.IsVisible = true;
            isValid = false;
        }

        if (!isValid)
        {
            ErrorMessage.Text = "Compila tutti i campi correttamente!";
            ErrorMessage.IsVisible = true;
            return;
        }

        // Logica di login
        DisplayAlert("Login", "Login eseguito con successo!", "OK");
    }

    private async void OnRegisterNowClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//RegisterPage");
    }
}
