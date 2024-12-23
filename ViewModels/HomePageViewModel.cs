using System.Collections.ObjectModel;
using System.Windows.Input;

namespace trovagiocatoriApp.ViewModels
{
    public class HomePageViewModel : BaseViewModel
    {
        private string _selectedRegion;
        private string _selectedProvince;
        private string _selectedSport;

        public ObservableCollection<string> Regions { get; } = new ObservableCollection<string>
        {
            "Abruzzo", "Basilicata", "Calabria", "Campania", "Emilia-Romagna", "Friuli-Venezia Giulia",
            "Lazio", "Liguria", "Lombardia", "Marche", "Molise", "Piemonte", "Puglia", "Sardegna",
            "Sicilia", "Toscana", "Trentino-Alto Adige", "Umbria", "Valle d'Aosta", "Veneto"
        };

        public ObservableCollection<string> Provinces { get; private set; } = new ObservableCollection<string>();

        public ObservableCollection<string> Sports { get; } = new ObservableCollection<string>
        {
            "Calcio", "Tennis", "Pallavolo", "Basket", "Padel"
        };

        public string SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                if (_selectedRegion != value)
                {
                    _selectedRegion = value;
                    OnPropertyChanged();
                    UpdateProvinces();
                }
            }
        }

        public string SelectedProvince
        {
            get => _selectedProvince;
            set
            {
                if (_selectedProvince != value)
                {
                    _selectedProvince = value;
                    OnPropertyChanged();
                    UpdateSportVisibility();
                }
            }
        }

        public string SelectedSport
        {
            get => _selectedSport;
            set
            {
                if (_selectedSport != value)
                {
                    _selectedSport = value;
                    OnPropertyChanged();
                    UpdatePlayButton();
                }
            }
        }

        public bool IsProvincePickerVisible => !string.IsNullOrEmpty(SelectedRegion);
        public bool IsSportPickerVisible => !string.IsNullOrEmpty(SelectedProvince);
        public bool IsPlayButtonEnabled => !string.IsNullOrEmpty(SelectedSport);

        public ICommand PlayCommand { get; }

        public HomePageViewModel()
        {

            PlayCommand = new Command(OnPlay);
        }

        private void UpdateProvinces()
        {
            Provinces.Clear();

            // Province suddivise per regione
            switch (SelectedRegion)
            {
                case "Abruzzo":
                    Provinces.Add("L'Aquila");
                    Provinces.Add("Teramo");
                    Provinces.Add("Pescara");
                    Provinces.Add("Chieti");
                    break;
                case "Basilicata":
                    Provinces.Add("Potenza");
                    Provinces.Add("Matera");
                    break;
                case "Calabria":
                    Provinces.Add("Catanzaro");
                    Provinces.Add("Cosenza");
                    Provinces.Add("Crotone");
                    Provinces.Add("Reggio Calabria");
                    Provinces.Add("Vibo Valentia");
                    break;
                case "Campania":
                    Provinces.Add("Napoli");
                    Provinces.Add("Salerno");
                    Provinces.Add("Caserta");
                    Provinces.Add("Avellino");
                    Provinces.Add("Benevento");
                    break;
                case "Emilia-Romagna":
                    Provinces.Add("Bologna");
                    Provinces.Add("Parma");
                    Provinces.Add("Modena");
                    Provinces.Add("Ravenna");
                    Provinces.Add("Ferrara");
                    Provinces.Add("Forlì-Cesena");
                    Provinces.Add("Rimini");
                    break;
                case "Friuli-Venezia Giulia":
                    Provinces.Add("Trieste");
                    Provinces.Add("Udine");
                    Provinces.Add("Pordenone");
                    Provinces.Add("Gorizia");
                    break;
                case "Lazio":
                    Provinces.Add("Roma");
                    Provinces.Add("Viterbo");
                    Provinces.Add("Rieti");
                    Provinces.Add("Latina");
                    Provinces.Add("Frosinone");
                    break;
                case "Liguria":
                    Provinces.Add("Genova");
                    Provinces.Add("Imperia");
                    Provinces.Add("La Spezia");
                    Provinces.Add("Savona");
                    break;
                case "Lombardia":
                    Provinces.Add("Milano");
                    Provinces.Add("Bergamo");
                    Provinces.Add("Brescia");
                    Provinces.Add("Como");
                    Provinces.Add("Cremona");
                    Provinces.Add("Lecco");
                    Provinces.Add("Lodi");
                    Provinces.Add("Mantova");
                    Provinces.Add("Monza e Brianza");
                    Provinces.Add("Pavia");
                    Provinces.Add("Sondrio");
                    Provinces.Add("Varese");
                    break;
                case "Marche":
                    Provinces.Add("Ancona");
                    Provinces.Add("Ascoli Piceno");
                    Provinces.Add("Fermo");
                    Provinces.Add("Macerata");
                    Provinces.Add("Pesaro e Urbino");
                    break;
                case "Molise":
                    Provinces.Add("Campobasso");
                    Provinces.Add("Isernia");
                    break;
                case "Piemonte":
                    Provinces.Add("Torino");
                    Provinces.Add("Alessandria");
                    Provinces.Add("Asti");
                    Provinces.Add("Biella");
                    Provinces.Add("Cuneo");
                    Provinces.Add("Novara");
                    Provinces.Add("Verbano-Cusio-Ossola");
                    Provinces.Add("Vercelli");
                    break;
                case "Puglia":
                    Provinces.Add("Bari");
                    Provinces.Add("Brindisi");
                    Provinces.Add("Foggia");
                    Provinces.Add("Lecce");
                    Provinces.Add("Taranto");
                    Provinces.Add("Barletta-Andria-Trani");
                    break;
                case "Sardegna":
                    Provinces.Add("Cagliari");
                    Provinces.Add("Sassari");
                    Provinces.Add("Nuoro");
                    Provinces.Add("Oristano");
                    Provinces.Add("Sud Sardegna");
                    break;
                case "Sicilia":
                    Provinces.Add("Palermo");
                    Provinces.Add("Agrigento");
                    Provinces.Add("Caltanissetta");
                    Provinces.Add("Catania");
                    Provinces.Add("Enna");
                    Provinces.Add("Messina");
                    Provinces.Add("Ragusa");
                    Provinces.Add("Siracusa");
                    Provinces.Add("Trapani");
                    break;
                case "Toscana":
                    Provinces.Add("Firenze");
                    Provinces.Add("Arezzo");
                    Provinces.Add("Grosseto");
                    Provinces.Add("Livorno");
                    Provinces.Add("Lucca");
                    Provinces.Add("Massa-Carrara");
                    Provinces.Add("Pisa");
                    Provinces.Add("Pistoia");
                    Provinces.Add("Prato");
                    Provinces.Add("Siena");
                    break;
                case "Trentino-Alto Adige":
                    Provinces.Add("Trento");
                    Provinces.Add("Bolzano");
                    break;
                case "Umbria":
                    Provinces.Add("Perugia");
                    Provinces.Add("Terni");
                    break;
                case "Valle d'Aosta":
                    Provinces.Add("Aosta");
                    break;
                case "Veneto":
                    Provinces.Add("Venezia");
                    Provinces.Add("Belluno");
                    Provinces.Add("Padova");
                    Provinces.Add("Rovigo");
                    Provinces.Add("Treviso");
                    Provinces.Add("Verona");
                    Provinces.Add("Vicenza");
                    break;
                default:
                    break;
            }

            OnPropertyChanged(nameof(Provinces));
            OnPropertyChanged(nameof(IsProvincePickerVisible));
        }

        private void UpdateSportVisibility()
        {
            OnPropertyChanged(nameof(IsSportPickerVisible));
        }

        private void UpdatePlayButton()
        {
            OnPropertyChanged(nameof(IsPlayButtonEnabled));
        }

        private void OnPlay()
        {
            App.Current.MainPage.DisplayAlert("Gioca", $"Regione: {SelectedRegion}\nProvincia: {SelectedProvince}\nSport: {SelectedSport}", "OK");
        }
    }
}
