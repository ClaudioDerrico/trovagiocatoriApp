using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class ProfilePage : ContentPage
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string apiBaseUrl = ApiConfig.BaseUrl;
        private readonly string pythonApiBaseUrl = ApiConfig.PythonApiUrl;

        // Lista per i preferiti
        public ObservableCollection<PostResponse> FavoritePosts { get; set; } = new ObservableCollection<PostResponse>();

        public ProfilePage()
        {
            InitializeComponent();
            FavoritesCollectionView.ItemsSource = FavoritePosts;
        }

        // Ricarica il profilo ogni volta che la pagina diventa visibile
        protected override void OnAppearing()
        {
            base.OnAppearing();
            LoadProfile();
            LoadFavorites();
        }

        private async void LoadProfile()
        {
            Debug.WriteLine("LoadProfile called");
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/profile");

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
                    var userProfile = JsonSerializer.Deserialize<User>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (userProfile != null)
                    {
                        Debug.WriteLine($"Profilo Utente Caricato: {userProfile.Username}");
                        UsernameLabel.Text = userProfile.Username;
                        NameLabel.Text = userProfile.Nome;
                        SurnameLabel.Text = userProfile.Cognome;
                        EmailLabel.Text = userProfile.Email;

                        // Se il profilo contiene un'immagine, la carichiamo; altrimenti, usiamo un'immagine predefinita
                        ProfileImage.Source = !string.IsNullOrEmpty(userProfile.ProfilePic)
                            ? $"{apiBaseUrl}/images/{userProfile.ProfilePic}"
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

        // NUOVA FUNZIONE PER CARICARE I PREFERITI
        private async void LoadFavorites()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{apiBaseUrl}/favorites");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result.ContainsKey("favorites") && result["favorites"] is JsonElement favoritesElement)
                    {
                        var favoriteIds = favoritesElement.EnumerateArray()
                            .Select(x => x.GetInt32())
                            .ToList();

                        Debug.WriteLine($"Caricati {favoriteIds.Count} preferiti");

                        // Carica i dettagli di ogni post preferito
                        FavoritePosts.Clear();
                        foreach (var postId in favoriteIds)
                        {
                            await LoadFavoritePostDetails(postId);
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"Errore nel caricamento preferiti: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante il caricamento dei preferiti: {ex.Message}");
            }
        }

        private async Task LoadFavoritePostDetails(int postId)
        {
            try
            {
                var response = await _client.GetAsync($"{pythonApiBaseUrl}/posts/{postId}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var post = JsonSerializer.Deserialize<PostResponse>(jsonResponse,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (post != null)
                    {
                        FavoritePosts.Add(post);
                        Debug.WriteLine($"Aggiunto post preferito: {post.titolo}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel caricamento dettagli post {postId}: {ex.Message}");
            }
        }

        // GESTIONE SELEZIONE PREFERITO
        // Questo dovrebbe già essere corretto
        private async void OnFavoriteSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                ((CollectionView)sender).SelectedItem = null;
                await Navigation.PushAsync(new PostDetailPage(selectedPost.id));
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

        private async void OnNavigateToChangePassword(object sender, EventArgs e)
        {
            // CAMBIA DA Shell.GoToAsync A Navigation.PushAsync
            await Navigation.PushAsync(new ChangePasswordPage());
        }
    }
}