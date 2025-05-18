using trovagiocatoriApp.ViewModels;
using Microsoft.Maui.Storage; // Questo namespace potrebbe non essere più necessario qui se non usato direttamente
using trovagiocatoriApp.Views; // Assicurati che questo using sia presente

namespace trovagiocatoriApp.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
        BindingContext = new HomePageViewModel();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // Impostare la dimensione per la pagina di login (1000x800)
        // Nota: Questa logica è più adatta per applicazioni Desktop.
        // Su mobile, l'app occupa solitamente l'intero schermo.
        if (Application.Current.MainPage?.Window != null) // Aggiunto controllo per nullità
        {
            Application.Current.MainPage.Window.Height = 800;
            Application.Current.MainPage.Window.Width = 1000;
        }
    }

    private async void PostPageClicked(object sender, EventArgs e)
    {
        var region = RegionPicker.SelectedItem?.ToString(); // Ho aggiunto anche la regione, se serve
        var province = ProvincePicker.SelectedItem?.ToString();
        var sport = SportPicker.SelectedItem?.ToString();

        // Potresti voler validare anche la regione se diventa obbligatoria
        if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(sport) || string.IsNullOrEmpty(region))
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = "Compilare Regione, Provincia e Sport.";
            return;
        }
        ErrorLabel.IsVisible = false;

        // Naviga alla pagina PostsListPage passando i criteri selezionati
        // Assumendo che PostPage accetti anche la regione
        await Navigation.PushAsync(new PostPage(province, sport)); // Modifica PostPage se devi passare anche la regione
    }

    private async void OnCreatePostButtonClicked(object sender, EventArgs e)
    {
        // Naviga alla pagina CreatePostPage
        await Navigation.PushAsync(new CreatePostPage());
    }

    // NUOVI METODI PER LA NAVIGAZIONE
    private async void OnAboutAppTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutAppPage());
    }

    private async void ProfilePageTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }
}