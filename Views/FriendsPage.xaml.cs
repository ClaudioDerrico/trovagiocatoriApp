using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class FriendsPage : ContentPage, INotifyPropertyChanged
    {
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
        }

        // ========== CARICAMENTO DATI DAL BACKEND ==========

        private async Task LoadFriendsFromBackend()
        {
            try
            {
                // TODO: Implementare chiamata API per ottenere la lista amici
                // var friends = await ApiService.GetFriendsListAsync();

                Friends.Clear();

                // Esempio di come popolare la lista quando avrai i dati:
                // foreach (var friend in friends)
                // {
                //     Friends.Add(new FriendInfo
                //     {
                //         UserId = friend.UserId,
                //         Username = friend.Username,
                //         FullName = $"{friend.Nome} {friend.Cognome}",
                //         Email = friend.Email,
                //         ProfilePicture = friend.ProfilePic ?? "default_avatar.png",
                //         FriendsSince = DateTime.Parse(friend.FriendsSince)
                //     });
                // }
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
                // TODO: Implementare chiamata API per ottenere le richieste inviate
                // var sentRequests = await ApiService.GetSentFriendRequestsAsync();

                SentRequests.Clear();

                // Esempio di come popolare la lista quando avrai i dati:
                // foreach (var request in sentRequests)
                // {
                //     SentRequests.Add(new FriendRequest
                //     {
                //         UserId = request.UserId,
                //         Username = request.Username,
                //         FullName = $"{request.Nome} {request.Cognome}",
                //         Email = request.Email,
                //         ProfilePicture = request.ProfilePic ?? "default_avatar.png",
                //         RequestSent = DateTime.Parse(request.RequestDate),
                //         Status = FriendRequestStatus.Pending
                //     });
                // }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore nel caricamento delle richieste inviate: {ex.Message}", "OK");
            }
        }

        private async Task LoadReceivedRequestsFromBackend()
        {
            try
            {
                // TODO: Implementare chiamata API per ottenere le richieste ricevute
                // var receivedRequests = await ApiService.GetReceivedFriendRequestsAsync();

                ReceivedRequests.Clear();

                // Esempio di come popolare la lista quando avrai i dati:
                // foreach (var request in receivedRequests)
                // {
                //     ReceivedRequests.Add(new FriendRequest
                //     {
                //         RequestId = request.RequestId, // Aggiungi questo campo alla classe FriendRequest
                //         UserId = request.UserId,
                //         Username = request.Username,
                //         FullName = $"{request.Nome} {request.Cognome}",
                //         Email = request.Email,
                //         ProfilePicture = request.ProfilePic ?? "default_avatar.png",
                //         RequestReceived = DateTime.Parse(request.RequestDate),
                //         Status = FriendRequestStatus.Pending
                //     });
                // }
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

                // TODO: Implementare chiamata API per la ricerca utenti
                // var searchResults = await ApiService.SearchUsersAsync(searchText);

                SearchResults.Clear();

                // Esempio di come popolare i risultati quando avrai i dati:
                // foreach (var user in searchResults)
                // {
                //     SearchResults.Add(new UserSearchResult
                //     {
                //         UserId = user.UserId,
                //         Username = user.Username,
                //         FullName = $"{user.Nome} {user.Cognome}",
                //         Email = user.Email,
                //         ProfilePicture = user.ProfilePic ?? "default_avatar.png"
                //     });
                // }

                // Mostra i risultati
                SearchResultsCollectionView.IsVisible = SearchResults.Count > 0;

                if (SearchResults.Count == 0)
                {
                    await DisplayAlert("Ricerca", "Nessun utente trovato con questi criteri", "OK");
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

                        // TODO: Implementare chiamata API per inviare richiesta di amicizia
                        // await ApiService.SendFriendRequestAsync(user.Email);

                        // Rimuovi dai risultati di ricerca
                        SearchResults.Remove(user);

                        // Ricarica le richieste inviate per mostrare la nuova richiesta
                        await LoadSentRequestsFromBackend();

                        await DisplayAlert("Successo", $"Richiesta di amicizia inviata a {user.Username}!", "OK");
                    }
                    catch (Exception ex)
                    {
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
                                // TODO: Implementare chiamata API per rimuovere amicizia
                                // await ApiService.RemoveFriendshipAsync(friend.Email);

                                Friends.Remove(friend);
                                await DisplayAlert("Amicizia rimossa", $"Hai rimosso {friend.Username} dai tuoi amici.", "OK");
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
                        // TODO: Implementare chiamata API per annullare richiesta
                        // await ApiService.CancelFriendRequestAsync(request.RequestId);

                        SentRequests.Remove(request);
                        await DisplayAlert("Richiesta annullata", $"Richiesta a {request.Username} annullata.", "OK");
                    }
                    catch (Exception ex)
                    {
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

                    // TODO: Implementare chiamata API per accettare richiesta
                    // await ApiService.AcceptFriendRequestAsync(request.RequestId);

                    // Rimuovi dalle richieste ricevute
                    ReceivedRequests.Remove(request);

                    // Ricarica la lista amici
                    await LoadFriendsFromBackend();

                    await DisplayAlert("Amicizia accettata", $"Ora sei amico di {request.Username}!", "OK");
                }
                catch (Exception ex)
                {
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

                        // TODO: Implementare chiamata API per rifiutare richiesta
                        // await ApiService.RejectFriendRequestAsync(request.RequestId);

                        ReceivedRequests.Remove(request);
                        await DisplayAlert("Richiesta rifiutata", $"Richiesta di {request.Username} rifiutata.", "OK");
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