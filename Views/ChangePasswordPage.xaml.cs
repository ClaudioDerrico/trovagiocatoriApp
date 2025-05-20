using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class ChangePasswordPage : ContentPage
    {
        private readonly HttpClient _client = new HttpClient();
        private string ApiBaseUrl =>
            DeviceInfo.Platform == DevicePlatform.Android
                ? "http://10.0.2.2:8080"    // loopback dell’emulatore Android
                : "http://localhost:8080";  // Windows, macOS, iOS, ecc.

        // Stato di visibilità delle password
        private bool _isCurrentVisible = false;
        private bool _isNewVisible = false;
        private bool _isConfirmVisible = false;

        public ChangePasswordPage()
        {
            InitializeComponent();
        }

        // Toggle Visibility Handlers
        private void OnToggleCurrentPassword(object sender, EventArgs e)
        {
            _isCurrentVisible = !_isCurrentVisible;
            CurrentPasswordEntry.IsPassword = !_isCurrentVisible;
            ToggleCurrentPasswordVisibility.Source = _isCurrentVisible ? "eye_open.png" : "eye_close.png";
        }

        private void OnToggleNewPassword(object sender, EventArgs e)
        {
            _isNewVisible = !_isNewVisible;
            NewPasswordEntry.IsPassword = !_isNewVisible;
            ToggleNewPasswordVisibility.Source = _isNewVisible ? "eye_open.png" : "eye_close.png";
        }

        private void OnToggleConfirmPassword(object sender, EventArgs e)
        {
            _isConfirmVisible = !_isConfirmVisible;
            ConfirmPasswordEntry.IsPassword = !_isConfirmVisible;
            ToggleConfirmPasswordVisibility.Source = _isConfirmVisible ? "eye_open.png" : "eye_close.png";
        }

        // Handler per il bottone "Aggiorna"
        private async void OnChangePasswordClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(CurrentPasswordEntry.Text) ||
                    string.IsNullOrWhiteSpace(NewPasswordEntry.Text) ||
                    string.IsNullOrWhiteSpace(ConfirmPasswordEntry.Text))
                {
                    await DisplayAlert("Errore", "Compila tutti i campi", "OK");
                    return;
                }

                if (NewPasswordEntry.Text != ConfirmPasswordEntry.Text)
                {
                    await DisplayAlert("Errore", "Le nuove password non coincidono", "OK");
                    return;
                }

                var payload = new
                {
                    current_password = CurrentPasswordEntry.Text,
                    new_password = NewPasswordEntry.Text
                };

                var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiBaseUrl}/update-password");

                if (Preferences.ContainsKey("session_id"))
                {
                    string sessionId = Preferences.Get("session_id", "");
                    request.Headers.Add("Cookie", $"session_id={sessionId}");
                }

                request.Content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Reset dei campi
                    CurrentPasswordEntry.Text = string.Empty;
                    NewPasswordEntry.Text = string.Empty;
                    ConfirmPasswordEntry.Text = string.Empty;

                    await DisplayAlert("Successo", "Password modificata con successo", "OK");
                    // Torna a ProfilePage
                    await Navigation.PopAsync();
                }
                else
                {
                    await DisplayAlert("Errore", "Modifica password fallita", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Eccezione durante modifica password: {ex.Message}");
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
        }

        // Handler per il bottone "Annulla"
        private async void OnCancelChangePassword(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}