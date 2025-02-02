using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace trovagiocatoriApp.Views
{
    public partial class CreatePostPage : ContentPage
    {
        // Proprietà per date
        public DateTime CurrentDate { get; set; }
        public DateTime MaxDate { get; set; }

        // Lista di sport predefiniti
        public List<string> SportOptions { get; set; }

        // Lista completa delle province italiane
        public List<string> ProvinceOptions { get; set; }

        // Lista filtrata in base al testo inserito nella Entry per provincia (ObservableCollection per notificare la UI)
        public ObservableCollection<string> FilteredProvinces { get; set; } = new ObservableCollection<string>();

        public CreatePostPage()
        {
            InitializeComponent();

            // Inizializza le date
            CurrentDate = DateTime.Now;
            MaxDate = CurrentDate.AddMonths(1);

            SportOptions = new List<string>
            {
                "Calcio", "Tennis", "Pallavolo", "Basket", "Padel"
            };

            ProvinceOptions = new List<string>
            {
                "Agrigento", "Alessandria", "Ancona", "Aosta", "Arezzo", "Ascoli Piceno", "Asti", "Avellino",
                "Bari", "Barletta-Andria-Trani", "Belluno", "Benevento", "Bergamo", "Biella", "Bologna", "Bolzano",
                "Brescia", "Brindisi", "Cagliari", "Caltanissetta", "Campobasso", "Caserta", "Catania", "Catanzaro",
                "Chieti", "Como", "Cosenza", "Cremona", "Crotone", "Cuneo", "Enna", "Fermo", "Ferrara",
                "Firenze", "Foggia", "Forlì-Cesena", "Frosinone", "Genova", "Gorizia", "Grosseto", "Imperia",
                "Isernia", "L'Aquila", "La Spezia", "Latina", "Lecce", "Lecco", "Livorno", "Lodi",
                "Lucca", "Macerata", "Mantova", "Massa-Carrara", "Matera", "Messina", "Milano", "Modena",
                "Monza e Brianza", "Napoli", "Novara", "Nuoro", "Oristano", "Padova", "Palermo", "Parma",
                "Pavia", "Perugia", "Pesaro e Urbino", "Pescara", "Piacenza", "Pisa", "Pistoia", "Pordenone",
                "Potenza", "Prato", "Ragusa", "Ravenna", "Reggio Calabria", "Reggio Emilia", "Rieti", "Rimini",
                "Roma", "Rovigo", "Salerno", "Sassari", "Savona", "Siena", "Siracusa", "Sondrio",
                "Sud Sardegna", "Taranto", "Teramo", "Terni", "Torino", "Trapani", "Trento", "Treviso",
                "Trieste", "Udine", "Varese", "Venezia", "Verbano-Cusio-Ossola", "Vercelli", "Verona", "Vibo Valentia", "Vicenza", "Viterbo"
            };

            // Imposta il BindingContext affinché i controlli possano leggere le proprietà
            BindingContext = this;
        }

        // Aggiorna il contatore dei caratteri per il titolo
        private void OnTitoloTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount(TitoloEntry.Text, CaratteriRimanenti, 50);
        }

        // Aggiorna il contatore dei caratteri per la città
        private void OnCittaTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount(CittaEntry.Text, CaratteriRimanentiCitta, 35);
        }

        // Aggiorna il contatore dei caratteri per il commento
        private void OnCommentoTextChanged(object sender, TextChangedEventArgs e)
        {
            var remainingChars = 155 - CommentoEditor.Text.Length;
            CharCountLabel.Text = $"Caratteri rimanenti: {remainingChars}";
        }

        // Metodo di supporto per aggiornare il contatore dei caratteri
        private void UpdateCharacterCount(string text, Label label, int maxLength)
        {
            var remainingChars = maxLength - text.Length;
            label.Text = $"Caratteri rimanenti: {remainingChars}";
        }

        // Gestione della modifica del testo nella Entry per la provincia
        private void ProvinciaEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ProvinciaEntry.Text?.ToLower() ?? "";

            // Filtra le province che contengono il testo inserito
            var filtered = ProvinceOptions
                .Where(p => p.ToLower().Contains(text))
                .ToList();

            // Aggiorna l'ObservableCollection
            FilteredProvinces.Clear();
            foreach (var prov in filtered)
            {
                FilteredProvinces.Add(prov);
            }

            // Mostra la ListView solo se ci sono suggerimenti e l'utente ha digitato almeno 1 carattere
            ProvinceSuggestionsList.IsVisible = !string.IsNullOrWhiteSpace(text) && FilteredProvinces.Any();
        }

        // Gestione della selezione di un suggerimento dalla ListView
        private void ProvinceSuggestionsList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is string selectedProvince)
            {
                ProvinciaEntry.Text = selectedProvince;
                ProvinceSuggestionsList.IsVisible = false;
            }
        }

        private async void OnCreatePostClicked(object sender, EventArgs e)
        {
            //  nessun campo può essere vuoto
            if (string.IsNullOrWhiteSpace(TitoloEntry.Text) ||
                string.IsNullOrWhiteSpace(ProvinciaEntry.Text) ||
                string.IsNullOrWhiteSpace(CittaEntry.Text) ||
                SportPicker.SelectedItem == null ||
                string.IsNullOrWhiteSpace(CommentoEditor.Text))
            {
                await DisplayAlert("Errore", "Tutti i campi sono obbligatori", "OK");
                return;
            }

            if (!ProvinceOptions.Contains(ProvinciaEntry.Text))
            {
                await DisplayAlert("Errore", "Seleziona una provincia valida dalla lista", "OK");
                return;
            }

            if (SportPicker.SelectedItem == null)
            {
                await DisplayAlert("Errore", "Seleziona uno sport dalla lista", "OK");
                return;
            }
            var formattedTime = OraPartitaPicker.Time.ToString(@"hh\:mm");

            // Costruzione dell'oggetto post
            var post = new
            {
                titolo = TitoloEntry.Text,
                provincia = ProvinciaEntry.Text,
                citta = CittaEntry.Text,
                sport = SportPicker.SelectedItem.ToString(),
                data_partita = DataPartitaPicker.Date.ToString("dd-MM-yyyy"),
                ora_partita = formattedTime,
                commento = CommentoEditor.Text
            };

            try
            {
                // Recupera il cookie di sessione salvato nelle Preferences
                var sessionCookie = Preferences.Get("session_id", string.Empty);
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    await DisplayAlert("Errore", "La sessione è scaduta. Effettua nuovamente il login.", "OK");
                    return;
                }

                // Crea un HttpClientHandler con un CookieContainer per gestire il cookie
                var handler = new HttpClientHandler();
                var baseUri = new Uri("http://localhost:8000/"); // Modifica l'URL se usi emulator o ambiente Docker
                handler.CookieContainer.Add(baseUri, new Cookie("session_id", sessionCookie));

                using (var client = new HttpClient(handler))
                {
                    var content = new StringContent(JsonSerializer.Serialize(post), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("http://localhost:8000/posts/", content);

                    if (response.IsSuccessStatusCode)
                    {
                        await DisplayAlert("Post creato", "Il tuo post è stato creato!", "OK");
                        await Shell.Current.GoToAsync("//HomePage");

                    }
                    else
                    {
                        await DisplayAlert("Errore", "Si è verificato un errore nel creare il post.", "OK");
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore durante la creazione del post: {ex.Message}", "OK");
            }
        }
    }
}
