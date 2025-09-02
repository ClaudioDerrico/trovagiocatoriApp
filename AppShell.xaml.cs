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


        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Sei sicuro di voler effettuare il logout?", "Sì", "No");
            if (confirm)
            {
                // Pulisci TUTTE le preferences relative alla sessione
                Preferences.Clear(); // O più specificamente:
                                     // Preferences.Remove("session_id");

                Debug.WriteLine($"[LOGOUT] Session cleared");

                Application.Current.MainPage = new NavigationPage(new LoginPage());
                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }
    }
}