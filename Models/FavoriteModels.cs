namespace trovagiocatoriApp.Models
{
    public class FavoriteRequest
    {
        public int post_id { get; set; }
    }

    public class FavoriteResponse
    {
        public bool success { get; set; }
        public bool is_favorite { get; set; }
    }

    public class FavoritesListResponse
    {
        public bool success { get; set; }
        public List<int> favorites { get; set; }
    }
}