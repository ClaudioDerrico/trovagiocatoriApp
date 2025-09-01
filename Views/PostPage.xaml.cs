using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostPage : ContentPage
    {
        private readonly string SelectedProvince;
        private readonly string SelectedSport;

        public PostPage(string province, string sport)
        {
            InitializeComponent();
            SelectedProvince = province;
            SelectedSport = sport;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            TitleLabel.Text = $"{SelectedProvince} - {SelectedSport}";
            await LoadPostsAsync();
        }



        private async Task LoadPostsAsync()
        {
            try
            {
                using var client = new HttpClient();
                var url = $"http://localhost:8000/posts/search?provincia={Uri.EscapeDataString(SelectedProvince)}&sport={Uri.EscapeDataString(SelectedSport)}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var posts = JsonSerializer.Deserialize<List<PostResponse>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    PostsCollectionView.ItemsSource = posts;
                }
                else
                {
                    await DisplayAlert("Nessun risultato", "Nessun post trovato.", "OK");
                    PostsCollectionView.ItemsSource = null;
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore: {ex.Message}", "OK");
            }
        }

        private async void OnPostTapped(object sender, ItemTappedEventArgs e)
        {
            if (e.Item is PostResponse post)
            {
                ((ListView)sender).SelectedItem = null;  // deseleziona
                await Navigation.PushAsync(new PostDetailPage(post.id));
            }
        }


        private void OnFilterButtonClicked(object sender, EventArgs e)
        {
            // Implementa la logica di filtro
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            // Implementa la logica di ricerca
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            // Aggiorna i dati
            await LoadPostsAsync();
            PostsRefreshView.IsRefreshing = false;
        }

        private async void OnAddPostClicked(object sender, EventArgs e)
        {
            // CAMBIA DA Shell.GoToAsync A Navigation.PushAsync
            await Navigation.PushAsync(new CreatePostPage());
        }

        private async void OnPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                PostsCollectionView.SelectedItem = null;

                // QUESTO È GIÀ CORRETTO - mantienilo così
                await Navigation.PushAsync(new PostDetailPage(selectedPost.id));
            }
        }
    }
}
