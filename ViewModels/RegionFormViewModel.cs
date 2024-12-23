using System.Collections.ObjectModel;
using System.Windows.Input;

namespace trovagiocatoriApp.ViewModels;

public class RegionFormViewModel : BaseViewModel
{
    public ObservableCollection<string> Regions { get; }
    public ObservableCollection<string> Provinces { get; }
    public ObservableCollection<string> Sports { get; }

    private string _selectedRegion;
    public string SelectedRegion
    {
        get => _selectedRegion;
        set
        {
            if (SetProperty(ref _selectedRegion, value))
            {
                UpdateProvinces(value);
            }
        }
    }

    private string _selectedProvince;
    public string SelectedProvince
    {
        get => _selectedProvince;
        set => SetProperty(ref _selectedProvince, value);
    }

    private string _selectedSport;
    public string SelectedSport
    {
        get => _selectedSport;
        set => SetProperty(ref _selectedSport, value);
    }

    // Comando per il pulsante "Gioca"
    public ICommand PlayCommand { get; }

    public RegionFormViewModel()
    {
        // Inizializza le liste delle regioni, province e sport
        Regions = new ObservableCollection<string>(Models.RegionsData.Regions.Keys);
        Provinces = new ObservableCollection<string>();
        Sports = new ObservableCollection<string>(Models.RegionsData.Sports);

        // Inizializza il comando per il pulsante
        PlayCommand = new Command(OnPlay);
    }

    private void UpdateProvinces(string region)
    {
        Provinces.Clear();
        if (Models.RegionsData.Regions.ContainsKey(region))
        {
            foreach (var province in Models.RegionsData.Regions[region])
            {
                Provinces.Add(province);
            }
        }
    }

    private async void OnPlay()
    {
        // Logica per il pulsante "Gioca"
        if (!string.IsNullOrEmpty(SelectedRegion) &&
            !string.IsNullOrEmpty(SelectedProvince) &&
            !string.IsNullOrEmpty(SelectedSport))
        {
            await App.Current.MainPage.DisplayAlert(
                "Selezione Completata",
                $"Regione: {SelectedRegion}\nProvincia: {SelectedProvince}\nSport: {SelectedSport}",
                "OK");
        }
        else
        {
            await App.Current.MainPage.DisplayAlert(
                "Errore",
                "Assicurati di selezionare una Regione, una Provincia e uno Sport.",
                "OK");
        }
    }
}
