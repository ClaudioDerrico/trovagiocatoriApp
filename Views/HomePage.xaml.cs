using trovagiocatoriApp.ViewModels;
using Microsoft.Maui.Storage; // Questo namespace potrebbe non essere più necessario qui se non usato direttamente
using trovagiocatoriApp.Views; // Assicurati che questo using sia presente

namespace trovagiocatoriApp.Views;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();

        bool hasSession = Preferences.ContainsKey("session_id") &&
                          !string.IsNullOrEmpty(Preferences.Get("session_id", ""));

        if (hasSession)
        {
            BindingContext = new HomePageViewModel();
        }
        else
        {
            BindingContext = new NavigationPage(new LoginPage());
        }
    }

    private async void PostPageClicked(object sender, EventArgs e)
    {
        var region = RegionPicker.SelectedItem?.ToString();
        var province = ProvincePicker.SelectedItem?.ToString();
        var sport = SportPicker.SelectedItem?.ToString();

        if (string.IsNullOrEmpty(province) || string.IsNullOrEmpty(sport) || string.IsNullOrEmpty(region))
        {
            ErrorLabel.IsVisible = true;
            ErrorLabel.Text = "Compilare Regione, Provincia e Sport.";
            return;
        }
        ErrorLabel.IsVisible = false;

        // USA NAVIGATION TRADIZIONALE
        await Navigation.PushAsync(new PostPage(province, sport));
    }

    private async void OnCreatePostButtonClicked(object sender, EventArgs e)
    {
        // USA NAVIGATION TRADIZIONALE
        await Navigation.PushAsync(new CreatePostPage());
    }

    private async void OnAboutAppTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutAppPage());
    }

    // NUOVO: Gestione navigazione alla pagina Amici
    private async void OnFriendsPageTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new FriendsPage());
    }

    private async void ProfilePageTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }
}