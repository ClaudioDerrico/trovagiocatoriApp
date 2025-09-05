using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;
using System.Diagnostics;

namespace trovagiocatoriApp.Views
{
    public partial class FriendsPage : ContentPage, INotifyPropertyChanged
    {
        // AGGIUNTO: HttpClient e URL base
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;

        // Stato dei tab
        private TabType _activeTab = TabType.Friends;

        // Collections per i diversi tipi di contenuto
        public ObservableCollection<FriendInfo> Friends { get; set; } = new ObservableCollection<FriendInfo>();
        public ObservableCollection<FriendRequest> SentRequests { get; set; } = new ObservableCollection<FriendRequest>();
        public ObservableCollection<FriendRequest> ReceivedRequests { get; set; } = new ObservableCollection<FriendRequest>();
        public ObservableCollection<UserSearchResult> SearchResults { get; set; } = new ObservableCollection<UserSearchResult>();

        // Enum per i tipi di tab
        private enum TabType
        {
            Friends,
            SentRequests,
            ReceivedRequests
        }

        // AGGIUNTO: Crea HttpClient
        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true
            };
            return new HttpClient(handler);
        }

        public FriendsPage()
        {
            InitializeComponent();

            // Imposta le ItemsSource
            FriendsCollectionView.ItemsSource = Friends;
            SentRequestsCollectionView.ItemsSource = SentRequests;
            ReceivedRequestsCollectionView.ItemsSource = ReceivedRequests;
            SearchResultsCollectionView.ItemsSource = SearchResults;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Carica tutti i dati dal backend
            await LoadFriendsFromBackend();
            await LoadSentRequestsFromBackend();
            await LoadReceivedRequestsFromBackend();
            Debug.WriteLine("[FRIENDS PAGE] Dati aggiornati - OnAppearing completato");
        }

     

        // ========== CARICAMENTO DATI DAL BACKEND ==========

        private async Task LoadFriendsFromBackend()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/friends/list");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (result.ContainsKey("friends") && result["friends"] is JsonElement friendsElement)
                    {
                        Friends.Clear();

                        foreach (var friendElement in friendsElement.EnumerateArray())
                        {
                            var friend = new FriendInfo
                            {
                                UserId = GetIntProperty(friendElement, "user_id"),
                                Username = GetStringProperty(friendElement, "username"),
                                FullName = $"{GetStringProperty(friendElement, "nome")} {GetStringProperty(friendElement, "cognome")}",
                                Email = GetStringProperty(friendElement, "email"),
                                ProfilePicture = GetStringProperty(friendElement, "profile_picture", "default_avatar.png"),
                                FriendsSince = DateTime.Parse(GetStringProperty(friendElement, "friends_since"))
                            };
                            Friends.Add(friend);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore nel caricamento degli amici: {ex.Message}", "OK");
            }
        }

        private async Task LoadSentRequestsFromBackend()
        {
            try
            {
                // CORREZIONE: Usa l'endpoint corretto
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/friends/sent-requests");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (result.ContainsKey("requests") && result["requests"] is JsonElement requestsElement)
                    {
                        SentRequests.Clear();

                        foreach (var requestElement in requestsElement.EnumerateArray())
                        {
                            var sentRequest = new FriendRequest
                            {
                                RequestId = GetIntProperty(requestElement, "request_id"),
                                UserId = GetIntProperty(requestElement, "user_id"),
                                Username = GetStringProperty(requestElement, "username"),
                                FullName = $"{GetStringProperty(requestElement, "nome")} {GetStringProperty(requestElement, "cognome")}",
                                Email = GetStringProperty(requestElement, "email"),
                                ProfilePicture = GetStringProperty(requestElement, "profile_picture", "default_avatar.png"),
                                RequestSent = DateTime.Parse(GetStringProperty(requestElement, "request_date")),
                                Status = FriendRequestStatus.Pending
                            };
                            SentRequests.Add(sentRequest);
                        }
                    }
                }
                else
                {
                    SentRequests.Clear();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore nel caricamento delle richieste inviate: {ex.Message}", "OK");
                SentRequests.Clear();
            }
        }

        private async Task LoadReceivedRequestsFromBackend()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/friends/requests");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (result.ContainsKey("requests") && result["requests"] is JsonElement requestsElement)
                    {
                        ReceivedRequests.Clear();

                        foreach (var requestElement in requestsElement.EnumerateArray())
                        {
                            var friendRequest = new FriendRequest
                            {
                                RequestId = GetIntProperty(requestElement, "request_id"),
                                UserId = GetIntProperty(requestElement, "user_id"),
                                Username = GetStringProperty(requestElement, "username"),
                                FullName = $"{GetStringProperty(requestElement, "nome")} {GetStringProperty(requestElement, "cognome")}",
                                Email = GetStringProperty(requestElement, "email"),
                                ProfilePicture = GetStringProperty(requestElement, "profile_picture", "default_avatar.png"),
                                RequestReceived = DateTime.Parse(GetStringProperty(requestElement, "request_date")),
                                Status = FriendRequestStatus.Pending
                            };
                            ReceivedRequests.Add(friendRequest);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore nel caricamento delle richieste ricevute: {ex.Message}", "OK");
            }
        }

        // ========== GESTIONE TAB ==========

        private void OnFriendsTabClicked(object sender, EventArgs e)
        {
            if (_activeTab != TabType.Friends)
            {
                _activeTab = TabType.Friends;
                UpdateTabsUI();
            }
        }

        private void OnSentRequestsTabClicked(object sender, EventArgs e)
        {
            if (_activeTab != TabType.SentRequests)
            {
                _activeTab = TabType.SentRequests;
                UpdateTabsUI();
            }
        }

        private void OnReceivedRequestsTabClicked(object sender, EventArgs e)
        {
            if (_activeTab != TabType.ReceivedRequests)
            {
                _activeTab = TabType.ReceivedRequests;
                UpdateTabsUI();
            }
        }

        private void UpdateTabsUI()
        {
            // Reset tutti i tab
            FriendsTabButton.Style = (Style)Resources["TabButtonStyle"];
            SentRequestsTabButton.Style = (Style)Resources["TabButtonStyle"];
            ReceivedRequestsTabButton.Style = (Style)Resources["TabButtonStyle"];

            // Nascondi tutti i contenuti
            FriendsContent.IsVisible = false;
            SentRequestsContent.IsVisible = false;
            ReceivedRequestsContent.IsVisible = false;

            // Attiva il tab selezionato
            switch (_activeTab)
            {
                case TabType.Friends:
                    FriendsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                    FriendsContent.IsVisible = true;
                    Grid.SetColumn(TabIndicator, 0);
                    break;

                case TabType.SentRequests:
                    SentRequestsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                    SentRequestsContent.IsVisible = true;
                    Grid.SetColumn(TabIndicator, 1);
                    break;

                case TabType.ReceivedRequests:
                    ReceivedRequestsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                    ReceivedRequestsContent.IsVisible = true;
                    Grid.SetColumn(TabIndicator, 2);
                    break;
            }
        }

        // ========== GESTIONE RICERCA ==========

        private async void OnSearchFriendsClicked(object sender, EventArgs e)
        {
            var searchText = SearchFriendsEntry.Text?.Trim();

            if (string.IsNullOrEmpty(searchText))
            {
                await DisplayAlert("Attenzione", "Inserisci un username o email per la ricerca", "OK");
                return;
            }

            if (searchText.Length < 3)
            {
                await DisplayAlert("Attenzione", "Inserisci almeno 3 caratteri per la ricerca", "OK");
                return;
            }

            await SearchUsers(searchText);
        }

        private async Task SearchUsers(string searchText)
        {
            try
            {
                SearchButton.IsEnabled = false;
                SearchButton.Text = "Ricerca...";

                // IMPLEMENTATO: Chiamata API per la ricerca utenti
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/friends/search?q={Uri.EscapeDataString(searchText)}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<JsonElement>>(json);

                    SearchResults.Clear();

                    foreach (var userElement in users ?? new List<JsonElement>())
                    {
                        var user = new UserSearchResult
                        {
                            UserId = GetIntProperty(userElement, "user_id"),
                            Username = GetStringProperty(userElement, "username"),
                            FullName = $"{GetStringProperty(userElement, "nome")} {GetStringProperty(userElement, "cognome")}",
                            Email = GetStringProperty(userElement, "email"),
                            ProfilePicture = GetStringProperty(userElement, "profile_picture", "default_avatar.png")
                        };
                        SearchResults.Add(user);
                    }

                    SearchResultsCollectionView.IsVisible = SearchResults.Count > 0;

                    if (SearchResults.Count == 0)
                    {
                        await DisplayAlert("Ricerca", "Nessun utente trovato con questi criteri", "OK");
                    }
                }
                else
                {
                    await DisplayAlert("Errore", "Errore durante la ricerca", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore durante la ricerca: {ex.Message}", "OK");
            }
            finally
            {
                SearchButton.IsEnabled = true;
                SearchButton.Text = "Cerca";
            }
        }

        // ========== GESTIONE AZIONI AMICI ==========

        private async void OnAddFriendClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is UserSearchResult user)
            {
                var confirm = await DisplayAlert(
                    "Aggiungi amico",
                    $"Vuoi inviare una richiesta di amicizia a {user.Username}?",
                    "Sì",
                    "No");

                if (confirm)
                {
                    try
                    {
                        button.IsEnabled = false;
                        button.Text = "Invio...";

                        var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/friends/request");

                        if (Preferences.ContainsKey("session_id"))
                        {
                            string sessionId = Preferences.Get("session_id", "");
                            request.Headers.Add("Cookie", $"session_id={sessionId}");
                        }

                        var payload = new { target_email = user.Email };
                        var json = JsonSerializer.Serialize(payload);
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                        var response = await _sharedClient.SendAsync(request);

                        if (response.IsSuccessStatusCode)
                        {
                            // Rimuovi l'utente dai risultati di ricerca
                            SearchResults.Remove(user);

                            // IMPORTANTE: Ricarica le richieste inviate per mostrare la nuova richiesta
                            await LoadSentRequestsFromBackend();

                            await DisplayAlert("Successo", $"Richiesta di amicizia inviata a {user.Username}!", "OK");

                            Debug.WriteLine($"[FRIENDS] ✅ Richiesta inviata a {user.Email}");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            Debug.WriteLine($"[FRIENDS] Errore invio richiesta: {errorContent}");

                            // Controlla se l'errore è dovuto a una richiesta già esistente
                            if (errorContent.Contains("già inviata") || errorContent.Contains("already sent"))
                            {
                                SearchResults.Remove(user);
                                await LoadSentRequestsFromBackend(); // Ricarica per essere sicuri
                                await DisplayAlert("Informazione", "Richiesta di amicizia già inviata precedentemente!", "OK");
                            }
                            else
                            {
                                await DisplayAlert("Errore", $"Errore nell'invio della richiesta: {errorContent}", "OK");
                                button.IsEnabled = true;
                                button.Text = "Aggiungi";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[FRIENDS] Eccezione invio richiesta: {ex.Message}");
                        await DisplayAlert("Errore", $"Errore nell'invio della richiesta: {ex.Message}", "OK");
                        button.IsEnabled = true;
                        button.Text = "Aggiungi";
                    }
                }
            }
        }

        private async void OnMessageFriendClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                await DisplayAlert("Messaggio", $"Aprendo chat con {friend.Username}...", "OK");
                // TODO: Implementare navigazione alla chat
                // await Navigation.PushAsync(new ChatPage(friend));
            }
        }

        private async void OnFriendOptionsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                var action = await DisplayActionSheet(
                    $"Opzioni per {friend.Username}",
                    "Annulla",
                    null,
                    "Visualizza profilo",
                    "Rimuovi amicizia");

                switch (action)
                {
                    case "Visualizza profilo":
                        await DisplayAlert("Profilo", $"Visualizzando profilo di {friend.Username}...", "OK");
                        // TODO: Implementare navigazione al profilo
                        break;

                    case "Rimuovi amicizia":
                        var confirm = await DisplayAlert(
                            "Rimuovi amicizia",
                            $"Sei sicuro di voler rimuovere {friend.Username} dai tuoi amici?",
                            "Sì",
                            "No");

                        if (confirm)
                        {
                            try
                            {
                                // IMPLEMENTATO: Chiamata API per rimuovere amicizia
                                var request = new HttpRequestMessage(HttpMethod.Delete, $"{_apiBaseUrl}/friends/remove");

                                if (Preferences.ContainsKey("session_id"))
                                {
                                    string sessionId = Preferences.Get("session_id", "");
                                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                                }

                                var payload = new { target_email = friend.Email };
                                var json = JsonSerializer.Serialize(payload);
                                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                                var response = await _sharedClient.SendAsync(request);

                                if (response.IsSuccessStatusCode)
                                {
                                    Friends.Remove(friend);
                                    await DisplayAlert("Amicizia rimossa", $"Hai rimosso {friend.Username} dai tuoi amici.", "OK");
                                }
                                else
                                {
                                    var errorContent = await response.Content.ReadAsStringAsync();
                                    await DisplayAlert("Errore", $"Errore nella rimozione dell'amicizia: {errorContent}", "OK");
                                }
                            }
                            catch (Exception ex)
                            {
                                await DisplayAlert("Errore", $"Errore nella rimozione dell'amicizia: {ex.Message}", "OK");
                            }
                        }
                        break;
                }
            }
        }

        // ========== GESTIONE RICHIESTE ==========

        private async void OnCancelRequestClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendRequest request)
            {
                var confirm = await DisplayAlert(
                    "Annulla richiesta",
                    $"Vuoi annullare la richiesta inviata a {request.Username}?",
                    "Sì",
                    "No");

                if (confirm)
                {
                    try
                    {
                        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/friends/cancel?request_id={request.RequestId}");

                        if (Preferences.ContainsKey("session_id"))
                        {
                            string sessionId = Preferences.Get("session_id", "");
                            httpRequest.Headers.Add("Cookie", $"session_id={sessionId}");
                        }

                        var response = await _sharedClient.SendAsync(httpRequest);

                        if (response.IsSuccessStatusCode)
                        {
                            // Rimuovi dalle richieste inviate
                            SentRequests.Remove(request);

                            await DisplayAlert("Richiesta annullata", $"Richiesta a {request.Username} annullata.", "OK");

                            Debug.WriteLine($"[FRIENDS] ✅ Richiesta annullata a {request.Email}");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            Debug.WriteLine($"[FRIENDS] Errore annullamento: {errorContent}");
                            await DisplayAlert("Errore", $"Errore nell'annullamento della richiesta: {errorContent}", "OK");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"[FRIENDS] Eccezione annullamento: {ex.Message}");
                        await DisplayAlert("Errore", $"Errore nell'annullamento della richiesta: {ex.Message}", "OK");
                    }
                }
            }
        }

        private async void OnAcceptRequestClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendRequest request)
            {
                try
                {
                    button.IsEnabled = false;
                    button.Text = "Accetto...";

                    var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/friends/accept?request_id={request.RequestId}");

                    if (Preferences.ContainsKey("session_id"))
                    {
                        string sessionId = Preferences.Get("session_id", "");
                        httpRequest.Headers.Add("Cookie", $"session_id={sessionId}");
                    }

                    var response = await _sharedClient.SendAsync(httpRequest);

                    if (response.IsSuccessStatusCode)
                    {
                        // Rimuovi dalle richieste ricevute
                        ReceivedRequests.Remove(request);

                        // IMPORTANTE: Ricarica sia gli amici che le richieste per aggiornare tutto
                        await LoadFriendsFromBackend();
                        await LoadReceivedRequestsFromBackend(); // Per sicurezza

                        await DisplayAlert("Amicizia accettata", $"Ora sei amico di {request.Username}!", "OK");

                        Debug.WriteLine($"[FRIENDS] ✅ Richiesta accettata da {request.Email}");
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"[FRIENDS] Errore accettazione: {errorContent}");
                        await DisplayAlert("Errore", $"Errore nell'accettazione della richiesta: {errorContent}", "OK");
                        button.IsEnabled = true;
                        button.Text = "Accetta";
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FRIENDS] Eccezione accettazione: {ex.Message}");
                    await DisplayAlert("Errore", $"Errore nell'accettazione della richiesta: {ex.Message}", "OK");
                    button.IsEnabled = true;
                    button.Text = "Accetta";
                }
            }
        }

        private async void OnRejectRequestClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendRequest request)
            {
                var confirm = await DisplayAlert(
                    "Rifiuta richiesta",
                    $"Vuoi rifiutare la richiesta di amicizia di {request.Username}?",
                    "Sì",
                    "No");

                if (confirm)
                {
                    try
                    {
                        button.IsEnabled = false;
                        button.Text = "Rifiuto...";

                        // IMPLEMENTATO: Chiamata API per rifiutare richiesta
                        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/friends/reject?request_id={request.RequestId}");

                        if (Preferences.ContainsKey("session_id"))
                        {
                            string sessionId = Preferences.Get("session_id", "");
                            httpRequest.Headers.Add("Cookie", $"session_id={sessionId}");
                        }

                        var response = await _sharedClient.SendAsync(httpRequest);

                        if (response.IsSuccessStatusCode)
                        {
                            ReceivedRequests.Remove(request);
                            await DisplayAlert("Richiesta rifiutata", $"Richiesta di {request.Username} rifiutata.", "OK");
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            await DisplayAlert("Errore", $"Errore nel rifiuto della richiesta: {errorContent}", "OK");
                            button.IsEnabled = true;
                            button.Text = "Rifiuta";
                        }
                    }
                    catch (Exception ex)
                    {
                        await DisplayAlert("Errore", $"Errore nel rifiuto della richiesta: {ex.Message}", "OK");
                        button.IsEnabled = true;
                        button.Text = "Rifiuta";
                    }
                }
            }
        }

        // ========== METODI HELPER ==========

        private string GetStringProperty(JsonElement element, string propertyName, string defaultValue = "")
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetString() ?? defaultValue
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetInt32()
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        // ========== INotifyPropertyChanged ==========

        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // ========== CLASSI MODELLO ==========

    public class FriendInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime FriendsSince { get; set; }
    }

    public class FriendRequest
    {
        public int RequestId { get; set; } // Aggiunto per identificare la richiesta specifica
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
        public DateTime RequestSent { get; set; }
        public DateTime RequestReceived { get; set; }
        public FriendRequestStatus Status { get; set; }
    }

    public class UserSearchResult
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePicture { get; set; }
    }

    public enum FriendRequestStatus
    {
        Pending,
        Accepted,
        Rejected,
        Cancelled
    }
}