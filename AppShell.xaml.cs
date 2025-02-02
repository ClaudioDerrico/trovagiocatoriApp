using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using System.Threading.Tasks;
using trovagiocatoriApp.Views;

namespace trovagiocatoriApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            // Registra le rotte
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool hasSession = Preferences.ContainsKey("session_id") &&
                              !string.IsNullOrEmpty(Preferences.Get("session_id", ""));
            // Se non c'è sessione, naviga alla LoginPage (utilizziamo percorso relativo)
            if (!hasSession)
            {
                await Shell.Current.GoToAsync("LoginPage");
            }
        }


        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Sei sicuro di voler effettuare il logout?", "Sì", "No");
            if (confirm)
            {
                if (Preferences.ContainsKey("session_id"))
                {
                    Preferences.Remove("session_id");
                }
                Debug.WriteLine($"Session_id AFTER REMOVE: {Preferences.Get("session_id", "")}");


                Application.Current.MainPage = new NavigationPage(new LoginPage());

                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }
    }
}
