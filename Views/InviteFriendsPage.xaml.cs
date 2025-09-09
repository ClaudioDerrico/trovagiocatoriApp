// Views/InviteFriendsPage.xaml.cs - VERSIONE AGGIORNATA CON FILTRAGGIO AUTOMATICO
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
        private readonly List<FriendInfo> _allFriends; // Mantenuto per fallback
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;

        // Traccia gli amici SELEZIONATI per l'invito (non ancora inviati)
        private readonly HashSet<string> _selectedFriendEmails = new HashSet<string>();
        private int _selectedCount = 0;

        public ObservableCollection<FriendInfo> Friends { get; set; } = new ObservableCollection<FriendInfo>();

        public InviteFriendsPage(int postId, PostResponse currentPost, List<FriendInfo> friends)
        {
            InitializeComponent();

            _postId = postId;
            _currentPost = currentPost;
            _allFriends = friends;

            FriendsCollectionView.ItemsSource = Friends;

            // Inizializza UI
            UpdateEventInfo();

            // NUOVO: Carica solo gli amici disponibili per l'invito
            _ = LoadAvailableFriendsAsync();
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
            var availableCount = Friends.Count; // Ora Friends contiene solo quelli disponibili
            TotalFriendsLabel.Text = availableCount.ToString();
            InvitedCountLabel.Text = _selectedCount.ToString();
            RemainingLabel.Text = (availableCount - _selectedCount).ToString();

            // Aggiorna il pulsante di fine
            FinishButton.Text = _selectedCount > 0
                ? $"✅ Invia {_selectedCount} Inviti"
                : "✅ Termina Inviti";
        }

        // NUOVO: Carica solo gli amici disponibili per essere invitati
        private async Task LoadAvailableFriendsAsync()
        {
            try
            {
                Debug.WriteLine($"[INVITE] Caricamento amici disponibili per post {_postId}...");

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/friends/available-for-invite?post_id={_postId}");

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

                    if (result.ContainsKey("available_friends") && result["available_friends"] is JsonElement friendsElement)
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

                        Debug.WriteLine($"[INVITE] ✅ Caricati {Friends.Count} amici disponibili per l'invito");
                    }
                }
                else
                {
                    Debug.WriteLine($"[INVITE] ⚠️ Errore nel caricamento amici disponibili: {response.StatusCode}");
                    await UseFallbackFriendsList();
                }

                // Aggiorna le statistiche dopo aver caricato i dati
                MainThread.BeginInvokeOnMainThread(() => UpdateStatistics());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITE] ❌ Eccezione durante caricamento amici disponibili: {ex.Message}");
                await UseFallbackFriendsList();

                MainThread.BeginInvokeOnMainThread(() => UpdateStatistics());
            }
        }

        // Fallback: usa la lista originale se l'API fallisce
        private async Task UseFallbackFriendsList()
        {
            await Task.Run(() =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Friends.Clear();
                    foreach (var friend in _allFriends)
                    {
                        Friends.Add(friend);
                    }
                    Debug.WriteLine($"[INVITE] 🔄 Usato fallback: {Friends.Count} amici dalla lista originale");
                });
            });
        }

        // Helper methods per parsing JSON
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

        // MODIFICATO: Ora il pulsante "Invita" seleziona/deseleziona l'amico
        private async void OnInviteFriendClicked(object sender, EventArgs e)
        {
            if (sender is Button button && button.CommandParameter is FriendInfo friend)
            {
                try
                {
                    // Controlla se l'amico è già selezionato
                    if (_selectedFriendEmails.Contains(friend.Email))
                    {
                        // DESELEZIONA l'amico
                        _selectedFriendEmails.Remove(friend.Email);
                        _selectedCount--;

                        // Aggiorna UI del pulsante per tornare a "Invita"
                        button.Text = "Invita";
                        button.Style = (Style)Resources["InviteButtonStyle"];
                        button.BackgroundColor = Color.FromArgb("#10B981");
                        button.IsEnabled = true;

                        Debug.WriteLine($"[INVITE] ❌ Deselezionato {friend.Email}");
                    }
                    else
                    {
                        // SELEZIONA l'amico
                        _selectedFriendEmails.Add(friend.Email);
                        _selectedCount++;

                        // Aggiorna UI del pulsante per mostrare "Selezionato"
                        button.Text = "✅ Selezionato";
                        button.Style = (Style)Resources["InvitedButtonStyle"];
                        button.BackgroundColor = Color.FromArgb("#FF9800");
                        button.IsEnabled = true; // Mantieni cliccabile per deselezionare

                        Debug.WriteLine($"[INVITE] ✅ Selezionato {friend.Email}");
                    }

                    // Aggiorna statistiche
                    UpdateStatistics();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[INVITE] Errore selezione amico: {ex.Message}");
                    await DisplayAlert("Errore", "Errore nella selezione dell'amico", "OK");
                }
            }
        }

        // NUOVO: Invia tutti gli inviti selezionati
        private async Task SendAllSelectedInvites()
        {
            var successCount = 0;
            var failCount = 0;

            foreach (var friendEmail in _selectedFriendEmails.ToList())
            {
                try
                {
                    bool success = await SendEventInvite(friendEmail);

                    if (success)
                    {
                        successCount++;
                        Debug.WriteLine($"[INVITE] ✅ Invito inviato a {friendEmail}");
                    }
                    else
                    {
                        failCount++;
                        Debug.WriteLine($"[INVITE] ❌ Invito fallito a {friendEmail}");
                    }

                    // Piccola pausa tra gli inviti per evitare sovraccarico
                    await Task.Delay(200);
                }
                catch (Exception ex)
                {
                    failCount++;
                    Debug.WriteLine($"[INVITE] Errore invio a {friendEmail}: {ex.Message}");
                }
            }

            // Mostra risultato finale
            var message = $"Inviti completati!\n✅ Inviati: {successCount}";
            if (failCount > 0)
            {
                message += $"\n❌ Falliti: {failCount}";
            }

            await DisplayAlert("Inviti Inviati", message, "OK");
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
            if (_selectedCount > 0)
            {
                bool confirm = await DisplayAlert(
                    "Conferma Annullamento",
                    $"Hai selezionato {_selectedCount} amici ma non hai ancora inviato gli inviti. Vuoi davvero annullare?",
                    "Sì, Annulla",
                    "No, Continua"
                );

                if (!confirm)
                    return;
            }

            await Navigation.PopAsync();
        }

        // MODIFICATO: Ora invia effettivamente tutti gli inviti selezionati
        private async void OnFinishInvitesClicked(object sender, EventArgs e)
        {
            if (_selectedCount == 0)
            {
                await DisplayAlert(
                    "Nessun Amico Selezionato",
                    "Non hai ancora selezionato nessun amico da invitare. Seleziona almeno un amico per continuare.",
                    "OK"
                );
                return;
            }

            // Conferma invio
            var confirmMessage = _selectedCount == 1
                ? "Vuoi inviare l'invito all'amico selezionato?"
                : $"Vuoi inviare gli inviti ai {_selectedCount} amici selezionati?";

            bool confirm = await DisplayAlert(
                "Conferma Invio",
                confirmMessage,
                "Invia Inviti",
                "Annulla"
            );

            if (!confirm)
                return;

            // Disabilita il pulsante durante l'invio
            FinishButton.IsEnabled = false;
            FinishButton.Text = "Invio in corso...";

            try
            {
                // Invia tutti gli inviti
                await SendAllSelectedInvites();

                // Torna alla pagina precedente
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITE] Errore durante invio massivo: {ex.Message}");
                await DisplayAlert("Errore", "Errore durante l'invio degli inviti", "OK");
            }
            finally
            {
                FinishButton.IsEnabled = true;
                FinishButton.Text = "✅ Termina Inviti";
            }
        }
    }
}