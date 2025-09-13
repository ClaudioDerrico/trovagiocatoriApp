// Views/NotificationsPage.xaml.cs - VERSIONE CORRETTA
using System.Text.Json;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class NotificationsPage : ContentPage
    {
        private readonly HttpClient _client = new HttpClient();
        private readonly string _apiBaseUrl = ApiConfig.BaseUrl;

        public NotificationsPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadNotifications();
        }

        private async Task LoadNotifications()
        {
            try
            {
                LoadingIndicator.IsVisible = true;
                LoadingIndicator.IsRunning = true;

                // NASCONDE l'empty state durante il caricamento
                EmptyStateLayout.IsVisible = false;

                var request = new HttpRequestMessage(HttpMethod.Get, $"{_apiBaseUrl}/notifications?limit=50");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[NOTIFICATIONS] Risposta API: {json}");

                    // GESTIONE PIÙ ROBUSTA del JSON
                    var notifications = new List<NotificationItem>();

                    if (!string.IsNullOrEmpty(json))
                    {
                        try
                        {
                            var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                            if (result != null && result.ContainsKey("data") && result["data"] is JsonElement dataElement)
                            {
                                if (dataElement.TryGetProperty("notifications", out var notificationsElement))
                                {
                                    notifications = ParseNotifications(notificationsElement);
                                    Debug.WriteLine($"[NOTIFICATIONS] Trovate {notifications.Count} notifiche");
                                }
                                else
                                {
                                    Debug.WriteLine("[NOTIFICATIONS] Proprietà 'notifications' non trovata nel JSON");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("[NOTIFICATIONS] Struttura JSON non riconosciuta");
                            }
                        }
                        catch (JsonException jsonEx)
                        {
                            Debug.WriteLine($"[NOTIFICATIONS] Errore parsing JSON: {jsonEx.Message}");
                            Debug.WriteLine($"[NOTIFICATIONS] JSON ricevuto: {json}");
                        }
                    }

                    // AGGIORNA UI sempre, anche se notifications è vuota
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DisplayNotifications(notifications);
                        UpdateSummary(notifications);
                    });
                }
                else
                {
                    Debug.WriteLine($"[NOTIFICATIONS] Errore HTTP: {response.StatusCode}");
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"[NOTIFICATIONS] Contenuto errore: {errorContent}");

                    // MOSTRA empty state anche per errori HTTP
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        DisplayNotifications(new List<NotificationItem>());
                        UpdateSummary(new List<NotificationItem>());
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore caricamento notifiche: {ex.Message}");
                Debug.WriteLine($"[NOTIFICATIONS] StackTrace: {ex.StackTrace}");

                // GESTIONE ERRORE: mostra empty state invece di alert
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    DisplayNotifications(new List<NotificationItem>());
                    UpdateSummary(new List<NotificationItem>());

                    // OPZIONALE: Mostra un messaggio di errore discreto
                    NotificationsSummaryLabel.Text = "Errore nel caricamento";
                });
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private List<NotificationItem> ParseNotifications(JsonElement notificationsElement)
        {
            var notifications = new List<NotificationItem>();

            try
            {
                // GESTISCE sia array che oggetti vuoti
                if (notificationsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var notifElement in notificationsElement.EnumerateArray())
                    {
                        try
                        {
                            var notification = new NotificationItem
                            {
                                Id = GetLongProperty(notifElement, "id"),
                                Type = GetStringProperty(notifElement, "type"),
                                Title = GetStringProperty(notifElement, "title"),
                                Message = GetStringProperty(notifElement, "message"),
                                Status = GetStringProperty(notifElement, "status"),
                                CreatedAt = GetStringProperty(notifElement, "created_at"),
                                SenderInfo = ParseSenderInfo(notifElement)
                            };

                            notifications.Add(notification);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[NOTIFICATIONS] Errore parsing singola notifica: {ex.Message}");
                            // Continua con le altre notifiche
                        }
                    }
                }
                else
                {
                    Debug.WriteLine($"[NOTIFICATIONS] Tipo JSON non aspettato: {notificationsElement.ValueKind}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore durante il parsing delle notifiche: {ex.Message}");
            }

            return notifications;
        }

        private SenderInfo ParseSenderInfo(JsonElement notifElement)
        {
            try
            {
                if (notifElement.TryGetProperty("sender_info", out var senderElement) &&
                    senderElement.ValueKind != JsonValueKind.Null)
                {
                    return new SenderInfo
                    {
                        Username = GetStringProperty(senderElement, "username"),
                        DisplayName = GetStringProperty(senderElement, "display_name"),
                        ProfilePic = GetStringProperty(senderElement, "profile_picture")
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore parsing sender info: {ex.Message}");
            }

            return null;
        }

        private void DisplayNotifications(List<NotificationItem> notifications)
        {
            NotificationsContainer.Clear();

            if (notifications.Count == 0)
            {
                EmptyStateLayout.IsVisible = true;
                Debug.WriteLine("[NOTIFICATIONS] Mostrando empty state - nessuna notifica");
                return;
            }

            EmptyStateLayout.IsVisible = false;
            Debug.WriteLine($"[NOTIFICATIONS] Mostrando {notifications.Count} notifiche");

            foreach (var notification in notifications)
            {
                try
                {
                    var notificationView = CreateNotificationView(notification);
                    NotificationsContainer.Add(notificationView);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NOTIFICATIONS] Errore creazione view notifica: {ex.Message}");
                }
            }
        }

        private Frame CreateNotificationView(NotificationItem notification)
        {
            var isUnread = notification.Status == "unread";
            var frame = new Frame
            {
                Style = isUnread ? (Style)Resources["UnreadNotificationStyle"] : (Style)Resources["NotificationCardStyle"]
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += async (s, e) => await OnNotificationTapped(notification);
            frame.GestureRecognizers.Add(tapGesture);

            var grid = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }
                },
                RowDefinitions =
                {
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) },
                    new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) }
                },
                RowSpacing = 8,
                ColumnSpacing = 12
            };

            // Icona tipo notifica
            var iconLabel = new Label
            {
                Text = GetNotificationIcon(notification.Type),
                FontSize = 24,
                VerticalOptions = LayoutOptions.Start
            };
            Grid.SetColumn(iconLabel, 0);
            Grid.SetRowSpan(iconLabel, 3);

            // Titolo
            var titleLabel = new Label
            {
                Text = notification.Title,
                FontAttributes = isUnread ? FontAttributes.Bold : FontAttributes.None,
                FontSize = 16,
                TextColor = Color.FromArgb("#1E293B")
            };
            Grid.SetColumn(titleLabel, 1);
            Grid.SetRow(titleLabel, 0);

            // Badge non letto
            if (isUnread)
            {
                var unreadBadge = new Frame
                {
                    BackgroundColor = Color.FromArgb("#EF4444"),
                    CornerRadius = 6,
                    Padding = new Thickness(6, 2),
                    HasShadow = false,
                    VerticalOptions = LayoutOptions.Start,
                    Content = new Label
                    {
                        Text = "NUOVO",
                        TextColor = Colors.White,
                        FontSize = 10,
                        FontAttributes = FontAttributes.Bold
                    }
                };
                Grid.SetColumn(unreadBadge, 2);
                Grid.SetRow(unreadBadge, 0);
                grid.Children.Add(unreadBadge);
            }

            // Messaggio
            var messageLabel = new Label
            {
                Text = notification.Message,
                FontSize = 14,
                TextColor = Color.FromArgb("#64748B"),
                LineBreakMode = LineBreakMode.WordWrap
            };
            Grid.SetColumn(messageLabel, 1);
            Grid.SetRow(messageLabel, 1);

            // Data e mittente
            var detailsText = FormatDateTime(notification.CreatedAt);
            if (notification.SenderInfo != null)
            {
                detailsText += $" • da {notification.SenderInfo.DisplayName}";
            }

            var detailsLabel = new Label
            {
                Text = detailsText,
                FontSize = 12,
                TextColor = Color.FromArgb("#9CA3AF")
            };
            Grid.SetColumn(detailsLabel, 1);
            Grid.SetRow(detailsLabel, 2);

            grid.Children.Add(iconLabel);
            grid.Children.Add(titleLabel);
            grid.Children.Add(messageLabel);
            grid.Children.Add(detailsLabel);

            frame.Content = grid;
            return frame;
        }

        private string GetNotificationIcon(string type)
        {
            return type switch
            {
                "friend_request" => "👥",
                "event_invite" => "🎉",
                "post_comment" => "💬",
                _ => "🔔"
            };
        }

        private string FormatDateTime(string dateTimeString)
        {
            if (DateTime.TryParse(dateTimeString, out DateTime dateTime))
            {
                var now = DateTime.Now;
                var diff = now - dateTime;

                if (diff.TotalMinutes < 1) return "Ora";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m fa";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h fa";
                if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}g fa";

                return dateTime.ToString("dd/MM/yyyy");
            }
            return dateTimeString;
        }

        private async Task OnNotificationTapped(NotificationItem notification)
        {
            try
            {
                // Segna come letta se non letta
                if (notification.Status == "unread")
                {
                    await MarkNotificationAsRead(notification.Id);
                }

                // Naviga alla sezione appropriata in base al tipo
                await NavigateToRelevantSection(notification);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore gestione tap notifica: {ex.Message}");
            }
        }

        private async Task NavigateToRelevantSection(NotificationItem notification)
        {
            try
            {
                switch (notification.Type)
                {
                    case "friend_request":
                        await Navigation.PushAsync(new FriendsPage());
                        break;
                    case "event_invite":
                        await Navigation.PushAsync(new ProfilePage()); // Tab "I Miei Eventi"
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore navigazione: {ex.Message}");
            }
        }

        private async Task MarkNotificationAsRead(long notificationId)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/notifications/read?id={notificationId}");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                await _client.SendAsync(request);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore segna notifica come letta: {ex.Message}");
            }
        }

        private async void OnMarkAllReadClicked(object sender, EventArgs e)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/notifications/read-all");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await LoadNotifications(); // Ricarica
                    await DisplayAlert("Successo", "Tutte le notifiche sono state segnate come lette", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore segna tutte come lette: {ex.Message}");
                await DisplayAlert("Errore", "Impossibile segnare le notifiche come lette", "OK");
            }
        }

        private void UpdateSummary(List<NotificationItem> notifications)
        {
            try
            {
                var unreadCount = notifications.Count(n => n.Status == "unread");
                var totalCount = notifications.Count;

                NotificationsSummaryLabel.Text = unreadCount > 0
                    ? $"{unreadCount} non lette su {totalCount}"
                    : totalCount > 0
                        ? $"{totalCount} notifiche (tutte lette)"
                        : "Nessuna notifica";

                Debug.WriteLine($"[NOTIFICATIONS] Summary aggiornato: {NotificationsSummaryLabel.Text}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[NOTIFICATIONS] Errore aggiornamento summary: {ex.Message}");
                NotificationsSummaryLabel.Text = "Errore caricamento";
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadNotifications();
            NotificationsRefreshView.IsRefreshing = false;
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        // Helper methods SICURI
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

        private long GetLongProperty(JsonElement element, string propertyName, long defaultValue = 0)
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetInt64()
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }
    }


}