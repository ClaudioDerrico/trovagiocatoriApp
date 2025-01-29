using Microsoft.Maui.Storage;
using System.IO;

namespace trovagiocatoriApp.Views;

public partial class RegisterPage : ContentPage
{
    private bool isPasswordVisible = false;
    private MemoryStream profilePhotoMemoryStream;

    public RegisterPage()
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

    private async void OnUploadPhotoClicked(object sender, EventArgs e)
    {
        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Seleziona una foto",
                FileTypes = FilePickerFileType.Images
            });

            if (result != null)
            {
                using var stream = await result.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                profilePhotoMemoryStream = new MemoryStream(memoryStream.ToArray());

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


    private async void OnRegisterClicked(object sender, EventArgs e)
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

        try
        {
            using var client = new HttpClient();
            var registerUrl = "http://localhost:8080/register";

            using var formData = new MultipartFormDataContent();

            formData.Add(new StringContent(NomeEntry.Text), "nome");
            formData.Add(new StringContent(CognomeEntry.Text), "cognome");
            formData.Add(new StringContent(UsernameEntry.Text), "username");
            formData.Add(new StringContent(EmailEntry.Text), "email");
            formData.Add(new StringContent(PasswordEntry.Text), "password");

            if (profilePhotoMemoryStream != null)
            {
                profilePhotoMemoryStream.Position = 0;
                var fileContent = new StreamContent(profilePhotoMemoryStream);
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");

                formData.Add(fileContent, "profile_picture", "userProfile.jpg");
            }

            // **Invio della richiesta POST**
            var response = await client.PostAsync(registerUrl, formData);

            if (response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Registrazione", "Registrazione completata con successo!", "OK");

                if (response.Headers.Contains("Set-Cookie"))
                {
                    var cookieHeader = response.Headers.GetValues("Set-Cookie").FirstOrDefault();
                }

                await Shell.Current.GoToAsync("//HomePage"); 
            }
            else
            {
                // **Gestione dell' lato server**
                string errorMsg = await response.Content.ReadAsStringAsync();
                await DisplayAlert("Errore Registrazione", $"Errore: {errorMsg}", "OK");
            }
        }
        catch (HttpRequestException httpEx)
        {
            await DisplayAlert("Errore di Connessione", "Non è possibile contattare il server. Controlla la connessione a Internet.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Errore", $"Si è verificato un errore imprevisto: {ex.Message}", "OK");
        }
    }


}
