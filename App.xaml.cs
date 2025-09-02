using trovagiocatoriApp.Views;

namespace trovagiocatoriApp
{
    public partial class App : Application
    {
        // Dimensioni smartphone
        private const int WINDOW_WIDTH = 500;
        private const int WINDOW_HEIGHT = 850;

        public App()
        {
            InitializeComponent();

            // Mostro splash iniziale
            MainPage = new SplashPage();

            // Avvio la logica asincrona
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            bool valid = await IsSessionValid();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (valid)
                {
                    MainPage = new AppShell();
                }
                else
                {
                    MainPage = new NavigationPage(new LoginPage());
                }

                // Imposta le dimensioni una volta sola
                SetSmartphoneSize();
            });
        }

        private async Task<bool> IsSessionValid()
        {
            string sessionId = Preferences.Get("session_id", "");
            if (string.IsNullOrEmpty(sessionId))
                return false;

            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiConfig.BaseUrl}/profile");
                request.Headers.Add("Cookie", $"session_id={sessionId}");

                var response = await client.SendAsync(request);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // UN SOLO METODO - privato, chiamato solo una volta all'avvio
        private void SetSmartphoneSize()
        {
            try
            {
                if (MainPage?.Window != null)
                {
                    MainPage.Window.Width = WINDOW_WIDTH;
                    MainPage.Window.Height = WINDOW_HEIGHT;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Errore nell'impostazione dimensioni: {ex.Message}");
            }
        }

        void OnBackButtonClicked(object sender, EventArgs e)
        {
            Shell.Current.GoToAsync("..");
        }
    }
}