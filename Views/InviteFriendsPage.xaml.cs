// Views/InviteFriendsPage.xaml.cs
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;
using System.Diagnostics;

namespace trovagiocatoriApp.Views
{
    public partial class InviteFriendsPage : ContentPage
    {
        private readonly int _postId;
        private readonly PostResponse _currentPost;
        private readonly List<FriendInfo> _allFriends;
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;

        // Traccia gli amici invitati
        private readonly HashSet<string> _invitedFriendEmails = new HashSet<string>();
        private int _invitedCount = 0;

        public ObservableCollection<FriendInfo> Friends { get; set; } = new ObservableCollection<FriendInfo>();

        public InviteFriendsPage(int postId, PostResponse currentPost, List<FriendInfo> friends)
        {
            InitializeComponent();

            _postId = postId;
            _currentPost = currentPost;
            _allFriends = friends;

            // Popola la collection
            foreach (var friend in friends)
            {
                Friends.Add(friend);
            }

            FriendsCollectionView.ItemsSource = Friends;

            // Inizializza UI
            UpdateEventInfo();
            UpdateStatistics();
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true
            };
            return new HttpClient(handler);
        }

        private void UpdateEventInfo()
        {
            EventTitleLabel.Text = _currentPost.titolo;
            EventDateLabel.Text = _currentPost.data_partita;
            EventTimeLabel.Text = _currentPost.ora_partita;
            EventLocationLabel.Text = $"{_currentPost.citta}, {_currentPost.provincia}";
        }

        private void UpdateStatistics()
        {
            TotalFriendsLabel.Text = _allFriends.Count.ToString();
            InvitedCountLabel.Text = _invitedCount.ToString();
            RemainingLabel.Text = (_allFriends.Count - _invitedCount).ToString();

            // Aggiorna il pulsante di fine
            FinishButton.Text = _invitedCount > 0
                ? $"✅ Termina ({_invitedCount} inviti)"
                : "✅ Termina Inviti";
        }

        private async void OnInviteFriendClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                try
                {
                    button.IsEnabled = false;
                    button.Text = "Invio...";

                    // Invia l'invito tramite API
                    bool success = await SendEventInvite(friend.Email);

                    if (success)
                    {
                        // Aggiungi all'elenco degli invitati
                        _invitedFriendEmails.Add(friend.Email);
                        _invitedCount++;

                        // Aggiorna UI del pulsante
                        button.Text = "✅ Invitato";
                        button.Style = (Style)Resources["InvitedButtonStyle"];
                        button.BackgroundColor = Color.FromArgb("#9E9E9E");

                        // Aggiorna statistiche
                        UpdateStatistics();

                        // Mostra notifica di successo
                        await DisplayAlert("Invito Inviato", $"Invito inviato a {friend.Username}!", "OK");

                        Debug.WriteLine($"[INVITE] ✅ Invito inviato a {friend.Email} per evento {_postId}");
                    }
                    else
                    {
                        button.IsEnabled = true;
                        button.Text = "Invita";
                        await DisplayAlert("Errore", "Impossibile inviare l'invito. Riprova.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[INVITE] Errore invio invito: {ex.Message}");
                    button.IsEnabled = true;
                    button.Text = "Invita";
                    await DisplayAlert("Errore", "Errore nell'invio dell'invito", "OK");
                }
            }
        }

        private async Task<bool> SendEventInvite(string friendEmail)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/events/invite");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var payload = new
                {
                    post_id = _postId,
                    friend_email = friendEmail,
                    message = $"Ti ho invitato a partecipare al mio evento: {_currentPost.titolo}"
                };

                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITE] Errore nella chiamata API: {ex.Message}");
                return false;
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            if (_invitedCount > 0)
            {
                bool confirm = await DisplayAlert(
                    "Conferma Annullamento",
                    $"Hai già inviato {_invitedCount} inviti. Vuoi davvero annullare?",
                    "Sì, Annulla",
                    "No, Continua"
                );

                if (!confirm)
                    return;
            }

            await Navigation.PopAsync();
        }

        private async void OnFinishInvitesClicked(object sender, EventArgs e)
        {
            if (_invitedCount == 0)
            {
                await DisplayAlert(
                    "Nessun Invito",
                    "Non hai ancora inviato nessun invito. Vuoi tornare indietro?",
                    "OK"
                );
                return;
            }

            await DisplayAlert(
                "Inviti Completati",
                $"Hai inviato {_invitedCount} inviti con successo! I tuoi amici riceveranno una notifica e potranno accettare l'invito dalla sezione 'I Miei Eventi' del loro profilo.",
                "Perfetto"
            );

            await Navigation.PopAsync();
        }
    }
}