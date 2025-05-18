using trovagiocatoriApp.Views;

namespace trovagiocatoriApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            bool hasSession = Preferences.ContainsKey("session_id") &&
                              !string.IsNullOrEmpty(Preferences.Get("session_id", ""));

            if (hasSession)
            {
                MainPage = new AppShell();
            }
            else
            {
                MainPage = new NavigationPage(new LoginPage());
            }
        }

        void OnBackButtonClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("..");
        }

    }
}
