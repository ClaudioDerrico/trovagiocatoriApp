using Microsoft.Maui.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using trovagiocatoriApp.Models;
using trovagiocatoriApp.Services;

namespace trovagiocatoriApp.Views
{
    public partial class ChatPage : ContentPage
    {
        private readonly ChatService _chatService;
        private readonly PostResponse _post;
        private readonly string _currentUserEmail;
        private readonly string _recipientEmail;
        private readonly bool _isPostAuthor;
        private readonly ObservableCollection<LiveChatMessage> _messages;

        private System.Timers.Timer _typingTimer;
        private bool _isTyping = false;

        public ChatPage(PostResponse post, string currentUserEmail, string recipientEmail, bool isPostAuthor)
        {
            InitializeComponent();

            _post = post;
            _currentUserEmail = currentUserEmail;
            _recipientEmail = recipientEmail;
            _isPostAuthor = isPostAuthor;

            _chatService = new ChatService();
            _messages = new ObservableCollection<LiveChatMessage>();

            InitializeUI();
            InitializeChatService();
        }

        private void InitializeUI()
        {
            PostTitleLabel.Text = _post.titolo;

            if (_isPostAuthor)
            {
                ChatWithLabel.Text = "Chat con partecipante";
            }
            else
            {
                ChatWithLabel.Text = "Chat con organizzatore";
            }

            UpdateConnectionStatus(false);

            // Timer per gestire il typing indicator
            _typingTimer = new System.Timers.Timer(3000); // 3 secondi
            _typingTimer.Elapsed += OnTypingTimerElapsed;
        }

        private async void InitializeChatService()
        {
            // Registra gli event handlers
            _chatService.NewMessageReceived += OnNewMessageReceived;
            _chatService.UserTyping += OnUserTyping;
            _chatService.UserStoppedTyping += OnUserStoppedTyping;
            _chatService.ConnectionStatusChanged += OnConnectionStatusChanged;
            _chatService.ChatHistoryReceived += OnChatHistoryReceived;

            // Connetti al server
            var connected = await _chatService.ConnectAsync();
            if (connected)
            {
                //  Entra nella chat con l'altro utente
                await _chatService.JoinChatAsync(_recipientEmail);

                // Ottieni i messaggi esistenti per questa chat
                var existingMessages = _chatService.GetMessagesForChat(_recipientEmail);

                // Se non ci sono messaggi in memoria, prova a caricare la cronologia
                if (existingMessages.Count == 0)
                {
                    Debug.WriteLine("[CHAT UI] Nessun messaggio in cache, in attesa cronologia dal server...");
                }
                else
                {
                    // Mostra i messaggi già in memoria
                    LoadMessagesFromCollection(existingMessages);
                }
            }
            else
            {
                await DisplayAlert("Errore", "Impossibile connettersi al server chat", "OK");
            }
        }

        private void LoadMessagesFromCollection(ObservableCollection<LiveChatMessage> messages)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesContainer.Clear();

                foreach (var message in messages.OrderBy(m => m.Timestamp))
                {
                    AddMessageToUI(message);
                }

