using trovagiocatoriApp.ViewModels;
using Microsoft.Maui.Storage;

namespace trovagiocatoriApp.Views;
//Commit di prova
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
        Application.Current.MainPage.Window.Width =1000;

    }



    private async void PostPageClicked(object sender, EventArgs e)
    {
        // Recupera i valori selezionati nei Picker
        var province = ProvincePicker.SelectedItem?.ToString();
        var sport = SportPicker.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(sport))
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = "Compilare tutti i campi";
            return;
        }
        ErrorLabel.IsVisible = false;

        //Naviga alla pagina PostsListPage passando i criteri selezionati
        await Navigation.PushAsync(new PostPage(province, sport));
    }


    private async void OnCreatePostButtonClicked(object sender, EventArgs e)
    {
        // Naviga alla pagina CreatePostPage
        await Navigation.PushAsync(new CreatePostPage());
    }




}
