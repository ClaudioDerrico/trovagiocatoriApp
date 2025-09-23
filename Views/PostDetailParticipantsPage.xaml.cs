using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostDetailParticipantsPage : ContentPage, INotifyPropertyChanged
    {
        private readonly int _postId;
        private readonly PostResponse _currentPost;
        private readonly bool _isPostAuthor;
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;
        private readonly string _pythonApiBaseUrl = ApiConfig.PythonApiUrl;

        // Stato dei tab
        private bool _isParticipantsTabActive = true; 

        // Dati partecipanti
        private int _participantsCount = 0;
        private int _postiDisponibili = 0;
        private string _postAuthorEmail = "";

        // Email utente corrente per la chat
        private string _currentUserEmail = "";

        // Proprietà pubblica per il binding XAML
        public bool IsPostAuthor => _isPostAuthor;

        // ObservableCollection per i partecipanti e commenti
        public ObservableCollection<ParticipantInfo> Participants { get; set; } = new ObservableCollection<ParticipantInfo>();
        public ObservableCollection<Comment> Comments { get; set; } = new ObservableCollection<Comment>();

        public PostDetailParticipantsPage(int postId, PostResponse currentPost, bool isPostAuthor, bool startWithChat = false)
        {
            InitializeComponent();
            _postId = postId;
            _currentPost = currentPost;
            _isPostAuthor = isPostAuthor;
            _postAuthorEmail = currentPost.autore_email;

            BindingContext = this;

            // Imposta le informazioni dell'evento nell'header
            UpdateEventHeader();

            // Mostra info chat per organizzatore       
            OrganizerChatInfo.IsVisible = _isPostAuthor;

           
            if (startWithChat) 
            {
                _isParticipantsTabActive = false;
                UpdateTabsUI();
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                UseCookies = true
            };
            return new HttpClient(handler);
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCurrentUserEmailAsync();
            await LoadParticipantsAsync();
            await LoadCommentsAsync();
        }

        // ========== CARICAMENTO DATI ==========

        private async Task LoadCurrentUserEmailAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/api/user");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json); //string: nome del campo json, es:"nome"
                    //object: valore del campo json es: ""Mario"

                    if (userData.ContainsKey("email"))
                    {
                        _currentUserEmail = userData["email"].ToString();
                        Debug.WriteLine($"[PARTICIPANTS] Email utente corrente: {_currentUserEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PARTICIPANTS] Errore nel caricamento email utente: {ex.Message}");
            }
        }

        private void UpdateEventHeader()
        {
            EventTitleLabel.Text = _currentPost.titolo;
            EventDateLabel.Text = $"{_currentPost.data_partita} - {_currentPost.ora_partita}";
            EventLocationLabel.Text = $"{_currentPost.citta}, {_currentPost.provincia}";
        }

        // ========== GESTIONE TAB ==========

        private void OnParticipantsTabClicked(object sender, EventArgs e)
        {
            if (!_isParticipantsTabActive)
            {
                _isParticipantsTabActive = true;
                UpdateTabsUI();
            }
        }

        private void OnChatTabClicked(object sender, EventArgs e)
        {
            if (_isParticipantsTabActive)
            {
                _isParticipantsTabActive = false;
                UpdateTabsUI();
            }
        }

        private void UpdateTabsUI()
        { //se isParticipantsTabActive è true, attiva il tab Partecipanti, altrimenti attiva il tab Chat
            if (_isParticipantsTabActive)
            {
                // Attiva tab Partecipanti
                ParticipantsTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                ChatTabButton.Style = (Style)Resources["TabButtonStyle"];

                ParticipantsContent.IsVisible = true;
                ChatContent.IsVisible = false;

                // Sposta l'indicatore
                Grid.SetColumn(TabIndicator, 0);
            }
            else
            {
                // Attiva tab Chat
                ChatTabButton.Style = (Style)Resources["ActiveTabButtonStyle"];
                ParticipantsTabButton.Style = (Style)Resources["TabButtonStyle"];

                ParticipantsContent.IsVisible = false;
                ChatContent.IsVisible = true;

                // Sposta l'indicatore
                Grid.SetColumn(TabIndicator, 1);

                // Scrolla alla fine dei messaggi
                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    await Task.Delay(100);
                    await MessagesScrollView.ScrollToAsync(0, MessagesScrollView.ContentSize.Height, false);
                });
            }
        }

        // ========== GESTIONE PARTECIPANTI ==========

        private async Task LoadParticipantsAsync()
        {
            try
            {
                // Carica i partecipanti e la disponibilità
                var participantsResponse = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/posts/{_postId}/participants-count");
                var availabilityResponse = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/posts/{_postId}/availability");

                if (participantsResponse.IsSuccessStatusCode && availabilityResponse.IsSuccessStatusCode)
                {
                    var participantsJson = await participantsResponse.Content.ReadAsStringAsync();
                    var availabilityJson = await availabilityResponse.Content.ReadAsStringAsync();

                    var participantsData = JsonSerializer.Deserialize<EventParticipantsResponse>(participantsJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    var availabilityData = JsonSerializer.Deserialize<PostAvailabilityResponse>(availabilityJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _participantsCount = participantsData.count;
                    _postiDisponibili = availabilityData.posti_disponibili;

                    // Aggiorna la UI delle statistiche
                    UpdateStatisticsUI();

                    // Aggiorna la collezione dei partecipanti
                    Participants.Clear();
                    foreach (var participant in participantsData.participants ?? new List<ParticipantInfo>())
                    {
                        // flag per identificare l'organizzatore
                        // Se organizzatore, IsOrganizer = true
                        participant.IsOrganizer = participant.email.Equals(_postAuthorEmail, StringComparison.OrdinalIgnoreCase);
                        Participants.Add(participant);
                    }

                    // Aggiorna la UI dopo aver caricato i partecipanti
                    UpdateParticipantsUI();

                    ParticipantsCollectionView.ItemsSource = Participants;
                    ParticipantsCountBadge.Text = _participantsCount.ToString();

                    Debug.WriteLine($"[PARTICIPANTS] Caricati {_participantsCount} partecipanti");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PARTICIPANTS] Errore nel caricamento partecipanti: {ex.Message}");
            }
        }

        private void UpdateParticipantsUI() // Aggiorna la UI in base al ruolo dell'utente
        {
            // Forza l'aggiornamento del binding context per far scattare i trigger XAML
            OnPropertyChanged(nameof(IsPostAuthor));
        }


        // ========== GESTIONE CHAT PRIVATA ==========
                private async void OnChatWithParticipantClicked(object sender, EventArgs e)
                {
            if (sender is ImageButton button && button.CommandParameter is ParticipantInfo participant)
            {
                Debug.WriteLine($"[CHAT] Organizzatore {_currentUserEmail} vuole chattare con {participant.email}");

                try
                {
                    // Verifica che l'utente corrente sia effettivamente l'organizzatore
                    if (!_isPostAuthor)
                    {
                        await DisplayAlert("Errore", "Solo l'organizzatore può iniziare chat con i partecipanti", "OK");
                        return;
                    }

                    // Verifica che il partecipante non sia l'organizzatore stesso
                    if (participant.IsOrganizer)
                    {
                        await DisplayAlert("Info", "Non puoi chattare con te stesso!", "OK");
                        return;
                    }

                    // Verifica che sia effettivamente un partecipante iscritto
                    if (string.IsNullOrEmpty(participant.email))
                    {
                        await DisplayAlert("Errore", "Informazioni partecipante non disponibili", "OK");
                        return;
                    }

                    // Conferma prima di avviare la chat
                    bool startChat = await DisplayAlert(
                        "Avvia Chat",
                        $"Vuoi iniziare una chat privata con {participant.DisplayName}?",
                        "Avvia Chat",
                        "Annulla"
                    );

                    if (!startChat)
                        return;

                    // Avvia la chat con il partecipante selezionato
                    var chatPage = new ChatPage(_currentPost, _currentUserEmail, participant.email, true); // true = isPostAuthor
                    await Navigation.PushAsync(chatPage);

                    Debug.WriteLine($"[CHAT] Chat avviata tra organizzatore {_currentUserEmail} e partecipante {participant.email}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore nell'avvio chat: {ex.Message}");
                    await DisplayAlert("Errore", "Impossibile avviare la chat. Riprova più tardi.", "OK");
                }
            }
            else
            {
                Debug.WriteLine("[CHAT] Errore: CommandParameter non valido o mancante");
                await DisplayAlert("Errore", "Impossibile identificare il partecipante selezionato", "OK");
            }
        }

        private void UpdateStatisticsUI()
        {
            RequiredPlayersLabel.Text = _currentPost.numero_giocatori.ToString();
            RegisteredPlayersLabel.Text = _participantsCount.ToString();
            AvailableSpotsLabel.Text = _postiDisponibili.ToString();

            // Cambia colore in base alla disponibilità
            if (_postiDisponibili == 0)
            {
                AvailableSpotsLabel.TextColor = Color.FromArgb("#EF4444"); // Rosso
            }
            else if (_postiDisponibili <= 2)
            {
                AvailableSpotsLabel.TextColor = Color.FromArgb("#F59E0B"); // Arancione
            }
            else
            {
                AvailableSpotsLabel.TextColor = Color.FromArgb("#10B981"); // Verde
            }
        }

        // ========== GESTIONE CHAT PUBBLICA (Commenti) ==========

        private async Task LoadCommentsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_pythonApiBaseUrl}/posts/{_postId}/comments/");
                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var comments = JsonSerializer.Deserialize<List<Comment>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Comments.Clear(); // Pulisce la collezione prima di ricaricare

                    foreach (var comment in comments ?? new List<Comment>())
                    {
                        comment.IsAuthorComment = comment.autore_email.Equals(_postAuthorEmail, StringComparison.OrdinalIgnoreCase);
                        Comments.Add(comment);
                    }

                    CommentsCollectionView.ItemsSource = Comments;
                    Debug.WriteLine($"[COMMENTS] Caricati {Comments.Count} commenti");

                    // Scrolla alla fine se la chat è visibile
                    if (!_isParticipantsTabActive)
                    {
                        MainThread.BeginInvokeOnMainThread(async () =>
                        {
                            await Task.Delay(100);
                            await MessagesScrollView.ScrollToAsync(0, MessagesScrollView.ContentSize.Height, false);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[COMMENTS] Errore nel caricamento commenti: {ex.Message}");
            }
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            string messaggio = MessageEntry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(messaggio))
            {
                await DisplayAlert("Attenzione", "Il messaggio non può essere vuoto.", "OK");
                return;
            }

            try
            {
                var commentCreate = new CommentCreate
                {
                    contenuto = messaggio
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_pythonApiBaseUrl}/posts/{_postId}/comments/");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }
                else
                {
                    await DisplayAlert("Errore", "Sessione non trovata. Effettua di nuovo il login.", "OK");
                    return;
                }

                var json = JsonSerializer.Serialize(commentCreate);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                // Disabilita temporaneamente il pulsante per evitare doppi invii
                SendMessageButton.IsEnabled = false;

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    MessageEntry.Text = string.Empty;
                    await LoadCommentsAsync(); // Ricarica i commenti

                    // Scrolla alla fine
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        await Task.Delay(200);
                        await MessagesScrollView.ScrollToAsync(0, MessagesScrollView.ContentSize.Height, true);
                    });

                    Debug.WriteLine($"[CHAT] Commento pubblico inviato da {_currentUserEmail}");
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile inviare il messaggio.", "OK");
                }
            }
            catch (HttpRequestException)
            {
                await DisplayAlert("Errore di Connessione", "Impossibile raggiungere il server.", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
            finally
            {
                SendMessageButton.IsEnabled = true;
            }
        }

        // ========== INotifyPropertyChanged ==========

        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

  
}