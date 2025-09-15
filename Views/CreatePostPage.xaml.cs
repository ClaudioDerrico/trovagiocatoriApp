using System.Collections.ObjectModel;
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

        // Proprietà per date
        public DateTime CurrentDate { get; set; }
        public DateTime MaxDate { get; set; }

        // Liste di opzioni
        public List<string> SportOptions { get; set; }
        public List<string> LivelloOptions { get; set; }
        public List<string> ProvinceOptions { get; set; }

        // Proprietà per livello selezionato
        private string _selectedLivello = "Intermedio";
        public string SelectedLivello
        {
            get => _selectedLivello;
            set { _selectedLivello = value; OnPropertyChanged(); }
        }

        // Proprietà per numero giocatori
        private int _numeroGiocatori = 1;
        public int NumeroGiocatori
        {
            get => _numeroGiocatori;
            set { _numeroGiocatori = value; OnPropertyChanged(); }
        }

        // Collezioni per campi sportivi
        public ObservableCollection<string> FilteredProvinces { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<SportField> SportFields { get; set; } = new ObservableCollection<SportField>();
        public ObservableCollection<SportField> FilteredSportFields { get; set; } = new ObservableCollection<SportField>();

        // Campo selezionato
        private SportField _selectedSportField;
        public SportField SelectedSportField
        {
            get => _selectedSportField;
            set { _selectedSportField = value; OnPropertyChanged(); UpdateFormWithSelectedField(); }
        }

        public CreatePostPage()
        {
            InitializeComponent();
            InitializeData();
            BindingContext = this;
            _ = LoadSportFields();
        }

        // Inizializza date e opzioni
        private void InitializeData()
        {
            CurrentDate = DateTime.Now;
            MaxDate = CurrentDate.AddMonths(1);

            SportOptions = new List<string> { "Calcio", "Tennis", "Pallavolo", "Basket", "Padel" };
            LivelloOptions = new List<string> { "Principiante", "Intermedio", "Avanzato" };
            ProvinceOptions = InitializeProvinces();
        }

        // Inizializza la lista delle province italiane
        private List<string> InitializeProvinces()
        {
            return new List<string>
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
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadSportFields();
        }

        // Carica tutti i campi sportivi disponibili
        private async Task LoadSportFields()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ApiConfig.PythonApiUrl}/fields/");
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

                    UpdateFilteredSportFields();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Impossibile caricare i campi sportivi: {ex.Message}", "OK");
            }
        }

        // Filtra i campi sportivi in base a provincia e sport
        private void UpdateFilteredSportFields(string provinciaFilter = null, string sportFilter = null)
        {
            FilteredSportFields.Clear();

            var fieldsToShow = SportFields.AsEnumerable();

            if (!string.IsNullOrEmpty(provinciaFilter))
            {
                fieldsToShow = fieldsToShow.Where(f => f.Provincia.Equals(provinciaFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrEmpty(sportFilter))
            {
                fieldsToShow = fieldsToShow.Where(f => f.SupportsSport(sportFilter));
            }

            foreach (var field in fieldsToShow)
            {
                FilteredSportFields.Add(field);
            }
        }

        // Aggiorna il form quando viene selezionato un campo
        private void UpdateFormWithSelectedField()
        {
            if (SelectedSportField != null)
            {
                ProvinciaEntry.Text = SelectedSportField.Provincia;
                CittaEntry.Text = SelectedSportField.Citta;
            }
        }

        // ========== EVENT HANDLERS ==========

        // Aggiorna contatore caratteri titolo
        private void OnTitoloTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount(TitoloEntry.Text, CaratteriRimanenti, 50);
        }

        // Aggiorna contatore caratteri città
        private void OnCittaTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateCharacterCount(CittaEntry.Text, CaratteriRimanentiCitta, 35);
        }

        // Aggiorna contatore caratteri commento
        private void OnCommentoTextChanged(object sender, TextChangedEventArgs e)
        {
            var remainingChars = 155 - (CommentoEditor.Text?.Length ?? 0);
            CharCountLabel.Text = $"Caratteri rimanenti: {remainingChars}";
        }

        // Gestisce il cambio del numero di giocatori
        private void OnNumeroGiocatoriChanged(object sender, ValueChangedEventArgs e)
        {
            var valore = (int)e.NewValue;
            NumeroGiocatori = valore;

            NumeroGiocatoriLabel.Text = valore == 1 ? "1 giocatore" : $"{valore} giocatori";
        }

        // Gestisce il cambio di testo nella provincia
        private void ProvinciaEntry_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = ProvinciaEntry.Text?.ToLower() ?? "";

            // Filtra le province che contengono il testo inserito
            var filtered = ProvinceOptions.Where(p => p.ToLower().Contains(text)).ToList();

            FilteredProvinces.Clear();
            foreach (var prov in filtered)
            {
                FilteredProvinces.Add(prov);
            }

            // Mostra suggerimenti se ci sono risultati e almeno 1 carattere
            ProvinceSuggestionsList.IsVisible = !string.IsNullOrWhiteSpace(text) && FilteredProvinces.Any();

            // Filtra anche i campi sportivi
            var selectedSport = SportPicker.SelectedItem?.ToString();
            UpdateFilteredSportFields(text, selectedSport);
        }

        // Gestisce la selezione di una provincia dai suggerimenti
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

        // Gestisce il cambio sport nel picker
        private void OnSportPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            var selectedSport = SportPicker.SelectedItem?.ToString();
            var selectedProvince = ProvinciaEntry.Text;
            UpdateFilteredSportFields(selectedProvince, selectedSport);
        }

        // Gestisce la selezione di un campo sportivo
        private void OnSportFieldSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is SportField selectedField)
            {
                SelectedSportField = selectedField;
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        // Gestisce la creazione del post
        private async void OnCreatePostClicked(object sender, EventArgs e)
        {
            if (!ValidateForm())
            {
                return;
            }

            try
            {
                var sessionCookie = Preferences.Get("session_id", string.Empty);
                if (string.IsNullOrEmpty(sessionCookie))
                {
                    await DisplayAlert("Errore", "La sessione è scaduta. Effettua nuovamente il login.", "OK");
                    return;
                }

                var post = CreatePostObject();
                var request = CreatePostRequest(post, sessionCookie);

                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await ShowSuccessMessage(post);
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

        // ========== HELPER METHODS ==========

        // Aggiorna il contatore caratteri per un campo di testo
        private void UpdateCharacterCount(string text, Label label, int maxLength)
        {
            var remainingChars = maxLength - (text?.Length ?? 0);
            label.Text = $"Caratteri rimanenti: {remainingChars}";
        }

        // Valida tutti i campi del form
        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TitoloEntry.Text) ||
                string.IsNullOrWhiteSpace(ProvinciaEntry.Text) ||
                string.IsNullOrWhiteSpace(CittaEntry.Text) ||
                SportPicker.SelectedItem == null ||
                LivelloPicker.SelectedItem == null ||
                string.IsNullOrWhiteSpace(CommentoEditor.Text))
            {
                DisplayAlert("Errore", "Tutti i campi sono obbligatori", "OK");
                return false;
            }

            if (!ProvinceOptions.Contains(ProvinciaEntry.Text))
            {
                DisplayAlert("Errore", "Seleziona una provincia valida dalla lista", "OK");
                return false;
            }

            return true;
        }

        // Crea l'oggetto post con tutti i dati
        private object CreatePostObject()
        {
            var formattedTime = OraPartitaPicker.Time.ToString(@"hh\:mm");

            return new
            {
                titolo = TitoloEntry.Text,
                provincia = ProvinciaEntry.Text,
                citta = CittaEntry.Text,
                sport = SportPicker.SelectedItem.ToString(),
                data_partita = DataPartitaPicker.Date.ToString("dd-MM-yyyy"),
                ora_partita = formattedTime,
                commento = CommentoEditor.Text,
                campo_id = SelectedSportField?.Id,
                livello = LivelloPicker.SelectedItem?.ToString() ?? "Intermedio",
                numero_giocatori = NumeroGiocatori
            };
        }

        // Crea la richiesta HTTP per creare il post
        private HttpRequestMessage CreatePostRequest(object post, string sessionCookie)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, $"{ApiConfig.PythonApiUrl}/posts/");
            request.Headers.Add("Cookie", $"session_id={sessionCookie}");

            var json = JsonSerializer.Serialize(post);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            return request;
        }

        // Mostra il messaggio di successo dopo la creazione
        private async Task ShowSuccessMessage(object post)
        {
            var giocatoriText = NumeroGiocatori == 1 ? "1 giocatore" : $"{NumeroGiocatori} giocatori";
            var successMessage = SelectedSportField != null
                ? $"Il tuo post è stato creato!\n\nCampo selezionato: {SelectedSportField.Nome}\nLivello: {((dynamic)post).livello}\nCerchi: {giocatoriText}"
                : $"Il tuo post è stato creato!\n\nLivello: {((dynamic)post).livello}\nCerchi: {giocatoriText}";

            await DisplayAlert("Post creato", successMessage, "OK");
        }

        // Implementazione INotifyPropertyChanged
        public new event PropertyChangedEventHandler PropertyChanged;

        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}