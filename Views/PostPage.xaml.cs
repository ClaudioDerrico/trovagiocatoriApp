using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Controls;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Views
{
    public partial class PostPage : ContentPage
    {
        private readonly string _selectedProvince;
        private readonly string _selectedSport;
        private List<PostResponse> _allPosts = new List<PostResponse>();
        private List<PostResponse> _filteredPosts = new List<PostResponse>();

        public PostPage(string province, string sport)
        {
            InitializeComponent();
            _selectedProvince = province;
            _selectedSport = sport;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            TitleLabel.Text = $"{_selectedProvince} - {_selectedSport}";
            await LoadPostsAsync();
        }

        // Carica tutti i post che corrispondono ai criteri di ricerca
        private async Task LoadPostsAsync()
        {
            try
            {
                using var client = new HttpClient();

                var url = $"{ApiConfig.PythonApiUrl}/posts/search?provincia={Uri.EscapeDataString(_selectedProvince)}&sport={Uri.EscapeDataString(_selectedSport)}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var jsonElements = JsonSerializer.Deserialize<List<JsonElement>>(json);

                    _allPosts = ParsePostsFromJson(jsonElements);
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

        // Converte gli elementi JSON in oggetti PostResponse
        private List<PostResponse> ParsePostsFromJson(List<JsonElement> jsonElements)
        {
            return jsonElements.Select(post => new PostResponse
            {
                id = GetIntProperty(post, "id"),
                titolo = GetStringProperty(post, "titolo"),
                provincia = GetStringProperty(post, "provincia"),
                citta = GetStringProperty(post, "citta"),
                sport = GetStringProperty(post, "sport"),
                data_partita = GetStringProperty(post, "data_partita"),
                ora_partita = GetStringProperty(post, "ora_partita"),
                commento = GetStringProperty(post, "commento"),
                autore_email = GetStringProperty(post, "autore_email"),
                campo_id = GetNullableIntProperty(post, "campo_id"),
                campo = GetCampoProperty(post),
                livello = GetStringProperty(post, "livello", "Intermedio"),
                numero_giocatori = GetIntProperty(post, "numero_giocatori", 1),
                partecipanti_iscritti = GetIntProperty(post, "partecipanti_iscritti", 0),
                posti_disponibili = GetIntProperty(post, "posti_disponibili", 1)
            }).ToList();
        }

        // ========== EVENT HANDLERS ==========

        // Mostra opzioni di filtro per livello
        private async void OnFilterButtonClicked(object sender, EventArgs e)
        {
            string action = await DisplayActionSheet("Filtra per livello", "Annulla", null,
                "Tutti i livelli", "🟢 Principiante", "🟡 Intermedio", "🔴 Avanzato");

            if (action != null && action != "Annulla")
            {
                ApplyLevelFilter(action);
            }
        }

        // Applica il filtro per livello selezionato
        private void ApplyLevelFilter(string selectedFilter)
        {
            _filteredPosts = selectedFilter switch
            {
                "Tutti i livelli" => new List<PostResponse>(_allPosts),
                "🟢 Principiante" => _allPosts.Where(p => p.livello == "Principiante").ToList(),
                "🟡 Intermedio" => _allPosts.Where(p => p.livello == "Intermedio").ToList(),
                "🔴 Avanzato" => _allPosts.Where(p => p.livello == "Avanzato").ToList(),
                _ => new List<PostResponse>(_allPosts)
            };

            PostsCollectionView.ItemsSource = _filteredPosts;

            // Aggiorna il titolo per mostrare il filtro applicato
            var filterText = selectedFilter == "Tutti i livelli" ? "" : $" - {selectedFilter}";
            TitleLabel.Text = $"{_selectedProvince} - {_selectedSport}{filterText}";
        }

        // Filtra i post in base al testo di ricerca
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

        // Gestisce il pull-to-refresh
        private async void OnRefreshing(object sender, EventArgs e)
        {
            await LoadPostsAsync();
            PostsRefreshView.IsRefreshing = false;
        }

        // Naviga alla pagina di creazione post
        private async void OnAddPostClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new CreatePostPage());
        }

        // Gestisce la selezione di un post
        private async void OnPostSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is PostResponse selectedPost)
            {
                PostsCollectionView.SelectedItem = null;
                await Navigation.PushAsync(new PostDetailMainPage(selectedPost.id));
            }
        }

        // ========== HELPER METHODS ==========

        // Metodi helper per estrarre proprietà dal JsonElement in modo sicuro
        private string GetStringProperty(JsonElement element, string propertyName, string defaultValue = "")
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetString() ?? defaultValue
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private int GetIntProperty(JsonElement element, string propertyName, int defaultValue = 0)
        {
            try
            {
                return element.TryGetProperty(propertyName, out var prop) && prop.ValueKind != JsonValueKind.Null
                    ? prop.GetInt32()
                    : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        private int? GetNullableIntProperty(JsonElement element, string propertyName)
        {
            try
            {
                if (element.TryGetProperty(propertyName, out var prop))
                {
                    if (prop.ValueKind == JsonValueKind.Null)
                        return null;
                    return prop.GetInt32();
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private CampoInfo GetCampoProperty(JsonElement element)
        {
            try
            {
                if (element.TryGetProperty("campo", out var campoProp) && campoProp.ValueKind != JsonValueKind.Null)
                {
                    return new CampoInfo
                    {
                        nome = GetStringProperty(campoProp, "nome"),
                        indirizzo = GetStringProperty(campoProp, "indirizzo")
                    };
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}