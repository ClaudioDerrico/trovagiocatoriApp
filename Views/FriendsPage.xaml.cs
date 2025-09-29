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
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private TabType _activeTab = TabType.Friends;

        // Collections per i diversi tipi di contenuto
        public ObservableCollection<FriendInfo> Friends { get; set; } = new ObservableCollection<FriendInfo>();
        public ObservableCollection<FriendRequest> SentRequests { get; set; } = new ObservableCollection<FriendRequest>();
        public ObservableCollection<FriendRequest> ReceivedRequests { get; set; } = new ObservableCollection<FriendRequest>();
        public ObservableCollection<UserSearchResult> SearchResults { get; set; } = new ObservableCollection<UserSearchResult>();

        private enum TabType { Friends, SentRequests, ReceivedRequests }

        public FriendsPage()
        {
            InitializeComponent();
            SetupCollectionViews();
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient(new HttpClientHandler { UseCookies = true });
        }

        // Configura le ItemsSource delle CollectionView
        private void SetupCollectionViews()
        {
            FriendsCollectionView.ItemsSource = Friends;
            SentRequestsCollectionView.ItemsSource = SentRequests;
            ReceivedRequestsCollectionView.ItemsSource = ReceivedRequests;
            SearchResultsCollectionView.ItemsSource = SearchResults;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAllData();
            Debug.WriteLine("[FRIENDS PAGE] Dati aggiornati - OnAppearing completato");
        }

        // Carica tutti i dati necessari per la pagina
        private async Task LoadAllData()
        {
            await Task.WhenAll(
                LoadFriendsFromBackend(),
                LoadSentRequestsFromBackend(),
                LoadReceivedRequestsFromBackend()
            );
        }

        // ========== CARICAMENTO DATI DAL BACKEND ==========

        // Carica la lista degli amici confermati
        private async Task LoadFriendsFromBackend()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/friends/list");
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

        // Carica le richieste di amicizia inviate
        private async Task LoadSentRequestsFromBackend()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/friends/sent-requests");
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

        // Carica le richieste di amicizia ricevute
        private async Task LoadReceivedRequestsFromBackend()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/friends/requests");
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
            SwitchToTab(TabType.Friends);
        }

        private void OnSentRequestsTabClicked(object sender, EventArgs e)
        {
            SwitchToTab(TabType.SentRequests);
        }

        private void OnReceivedRequestsTabClicked(object sender, EventArgs e)
        {
            SwitchToTab(TabType.ReceivedRequests);
        }

        // Cambia il tab attivo e aggiorna l'UI
        private void SwitchToTab(TabType newTab)
        {
            if (_activeTab == newTab) return;

            _activeTab = newTab;
            UpdateTabsUI();
        }

        // Aggiorna l'interfaccia dei tab
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

        // Avvia la ricerca di nuovi utenti
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

        // Esegue la ricerca di utenti nel database
        private async Task SearchUsers(string searchText)
        {
            try
            {
                SearchButton.IsEnabled = false;
                SearchButton.Text = "Ricerca...";

                var request = CreateAuthenticatedRequest(HttpMethod.Get,
                    $"/friends/search?q={Uri.EscapeDataString(searchText)}");
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

        // Invia una richiesta di amicizia
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
                    await SendFriendRequest(button, user);
                }
            }
        }

        // Invia la richiesta di amicizia tramite API
        private async Task SendFriendRequest(Button button, UserSearchResult user)
        {
            try
            {
                button.IsEnabled = false;
                button.Text = "Invio...";

                var request = CreateAuthenticatedRequest(HttpMethod.Post, "/friends/request");
                var payload = new { target_email = user.Email };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    SearchResults.Remove(user);
                    await LoadSentRequestsFromBackend();
                    await DisplayAlert("Successo", $"Richiesta di amicizia inviata a {user.Username}!", "OK");
                    Debug.WriteLine($"[FRIENDS] Richiesta inviata a {user.Email}");
                }
                else
                {
                    await HandleFriendRequestError(response, button, user);
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

        // Gestisce gli errori nella richiesta di amicizia
        private async Task HandleFriendRequestError(HttpResponseMessage response, Button button, UserSearchResult user)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Debug.WriteLine($"[FRIENDS] Errore invio richiesta: {errorContent}");

            if (errorContent.Contains("già inviata") || errorContent.Contains("already sent"))
            {
                SearchResults.Remove(user);
                await LoadSentRequestsFromBackend();
                await DisplayAlert("Informazione", "Richiesta di amicizia già inviata precedentemente!", "OK");
            }
            else
            {
                await DisplayAlert("Errore", $"Errore nell'invio della richiesta: {errorContent}", "OK");
                button.IsEnabled = true;
                button.Text = "Aggiungi";
            }
        }

        // Avvia una chat diretta con un amico
        private async void OnMessageFriendClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                try
                {
                    string currentUserEmail = await GetCurrentUserEmailAsync();

                    if (string.IsNullOrEmpty(currentUserEmail))
                    {
                        await DisplayAlert("Errore", "Impossibile identificare l'utente corrente", "OK");
                        return;
                    }

                    // Crea un post fittizio per la chat diretta tra amici
                    var directChatPost = new PostResponse
                    {
                        id = -1,
                        titolo = $"Chat Diretta con {friend.Username}",
                        autore_email = friend.Email,
                        sport = "Chat",
                        citta = "Chat Diretta",
                        provincia = "",
                        data_partita = DateTime.Now.ToString("dd/MM/yyyy"),
                        ora_partita = DateTime.Now.ToString("HH:mm"),
                        commento = "Chat privata tra amici"
                    };

                    var chatPage = new ChatPage(directChatPost, currentUserEmail, friend.Email, false);
                    await Navigation.PushAsync(chatPage);

                    Debug.WriteLine($"[FRIENDS CHAT] Apertura chat tra {currentUserEmail} e {friend.Email}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[FRIENDS CHAT] Errore: {ex.Message}");
                    await DisplayAlert("Errore", "Impossibile aprire la chat. Riprova più tardi.", "OK");
                }
            }
        }

        // Mostra le opzioni per un amico (rimuovere, ecc.)
        private async void OnFriendOptionsClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                var action = await DisplayActionSheet(
                    $"Opzioni per {friend.Username}",
                    "Annulla",
                    null,
                    "Rimuovi amicizia");

                if (action == "Rimuovi amicizia")
                {
                    await RemoveFriend(friend);
                }
            }
        }

        // Rimuove un amico dalla lista
        private async Task RemoveFriend(FriendInfo friend)
        {
            var confirm = await DisplayAlert(
                "Rimuovi amicizia",
                $"Sei sicuro di voler rimuovere {friend.Username} dai tuoi amici?",
                "Sì",
                "No");

            if (!confirm) return;

            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Delete, "/friends/remove");
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

        // ========== GESTIONE RICHIESTE ==========

        // Annulla una richiesta di amicizia inviata
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
                    await CancelFriendRequest(request);
                }
            }
        }

        // Esegue l'annullamento della richiesta tramite API
        private async Task CancelFriendRequest(FriendRequest request)
        {
            try
            {
                var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post,
                    $"/friends/cancel?request_id={request.RequestId}");
                var response = await _sharedClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    SentRequests.Remove(request);
                    await DisplayAlert("Richiesta annullata", $"Richiesta a {request.Username} annullata.", "OK");
                    Debug.WriteLine($"[FRIENDS] Richiesta annullata a {request.Email}");
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

        // Accetta una richiesta di amicizia ricevuta
        private async void OnAcceptRequestClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendRequest request)
            {
                await AcceptFriendRequest(button, request);
            }
        }

        // Esegue l'accettazione della richiesta tramite API
        private async Task AcceptFriendRequest(Button button, FriendRequest request)
        {
            try
            {
                button.IsEnabled = false;
                button.Text = "Accetto...";

                var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post,
                    $"/friends/accept?request_id={request.RequestId}");
                var response = await _sharedClient.SendAsync(httpRequest);

                if (response.IsSuccessStatusCode)
                {
                    ReceivedRequests.Remove(request);
                    await LoadFriendsFromBackend();
                    await LoadReceivedRequestsFromBackend();
                    await DisplayAlert("Amicizia accettata", $"Ora sei amico di {request.Username}!", "OK");
                    Debug.WriteLine($"[FRIENDS] Richiesta accettata da {request.Email}");
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

        // Rifiuta una richiesta di amicizia ricevuta
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
                    await RejectFriendRequest(button, request);
                }
            }
        }

        // Esegue il rifiuto della richiesta tramite API
        private async Task RejectFriendRequest(Button button, FriendRequest request)
        {
            try
            {
                button.IsEnabled = false;
                button.Text = "Rifiuto...";

                var httpRequest = CreateAuthenticatedRequest(HttpMethod.Post,
                    $"/friends/reject?request_id={request.RequestId}");
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

        // ========== HELPER METHODS ==========

        // Ottiene l'email dell'utente corrente
        private async Task<string> GetCurrentUserEmailAsync()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, "/api/user");
                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (userData.ContainsKey("email"))
                    {
                        return userData["email"].ToString();
                    }
                }

                return "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FRIENDS] Errore nel caricamento email utente: {ex.Message}");
                return "";
            }
        }

        // Crea una richiesta HTTP autenticata con cookie di sessione
        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string endpoint)
        {
            var request = new HttpRequestMessage(method, $"{ApiConfig.BaseUrl}{endpoint}");

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            return request;
        }

        // Metodi helper per parsing JSON sicuro
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

    }
}