                ScrollToBottom();
                Debug.WriteLine($"[CHAT UI] Caricati {messages.Count} messaggi dalla collezione");
            });
        }

        private void OnChatHistoryReceived(List<LiveChatMessage> historyMessages)
        {
            // Filtra solo i messaggi di questa chat
            var relevantMessages = historyMessages.Where(m =>
                (m.SenderEmail == _currentUserEmail && m.RecipientEmail == _recipientEmail) ||
                (m.SenderEmail == _recipientEmail && m.RecipientEmail == _currentUserEmail)
            ).ToList();

            if (relevantMessages.Count == 0)
            {
                Debug.WriteLine("[CHAT UI] Nessun messaggio storico per questa chat");
                // Assicurati che il placeholder sia visibile
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    EmptyStateContainer.IsVisible = true;
                });
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesContainer.Clear();
                EmptyStateContainer.IsVisible = false; // Nascondi il placeholder

                foreach (var message in relevantMessages.OrderBy(m => m.Timestamp))
                {
                    AddMessageToUI(message);

                    // Aggiungi anche alla collezione locale se non c'è già
                    var chatMessages = _chatService.GetMessagesForChat(_recipientEmail);
                    if (!chatMessages.Any(m => m.Id == message.Id))
                    {
                        chatMessages.Add(message);
                    }
                }

                ScrollToBottom();
                Debug.WriteLine($"[CHAT UI] Cronologia caricata: {relevantMessages.Count} messaggi");
            });
        }

        private void OnNewMessageReceived(LiveChatMessage message)
        {
            // Verifica che il messaggio sia per questa chat
            if (message.SenderEmail != _recipientEmail && message.RecipientEmail != _recipientEmail)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddMessageToUI(message);
                ScrollToBottom();
                Debug.WriteLine($"[CHAT UI] Nuovo messaggio ricevuto da {message.SenderEmail}");
            });
        }

        private void OnUserTyping(string userEmail)
        {
            if (userEmail == _recipientEmail)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TypingIndicator.IsVisible = true;
                    Debug.WriteLine($"[CHAT UI] {userEmail} sta scrivendo...");
                });
            }
        }

        private void OnUserStoppedTyping(string userEmail)
        {
            if (userEmail == _recipientEmail)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TypingIndicator.IsVisible = false;
                    Debug.WriteLine($"[CHAT UI] {userEmail} ha smesso di scrivere");
                });
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateConnectionStatus(isConnected);
                CheckSendButtonState();
            });
        }

        private void UpdateConnectionStatus(bool isConnected)
        {
            if (isConnected)
            {
                ConnectionStatusFrame.BackgroundColor = Color.FromArgb("#10B981"); // Verde
                ConnectionStatusLabel.Text = "Online";
            }
            else
            {
                ConnectionStatusFrame.BackgroundColor = Color.FromArgb("#EF4444"); // Rosso
                ConnectionStatusLabel.Text = "Offline";
            }
        }

        private void AddMessageToUI(LiveChatMessage message)
        {
            var messageFrame = new Frame();
            var messageLabel = new Label
            {
                Text = message.Content,
                LineBreakMode = LineBreakMode.WordWrap
            };

            // Timestamp
            var timestampLabel = new Label
            {
                Text = message.Timestamp.ToString("HH:mm"),
                FontSize = 12,
                Opacity = 0.7,
                HorizontalOptions = LayoutOptions.End
            };

            var messageContent = new StackLayout
            {
                Spacing = 4,
                Children = { messageLabel, timestampLabel }
            };

            messageFrame.Content = messageContent;

            // Applica lo stile in base al mittente
            if (message.IsSentByMe)
            {
                messageFrame.Style = (Style)Resources["SentMessageStyle"];
                messageLabel.Style = (Style)Resources["SentMessageTextStyle"];
                timestampLabel.TextColor = Colors.White;
                timestampLabel.Opacity = 0.8;
            }
            else
            {
                messageFrame.Style = (Style)Resources["ReceivedMessageStyle"];
                messageLabel.Style = (Style)Resources["ReceivedMessageTextStyle"];
                timestampLabel.TextColor = Colors.Gray;
            }

            MessagesContainer.Children.Add(messageFrame);
        }

        private async void OnSendMessageClicked(object sender, EventArgs e)
        {
            var message = MessageEntry.Text?.Trim();
            if (string.IsNullOrEmpty(message))
                return;

            Debug.WriteLine($"[CHAT UI] Invio messaggio: {message}");

            // Pulisci il campo di input immediatamente
            var messageToSend = message;
            MessageEntry.Text = "";

            // Disabilita il pulsante temporaneamente
            SendButton.IsEnabled = false;

            try
            {
                // Ferma il typing indicator prima di inviare
                if (_isTyping)
                {
                    await _chatService.NotifyTypingStopAsync(_recipientEmail);
                    _isTyping = false;
                }

                
                // Il messaggio arriverà dal server tramite new_private_message
                await _chatService.SendMessageAsync(_recipientEmail, messageToSend);

                Debug.WriteLine($"[CHAT UI] Messaggio inviato al server, in attesa di ricezione...");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT UI] Errore invio messaggio: {ex.Message}");

                // In caso di errore, ripristina il messaggio nel campo input
                MessageEntry.Text = messageToSend;
                await DisplayAlert("Errore", "Impossibile inviare il messaggio", "OK");
            }
            finally
            {
                // Riabilita il pulsante
                CheckSendButtonState();
            }
        }

        private async void OnMessageTextChanged(object sender, TextChangedEventArgs e)
        {
            var hasText = !string.IsNullOrWhiteSpace(e.NewTextValue);

            // Aggiorna il pulsante di invio
            CheckSendButtonState();

            // Gestisci il typing indicator
            if (hasText && !_isTyping)
            {
                _isTyping = true;
                await _chatService.NotifyTypingStartAsync(_recipientEmail);
            }

            // Reset del timer typing
            if (hasText)
            {
                _typingTimer.Stop();
                _typingTimer.Start();
            }
            else if (_isTyping)
            {
                // Se il testo è vuoto, ferma subito il typing
                _isTyping = false;
                await _chatService.NotifyTypingStopAsync(_recipientEmail);
                _typingTimer.Stop();
            }
        }

        private async void OnTypingTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isTyping)
            {
                _isTyping = false;
                await _chatService.NotifyTypingStopAsync(_recipientEmail);
                _typingTimer.Stop();
            }
        }

        private void CheckSendButtonState()
        {
            SendButton.IsEnabled = !string.IsNullOrWhiteSpace(MessageEntry.Text) && _chatService.IsConnected;
        }

        private void ScrollToBottom()
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(100); // Piccolo delay per assicurarsi che il layout sia completato
                try
                {
                    await MessagesScrollView.ScrollToAsync(0, MessagesScrollView.ContentSize.Height, true);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[CHAT UI] Errore scroll: {ex.Message}");
                }
            });
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Esci dalla chat e disconnetti
            try
            {
                await _chatService.LeaveChatAsync(_recipientEmail);
                await _chatService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT UI] Errore durante uscita: {ex.Message}");
            }

            await Navigation.PopAsync();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();

            // Cleanup
            try
            {
                if (_typingTimer != null)
                {
                    _typingTimer.Stop();
                    _typingTimer.Dispose();
                }

                if (_isTyping)
                {
                    await _chatService.NotifyTypingStopAsync(_recipientEmail);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT UI] Errore durante cleanup: {ex.Message}");
            }
        }
    }
}