using Microsoft.Maui.Storage;
using System.IO;

namespace trovagiocatoriApp.Views;

public partial class RegisterPage : ContentPage
{
    private bool isPasswordVisible = false;

    public RegisterPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Impostare la dimensione per la pagina di registrazione (800x600)

        Application.Current.MainPage.Window.Height = 600;
        Application.Current.MainPage.Window.Width = 800;
    }

    


    private void OnTogglePasswordVisibility(object sender, EventArgs e)
    {
        isPasswordVisible = !isPasswordVisible;
        PasswordEntry.IsPassword = !isPasswordVisible;

        var button = sender as ImageButton;
        button.Source = isPasswordVisible ? "eye_close.png" : "eye_open.png";
    }

    private async void OnUploadPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            // Apri il file picker per selezionare un'immagine
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona una foto",
                FileTypes = FilePickerFileType.Images // Solo immagini
            });

            if (result != null)
            {
                // Leggi il contenuto del file in un buffer
                var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                // Imposta la sorgente dell'immagine per l'anteprima
                ProfilePhotoPreview.Source = ImageSource.FromStream(() => new MemoryStream(memoryStream.ToArray()));
                ProfilePhotoPreview.IsVisible = true;
            }
            else
            {
                await DisplayAlert("Errore", "Nessun file selezionato.", "OK");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Si è verificato un errore durante il caricamento dell'immagine: {ex.Message}", "OK");
        }
    }

    private void OnRegisterClicked(object sender, EventArgs e)
    {
        ErrorMessage.IsVisible = false;
        NomeError.IsVisible = false;
        CognomeError.IsVisible = false;
        UsernameError.IsVisible = false;
        EmailError.IsVisible = false;
        PasswordError.IsVisible = false;

        bool isValid = true;

        if (string.IsNullOrWhiteSpace(NomeEntry.Text))
        {
            NomeError.IsVisible = true;
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(CognomeEntry.Text))
        {
            CognomeError.IsVisible = true;
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(UsernameEntry.Text))
        {
            UsernameError.IsVisible = true;
            isValid = false;
        }

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
            return;
        }

        DisplayAlert("Registrazione", "Registrazione completata con successo!", "OK");
    }
}
