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
        private readonly string _apiBaseUrl = "http://localhost:8080";

        public ChangePasswordPage()
        {
            InitializeComponent();
        }

        // Questo è il tuo handler per il bottone "Aggiorna"
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

                var request = new HttpRequestMessage(HttpMethod.Post, $"{_apiBaseUrl}/update-password");

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
                    CurrentPasswordEntry.Text = "";
                    NewPasswordEntry.Text = "";
                    ConfirmPasswordEntry.Text = "";
                    await DisplayAlert("Successo", "Password modificata con successo", "OK");
                    // Torna indietro alla ProfilePage
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

        // Handler per il tasto Annulla
        private async void OnCancelChangePassword(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }
    }
}
