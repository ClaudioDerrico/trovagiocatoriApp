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
                ChatWithLabel.Text = $"Chat con organizzatore";
            }

            UpdateConnectionStatus(false);

            // Timer per gestire il typing indicator
            _typingTimer = new System.Timers.Timer(3000); // 3 secondi
            _typingTimer.Elapsed += OnTypingTimerElapsed;
        }

        private async void InitializeChatService()
        {
            // Registra gli event handlers - Ora usa LiveChatMessage
            _chatService.NewMessageReceived += OnNewMessageReceived;
            _chatService.UserTyping += OnUserTyping;
            _chatService.UserStoppedTyping += OnUserStoppedTyping;
            _chatService.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Connetti al server
            var connected = await _chatService.ConnectAsync();
            if (connected)
            {
                // Entra nella chat del post
                await _chatService.JoinPostChatAsync(_post.id, _post.autore_email);

                // Carica i messaggi esistenti
                LoadExistingMessages();


            }
            else
            {
                await DisplayAlert("Errore", "Impossibile connettersi al server chat", "OK");
            }
        }

        private void LoadExistingMessages()
        {
            // Carica i messaggi dalla collezione del ChatService
            var messages = _chatService.GetMessagesForPost(_post.id);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                MessagesContainer.Clear();
                foreach (var message in messages)
                {
                    AddMessageToUI(message);
                }
                ScrollToBottom();
            });
        }

        private void OnNewMessageReceived(LiveChatMessage message)
        {
            // Verifica che il messaggio sia per questo post
            if (message.PostId != _post.id)
                return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                AddMessageToUI(message);
                ScrollToBottom();


            });
        }

        private void OnUserTyping(string userEmail)
        {
            if (userEmail == _recipientEmail)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    TypingIndicator.IsVisible = true;
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
                });
            }
        }

        private void OnConnectionStatusChanged(bool isConnected)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                UpdateConnectionStatus(isConnected);
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

            // Disabilita il pulsante temporaneamente
            SendButton.IsEnabled = false;

            try
            {
                // Invia il messaggio
                await _chatService.SendMessageAsync(_post.id, _recipientEmail, message);

                // Pulisci il campo di input
                MessageEntry.Text = "";

                // Ferma il typing indicator
                if (_isTyping)
                {
                    await _chatService.NotifyTypingStopAsync(_post.id, _recipientEmail);
                    _isTyping = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore invio messaggio: {ex.Message}");
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
                await _chatService.NotifyTypingStartAsync(_post.id, _recipientEmail);
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
                await _chatService.NotifyTypingStopAsync(_post.id, _recipientEmail);
                _typingTimer.Stop();
            }
        }

        private async void OnTypingTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_isTyping)
            {
                _isTyping = false;
                await _chatService.NotifyTypingStopAsync(_post.id, _recipientEmail);
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
                await MessagesScrollView.ScrollToAsync(0, MessagesScrollView.ContentSize.Height, true);
            });
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            // Esci dalla chat e disconnetti
            try
            {
                await _chatService.LeavePostChatAsync(_post.id, _recipientEmail);
                await _chatService.DisconnectAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore durante uscita: {ex.Message}");
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
                    await _chatService.NotifyTypingStopAsync(_post.id, _recipientEmail);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[CHAT] Errore durante cleanup: {ex.Message}");
            }
        }
    }
}