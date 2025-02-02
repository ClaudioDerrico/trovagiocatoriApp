using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;

namespace trovagiocatoriApp.Views
{
    public partial class PostPage : ContentPage
    {
        // Criteri di ricerca
        private string SelectedProvince { get; set; }
        private string SelectedSport { get; set; }

        public PostPage(string province, string sport)
        {
            InitializeComponent();
            SelectedProvince = province;
            SelectedSport = sport;
            TitleLabel.Text = $"{SelectedProvince} - {SelectedSport}";
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPosts();
        }

        // Classe per deserializzare la risposta JSON dal backend
        public class PostResponse
        {
            public int id { get; set; }
            public string titolo { get; set; }
            public string provincia { get; set; }
            public string citta { get; set; }
            public string sport { get; set; }
            public string data_partita { get; set; }
            public string ora_partita { get; set; }
            public string commento { get; set; }
            public string autore_email { get; set; }
        }

        private async Task LoadPosts()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string url = $"http://localhost:8000/posts/search?provincia={Uri.EscapeDataString(SelectedProvince)}&sport={Uri.EscapeDataString(SelectedSport)}";
                    var response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        var posts = JsonSerializer.Deserialize<List<PostResponse>>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (posts != null && posts.Count > 0)
                        {
                            PostsListView.ItemsSource = posts;
                        }

                    }
                    else
                    {
                        await DisplayAlert("Nessun risultato", "Non sono stati trovati post per i criteri selezionati.", "OK");
                        PostsListView.ItemsSource = null;
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Errore", $"Errore durante la ricerca dei post: {ex.Message}", "OK");
            }
        }
    }
}
