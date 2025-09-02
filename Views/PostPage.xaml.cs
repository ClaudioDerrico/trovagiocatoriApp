using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using trovagiocatoriApp.Models;
using System.Linq;

namespace trovagiocatoriApp.Views
{
    public partial class PostPage : ContentPage
    {
        private readonly string SelectedProvince;
        private readonly string SelectedSport;
        private List<PostResponse> _allPosts = new List<PostResponse>();
        private List<PostResponse> _filteredPosts = new List<PostResponse>();

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
                var url = $"{ApiConfig.PythonApiUrl}/posts/search?provincia={Uri.EscapeDataString(SelectedProvince)}&sport={Uri.EscapeDataString(SelectedSport)}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var posts = JsonSerializer.Deserialize<List<PostResponse>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _allPosts = posts ?? new List<PostResponse>();
                    _filteredPosts = new List<PostResponse>(_allPosts);
                    PostsCollectionView.ItemsSource = _filteredPosts;
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

        private async void OnFilterButtonClicked(object sender, EventArgs e)
        {
            // Mostra un action sheet per filtrare per livello
            string action = await DisplayActionSheet("Filtra per livello", "Annulla", null,
                "Tutti i livelli", "🟢 Principiante", "🟡 Intermedio", "🔴 Avanzato");

            if (action != null && action != "Annulla")
            {
                ApplyLevelFilter(action);
            }
        }

        private void ApplyLevelFilter(string selectedFilter)
        {
            switch (selectedFilter)
            {
                case "Tutti i livelli":
                    _filteredPosts = new List<PostResponse>(_allPosts);
                    break;
                case "🟢 Principiante":
                    _filteredPosts = _allPosts.Where(p => p.livello == "Principiante").ToList();
                    break;
                case "🟡 Intermedio":
                    _filteredPosts = _allPosts.Where(p => p.livello == "Intermedio").ToList();
                    break;
                case "🔴 Avanzato":
                    _filteredPosts = _allPosts.Where(p => p.livello == "Avanzato").ToList();
                    break;
            }

            PostsCollectionView.ItemsSource = _filteredPosts;

            // Aggiorna il titolo per mostrare il filtro applicato
            var filterText = selectedFilter == "Tutti i livelli" ? "" : $" - {selectedFilter}";
            TitleLabel.Text = $"{SelectedProvince} - {SelectedSport}{filterText}";
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? "";

            if (string.IsNullOrWhiteSpace(searchText))
            {
                PostsCollectionView.ItemsSource = _filteredPosts;
            }
            else
            {
                var searchResults = _filteredPosts.Where(p =>
                    p.titolo.ToLower().Contains(searchText) ||
                    p.commento.ToLower().Contains(searchText) ||
                    p.citta.ToLower().Contains(searchText)
                ).ToList();

                PostsCollectionView.ItemsSource = searchResults;
            }
        }

        private async void OnRefreshing(object sender, EventArgs e)
        {
            // Aggiorna i dati
            await LoadPostsAsync();
            PostsRefreshView.IsRefreshing = false;
        }

        private async void OnAddPostClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreatePostPage());
        }

        private async void OnPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                PostsCollectionView.SelectedItem = null;
                await Navigation.PushAsync(new PostDetailPage(selectedPost.id));
            }
        }
    }
}