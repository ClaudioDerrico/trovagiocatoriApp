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

            // Registra tutte le route necessarie
            Routing.RegisterRoute("LoginPage", typeof(LoginPage));
            Routing.RegisterRoute("RegisterPage", typeof(RegisterPage));
            Routing.RegisterRoute("HomePage", typeof(HomePage));
            Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
            Routing.RegisterRoute("PostPage", typeof(PostPage));
            Routing.RegisterRoute("PostDetailPage", typeof(PostDetailPage));
            Routing.RegisterRoute("CreatePostPage", typeof(CreatePostPage));
            Routing.RegisterRoute("AboutAppPage", typeof(AboutAppPage));
            Routing.RegisterRoute("ChangePasswordPage", typeof(ChangePasswordPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            bool hasSession = Preferences.ContainsKey("session_id") &&
                              !string.IsNullOrEmpty(Preferences.Get("session_id", ""));

            // Se non c'è sessione, naviga alla LoginPage
            if (!hasSession)
            {
                // Usa MainPage invece della navigazione relativa per evitare problemi
                Application.Current.MainPage = new NavigationPage(new LoginPage());
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