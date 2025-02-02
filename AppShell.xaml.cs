using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Views;  // Assicurati che questo namespace sia corretto
using System.Linq;
using System.Diagnostics;

namespace trovagiocatoriApp
{
    public partial class AppShell : Shell
    {
        // Elementi dinamici
        private FlyoutItem loginFlyoutItem;
        private FlyoutItem registerFlyoutItem;
        private FlyoutItem profileFlyoutItem;
        private MenuItem logoutMenuItem;

        public AppShell()
        {
            InitializeComponent();
            InitializeMenuItems();
            UpdateMenuItems();

            // Registra le rotte
            Routing.RegisterRoute(nameof(LoginPage), typeof(LoginPage));
            Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
            Routing.RegisterRoute(nameof(ProfilePage), typeof(ProfilePage));
            Routing.RegisterRoute(nameof(HomePage), typeof(HomePage));
        }

        private void InitializeMenuItems()
        {
            loginFlyoutItem = new FlyoutItem
            {
                Title = "Login",
                Icon = "login_icon.png",
                Items =
                {
                    new ShellContent
                    {
                        ContentTemplate = new DataTemplate(typeof(LoginPage)),
                        Route = "LoginPage"
                    }
                }
            };

            registerFlyoutItem = new FlyoutItem
            {
                Title = "Registrazione",
                Icon = "register_icon.png",
                Items =
                {
                    new ShellContent
                    {
                        ContentTemplate = new DataTemplate(typeof(RegisterPage)),
                        Route = "RegisterPage"
                    }
                }
            };

            profileFlyoutItem = new FlyoutItem
            {
                Title = "Profilo",
                Icon = "profile_icon.png",
                Items =
                {
                    new ShellContent
                    {
                        ContentTemplate = new DataTemplate(typeof(ProfilePage)),
                        Route = "ProfilePage"
                    }
                }
            };

            logoutMenuItem = new MenuItem
            {
                Text = "Logout",
                IconImageSource = "logout_icon.png"
            };
            logoutMenuItem.Clicked += OnLogoutClicked;
        }

        public void UpdateMenuItems()
        {
            if (this.Items.Contains(loginFlyoutItem))
                this.Items.Remove(loginFlyoutItem);
            if (this.Items.Contains(registerFlyoutItem))
                this.Items.Remove(registerFlyoutItem);
            if (this.Items.Contains(profileFlyoutItem))
                this.Items.Remove(profileFlyoutItem);
            if (this.Items.Contains(logoutMenuItem))
                Shell.Current.Items.Remove(logoutMenuItem);


            bool hasSession = Preferences.ContainsKey("session_id") &&
                              !string.IsNullOrEmpty(Preferences.Get("session_id", ""));

            if (hasSession)
            {
                // Se c'è sessione, mostra Profilo e Logout
                this.Items.Add(profileFlyoutItem);
                this.Items.Add(logoutMenuItem);
            }
            else
            {
                this.Items.Add(loginFlyoutItem);
                this.Items.Add(registerFlyoutItem);
            }
        }

        public void RefreshMenu()
        {
            UpdateMenuItems();
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

                RefreshMenu();

                await Shell.Current.GoToAsync("//LoginPage");

                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }
    }
}
