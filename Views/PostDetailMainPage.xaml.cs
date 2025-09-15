using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using trovagiocatoriApp.Models;
using System.Globalization;

namespace trovagiocatoriApp.Views
{
    public partial class PostDetailMainPage : ContentPage, INotifyPropertyChanged
    {
        private readonly int _postId;
        private static readonly HttpClient _sharedClient = CreateHttpClient();

        // Stato del post
        private PostResponse _currentPost;
        private SportField _campo;
        private bool _isFavorite = false;
        private bool _isParticipant = false;
        private int _participantsCount = 0;
        private int _postiDisponibili = 0;
        private bool _isEventFull = false;

        // Dati utenti
        private string _postAuthorEmail = "";
        private string _currentUserEmail = "";
        private bool _isPostAuthor = false;
        private bool _isAuthorFriend = false;

        // Proprietà per binding
        public SportField Campo
        {
            get => _campo;
            set { _campo = value; OnPropertyChanged(); }
        }

        public bool IsPostAuthor => _isPostAuthor;

        public PostDetailMainPage(int postId)
        {
            InitializeComponent();
            _postId = postId;
            BindingContext = this;
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient(new HttpClientHandler { UseCookies = true });
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCurrentUserEmailAsync();
            await LoadPostDetailAsync();
            await CheckFavoriteStatusAsync();
            await CheckParticipationStatusAsync();
            await LoadParticipantsCountAsync();
            await CheckFriendshipStatusAsync();
        }

