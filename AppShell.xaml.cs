using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Linq;

namespace trovagiocatoriApp
{
    public partial class AppShell : Shell
    {
        private MenuItem logoutMenuItem;

        public AppShell()
        {
            InitializeComponent();
            InitializeLogoutMenuItem();
            RefreshMenu();
        }


        private void InitializeLogoutMenuItem()
        {
            logoutMenuItem = new MenuItem
            {
                Text = "Logout",
                IconImageSource = "logout_icon.png",
                IsDestructive = true
            };
            logoutMenuItem.Clicked += OnLogoutClicked;
        }

        /// Legge la session_id dalle Preferences e imposta le voci di menu visibili/invisibili.
        public void RefreshMenu()
        {
            var sessionId = Preferences.Get("session_id", string.Empty);
            bool hasSession = !string.IsNullOrEmpty(sessionId);



            ProfileFlyoutItem.IsVisible = hasSession;

            LoginFlyoutItem.IsVisible = !hasSession;
            RegisterFlyoutItem.IsVisible = !hasSession;

            if (hasSession)
            {
                if (!this.Items.Contains(logoutMenuItem))
                {
                    Console.WriteLine("Aggiungo LogoutMenuItem");
                    this.Items.Add(logoutMenuItem);
                }
            }
            else
            {
                if (this.Items.Contains(logoutMenuItem))
                {
                    Console.WriteLine("Rimuovo LogoutMenuItem");
                    this.Items.Remove(logoutMenuItem);
                }
            }
        }


        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            bool confirm = await DisplayAlert("Logout", "Sei sicuro di voler effettuare il logout?", "Sì", "No");
            if (confirm)
            {

                Preferences.Remove("session_id");

                RefreshMenu();

                await Shell.Current.GoToAsync("//LoginPage");

                await DisplayAlert("Logout", "Sei stato disconnesso con successo.", "OK");
            }
        }
    }
}
