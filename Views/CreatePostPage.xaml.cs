using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace trovagiocatoriApp.Views
{
    public partial class CreatePostPage : ContentPage
    {
        public DateTime CurrentDate { get; set; }
        public DateTime MaxDate { get; set; }

        public CreatePostPage()
        {
            InitializeComponent();
            CurrentDate = DateTime.Now;
            MaxDate = CurrentDate.AddMonths(1);
            BindingContext = this;
        }

        private void OnTitoloTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount(TitoloEntry.Text, CaratteriRimanenti, 50);
        }

        private void OnCittaTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount(CittaEntry.Text, CaratteriRimanentiCitta, 35);
        }

        private void OnCommentoTextChanged(object sender, TextChangedEventArgs e)
        {
            var remainingChars = 155 - CommentoEditor.Text.Length;
            CharCountLabel.Text = $"Caratteri rimanenti: {remainingChars}";
        }

        private void UpdateCharacterCount(string text, Label label, int maxLength)
        {
            var remainingChars = maxLength - text.Length;
            label.Text = $"Caratteri rimanenti: {remainingChars}";
        }

        private async void OnCreatePostClicked(object sender, EventArgs e)
        {
            // Raccogli i dati del post
            var post = new
            {
                titolo = TitoloEntry.Text,
                provincia = ProvinciaEntry.Text,  // Provincia dall'input dell'utente
                citta = CittaEntry.Text,
                sport = SportEntry.Text,  // Sport dall'input dell'utente
                data_partita = DataPartitaPicker.Date.ToString("yyyy-MM-dd"),
                ora_partita = OraPartitaPicker.Time.ToString(@"hh\:mm"),
                commento = CommentoEditor.Text
            };

            // Crea la richiesta HTTP per inviare il post
            try
            {
                using (var client = new HttpClient())
                {
                    // Ottieni il session_id dalle preferenze
                    var sessionCookie = Preferences.Get("session_id", string.Empty);
                    if (string.IsNullOrEmpty(sessionCookie))
                    {
                        await DisplayAlert("Errore", "La sessione è scaduta. Effettua nuovamente il login.", "OK");
                        return;
                    }

                    // Aggiungi il cookie direttamente nell'header della richiesta
                    client.DefaultRequestHeaders.Add("Cookie", $"session_id={sessionCookie}");

                    // Serializza il post come JSON
                    var content = new StringContent(JsonSerializer.Serialize(post), Encoding.UTF8, "application/json");

                    // Invia la richiesta HTTP
                    var response = await client.PostAsync("http://my_backend_python:8000/posts/", content);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Post creato", "Il tuo post è stato creato!", "OK");
                    }
                    else
                    {
                        await DisplayAlert("Errore", "Si è verificato un errore nel creare il post.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                // Gestisci eventuali errori
                await DisplayAlert("Errore", $"Errore durante la creazione del post: {ex.Message}", "OK");
            }
        }
    }
}
