using System.Windows.Input;

namespace trovagiocatoriApp.ViewModels;

public class HomePageViewModel : BaseViewModel
{
    public ICommand NavigateToSearchCommand { get; }
    public ICommand NavigateToChatCommand { get; }
    public ICommand StartGameCommand { get; } // Aggiunto comando per il pulsante "Inizia a giocare"

    public HomePageViewModel()
    {
        NavigateToSearchCommand = new Command(async () => await NavigateToSearch());
        NavigateToChatCommand = new Command(async () => await NavigateToChat());
        StartGameCommand = new Command(async () => await StartGame()); // Inizializza il comando
    }

    private async Task NavigateToSearch() =>
        await Shell.Current.GoToAsync("//RegionForm");

    private async Task NavigateToChat() =>
        await Shell.Current.GoToAsync("//ChatPage");

    private async Task StartGame()
    {
        // Logica per il pulsante "Inizia a giocare"
        await App.Current.MainPage.DisplayAlert(
            "Inizia a giocare",
            "Comincia a selezionare uno sport e la tua provincia!",
            "OK");
    }
}
