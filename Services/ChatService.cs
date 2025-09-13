// Services/ChatService.cs - VERSIONE CORRETTA
using Microsoft.Maui.Storage;
using SocketIOClient;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;

namespace trovagiocatoriApp.Services
{
    public class LiveChatMessage
    {
        public string Id { get; set; }
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

        // Dizionario per tenere traccia dei messaggi per ogni chat (basato su email)
        public Dictionary<string, ObservableCollection<LiveChatMessage>> ChatMessages { get; private set; }

        public ChatService()
        {
            _serverUrl = ApiConfig.PythonApiUrl;
            ChatMessages = new Dictionary<string, ObservableCollection<LiveChatMessage>>();
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                var sessionCookie = Preferences.Get("session_id", "");
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    Debug.WriteLine("[CHAT] Session cookie non trovato");
                    return false;
                }

                _currentUserEmail = await GetCurrentUserEmailAsync();
                if (string.IsNullOrEmpty(_currentUserEmail))
                {
                    Debug.WriteLine("[CHAT] Impossibile ottenere email utente");
                    return false;
                }

                _socket = new SocketIOClient.SocketIO(_serverUrl, new SocketIOOptions
                {
                    Path = "/ws/socket.io",
                    Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                    Auth = new Dictionary<string, string>
                    {
                        {"session_cookie", sessionCookie}
                    }
                });

                RegisterEventHandlers();
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

            // Conferma di connessione dal server
            _socket.On("connected", response =>
            {
                try
                {
                    var data = response.GetValue<JsonElement>();
                    var message = data.GetProperty("message").GetString();
                    Debug.WriteLine($"[CHAT] Conferma connessione: {message}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing connected: {ex.Message}");
                }
            });

            // Cronologia messaggi ricevuta
            _socket.On("chat_history", response =>
            {
                try
                {
                    var historyJson = response.GetValue<JsonElement>();
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
                        ChatHistoryReceived?.Invoke(historyMessages);
                    });

                    Debug.WriteLine($"[CHAT] Cronologia caricata: {historyMessages.Count} messaggi");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing cronologia: {ex.Message}");
                }
            });

            // CORREZIONE: TUTTI i messaggi vengono gestiti qui (sia nostri che ricevuti)
            _socket.On("new_private_message", response =>
            {
                try
                {
                    var messageJson = response.GetValue<JsonElement>();
                    var message = ParseChatMessage(messageJson);

                    // Determina se il messaggio è nostro o dell'altro utente
                    message.IsSentByMe = message.SenderEmail == _currentUserEmail;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        // Trova la chat room corretta
                        var chatKey = GetChatKey(message.SenderEmail, message.RecipientEmail);
                        if (!ChatMessages.ContainsKey(chatKey))
                        {
                            ChatMessages[chatKey] = new ObservableCollection<LiveChatMessage>();
                        }

                        // Aggiungi TUTTI i messaggi (sia nostri che ricevuti)
                        ChatMessages[chatKey].Add(message);

                        // Notifica la UI
                        NewMessageReceived?.Invoke(message);
                    });

                    Debug.WriteLine($"[CHAT] Nuovo messaggio: da {message.SenderEmail}, nostro={message.IsSentByMe}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing messaggio: {ex.Message}");
                }
            });

            // Conferma di invio messaggio
            _socket.On("message_sent", response =>
            {
                try
                {
                    var data = response.GetValue<JsonElement>();
                    var messageId = data.GetProperty("message_id").GetString();
                    var status = data.GetProperty("status").GetString();

                    Debug.WriteLine($"[CHAT] Messaggio {messageId} {status}");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT] Errore parsing message_sent: {ex.Message}");
                }
            });

            // Altri event handlers rimangono invariati...
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

        public async Task JoinChatAsync(string otherUserEmail)
        {
            if (!_isConnected)
            {
                Debug.WriteLine("[CHAT] Non connesso - impossibile joinare chat");
                return;
            }

            try
            {
                await _socket.EmitAsync("join_chat", new
                {
                    other_user_email = otherUserEmail
                });

                Debug.WriteLine($"[CHAT] Entrato nella chat con {otherUserEmail}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore join chat: {ex.Message}");
            }
        }

        // CORREZIONE: Invia messaggio e ASPETTA la conferma dal server
        public async Task SendMessageAsync(string recipientEmail, string message)
        {
            if (!_isConnected)
            {
                Debug.WriteLine("[CHAT] Non connesso - impossibile inviare messaggio");
                throw new InvalidOperationException("Non connesso al server chat");
            }

            if (string.IsNullOrWhiteSpace(message))
                return;

            try
            {
                Debug.WriteLine($"[CHAT] Invio messaggio a {recipientEmail}: {message}");

                // Invia al server - il messaggio tornerà tramite new_private_message
                await _socket.EmitAsync("send_private_message", new
                {
                    recipient_email = recipientEmail,
                    message = message.Trim()
                });

                Debug.WriteLine($"[CHAT] Messaggio inviato al server, in attesa echo...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore invio messaggio: {ex.Message}");
                throw;
            }
        }

        // Resto dei metodi rimangono invariati...
        public async Task NotifyTypingStartAsync(string recipientEmail)
        {
            if (!_isConnected) return;

            try
            {
                await _socket.EmitAsync("typing_start", new
                {
                    recipient_email = recipientEmail
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore typing_start: {ex.Message}");
            }
        }

        public async Task NotifyTypingStopAsync(string recipientEmail)
        {
            if (!_isConnected) return;

            try
            {
                await _socket.EmitAsync("typing_stop", new
                {
                    recipient_email = recipientEmail
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore typing_stop: {ex.Message}");
            }
        }

        public async Task LeaveChatAsync(string otherUserEmail)
        {
            if (!_isConnected) return;

            try
            {
                await _socket.EmitAsync("leave_chat", new
                {
                    other_user_email = otherUserEmail
                });

                Debug.WriteLine($"[CHAT] Uscito dalla chat con {otherUserEmail}");
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

        public ObservableCollection<LiveChatMessage> GetMessagesForChat(string otherUserEmail)
        {
            var chatKey = GetChatKey(_currentUserEmail, otherUserEmail);
            if (!ChatMessages.ContainsKey(chatKey))
            {
                ChatMessages[chatKey] = new ObservableCollection<LiveChatMessage>();
            }
            return ChatMessages[chatKey];
        }

        public bool IsConnected => _isConnected;

        // METODI HELPER PRIVATI
        private string GetChatKey(string email1, string email2)
        {
            var emails = new[] { email1, email2 }.OrderBy(e => e).ToArray();
            return $"{emails[0]}|{emails[1]}";
        }

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
                SenderEmail = messageJson.GetProperty("sender_email").GetString(),
                RecipientEmail = messageJson.GetProperty("recipient_email").GetString(),
                Content = messageJson.GetProperty("content").GetString(),
                Timestamp = DateTime.Parse(messageJson.GetProperty("timestamp").GetString()),
                Read = messageJson.GetProperty("read").GetBoolean()
            };
        }
    }
}