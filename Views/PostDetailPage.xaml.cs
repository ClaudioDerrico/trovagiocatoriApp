using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostDetailPage : ContentPage, INotifyPropertyChanged
    {
        private readonly int _postId;
        private static readonly HttpClient _sharedClient = CreateHttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;
        private readonly string _pythonApiBaseUrl = ApiConfig.PythonApiUrl;

        // Stato del preferito
        private bool _isFavorite = false;

        // Stato della partecipazione
        private bool _isParticipant = false;
        private int _participantsCount = 0;
        private int _postiDisponibili = 0;
        private bool _isEventFull = false;
        private List<ParticipantInfo> _participants = new List<ParticipantInfo>();

        // Dati del post per riferimento
        private PostResponse _currentPost;

        // NUOVO: Per identificare se l'utente corrente è l'autore del post
        private string _postAuthorEmail = "";
        private string _currentUserEmail = "";
        private bool _isPostAuthor = false;

        // ObservableCollection per i commenti e partecipanti
        public ObservableCollection<Comment> Comments { get; set; } = new ObservableCollection<Comment>();
        public ObservableCollection<ParticipantInfo> Participants { get; set; } = new ObservableCollection<ParticipantInfo>();

        // Proprietà per il campo sportivo
        private SportField _campo;
        public SportField Campo
        {
            get => _campo;
            set
            {
                _campo = value;
                OnPropertyChanged();
            }
        }

        public PostDetailPage(int postId)
        {
            InitializeComponent();
            _postId = postId;
            BindingContext = this;
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
            await LoadCurrentUserEmailAsync(); // NUOVO: Carica l'email dell'utente corrente
            await LoadPostDetailAsync();
            await LoadCommentsAsync();
            await CheckFavoriteStatusAsync();
            await CheckParticipationStatusAsync();
            await LoadParticipantsAsync();
        }

        // NUOVO: Metodo per ottenere l'email dell'utente corrente
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
                    var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (userData.ContainsKey("email"))
                    {
                        _currentUserEmail = userData["email"].ToString();
                        Debug.WriteLine($"[DEBUG] Email utente corrente: {_currentUserEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel caricamento dell'email utente: {ex.Message}");
            }
        }

        private async Task LoadPostDetailAsync()
        {
            try
            {
                // 1. Carica i dati del post
                var post = await LoadPostDataAsync();
                _currentPost = post;

                // 2. Salva l'email dell'autore del post
                _postAuthorEmail = post.autore_email;

                // NUOVO: 3. Determina se l'utente corrente è l'autore del post
                _isPostAuthor = !string.IsNullOrEmpty(_currentUserEmail) &&
                                _currentUserEmail.Equals(_postAuthorEmail, StringComparison.OrdinalIgnoreCase);

                Debug.WriteLine($"[DEBUG] Post autore: {_postAuthorEmail}");
                Debug.WriteLine($"[DEBUG] Utente corrente: {_currentUserEmail}");
                Debug.WriteLine($"[DEBUG] È l'autore del post: {_isPostAuthor}");

                // 4. Mostra/nascondi le sezioni appropriate
                UpdateUIBasedOnAuthorStatus();

                // 5. Carica i dati dell'utente
                var user = await LoadUserDataAsync(post.autore_email);

                // 6. Carica le informazioni del campo se presente
                if (post.campo_id.HasValue)
                {
                    Campo = await LoadSportFieldAsync(post.campo_id.Value);
                }

                // 7. Aggiorna la UI
                UpdateUI(post, user);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", ex.Message, "OK");
            }
        }

        // NUOVO: Metodo per aggiornare la UI in base allo status dell'autore
        private void UpdateUIBasedOnAuthorStatus()
        {
            if (_isPostAuthor)
            {
                // L'utente è l'autore del post
                ParticipationFrame.IsVisible = false;  // Nascondi sezione partecipazione
                OrganizerFrame.IsVisible = true;       // Mostra sezione organizzatore

                // Cambia il placeholder dell'editor per i commenti
                RispostaEditor.Placeholder = "Rispondi ai partecipanti e organizza i dettagli...";

                Debug.WriteLine("[DEBUG] UI configurata per l'autore del post");
            }
            else
            {
                // L'utente NON è l'autore del post
                ParticipationFrame.IsVisible = true;   // Mostra sezione partecipazione
                OrganizerFrame.IsVisible = false;      // Nascondi sezione organizzatore

                // Mantieni il placeholder standard
                RispostaEditor.Placeholder = "Scrivi un messaggio per l'organizzatore...";

                Debug.WriteLine("[DEBUG] UI configurata per partecipante");
            }
        }

        private async Task<PostResponse> LoadPostDataAsync()
        {
            var response = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/posts/{_postId}");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Post non trovato");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PostResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<User> LoadUserDataAsync(string email)
        {
            var encodedEmail = Uri.EscapeDataString(email);

            var response = await _sharedClient.GetAsync(
                $"{_apiBaseUrl}/api/user/by-email?email={encodedEmail}");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Utente non trovato");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private async Task<SportField> LoadSportFieldAsync(int fieldId)
        {
            try
            {
                var response = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/fields/{fieldId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SportField>(
                        json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel caricamento del campo: {ex.Message}");
            }

            return null;
        }

        private void UpdateUI(PostResponse post, User user)
        {
            // Dati utente
            AutoreLabel.Text = $"{user.Username}";

            ProfileImage.Source = !string.IsNullOrEmpty(user.ProfilePic)
                ? $"{_apiBaseUrl}/images/{user.ProfilePic}"
                : "default_images.jpg";

            // Dati post
            TitoloLabel.Text = post.titolo;
            DataOraLabel.Text = $"{post.data_partita} alle {post.ora_partita}";
            LocalitaLabel.Text = $"{post.citta}, {post.provincia}";
            SportLabel.Text = post.sport;
            CommentoLabel.Text = post.commento;

            // Gestione numero giocatori
            if (post.numero_giocatori > 0)
            {
                NumeroGiocatoriLabel.Text = post.numero_giocatori == 1
                    ? "Cerco 1 giocatore"
                    : $"Cerco {post.numero_giocatori} giocatori";
            }
            else
            {
                NumeroGiocatoriLabel.Text = "Cerco giocatori";
            }

            // Gestione del livello
            if (!string.IsNullOrEmpty(post.livello))
            {
                LivelloLabel.Text = post.livello switch
                {
                    "Principiante" => "🟢 Principiante",
                    "Intermedio" => "🟡 Intermedio",
                    "Avanzato" => "🔴 Avanzato",
                    _ => "🟡 Intermedio"
                };

                // Imposta il colore del livello
                LivelloLabel.TextColor = post.livello switch
                {
                    "Principiante" => Colors.Green,
                    "Intermedio" => Colors.Orange,
                    "Avanzato" => Colors.Red,
                    _ => Colors.Orange
                };
            }
            else
            {
                LivelloLabel.Text = "🟡 Intermedio";
                LivelloLabel.TextColor = Colors.Orange;
            }

            // Aggiorna le informazioni del campo se presente
            if (Campo != null)
            {
                CampoNomeLabel.Text = Campo.Nome;
                CampoIndirizzoLabel.Text = Campo.Indirizzo;
                CampoTipoLabel.Text = Campo.Tipo ?? "Non specificato";
                CampoDescrizioneLabel.Text = Campo.Descrizione ?? "Nessuna descrizione disponibile";
            }
        }

        // ========== FUNZIONI PER I PREFERITI ==========

        private async Task CheckFavoriteStatusAsync()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/favorites/check/{_postId}");

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

                    if (result.ContainsKey("is_favorite") && result["is_favorite"] is JsonElement element)
                    {
                        _isFavorite = element.GetBoolean();
                        UpdateFavoriteIcon();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel controllo preferiti: {ex.Message}");
            }
        }

        private async void OnFavoriteButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    _isFavorite ? $"{_apiBaseUrl}/favorites/remove" : $"{_apiBaseUrl}/favorites/add"
                );

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var payload = new { post_id = _postId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _isFavorite = !_isFavorite;
                    UpdateFavoriteIcon();

                    var message = _isFavorite ? "Aggiunto ai preferiti!" : "Rimosso dai preferiti!";
                    await DisplayAlert("Preferiti", message, "OK");
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile aggiornare i preferiti", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
        }

        private void UpdateFavoriteIcon()
        {
            FavoriteButton.Source = _isFavorite ? "heart_filled.png" : "heart_empty.png";
        }

        // ========== FUNZIONI PER LA PARTECIPAZIONE ==========

        private async Task CheckParticipationStatusAsync()
        {
            // MODIFICATO: Non controllare la partecipazione se l'utente è l'autore
            if (_isPostAuthor)
            {
                Debug.WriteLine("[DEBUG] Utente è l'autore, skip controllo partecipazione");
                return;
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/events/check/{_postId}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ParticipationResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _isParticipant = result.is_participant;
                    UpdateParticipationUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel controllo partecipazione: {ex.Message}");
            }
        }

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
                    _isEventFull = availabilityData.is_full;

                    // Aggiorna la collezione dei partecipanti
                    Participants.Clear();
                    foreach (var participant in participantsData.participants ?? new List<ParticipantInfo>())
                    {
                        // Aggiungi flag per identificare l'organizzatore
                        participant.IsOrganizer = participant.email.Equals(_postAuthorEmail, StringComparison.OrdinalIgnoreCase);
                        Participants.Add(participant);
                    }

                    UpdateParticipationUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel caricamento partecipanti: {ex.Message}");
            }
        }

        private async void OnJoinLeaveEventClicked(object sender, EventArgs e)
        {
            // MODIFICATO: Controllo aggiuntivo per sicurezza
            if (_isPostAuthor)
            {
                await DisplayAlert("Informazione", "Non puoi partecipare al tuo stesso evento!", "OK");
                return;
            }

            try
            {
                string endpoint = _isParticipant ? "/events/leave" : "/events/join";
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}{endpoint}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }
                else
                {
                    await DisplayAlert("Errore", "Effettua il login per partecipare agli eventi.", "OK");
                    return;
                }

                var payload = new ParticipationRequest { post_id = _postId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<ParticipationResponse>(responseJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _isParticipant = result.is_participant;

                    await DisplayAlert("Successo", result.message, "OK");

                    // Ricarica i dati dei partecipanti
                    await LoadParticipantsAsync();
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile aggiornare la partecipazione", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
        }

        private void UpdateParticipationUI()
        {
            if (_currentPost == null) return;

            // Aggiorna il testo del numero di giocatori con informazioni sui partecipanti
            var baseText = _currentPost.numero_giocatori == 1
                ? "Cerco 1 giocatore"
                : $"Cerco {_currentPost.numero_giocatori} giocatori";

            if (_participantsCount > 0)
            {
                NumeroGiocatoriLabel.Text = $"{baseText} • {_participantsCount}/{_currentPost.numero_giocatori} iscritti";

                if (_postiDisponibili > 0)
                {
                    NumeroGiocatoriLabel.Text += $" • {_postiDisponibili} posti liberi";
                    NumeroGiocatoriLabel.TextColor = Colors.Green;
                }
                else
                {
                    NumeroGiocatoriLabel.Text += " • COMPLETO";
                    NumeroGiocatoriLabel.TextColor = Colors.Red;
                }
            }
            else
            {
                NumeroGiocatoriLabel.Text = baseText;
                NumeroGiocatoriLabel.TextColor = Colors.Blue;
            }

            // NUOVO: Aggiorna anche il label della sezione organizzatore se applicabile
            if (_isPostAuthor && OrganizerPostiLabel != null)
            {
                if (_isEventFull)
                {
                    OrganizerPostiLabel.Text = "Evento completo! 🎉";
                    OrganizerPostiLabel.TextColor = Colors.Green;
                }
                else
                {
                    OrganizerPostiLabel.Text = $"{_postiDisponibili} posti disponibili su {_currentPost.numero_giocatori}";
                    OrganizerPostiLabel.TextColor = Colors.Orange;
                }
            }

            // Aggiorna le informazioni sui posti disponibili per i non-autori
            if (!_isPostAuthor)
            {
                if (PostiDisponibiliLabel != null)
                {
                    if (_isEventFull)
                    {
                        PostiDisponibiliLabel.Text = "Evento completo";
                        PostiDisponibiliLabel.TextColor = Colors.Red;
                    }
                    else
                    {
                        PostiDisponibiliLabel.Text = $"{_postiDisponibili} posti disponibili su {_currentPost.numero_giocatori}";
                        PostiDisponibiliLabel.TextColor = Colors.Green;
                    }
                }

                // Aggiorna lo status di partecipazione
                if (StatusPartecipazioneLabel != null)
                {
                    if (_isParticipant)
                    {
                        StatusPartecipazioneLabel.Text = "✅ Sei iscritto a questo evento";
                        StatusPartecipazioneLabel.TextColor = Colors.Green;
                    }
                    else
                    {
                        StatusPartecipazioneLabel.Text = "Non sei ancora iscritto a questo evento";
                        StatusPartecipazioneLabel.TextColor = Colors.Gray;
                    }
                }

                // Aggiorna il pulsante di partecipazione
                if (JoinLeaveButton != null)
                {
                    if (_isEventFull && !_isParticipant)
                    {
                        JoinLeaveButton.Text = "EVENTO COMPLETO";
                        JoinLeaveButton.IsEnabled = false;
                        JoinLeaveButton.BackgroundColor = Colors.Gray;
                    }
                    else
                    {
                        JoinLeaveButton.Text = _isParticipant ? "DISISCRIVITI" : "PARTECIPA ALL'EVENTO";
                        JoinLeaveButton.IsEnabled = true;
                        JoinLeaveButton.BackgroundColor = _isParticipant ? Colors.Orange : Colors.Green;
                    }
                }
            }

            // Aggiorna il badge contatore partecipanti
            if (ParticipantsCountBadge != null)
            {
                ParticipantsCountBadge.Text = _participantsCount.ToString();
            }
        }

        // ========== FUNZIONI PER I COMMENTI ==========

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

                    Comments.Clear();
                    foreach (var comment in comments ?? new List<Comment>())
                    {
                        comment.autore_username = await GetUsernameByEmail(comment.autore_email);
                        comment.IsAuthorComment = comment.autore_email.Equals(_postAuthorEmail, StringComparison.OrdinalIgnoreCase);
                        Comments.Add(comment);
                    }
                }
            }
            catch (Exception)
            {
                // Fallback silenzioso
            }
        }

        private async Task<string> GetUsernameByEmail(string email)
        {
            try
            {
                var encodedEmail = Uri.EscapeDataString(email);

                var response = await _sharedClient.GetAsync($"{_apiBaseUrl}/api/user/by-email?email={encodedEmail}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var user = JsonSerializer.Deserialize<User>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return user?.Username ?? email;
                }

                return email;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nel recupero username per {email}: {ex.Message}");
                return email;
            }
        }

        private async void OnInviaRispostaClicked(object sender, EventArgs e)
        {
            string messaggio = RispostaEditor.Text;
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

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Successo", "Commento inviato!", "OK");
                    RispostaEditor.Text = string.Empty;
                    await LoadCommentsAsync();
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile inviare il commento.", "OK");
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
        }

        // ========== ALTRE FUNZIONI ==========

        private async void OnViewOnMapClicked(object sender, EventArgs e)
        {
            if (Campo == null)
            {
                await DisplayAlert("Info", "Coordinate o indirizzo del campo non disponibili.", "OK");
                return;
            }

            try
            {
                string uriString;
                string latStr = Campo.Lat.ToString(CultureInfo.InvariantCulture);
                string lngStr = Campo.Lng.ToString(CultureInfo.InvariantCulture);
                uriString = $"https://www.google.com/maps/search/?api=1&query={latStr},{lngStr}";

                await Launcher.OpenAsync(new Uri(uriString));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fallback map open failed: {ex.Message}");
                await DisplayAlert("Errore", $"Impossibile aprire la mappa: {ex.Message}", "OK");
            }
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Implementazione INotifyPropertyChanged
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}