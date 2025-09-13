using Microsoft.Maui.Storage;
using SocketIOClient; // Assicurati che questa using sia presente
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace trovagiocatoriApp.Services
{
    // Rinominiamo la classe per evitare conflitti
    public class LiveChatMessage
    {
        public string Id { get; set; }
        public int PostId { get; set; }
        public string SenderEmail { get; set; }
        public string RecipientEmail { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public bool Read { get; set; }
        public bool IsSentByMe { get; set; }
    }

    public class ChatService
    {
        private SocketIOClient.SocketIO _socket;
        private readonly string _serverUrl;
        private string _currentUserEmail;
        private bool _isConnected = false;

        // Eventi per notificare la UI
        public event Action<LiveChatMessage> NewMessageReceived;
        public event Action<string> UserTyping;
        public event Action<string> UserStoppedTyping;
        public event Action<string> UserOnline;
        public event Action<string> UserOffline;
        public event Action<bool> ConnectionStatusChanged;
        public event Action<List<LiveChatMessage>> ChatHistoryReceived; 

        // Dizionario per tenere traccia dei messaggi per ogni post
        public Dictionary<int, ObservableCollection<LiveChatMessage>> PostMessages { get; private set; }

        public ChatService()
        {
            _serverUrl = ApiConfig.PythonApiUrl; // Usa lo stesso URL del backend Python
            PostMessages = new Dictionary<int, ObservableCollection<LiveChatMessage>>();
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                // Recupera il session cookie dalle preferences
                var sessionCookie = Preferences.Get("session_id", "");
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    Debug.WriteLine("[CHAT] Session cookie non trovato");
                    return false;
                }

                // Recupera l'email dell'utente corrente
                _currentUserEmail = await GetCurrentUserEmailAsync();
                if (string.IsNullOrEmpty(_currentUserEmail))
                {
                    Debug.WriteLine("[CHAT] Impossibile ottenere email utente");
                    return false;
                }

                // Configura Socket.IO
                _socket = new SocketIOClient.SocketIO(_serverUrl, new SocketIOOptions
                {
                    Path = "/ws/socket.io",
                    Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                    Auth = new Dictionary<string, string>
                    {
                        {"session_cookie", sessionCookie}
                    }
                });

                // Registra gli event handlers
                RegisterEventHandlers();

                // Connetti
                await _socket.ConnectAsync();

                Debug.WriteLine($"[CHAT] Connesso come {_currentUserEmail}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore connessione: {ex.Message}");
                return false;
            }
        }

        private void RegisterEventHandlers()
        {
            // Connessione stabilita
            _socket.OnConnected += (sender, e) =>
            {
                _isConnected = true;
                Debug.WriteLine("[CHAT] Socket connesso");
                MainThread.BeginInvokeOnMainThread(() => ConnectionStatusChanged?.Invoke(true));
            };

            // Disconnessione
            _socket.OnDisconnected += (sender, e) =>
            {
                _isConnected = false;
                Debug.WriteLine("[CHAT] Socket disconnesso");
                MainThread.BeginInvokeOnMainThread(() => ConnectionStatusChanged?.Invoke(false));
            };

            // Cronologia messaggi ricevuta
            _socket.On("chat_history", response =>
            {
                try
                {
                    var historyJson = response.GetValue<JsonElement>();
                    var postId = historyJson.GetProperty("post_id").GetInt32();
                    var messagesArray = historyJson.GetProperty("messages").EnumerateArray();

                    var historyMessages = new List<LiveChatMessage>();
                    foreach (var messageElement in messagesArray)
                    {
                        var message = ParseChatMessage(messageElement);
                        message.IsSentByMe = message.SenderEmail == _currentUserEmail;
                        historyMessages.Add(message);
                    }

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Inizializza la collezione se non esiste
                        if (!PostMessages.ContainsKey(postId))
                        {
                            PostMessages[postId] = new ObservableCollection<LiveChatMessage>();
                        }

                        // Aggiungi i messaggi alla collezione
                        var collection = PostMessages[postId];
                        collection.Clear(); // Pulisci eventuali messaggi esistenti

                        foreach (var msg in historyMessages.OrderBy(m => m.Timestamp))
                        {
                            collection.Add(msg);
                        }

                        ChatHistoryReceived?.Invoke(historyMessages);
                    });

                    Debug.WriteLine($"[CHAT] Cronologia caricata: {historyMessages.Count} messaggi per post {postId}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing cronologia: {ex.Message}");
                }
            });

            // messaggio privato ricevuto (SOLO per messaggi degli altri)
            _socket.On("new_private_message", response =>
            {
                try
                {
                    var messageJson = response.GetValue<JsonElement>();
                    var message = ParseChatMessage(messageJson);

                    // IMPORTANTE: Questo evento arriva SOLO per messaggi degli altri utenti
                    message.IsSentByMe = false;

                    // Aggiungi alla collezione appropriata
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!PostMessages.ContainsKey(message.PostId))
                        {
                            PostMessages[message.PostId] = new ObservableCollection<LiveChatMessage>();
                        }
                        PostMessages[message.PostId].Add(message);

                        NewMessageReceived?.Invoke(message);
                    });

                    Debug.WriteLine($"[CHAT] Nuovo messaggio ricevuto da {message.SenderEmail}: {message.Content}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing messaggio: {ex.Message}");
                }
            });

            // Conferma di invio messaggio (per i nostri messaggi)
            _socket.On("message_sent", response =>
            {
                try
                {
                    var data = response.GetValue<JsonElement>();
                    var messageId = data.GetProperty("message_id").GetString();
                    var status = data.GetProperty("status").GetString();

                    Debug.WriteLine($"[CHAT] Messaggio {messageId} {status}");
                    // Qui potresti aggiornare l'UI per mostrare lo stato del messaggio
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing message_sent: {ex.Message}");
                }
            });

            // Notifica che un utente sta scrivendo
            _socket.On("user_typing", response =>
            {
                try
                {
                    var data = response.GetValue<JsonElement>();
                    var userEmail = data.GetProperty("user_email").GetString();
                    var isTyping = data.GetProperty("typing").GetBoolean();

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (isTyping)
                            UserTyping?.Invoke(userEmail);
                        else
                            UserStoppedTyping?.Invoke(userEmail);
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing typing: {ex.Message}");
                }
            });

            // Utente online
            _socket.On("user_online", response =>
            {
                try
                {
                    var data = response.GetValue<JsonElement>();
                    var userEmail = data.GetProperty("user_email").GetString();
                    MainThread.BeginInvokeOnMainThread(() => UserOnline?.Invoke(userEmail));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing user_online: {ex.Message}");
                }
            });

            // Utente offline
            _socket.On("user_offline", response =>
            {
                try
                {
                    var data = response.GetValue<JsonElement>();
                    var userEmail = data.GetProperty("user_email").GetString();
                    MainThread.BeginInvokeOnMainThread(() => UserOffline?.Invoke(userEmail));
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing user_offline: {ex.Message}");
                }
            });

            // Errori
            _socket.On("error", response =>
            {
                try
                {
                    var error = response.GetValue<JsonElement>();
                    var message = error.GetProperty("message").GetString();
                    Debug.WriteLine($"[CHAT] Errore dal server: {message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing errore: {ex.Message}");
                }
            });
        }

        public async Task JoinPostChatAsync(int postId, string postAuthorEmail)
        {
            if (!_isConnected)
            {
                Debug.WriteLine("[CHAT] Non connesso - impossibile joinare chat");
                return;
            }

            try
            {
                await _socket.EmitAsync("join_post_chat", new
                {
                    post_id = postId,
                    post_author_email = postAuthorEmail
                });

                Debug.WriteLine($"[CHAT] Entrato nella chat del post {postId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore join chat: {ex.Message}");
            }
        }

        // Metodo per inviare messaggi che aggiunge immediatamente alla UI
        public async Task SendMessageAsync(int postId, string recipientEmail, string message)
        {
            if (!_isConnected)
            {
                Debug.WriteLine("[CHAT] Non connesso - impossibile inviare messaggio");
                return;
            }

            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                // PRIMA aggiungi il messaggio alla UI locale
                var localMessage = new LiveChatMessage
                {
                    Id = Guid.NewGuid().ToString(), // ID temporaneo
                    PostId = postId,
                    SenderEmail = _currentUserEmail,
                    RecipientEmail = recipientEmail,
                    Content = message.Trim(),
                    Timestamp = DateTime.Now,
                    IsSentByMe = true,
                    Read = false
                };

                // Aggiungi alla collezione
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (!PostMessages.ContainsKey(postId))
                    {
                        PostMessages[postId] = new ObservableCollection<LiveChatMessage>();
                    }
                    PostMessages[postId].Add(localMessage);
                });

                // POI invia al server
                await _socket.EmitAsync("send_private_message", new
                {
                    post_id = postId,
                    recipient_email = recipientEmail,
                    message = message.Trim()
                });

                Debug.WriteLine($"[CHAT] Messaggio inviato a {recipientEmail}: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore invio messaggio: {ex.Message}");

                // In caso di errore, potresti rimuovere il messaggio dalla UI
                // o mostrare un indicatore di errore
            }
        }

        public async Task NotifyTypingStartAsync(int postId, string recipientEmail)
        {
            if (!_isConnected) return;

            try
            {
                await _socket.EmitAsync("typing_start", new
                {
                    post_id = postId,
                    recipient_email = recipientEmail
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore typing_start: {ex.Message}");
            }
        }

        public async Task NotifyTypingStopAsync(int postId, string recipientEmail)
        {
            if (!_isConnected) return;

            try
            {
                await _socket.EmitAsync("typing_stop", new
                {
                    post_id = postId,
                    recipient_email = recipientEmail
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore typing_stop: {ex.Message}");
            }
        }

        public async Task LeavePostChatAsync(int postId, string recipientEmail)
        {
            if (!_isConnected) return;

            try
            {
                await _socket.EmitAsync("leave_post_chat", new
                {
                    post_id = postId,
                    recipient_email = recipientEmail
                });

                Debug.WriteLine($"[CHAT] Uscito dalla chat del post {postId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore leave chat: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_socket != null && _isConnected)
            {
                await _socket.DisconnectAsync();
                _socket.Dispose();
                _socket = null;
                _isConnected = false;
                Debug.WriteLine("[CHAT] Socket disconnesso manualmente");
            }
        }

        public ObservableCollection<LiveChatMessage> GetMessagesForPost(int postId)
        {
            if (!PostMessages.ContainsKey(postId))
            {
                PostMessages[postId] = new ObservableCollection<LiveChatMessage>();
            }
            return PostMessages[postId];
        }

        public bool IsConnected => _isConnected;

        private async Task<string> GetCurrentUserEmailAsync()
        {
            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/api/user");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var userData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                    if (userData.ContainsKey("email"))
                    {
                        return userData["email"].ToString();
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore nel recupero email utente: {ex.Message}");
                return null;
            }
        }

        private LiveChatMessage ParseChatMessage(JsonElement messageJson)
        {
            return new LiveChatMessage
            {
                Id = messageJson.GetProperty("id").GetString(),
                PostId = messageJson.GetProperty("post_id").GetInt32(),
                SenderEmail = messageJson.GetProperty("sender_email").GetString(),
                RecipientEmail = messageJson.GetProperty("recipient_email").GetString(),
                Content = messageJson.GetProperty("content").GetString(),
                Timestamp = DateTime.Parse(messageJson.GetProperty("timestamp").GetString()),
                Read = messageJson.GetProperty("read").GetBoolean()
            };
        }
    }
}