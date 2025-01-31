using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using static Microsoft.Maui.Controls.Shell;

namespace trovagiocatoriApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private HttpClient _client = new HttpClient();
        private string _apiBaseUrl = "http://localhost:8080"; 

        public ProfilePage()
        {
            InitializeComponent();
            LoadProfile(); 
        }

        private async void LoadProfile()
        {
            Debug.WriteLine("LoadProfile called");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/profile");

                // Recupera i cookie salvati
                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"jsonResponse: {jsonResponse}");
                    var userProfile = JsonSerializer.Deserialize<UserModel>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    Debug.Write(userProfile);
                    if (userProfile != null)
                    {
                        Debug.WriteLine($"Profilo Utente Caricato:");
                        Debug.WriteLine($"Username: {userProfile.Username}");
                        Debug.WriteLine($"ProfilePic: {userProfile.ProfilePic}");

                        UsernameLabel.Text = userProfile.Username;
                        ProfileImage.Source = !string.IsNullOrEmpty(userProfile.ProfilePic)
                            ? $"{_apiBaseUrl}/images/{userProfile.ProfilePic}"
                            : "default.jpg"; // Usa l'immagine predefinita
                    }
                    else
                    {
                        Console.WriteLine("Errore: userProfile è null.");
                        await DisplayAlert("Errore", "Profilo utente non valido.", "OK");
                    }
                }
                else
                {
                    Console.WriteLine($"Errore: Stato della risposta {response.StatusCode}");
                    await DisplayAlert("Errore", "Impossibile caricare il profilo.", "OK");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Eccezione durante il caricamento del profilo: {ex.Message}");
                await DisplayAlert("Errore", $"Errore durante il caricamento: {ex.Message}", "OK");
            }
        }


        private async void OnLogoutButtonClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Sei sicuro di voler effettuare il logout?", "Sì", "No");
            if (confirm)
            {
                if (Preferences.ContainsKey("session_id"))
                {
                    Preferences.Remove("session_id");
                }

                if (Application.Current.MainPage is Shell shell)
                {
                    ((AppShell)shell).RefreshMenu();
                }

                await Shell.Current.GoToAsync("//LoginPage");
                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }
    


    public class UserModel
        {
            [JsonPropertyName("username")]
            public string Username { get; set; }

            [JsonPropertyName("profile_picture")] 
            public string ProfilePic { get; set; }

            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("nome")]
            public string Nome { get; set; }

            [JsonPropertyName("cognome")]
            public string Cognome { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; }
        }

    }
}