        // Carica l'email dell'utente corrente per identificazione
        private async Task LoadCurrentUserEmailAsync()
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
                        _currentUserEmail = userData["email"].ToString();
                        Debug.WriteLine($"[DEBUG] Email utente corrente: {_currentUserEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore caricamento email utente: {ex.Message}");
            }
        }

        // Carica tutti i dettagli del post e configura l'UI
        private async Task LoadPostDetailAsync()
        {
            try
            {
                _currentPost = await LoadPostDataAsync();
                _postAuthorEmail = _currentPost.autore_email;
                _isPostAuthor = !string.IsNullOrEmpty(_currentUserEmail) &&
                                _currentUserEmail.Equals(_postAuthorEmail, StringComparison.OrdinalIgnoreCase);

                UpdateUIBasedOnAuthorStatus();

                var user = await LoadUserDataAsync(_currentPost.autore_email);

                if (_currentPost.campo_id.HasValue)
                {
                    Campo = await LoadSportFieldAsync(_currentPost.campo_id.Value);
                }

                UpdateUI(_currentPost, user);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", ex.Message, "OK");
            }
        }

        // Carica i dati del post dal server
        private async Task<PostResponse> LoadPostDataAsync()
        {
            var response = await _sharedClient.GetAsync($"{ApiConfig.PythonApiUrl}/posts/{_postId}");
            if (!response.IsSuccessStatusCode)
                throw new Exception("Post non trovato");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<PostResponse>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // Carica i dati dell'utente autore del post
        private async Task<User> LoadUserDataAsync(string email)
        {
            var encodedEmail = Uri.EscapeDataString(email);
            var response = await _sharedClient.GetAsync(
                $"{ApiConfig.BaseUrl}/api/user/by-email?email={encodedEmail}");

            if (!response.IsSuccessStatusCode)
                throw new Exception("Utente non trovato");

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<User>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // Carica i dettagli del campo sportivo se presente
        private async Task<SportField> LoadSportFieldAsync(int fieldId)
        {
            try
            {
                var response = await _sharedClient.GetAsync($"{ApiConfig.PythonApiUrl}/fields/{fieldId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<SportField>(
                        json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore caricamento campo: {ex.Message}");
            }
            return null;
        }

        // Configura l'UI in base al ruolo dell'utente (autore vs partecipante)
        private void UpdateUIBasedOnAuthorStatus()
        {
            if (_isPostAuthor)
            {
                ParticipationFrame.IsVisible = false;
                OrganizerFrame.IsVisible = true;
                Debug.WriteLine("[DEBUG] UI configurata per autore post");
            }
            else
            {
                ParticipationFrame.IsVisible = true;
                OrganizerFrame.IsVisible = false;
                Debug.WriteLine("[DEBUG] UI configurata per partecipante");
            }
        }

        // Aggiorna tutti gli elementi dell'interfaccia con i dati caricati
        private void UpdateUI(PostResponse post, User user)
        {
            // Informazioni utente
            AutoreLabel.Text = GetUserDisplayName(user);
            AutoreUsernameLabel.Text = GetUsernameDisplay(user);
            ProfileImage.Source = !string.IsNullOrEmpty(user.ProfilePic)
                ? $"{ApiConfig.BaseUrl}/images/{user.ProfilePic}"
                : "default_images.jpg";

            // Informazioni post
            TitoloLabel.Text = post.titolo;
            DataOraLabel.Text = $"{post.data_partita} alle {post.ora_partita.Substring(0, 5)}";
            LocalitaLabel.Text = $"{post.citta}, {post.provincia}";
            SportLabel.Text = post.sport;
            CommentoLabel.Text = post.commento;

            // Numero giocatori
            NumeroGiocatoriLabel.Text = post.numero_giocatori == 1
                ? "Cerco 1 giocatore"
                : $"Cerco {post.numero_giocatori} giocatori";

            // Livello con icone colorate
            SetLevelDisplay(post.livello);

            // Informazioni campo se presente
            if (Campo != null)
            {
                CampoNomeLabel.Text = Campo.Nome;
                CampoIndirizzoLabel.Text = Campo.Indirizzo;
                CampoTipoLabel.Text = Campo.Tipo ?? "Non specificato";
                CampoDescrizioneLabel.Text = Campo.Descrizione ?? "Nessuna descrizione disponibile";
            }
        }

        // Imposta la visualizzazione del livello con colori appropriati
        private void SetLevelDisplay(string livello)
        {
            var (text, color) = livello switch
            {
                "Principiante" => ("🟢 Principiante", Colors.Green),
                "Intermedio" => ("🟡 Intermedio", Colors.Orange),
                "Avanzato" => ("🔴 Avanzato", Colors.Red),
                _ => ("🟡 Intermedio", Colors.Orange)
            };

            LivelloLabel.Text = text;
            LivelloLabel.TextColor = color;
        }

        // Genera il nome visualizzato per l'utente con priorità: Nome Cognome > Nome > Username > Email
        private string GetUserDisplayName(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Nome) && !string.IsNullOrWhiteSpace(user.Cognome))
                return $"{user.Nome} {user.Cognome}";
            if (!string.IsNullOrWhiteSpace(user.Nome))
                return user.Nome;
            if (!string.IsNullOrWhiteSpace(user.Username))
                return user.Username;
            if (!string.IsNullOrWhiteSpace(user.Email))
                return user.Email.Split('@')[0];
            return "Utente sconosciuto";
        }

        // Genera il display username con @ prefix
        private string GetUsernameDisplay(User user)
        {
            if (!string.IsNullOrWhiteSpace(user.Username))
                return $"@{user.Username}";
            if (!string.IsNullOrWhiteSpace(user.Email))
                return $"@{user.Email.Split('@')[0]}";
            return "@organizzatore";
        }

        // ========== GESTIONE PREFERITI ==========

        // Verifica se il post è nei preferiti dell'utente
        private async Task CheckFavoriteStatusAsync()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/favorites/check/{_postId}");
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
                Debug.WriteLine($"Errore controllo preferiti: {ex.Message}");
            }
        }

        // Aggiunge o rimuove il post dai preferiti
        private async void OnFavoriteButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var endpoint = _isFavorite ? "/favorites/remove" : "/favorites/add";
                var request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint);

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

        // Aggiorna l'icona dei preferiti
        private void UpdateFavoriteIcon()
        {
            FavoriteButton.Source = _isFavorite ? "heart_filled.png" : "heart_empty.png";
        }

        // ========== GESTIONE PARTECIPAZIONE ==========

        // Verifica se l'utente partecipa già all'evento
        private async Task CheckParticipationStatusAsync()
        {
            if (_isPostAuthor) return;

            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"/events/check/{_postId}");
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
                Debug.WriteLine($"Errore controllo partecipazione: {ex.Message}");
            }
        }

        // Carica il numero di partecipanti e posti disponibili
        private async Task LoadParticipantsCountAsync()
        {
            try
            {
                var response = await _sharedClient.GetAsync($"{ApiConfig.PythonApiUrl}/posts/{_postId}/availability");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var data = JsonSerializer.Deserialize<PostAvailabilityResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _participantsCount = data.partecipanti_iscritti;
                    _postiDisponibili = data.posti_disponibili;
                    _isEventFull = data.is_full;

                    UpdateParticipationUI();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore caricamento partecipanti: {ex.Message}");
            }
        }

        // Gestisce l'iscrizione/disiscrizione dall'evento
        private async void OnJoinLeaveEventClicked(object sender, EventArgs e)
        {
            if (_isPostAuthor)
            {
                await DisplayAlert("Informazione", "Non puoi partecipare al tuo stesso evento!", "OK");
                return;
            }

            try
            {
                var endpoint = _isParticipant ? "/events/leave" : "/events/join";
                var request = CreateAuthenticatedRequest(HttpMethod.Post, endpoint);

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

        // Aggiorna l'UI della partecipazione con contatori e stati
        private void UpdateParticipationUI()
        {
            if (_currentPost == null) return;

            // Aggiorna contatori partecipanti
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

            // Aggiorna UI organizzatore
            if (_isPostAuthor && OrganizerPostiLabel != null)
            {
                OrganizerPostiLabel.Text = _isEventFull
                    ? "Evento completo! 🎉"
                    : $"{_postiDisponibili} posti disponibili su {_currentPost.numero_giocatori}";
                OrganizerPostiLabel.TextColor = _isEventFull ? Colors.Green : Colors.Orange;
            }

            // Aggiorna UI partecipante
            if (!_isPostAuthor)
            {
                UpdateParticipantUI();
            }
        }

        // Aggiorna l'UI specifica per i partecipanti
        private void UpdateParticipantUI()
        {
            if (PostiDisponibiliLabel != null)
            {
                PostiDisponibiliLabel.Text = _isEventFull
                    ? "Evento completo"
                    : $"{_postiDisponibili} posti disponibili su {_currentPost.numero_giocatori}";
                PostiDisponibiliLabel.TextColor = _isEventFull ? Colors.Red : Colors.Green;
            }

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

        // ========== GESTIONE AMICIZIE ==========

        // Verifica se l'autore del post è un amico
        private async Task CheckFriendshipStatusAsync()
        {
            if (string.IsNullOrEmpty(_postAuthorEmail) || _isPostAuthor) return;

            try
            {
                _isAuthorFriend = await CheckIfUserIsFriendAsync(_postAuthorEmail);
                UpdateFriendshipUI();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FRIENDS] Errore controllo amicizia: {ex.Message}");
                _isAuthorFriend = false;
                UpdateFriendshipUI();
            }
        }

        // Controlla se un utente è amico tramite API
        private async Task<bool> CheckIfUserIsFriendAsync(string userEmail)
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get,
                    $"/friends/check?email={Uri.EscapeDataString(userEmail)}");
                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (result.ContainsKey("is_friend") && result["is_friend"] is JsonElement element)
                    {
                        return element.GetBoolean();
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FRIENDS] Errore verifica amicizia: {ex.Message}");
                return false;
            }
        }

        // Aggiorna l'UI in base allo stato dell'amicizia
        private void UpdateFriendshipUI()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (FriendBadge != null)
                {
                    FriendBadge.IsVisible = _isAuthorFriend;
                }

                if (FriendsButton != null)
                {
                    FriendsButton.BackgroundColor = _isAuthorFriend
                        ? Color.FromArgb("#4CAF50")  // Verde per amico
                        : _isPostAuthor
                            ? Color.FromArgb("#FF9800")  // Arancione per proprio post
                            : Color.FromArgb("#2196F3"); // Blu per non amico
                }
            });
        }

        // ========== NAVIGAZIONE ==========

        // Naviga alla pagina dei partecipanti
        private async void OnNavigateToParticipantsClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new PostDetailParticipantsPage(_postId, _currentPost, _isPostAuthor));
        }

        // Gestisce l'apertura della chat
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
                    await HandleOrganizerChat();
                }
                else if (!_isParticipant)
                {
                    await HandleNonParticipantChat();
                }
                else
                {
                    await StartDirectChat();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore apertura chat: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile aprire la chat. Riprova più tardi.", "OK");
            }
        }

        // Gestisce la chat per l'organizzatore
        private async Task HandleOrganizerChat()
        {
            await DisplayAlert(
                "Chat Organizzatore",
                "Come organizzatore, puoi chattare con i partecipanti dalla sezione 'Partecipanti'. Clicca sull'icona chat accanto a ogni partecipante per iniziare una conversazione.",
                "Vai ai Partecipanti",
                "Annulla"
            );

            await Navigation.PushAsync(new PostDetailParticipantsPage(_postId, _currentPost, _isPostAuthor, startWithChat: false));
        }

        // Gestisce la chat per non partecipanti
        private async Task HandleNonParticipantChat()
        {
            var shouldJoin = await DisplayAlert(
                "Iscriviti per chattare",
                "Devi essere iscritto all'evento per poter chattare con l'organizzatore. Vuoi iscriverti ora?",
                "Iscriviti",
                "Annulla"
            );

            if (shouldJoin)
            {
                await JoinEventAutomatically();
            }
        }

        // Avvia una chat diretta con l'organizzatore
        private async Task StartDirectChat()
        {
            var chatPage = new ChatPage(_currentPost, _currentUserEmail, _currentPost.autore_email, false);
            await Navigation.PushAsync(chatPage);
        }

        // Iscrive automaticamente l'utente all'evento e avvia la chat
        private async Task JoinEventAutomatically()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Post, "/events/join");
                var payload = new ParticipationRequest { post_id = _postId };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _isParticipant = true;
                    await LoadParticipantsCountAsync();
                    await DisplayAlert("Successo", "Ti sei iscritto all'evento! Ora puoi chattare con l'organizzatore.", "OK");
                    await StartDirectChat();
                }
                else
                {
                    await DisplayAlert("Errore", "Impossibile iscriversi all'evento. Riprova più tardi.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore iscrizione automatica: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'iscrizione all'evento.", "OK");
            }
        }

        // Apre la mappa con la posizione del campo
        private async void OnViewOnMapClicked(object sender, EventArgs e)
        {
            if (Campo == null)
            {
                await DisplayAlert("Info", "Coordinate o indirizzo del campo non disponibili.", "OK");
                return;
            }

            try
            {
                string latStr = Campo.Lat.ToString(CultureInfo.InvariantCulture);
                string lngStr = Campo.Lng.ToString(CultureInfo.InvariantCulture);
                string uriString = $"https://www.google.com/maps/search/?api=1&query={latStr},{lngStr}";

                await Launcher.OpenAsync(new Uri(uriString));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Errore apertura mappa: {ex.Message}");
                await DisplayAlert("Errore", $"Impossibile aprire la mappa: {ex.Message}", "OK");
            }
        }

        // ========== GESTIONE AMICI ==========

        // Gestisce il click sul pulsante amici
        private async void OnFriendsButtonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_postAuthorEmail))
            {
                await DisplayAlert("Errore", "Informazioni autore non disponibili", "OK");
                return;
            }

            try
            {
                if (_isPostAuthor)
                {
                    await ShowOwnPostFriendsOptions();
                }
                else if (_isAuthorFriend)
                {
                    await ShowFriendOptions();
                }
                else
                {
                    await ShowAddFriendOptions();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FRIENDS] Errore gestione click amici: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'apertura opzioni amici", "OK");
            }
        }

        // Mostra opzioni per il proprio post
        private async Task ShowOwnPostFriendsOptions()
        {
            var action = await DisplayActionSheet(
                "Gestisci Amici per questo Evento",
                "Annulla",
                null,
                "👥 Invita Amici all'Evento"
            );

            if (action == "👥 Invita Amici all'Evento")
            {
                await InviteFriendsToEvent();
            }
        }

        // Mostra opzioni per un amico
        private async Task ShowFriendOptions()
        {
            var action = await DisplayActionSheet(
                "Opzioni Amico",
                "Annulla",
                null,
                "❌ Rimuovi amicizia"
            );

            if (action == "❌ Rimuovi amicizia")
            {
                await RemoveFriend();
            }
        }

        // Mostra opzioni per aggiungere come amico
        private async Task ShowAddFriendOptions()
        {
            var action = await DisplayActionSheet(
                "Opzioni Utente",
                "Annulla",
                null,
                "👥 Aggiungi come Amico"
            );

            if (action == "👥 Aggiungi come Amico")
            {
                await SendFriendRequest();
            }
        }

        // Invia una richiesta di amicizia
        private async Task SendFriendRequest()
        {
            var confirm = await DisplayAlert(
                "Richiesta di Amicizia",
                "Vuoi inviare una richiesta di amicizia all'organizzatore?",
                "Invia",
                "Annulla"
            );

            if (!confirm) return;

            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Post, "/friends/request");
                var payload = new { target_email = _postAuthorEmail };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await DisplayAlert("Successo", "Richiesta di amicizia inviata!", "OK");
                    Debug.WriteLine($"[FRIENDS] ✅ Richiesta amicizia inviata a {_postAuthorEmail}");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Errore", $"Impossibile inviare la richiesta: {errorContent}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FRIENDS] Errore invio richiesta amicizia: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'invio della richiesta", "OK");
            }
        }

        // Rimuove un amico
        private async Task RemoveFriend()
        {
            var confirm = await DisplayAlert(
                "Rimuovi Amicizia",
                "Sei sicuro di voler rimuovere questa persona dai tuoi amici?",
                "Rimuovi",
                "Annulla"
            );

            if (!confirm) return;

            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Delete, "/friends/remove");
                var payload = new { target_email = _postAuthorEmail };
                var json = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _sharedClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _isAuthorFriend = false;
                    UpdateFriendshipUI();
                    await DisplayAlert("Amicizia Rimossa", "L'amicizia è stata rimossa con successo", "OK");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Errore", $"Impossibile rimuovere l'amicizia: {errorContent}", "OK");
                }

                Debug.WriteLine($"[FRIENDS] Amicizia rimossa con {_postAuthorEmail}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[FRIENDS] Errore rimozione amicizia: {ex.Message}");
                await DisplayAlert("Errore", "Errore nella rimozione dell'amicizia", "OK");
            }
        }

        // Invita amici all'evento
        private async Task InviteFriendsToEvent()
        {
            try
            {
                var inviteFriendsPage = new InviteFriendsPage(_postId, _currentPost, new List<FriendInfo>());
                await Navigation.PushAsync(inviteFriendsPage);
                Debug.WriteLine($"[INVITE] Apertura pagina inviti per evento {_postId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[INVITE] Errore invito amici: {ex.Message}");
                await DisplayAlert("Errore", "Errore nell'apertura inviti", "OK");
            }
        }

        // ========== HELPER METHODS ==========

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

        // Implementazione INotifyPropertyChanged
        public new event PropertyChangedEventHandler PropertyChanged;

        protected new void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}