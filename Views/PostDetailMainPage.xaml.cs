// Views/PostDetailMainPage.xaml.cs
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostDetailMainPage : ContentPage, INotifyPropertyChanged
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

        // Dati del post per riferimento
        private PostResponse _currentPost;

        // Per identificare se l'utente corrente è l'autore del post
        private string _postAuthorEmail = "";
        private string _currentUserEmail = "";
        private bool _isPostAuthor = false;

        // NUOVO: Proprietà pubblica per il binding XAML
        public bool IsPostAuthor => _isPostAuthor;

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

        public PostDetailMainPage(int postId)
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
            await LoadCurrentUserEmailAsync();
            await LoadPostDetailAsync();
            await CheckFavoriteStatusAsync();
            await CheckParticipationStatusAsync();
            await LoadParticipantsCountAsync();
        }

        // Carica l'email dell'utente corrente
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

                // 3. Determina se l'utente corrente è l'autore del post
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

        // Aggiorna la UI in base allo status dell'autore
        private void UpdateUIBasedOnAuthorStatus()
        {
            if (_isPostAuthor)
            {
                // L'utente è l'autore del post
                ParticipationFrame.IsVisible = false;  // Nascondi sezione partecipazione
                OrganizerFrame.IsVisible = true;       // Mostra sezione organizzatore

                Debug.WriteLine("[DEBUG] UI configurata per l'autore del post");
            }
            else
            {
                // L'utente NON è l'autore del post
                ParticipationFrame.IsVisible = true;   // Mostra sezione partecipazione
                OrganizerFrame.IsVisible = false;      // Nascondi sezione organizzatore

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

        private async Task LoadParticipantsCountAsync()
        {
            try
            {
                var availabilityResponse = await _sharedClient.GetAsync($"{_pythonApiBaseUrl}/posts/{_postId}/availability");

                if (availabilityResponse.IsSuccessStatusCode)
                {
                    var availabilityJson = await availabilityResponse.Content.ReadAsStringAsync();
                    var availabilityData = JsonSerializer.Deserialize<PostAvailabilityResponse>(availabilityJson,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _participantsCount = availabilityData.partecipanti_iscritti;
                    _postiDisponibili = availabilityData.posti_disponibili;
                    _isEventFull = availabilityData.is_full;

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
                    await LoadParticipantsCountAsync();
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

            // Aggiorna il label della sezione organizzatore se applicabile
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
        }

        // ========== NAVIGAZIONE ==========

        private async void OnNavigateToParticipantsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PostDetailParticipantsPage(_postId, _currentPost, _isPostAuthor));
        }

        // AGGIORNATO: Logica Chat corretta
        private async void OnNavigateToChatClicked(object sender, EventArgs e)
        {
            if (_currentPost == null)
            {
                await DisplayAlert("Errore", "Informazioni del post non disponibili", "OK");
                return;
            }

            try
            {
                if (_isPostAuthor)
                {
                    // SE SONO L'ORGANIZZATORE: non posso iniziare direttamente la chat
                    // Devo andare nella sezione partecipanti e scegliere con chi chattare
                    await DisplayAlert(
                        "Chat Organizzatore",
                        "Come organizzatore, puoi chattare con i partecipanti dalla sezione 'Partecipanti'. Clicca sull'icona chat accanto a ogni partecipante per iniziare una conversazione.",
                        "Vai ai Partecipanti",
                        "Annulla"
                    );

                    // Naviga automaticamente alla sezione partecipanti
                    await Navigation.PushAsync(new PostDetailParticipantsPage(_postId, _currentPost, _isPostAuthor, startWithChat: false));
                    return;
                }

                // SE SONO UN PARTECIPANTE: posso chattare direttamente con l'organizzatore
                if (!_isParticipant)
                {
                    var shouldJoin = await DisplayAlert(
                        "Iscriviti per chattare",
                        "Devi essere iscritto all'evento per poter chattare con l'organizzatore. Vuoi iscriverti ora?",
                        "Iscriviti",
                        "Annulla"
                    );

                    if (shouldJoin)
                    {
                        // Prova ad iscriversi automaticamente
                        await JoinEventAutomatically();
                        return;
                    }
                    else
                    {
                        return;
                    }
                }

                // Se arrivo qui, sono un partecipante iscritto che può chattare con l'organizzatore
                string recipientEmail = _currentPost.autore_email;
                var chatPage = new ChatPage(_currentPost, _currentUserEmail, recipientEmail, false);
                await Navigation.PushAsync(chatPage);

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore apertura chat: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire la chat. Riprova più tardi.", "OK");
            }
        }

        private async Task JoinEventAutomatically()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/events/join");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var payload = new ParticipationRequest { post_id = _postId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _isParticipant = true;
                    await LoadParticipantsCountAsync();

                    await DisplayAlert("Successo", "Ti sei iscritto all'evento! Ora puoi chattare con l'organizzatore.", "OK");

                    // Ora avvia la chat
                    string recipientEmail = _currentPost.autore_email;
                    var chatPage = new ChatPage(_currentPost, _currentUserEmail, recipientEmail, false);
                    await Navigation.PushAsync(chatPage);
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile iscriversi all'evento. Riprova più tardi.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore nell'iscrizione automatica: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'iscrizione all'evento.", "OK");
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

        // Implementazione INotifyPropertyChanged
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}