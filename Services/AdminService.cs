using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Services
{
    public interface IAdminService
    {
        Task<AdminStats> GetStatsAsync();
        Task<List<AdminPostInfo>> GetAllPostsAsync();
        Task<List<AdminCommentInfo>> GetAllCommentsAsync();
        Task<List<AdminUserInfo>> GetAllUsersAsync();
        Task<bool> DeletePostAsync(int postId);
        Task<bool> DeleteCommentAsync(int commentId);
        Task<bool> ToggleUserStatusAsync(int userId);
    }

    public class AdminService : IAdminService
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl = ApiConfig.BaseUrl;
        private readonly string _pythonApiUrl = ApiConfig.PythonApiUrl;

        public AdminService()
        {
            _client = new HttpClient();
        }

        private HttpRequestMessage CreateAuthenticatedRequest(HttpMethod method, string url)
        {
            var request = new HttpRequestMessage(method, url);

            if (Preferences.ContainsKey("session_id"))
            {
                string sessionId = Preferences.Get("session_id", "");
                request.Headers.Add("Cookie", $"session_id={sessionId}");
            }

            return request;
        }

        public async Task<AdminStats> GetStatsAsync()
        {
            try
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento statistiche...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_pythonApiUrl}/admin/dashboard-stats");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var stats = JsonSerializer.Deserialize<AdminStats>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Statistiche caricate: {stats.TotalPosts} post");
                    return stats;
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API: {response.StatusCode}");
                    return GetFallbackStats();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore caricamento stats: {ex.Message}");
                return GetFallbackStats();
            }
        }

        public async Task<List<AdminPostInfo>> GetAllPostsAsync()
        {
            try
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento post...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_pythonApiUrl}/admin/posts");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var posts = JsonSerializer.Deserialize<List<AdminPostInfo>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Caricati {posts?.Count ?? 0} post");
                    return posts ?? new List<AdminPostInfo>();
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API posts: {response.StatusCode}");
                    return new List<AdminPostInfo>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore caricamento posts: {ex.Message}");
                return new List<AdminPostInfo>();
            }
        }

        public async Task<List<AdminCommentInfo>> GetAllCommentsAsync()
        {
            try
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento commenti...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_pythonApiUrl}/admin/comments");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var comments = JsonSerializer.Deserialize<List<AdminCommentInfo>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Caricati {comments?.Count ?? 0} commenti");
                    return comments ?? new List<AdminCommentInfo>();
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API comments: {response.StatusCode}");
                    return new List<AdminCommentInfo>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore caricamento comments: {ex.Message}");
                return new List<AdminCommentInfo>();
            }
        }

        public async Task<List<AdminUserInfo>> GetAllUsersAsync()
        {
            try
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento utenti...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_baseUrl}/admin/users");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<AdminUserInfo>>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Caricati {users?.Count ?? 0} utenti");
                    return users ?? new List<AdminUserInfo>();
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API users: {response.StatusCode}");
                    return new List<AdminUserInfo>();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore caricamento users: {ex.Message}");
                return new List<AdminUserInfo>();
            }
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            try
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione post {postId}...");

                var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"{_baseUrl}/admin/posts/{postId}");
                var response = await _client.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione post {postId}: {(success ? "✅" : "❌")}");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore eliminazione post {postId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            try
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione commento {commentId}...");

                var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"{_baseUrl}/admin/comments/{commentId}");
                var response = await _client.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione commento {commentId}: {(success ? "✅" : "❌")}");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore eliminazione commento {commentId}: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            try
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Toggle status utente {userId}...");

                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"{_baseUrl}/admin/users/{userId}/toggle-status");
                var response = await _client.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ADMIN_SERVICE] Toggle status utente {userId}: {(success ? "✅" : "❌")}");

                return success;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore toggle status utente {userId}: {ex.Message}");
                return false;
            }
        }

 
    }
}