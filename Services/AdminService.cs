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
        Task<AdminResponse<T>> ExecuteAdminActionAsync<T>(Func<Task<T>> action, string operationName);
    }

    public class AdminService : IAdminService, IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl = ApiConfig.BaseUrl;
        private readonly string _pythonApiUrl = ApiConfig.PythonApiUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public AdminService()
        {
            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
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
                    var stats = JsonSerializer.Deserialize<AdminStats>(json, _jsonOptions);

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Statistiche caricate: {stats.TotalPosts} post");
                    return stats;
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API: {response.StatusCode}");
                    return await GetFallbackStatsAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore caricamento stats: {ex.Message}");
                return await GetFallbackStatsAsync();
            }
        }

        public async Task<List<AdminPostInfo>> GetAllPostsAsync()
        {
            return await ExecuteWithFallback(async () =>
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento post...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_pythonApiUrl}/admin/posts");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var posts = JsonSerializer.Deserialize<List<AdminPostInfo>>(json, _jsonOptions);

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Caricati {posts?.Count ?? 0} post");
                    return posts ?? new List<AdminPostInfo>();
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API posts: {response.StatusCode}");
                    return new List<AdminPostInfo>();
                }
            }, "caricamento post");
        }

        public async Task<List<AdminCommentInfo>> GetAllCommentsAsync()
        {
            return await ExecuteWithFallback(async () =>
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento commenti...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_pythonApiUrl}/admin/comments");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var comments = JsonSerializer.Deserialize<List<AdminCommentInfo>>(json, _jsonOptions);

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Caricati {comments?.Count ?? 0} commenti");
                    return comments ?? new List<AdminCommentInfo>();
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API comments: {response.StatusCode}");
                    return new List<AdminCommentInfo>();
                }
            }, "caricamento commenti");
        }

        public async Task<List<AdminUserInfo>> GetAllUsersAsync()
        {
            return await ExecuteWithFallback(async () =>
            {
                Debug.WriteLine("[ADMIN_SERVICE] Caricamento utenti...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_baseUrl}/admin/users");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonSerializer.Deserialize<List<AdminUserInfo>>(json, _jsonOptions);

                    Debug.WriteLine($"[ADMIN_SERVICE] ✅ Caricati {users?.Count ?? 0} utenti");
                    return users ?? new List<AdminUserInfo>();
                }
                else
                {
                    Debug.WriteLine($"[ADMIN_SERVICE] Errore API users: {response.StatusCode}");
                    return new List<AdminUserInfo>();
                }
            }, "caricamento utenti");
        }

        public async Task<bool> DeletePostAsync(int postId)
        {
            return await ExecuteAdminAction(async () =>
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione post {postId}...");

                var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"{_baseUrl}/admin/posts/{postId}");
                var response = await _client.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione post {postId}: {(success ? "✅" : "❌")}");

                return success;
            }, $"eliminazione post {postId}");
        }

        public async Task<bool> DeleteCommentAsync(int commentId)
        {
            return await ExecuteAdminAction(async () =>
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione commento {commentId}...");

                var request = CreateAuthenticatedRequest(HttpMethod.Delete, $"{_baseUrl}/admin/comments/{commentId}");
                var response = await _client.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ADMIN_SERVICE] Eliminazione commento {commentId}: {(success ? "✅" : "❌")}");

                return success;
            }, $"eliminazione commento {commentId}");
        }

        public async Task<bool> ToggleUserStatusAsync(int userId)
        {
            return await ExecuteAdminAction(async () =>
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Toggle status utente {userId}...");

                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"{_baseUrl}/admin/users/{userId}/toggle-status");
                var response = await _client.SendAsync(request);

                var success = response.IsSuccessStatusCode;
                Debug.WriteLine($"[ADMIN_SERVICE] Toggle status utente {userId}: {(success ? "✅" : "❌")}");

                return success;
            }, $"toggle status utente {userId}");
        }

        public async Task<AdminResponse<T>> ExecuteAdminActionAsync<T>(Func<Task<T>> action, string operationName)
        {
            try
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Esecuzione {operationName}...");

                var result = await action();

                return new AdminResponse<T>
                {
                    Success = true,
                    Data = result,
                    Message = $"{operationName} completata con successo"
                };
            }
            catch (HttpRequestException httpEx)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore HTTP durante {operationName}: {httpEx.Message}");
                return new AdminResponse<T>
                {
                    Success = false,
                    Error = "Errore di connessione al server",
                    Message = $"Impossibile completare {operationName}"
                };
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Timeout durante {operationName}: {tcEx.Message}");
                return new AdminResponse<T>
                {
                    Success = false,
                    Error = "Timeout della richiesta",
                    Message = $"Timeout durante {operationName}"
                };
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore deserializzazione durante {operationName}: {jsonEx.Message}");
                return new AdminResponse<T>
                {
                    Success = false,
                    Error = "Errore nel formato dei dati",
                    Message = $"Dati non validi ricevuti durante {operationName}"
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore generico durante {operationName}: {ex.Message}");
                return new AdminResponse<T>
                {
                    Success = false,
                    Error = ex.Message,
                    Message = $"Errore imprevisto durante {operationName}"
                };
            }
        }

        // ========== METODI HELPER PRIVATI ==========

        private async Task<AdminStats> GetFallbackStatsAsync()
        {
            return await Task.FromResult(new AdminStats
            {
                TotalPosts = 0,
                TotalComments = 0,
                TotalUsers = 0,
                TotalSportFields = 0,
                PostsThisWeek = 0,
                CommentsToday = 0,
                GeneratedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        private async Task<T> ExecuteWithFallback<T>(Func<Task<T>> operation, string operationName) where T : new()
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore durante {operationName}: {ex.Message}");
                return new T(); // Restituisce istanza vuota del tipo
            }
        }

        private async Task<bool> ExecuteAdminAction(Func<Task<bool>> action, string operationName)
        {
            try
            {
                return await action();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Errore durante {operationName}: {ex.Message}");
                return false;
            }
        }

        private void LogRequest(HttpRequestMessage request, string operation)
        {
            Debug.WriteLine($"[ADMIN_SERVICE] {operation} - {request.Method} {request.RequestUri}");
        }

        private void LogResponse(HttpResponseMessage response, string operation)
        {
            Debug.WriteLine($"[ADMIN_SERVICE] {operation} - Response: {response.StatusCode}");
        }

        // ========== METODI DI VALIDAZIONE ==========

        private bool ValidatePostData(AdminPostInfo post)
        {
            return post != null &&
                   !string.IsNullOrEmpty(post.Titolo) &&
                   !string.IsNullOrEmpty(post.AutoreEmail);
        }

        private bool ValidateCommentData(AdminCommentInfo comment)
        {
            return comment != null &&
                   !string.IsNullOrEmpty(comment.Contenuto) &&
                   !string.IsNullOrEmpty(comment.AutoreEmail);
        }

        private bool ValidateUserData(AdminUserInfo user)
        {
            return user != null &&
                   !string.IsNullOrEmpty(user.Username) &&
                   !string.IsNullOrEmpty(user.Email);
        }

        // ========== METODI DI DIAGNOSTICA ==========

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_baseUrl}/health");
                var response = await _client.SendAsync(request);

                Debug.WriteLine($"[ADMIN_SERVICE] Test connessione: {response.StatusCode}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ADMIN_SERVICE] Test connessione fallito: {ex.Message}");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> GetDiagnosticInfoAsync()
        {
            return new Dictionary<string, object>
            {
                {"BaseUrl", _baseUrl},
                {"PythonApiUrl", _pythonApiUrl},
                {"HasSessionId", Preferences.ContainsKey("session_id")},
                {"ClientTimeout", _client.Timeout.TotalSeconds},
                {"Timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
            };
        }

        // ========== CLEANUP E DISPOSE ==========

        public void Dispose()
        {
            _client?.Dispose();
            Debug.WriteLine("[ADMIN_SERVICE] Service disposed");
        }

        ~AdminService()
        {
            Dispose();
        }
    }

    // ========== CLASSI DI SUPPORTO ==========

    public static class AdminServiceExtensions
    {
        public static async Task<List<T>> ExecuteWithRetry<T>(this IAdminService service,
            Func<Task<List<T>>> operation,
            int maxRetries = 3,
            TimeSpan? delay = null) where T : class
        {
            var actualDelay = delay ?? TimeSpan.FromSeconds(1);
            Exception lastException = null;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    Debug.WriteLine($"[ADMIN_SERVICE] Tentativo {attempt + 1}/{maxRetries} fallito: {ex.Message}");

                    if (attempt < maxRetries - 1)
                    {
                        await Task.Delay(actualDelay);
                        actualDelay = TimeSpan.FromMilliseconds(actualDelay.TotalMilliseconds * 1.5); // Backoff esponenziale
                    }
                }
            }

            Debug.WriteLine($"[ADMIN_SERVICE] Tutti i tentativi falliti. Ultimo errore: {lastException?.Message}");
            return new List<T>();
        }
    }
}