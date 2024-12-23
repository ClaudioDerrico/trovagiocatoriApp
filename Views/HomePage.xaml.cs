using trovagiocatoriApp.ViewModels;

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

        Application.Current.MainPage.Window.Height = 800;
        Application.Current.MainPage.Window.Width = 1000;

    }
}
