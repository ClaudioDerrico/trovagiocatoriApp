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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class CreatePostPage : ContentPage, INotifyPropertyChanged
    {
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string _pythonApiUrl = ApiConfig.PythonApiUrl;

        // Proprietà per date
        public DateTime CurrentDate { get; set; }
        public DateTime MaxDate { get; set; }

        // Lista di sport predefiniti
        public List<string> SportOptions { get; set; }

        // NUOVA: Lista livelli
        public List<string> LivelloOptions { get; set; }
        private string _selectedLivello = "Intermedio";
        public string SelectedLivello
        {
            get => _selectedLivello;
            set
            {
                _selectedLivello = value;
                OnPropertyChanged();
            }
        }

        // NUOVO: Numero giocatori
        private int _numeroGiocatori = 1;
        public int NumeroGiocatori
        {
            get => _numeroGiocatori;
            set
            {
                _numeroGiocatori = value;
                OnPropertyChanged();
            }
        }

        // Lista completa delle province italiane
        public List<string> ProvinceOptions { get; set; }

        // Lista filtrata in base al testo inserito nella Entry per provincia
        public ObservableCollection<string> FilteredProvinces { get; set; } = new ObservableCollection<string>();

        // Lista dei campi sportivi
        public ObservableCollection<SportField> SportFields { get; set; } = new ObservableCollection<SportField>();
        public ObservableCollection<SportField> FilteredSportFields { get; set; } = new ObservableCollection<SportField>();

        // Campo selezionato
        private SportField _selectedSportField;
        public SportField SelectedSportField
        {
            get => _selectedSportField;
            set
            {
                _selectedSportField = value;
                OnPropertyChanged();
                UpdateFormWithSelectedField();
            }
        }

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

            // NUOVO: Inizializza le opzioni livello
            LivelloOptions = new List<string>
            {
                "Principiante", "Intermedio", "Avanzato"
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

            // Imposta il BindingContext
            BindingContext = this;

            // Carica i campi sportivi
            _ = LoadSportFields();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSportFields();
        }

        private async Task LoadSportFields()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_pythonApiUrl}/fields/");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var fields = JsonSerializer.Deserialize<List<SportField>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    SportFields.Clear();
                    foreach (var field in fields ?? new List<SportField>())
                    {
                        SportFields.Add(field);
                    }

                    // Inizialmente mostra tutti i campi
                    UpdateFilteredSportFields();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Impossibile caricare i campi sportivi: {ex.Message}", "OK");
            }
        }

        private void UpdateFilteredSportFields(string provinciaFilter = null, string sportFilter = null)
        {
            FilteredSportFields.Clear();

            var fieldsToShow = SportFields.AsEnumerable();

            // Filtra per provincia se specificata
            if (!string.IsNullOrEmpty(provinciaFilter))
            {
                fieldsToShow = fieldsToShow.Where(f => f.Provincia.Equals(provinciaFilter, StringComparison.OrdinalIgnoreCase));
            }

            // Filtra per sport se specificato
            if (!string.IsNullOrEmpty(sportFilter))
            {
                fieldsToShow = fieldsToShow.Where(f => f.SupportsSport(sportFilter));
            }

            foreach (var field in fieldsToShow)
            {
                FilteredSportFields.Add(field);
            }
        }

        private void UpdateFormWithSelectedField()
        {
            if (SelectedSportField != null)
            {
                ProvinciaEntry.Text = SelectedSportField.Provincia;
                CittaEntry.Text = SelectedSportField.Citta;
            }
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

        // NUOVO: Gestione cambio numero giocatori
        private void OnNumeroGiocatoriChanged(object sender, ValueChangedEventArgs e)
        {
            var valore = (int)e.NewValue;
            NumeroGiocatori = valore;

            // Aggiorna il testo del label
            if (valore == 1)
                NumeroGiocatoriLabel.Text = "1 giocatore";
            else
                NumeroGiocatoriLabel.Text = $"{valore} giocatori";
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

            // Filtra anche i campi sportivi in base alla provincia e allo sport selezionato
            var selectedSport = SportPicker.SelectedItem?.ToString();
            UpdateFilteredSportFields(text, selectedSport);
        }

        // Gestione della selezione di un suggerimento dalla ListView
        private void ProvinceSuggestionsList_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            if (e.SelectedItem is string selectedProvince)
            {
                ProvinciaEntry.Text = selectedProvince;
                ProvinceSuggestionsList.IsVisible = false;
                var selectedSport = SportPicker.SelectedItem?.ToString();
                UpdateFilteredSportFields(selectedProvince, selectedSport);
            }
        }

        // Gestione cambio sport nel picker
        private void OnSportPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedSport = SportPicker.SelectedItem?.ToString();
            var selectedProvince = ProvinciaEntry.Text;
            UpdateFilteredSportFields(selectedProvince, selectedSport);
        }

        // Gestione selezione campo sportivo
        private void OnSportFieldSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is SportField selectedField)
            {
                SelectedSportField = selectedField;
                // Deseleziona per evitare evidenziazione permanente
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private async void OnCreatePostClicked(object sender, EventArgs e)
        {
            // Validazione dei campi obbligatori
            if (string.IsNullOrWhiteSpace(TitoloEntry.Text) ||
                string.IsNullOrWhiteSpace(ProvinciaEntry.Text) ||
                string.IsNullOrWhiteSpace(CittaEntry.Text) ||
                SportPicker.SelectedItem == null ||
                LivelloPicker.SelectedItem == null ||  // NUOVA VALIDAZIONE
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

            // Costruzione dell'oggetto post con il livello e numero giocatori
            var post = new
            {
                titolo = TitoloEntry.Text,
                provincia = ProvinciaEntry.Text,
                citta = CittaEntry.Text,
                sport = SportPicker.SelectedItem.ToString(),
                data_partita = DataPartitaPicker.Date.ToString("dd-MM-yyyy"),
                ora_partita = formattedTime,
                commento = CommentoEditor.Text,
                campo_id = SelectedSportField?.Id,
                livello = LivelloPicker.SelectedItem?.ToString() ?? "Intermedio",  // NUOVO CAMPO
                numero_giocatori = NumeroGiocatori  // NUOVO CAMPO
            };

            try
            {
                // Recupera il cookie di sessione
                var sessionCookie = Preferences.Get("session_id", string.Empty);
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    await DisplayAlert("Errore", "La sessione è scaduta. Effettua nuovamente il login.", "OK");
                    return;
                }

                // Crea la richiesta HTTP
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_pythonApiUrl}/posts/");

                // Aggiungi il cookie di sessione
                request.Headers.Add("Cookie", $"session_id={sessionCookie}");

                // Aggiungi il contenuto JSON
                var json = JsonSerializer.Serialize(post);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var giocatoriText = NumeroGiocatori == 1 ? "1 giocatore" : $"{NumeroGiocatori} giocatori";
                    var successMessage = SelectedSportField != null
                        ? $"Il tuo post è stato creato!\n\nCampo selezionato: {SelectedSportField.Nome}\nLivello: {post.livello}\nCerchi: {giocatoriText}"
                        : $"Il tuo post è stato creato!\n\nLivello: {post.livello}\nCerchi: {giocatoriText}";

                    await DisplayAlert("Post creato", successMessage, "OK");
                    await Shell.Current.GoToAsync("//HomePage");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    await DisplayAlert("Errore", $"Si è verificato un errore nel creare il post: {errorContent}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore durante la creazione del post: {ex.Message}", "OK");
            }
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}