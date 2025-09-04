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

            // Carica dati di esempio (da rimuovere quando implementerai il backend)
            LoadSampleData();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Qui caricherai i dati dal backend
            // LoadFriends();
            // LoadSentRequests();
            // LoadReceivedRequests();
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

            // Simula ricerca (sostituire con chiamata API)
            await SearchUsers(searchText);
        }

        private async Task SearchUsers(string searchText)
        {
            try
            {
                // Simula caricamento
                SearchButton.IsEnabled = false;
                SearchButton.Text = "⏳";

                // Simula delay API
                await Task.Delay(1000);

                // Dati di esempio - sostituire con chiamata API reale
                SearchResults.Clear();

                // Aggiungi alcuni risultati di esempio se il testo contiene certe parole
                if (searchText.ToLower().Contains("test") || searchText.ToLower().Contains("demo"))
                {
                    SearchResults.Add(new UserSearchResult
                    {
                        UserId = 1,
                        Username = "test_user1",
                        FullName = "Mario Rossi",
                        Email = "mario.rossi@example.com",
                        ProfilePicture = "default_avatar.png"
                    });

                    SearchResults.Add(new UserSearchResult
                    {
                        UserId = 2,
                        Username = "demo_user",
                        FullName = "Anna Verdi",
                        Email = "anna.verdi@example.com",
                        ProfilePicture = "default_avatar.png"
                    });
                }

                // Mostra i risultati
                SearchResultsCollectionView.IsVisible = SearchResults.Count > 0;
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore durante la ricerca: {ex.Message}", "OK");
            }
            finally
            {
                SearchButton.IsEnabled = true;
                SearchButton.Text = "🔍";
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
                    // Simula invio richiesta
                    button.IsEnabled = false;
                    button.Text = "⏳ Invio...";

                    await Task.Delay(1000);

                    // Aggiungi alla lista delle richieste inviate
                    SentRequests.Add(new FriendRequest
                    {
                        UserId = user.UserId,
                        Username = user.Username,
                        FullName = user.FullName,
                        Email = user.Email,
                        ProfilePicture = user.ProfilePicture,
                        RequestSent = DateTime.Now,
                        Status = FriendRequestStatus.Pending
                    });

                    // Rimuovi dai risultati di ricerca
                    SearchResults.Remove(user);

                    button.IsEnabled = true;
                    button.Text = "✅ Inviata";

                    await DisplayAlert("Successo", $"Richiesta di amicizia inviata a {user.Username}!", "OK");
                }
            }
        }

        private async void OnMessageFriendClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                await DisplayAlert("Messaggio", $"Aprendo chat con {friend.Username}...", "OK");
                // Qui implementerai la navigazione alla chat
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
                        // Implementa navigazione al profilo
                        break;

                    case "Rimuovi amicizia":
                        var confirm = await DisplayAlert(
                            "Rimuovi amicizia",
                            $"Sei sicuro di voler rimuovere {friend.Username} dai tuoi amici?",
                            "Sì",
                            "No");

                        if (confirm)
                        {
                            Friends.Remove(friend);
                            await DisplayAlert("Amicizia rimossa", $"Hai rimosso {friend.Username} dai tuoi amici.", "OK");
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
                    SentRequests.Remove(request);
                    await DisplayAlert("Richiesta annullata", $"Richiesta a {request.Username} annullata.", "OK");
                }
            }
        }

        private async void OnAcceptRequestClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendRequest request)
            {
                // Aggiungi agli amici
                Friends.Add(new FriendInfo
                {
                    UserId = request.UserId,
                    Username = request.Username,
                    FullName = request.FullName,
                    Email = request.Email,
                    ProfilePicture = request.ProfilePicture,
                    FriendsSince = DateTime.Now
                });

                // Rimuovi dalle richieste ricevute
                ReceivedRequests.Remove(request);

                await DisplayAlert("Amicizia accettata", $"Ora sei amico di {request.Username}!", "OK");
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
                    ReceivedRequests.Remove(request);
                    await DisplayAlert("Richiesta rifiutata", $"Richiesta di {request.Username} rifiutata.", "OK");
                }
            }
        }

        // ========== DATI DI ESEMPIO ==========

        private void LoadSampleData()
        {
            // Aggiungi alcuni amici di esempio
            Friends.Add(new FriendInfo
            {
                UserId = 1,
                Username = "mario_rossi",
                FullName = "Mario Rossi",
                Email = "mario.rossi@example.com",
                ProfilePicture = "default_avatar.png",
                FriendsSince = DateTime.Now.AddDays(-30)
            });

            Friends.Add(new FriendInfo
            {
                UserId = 2,
                Username = "anna_verdi",
                FullName = "Anna Verdi",
                Email = "anna.verdi@example.com",
                ProfilePicture = "default_avatar.png",
                FriendsSince = DateTime.Now.AddDays(-15)
            });

            // Aggiungi alcune richieste inviate di esempio
            SentRequests.Add(new FriendRequest
            {
                UserId = 3,
                Username = "luca_bianchi",
                FullName = "Luca Bianchi",
                Email = "luca.bianchi@example.com",
                ProfilePicture = "default_avatar.png",
                RequestSent = DateTime.Now.AddDays(-2),
                Status = FriendRequestStatus.Pending
            });

            // Aggiungi alcune richieste ricevute di esempio
            ReceivedRequests.Add(new FriendRequest
            {
                UserId = 4,
                Username = "sara_neri",
                FullName = "Sara Neri",
                Email = "sara.neri@example.com",
                ProfilePicture = "default_avatar.png",
                RequestReceived = DateTime.Now.AddDays(-1),
                Status = FriendRequestStatus.Pending
            });

            ReceivedRequests.Add(new FriendRequest
            {
                UserId = 5,
                Username = "giuseppe_blu",
                FullName = "Giuseppe Blu",
                Email = "giuseppe.blu@example.com",
                ProfilePicture = "default_avatar.png",
                RequestReceived = DateTime.Now.AddHours(-3),
                Status = FriendRequestStatus.Pending
            });
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