using trovagiocatoriApp.ViewModels;
using Microsoft.Maui.Storage;

namespace trovagiocatoriApp.Views;
//Commit di prova
public partial class HomePage : ContentPage
{
    private HomePageViewModel ViewModel;
    public HomePage()
    {
        InitializeComponent();
        BindingContext = new HomePageViewModel();
        
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Impostare la dimensione per la pagina di login (1000x800)

        Application.Current.MainPage.Window.Height = 800;
        Application.Current.MainPage.Window.Width =1000;

    }



private async void PostPageClicked(object sender, EventArgs e)
{
    var viewModel = BindingContext as HomePageViewModel;

    // Se BindingContext non è impostato correttamente
    if (viewModel == null)
    {
        await DisplayAlert("Errore", "Impossibile trovare il ViewModel", "OK");
        return;
    }

    // Verifica se tutti i campi sono compilati
    if (string.IsNullOrEmpty(viewModel.SelectedRegion) ||
        string.IsNullOrEmpty(viewModel.SelectedProvince) ||
        string.IsNullOrEmpty(viewModel.SelectedSport))
    {
        // Mostra il messaggio di errore sotto il bottone
        ErrorLabel.IsVisible = true;
        ErrorLabel.Text = "Compilare tutti i campi";  // Messaggio di errore
        return;
    }

    // Salva le scelte usando Preferences
    Preferences.Set("provincia", viewModel.SelectedProvince);
    Preferences.Set("opzionesport", viewModel.SelectedSport);

        // Se i campi sono compilati, naviga verso la PostPage
        await Navigation.PushAsync(new PostPage());
    }


    private async void OnCreatePostButtonClicked(object sender, EventArgs e)
    {
        // Naviga alla pagina CreatePostPage
        await Navigation.PushAsync(new CreatePostPage());
    }




}
