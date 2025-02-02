using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace trovagiocatoriApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _apiBaseUrl = "http://localhost:8080";

        public ProfilePage()
        {
            InitializeComponent();
        }

        // Ricarica il profilo ogni volta che la pagina diventa visibile
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();
        }

        private async void LoadProfile()
        {
            Debug.WriteLine("LoadProfile called");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/profile");

                // Recupera il cookie di sessione salvato nelle Preferences
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

                    if (userProfile != null)
                    {
                        Debug.WriteLine($"Profilo Utente Caricato: {userProfile.Username}");
                        UsernameLabel.Text = userProfile.Username;
                        // Se il profilo contiene un'immagine, la carichiamo; altrimenti, usiamo un'immagine predefinita
                        ProfileImage.Source = !string.IsNullOrEmpty(userProfile.ProfilePic)
                            ? $"{_apiBaseUrl}/images/{userProfile.ProfilePic}"
                            : "default_images.jpg";
                    }
                    else
                    {
                        Debug.WriteLine("Errore: userProfile è null.");
                        await DisplayAlert("Errore", "Profilo utente non valido.", "OK");
                    }
                }
                else
                {
                    Debug.WriteLine($"Errore: Stato della risposta {response.StatusCode}");
                    await DisplayAlert("Errore", "Impossibile caricare il profilo.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante il caricamento del profilo: {ex.Message}");
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

                // Dopo il logout, imposta l'AppShell in modo che l'utente venga reindirizzato al login.
                // Se la logica per aggiornare il menu è gestita in AppShell, questa chiamata non è necessaria.
                // Puoi semplicemente reimpostare la MainPage:
                Application.Current.MainPage = new NavigationPage(new LoginPage());

                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }
    }

    // Modello per deserializzare il profilo utente ricevuto dal backend
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
