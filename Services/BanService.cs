using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using trovagiocatoriApp.Models;

namespace trovagiocatoriApp.Services
{
    public interface IBanService
    {
        Task<BanResponse> BanUserAsync(BanUserRequest banRequest);
        Task<BanResponse> UnbanUserAsync(long userId);
        Task<List<UserBan>> GetActiveBansAsync();
        Task<UserBan> GetUserBanAsync(long userId);
        Task<BanStats> GetBanStatsAsync();
        Task<bool> CheckUserBanStatusAsync(long userId);
    }

    public class BanService : IBanService, IDisposable
    {
        private readonly HttpClient _client;
        private readonly string _baseUrl = ApiConfig.BaseUrl;
        private readonly JsonSerializerOptions _jsonOptions;

        public BanService()
        {
            _client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase //In JSON la convenzione è camelCase: prima lettera minuscola, poi maiuscola per le successive parole
                //es: "userId"
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

        public async Task<BanResponse> BanUserAsync(BanUserRequest banRequest)
        {
            try
            {
                Debug.WriteLine($"[BAN_SERVICE] Richiesta ban permanente per user ID: {banRequest.UserId}");

                banRequest.BanType = "permanent";

                var json = JsonSerializer.Serialize(banRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"{_baseUrl}/admin/ban/user");
                request.Content = content;

                var response = await _client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync(); // Legge il contenuto della risposta come stringa

                if (response.IsSuccessStatusCode)
                {
                    var banResponse = JsonSerializer.Deserialize<BanResponse>(responseJson, _jsonOptions);
                    Debug.WriteLine($"[BAN_SERVICE] Utente {banRequest.UserId} bannato permanentemente");
                    return banResponse;
                }
                else
                {
                    Debug.WriteLine($"[BAN_SERVICE] Errore API: {response.StatusCode} - {responseJson}");
                    return new BanResponse
                    {
                        Success = false,
                        Error = $"Errore del server: {response.StatusCode}",
                        Message = responseJson
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore ban user: {ex.Message}");
                return new BanResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "Errore durante il ban dell'utente"
                };
            }
        }

        public async Task<BanResponse> UnbanUserAsync(long userId)
        {
            try
            {
                Debug.WriteLine($"[BAN_SERVICE] Richiesta unban per user ID: {userId}");

                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"{_baseUrl}/admin/unban/{userId}");
                var response = await _client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var banResponse = JsonSerializer.Deserialize<BanResponse>(responseJson, _jsonOptions);
                    Debug.WriteLine($"[BAN_SERVICE] Utente {userId} sbannato con successo");
                    return banResponse;
                }
                else
                {
                    Debug.WriteLine($"[BAN_SERVICE] Errore API unban: {response.StatusCode} - {responseJson}");
                    return new BanResponse
                    {
                        Success = false,
                        Error = $"Errore del server: {response.StatusCode}",
                        Message = responseJson
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore unban user: {ex.Message}");
                return new BanResponse
                {
                    Success = false,
                    Error = ex.Message,
                    Message = "Errore durante l'unban dell'utente"
                };
            }
        }

        public async Task<List<UserBan>> GetActiveBansAsync()
        {
            try
            {
                Debug.WriteLine("[BAN_SERVICE] Recupero ban attivi...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_baseUrl}/admin/bans");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var banResponse = JsonSerializer.Deserialize<BanResponse>(responseJson, _jsonOptions);

                    if (banResponse.Success && banResponse.Data != null)
                    {
                        var bansJson = JsonSerializer.Serialize(banResponse.Data);
                        var bans = JsonSerializer.Deserialize<List<UserBan>>(bansJson, _jsonOptions);

                        Debug.WriteLine($"[BAN_SERVICE] Recuperati {bans?.Count ?? 0} ban attivi");
                        return bans ?? new List<UserBan>();
                    }
                }

                Debug.WriteLine($"[BAN_SERVICE] Errore API get bans: {response.StatusCode}");
                return new List<UserBan>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore get active bans: {ex.Message}");
                return new List<UserBan>();
            }
        }

        public async Task<UserBan> GetUserBanAsync(long userId)
        {
            try
            {
                Debug.WriteLine($"[BAN_SERVICE] Recupero ban per user ID: {userId}");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_baseUrl}/admin/ban/info/{userId}");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var banResponse = JsonSerializer.Deserialize<BanResponse>(responseJson, _jsonOptions);

                    if (banResponse.Success && banResponse.Data != null)
                    {
                        var banJson = JsonSerializer.Serialize(banResponse.Data);
                        var userBan = JsonSerializer.Deserialize<UserBan>(banJson, _jsonOptions);

                        Debug.WriteLine($"[BAN_SERVICE] Recuperato ban per user {userId}");
                        return userBan;
                    }
                }

                Debug.WriteLine($"[BAN_SERVICE] Nessun ban trovato per user {userId}");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore get user ban: {ex.Message}");
                return null;
            }
        }

        public async Task<BanStats> GetBanStatsAsync()
        {
            try
            {
                Debug.WriteLine("[BAN_SERVICE] Recupero statistiche ban...");

                var request = CreateAuthenticatedRequest(HttpMethod.Get, $"{_baseUrl}/admin/ban/stats");
                var response = await _client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var banResponse = JsonSerializer.Deserialize<BanResponse>(responseJson, _jsonOptions);

                    if (banResponse.Success && banResponse.Data != null)
                    {
                        var statsJson = JsonSerializer.Serialize(banResponse.Data);
                        var stats = JsonSerializer.Deserialize<BanStats>(statsJson, _jsonOptions);

                        Debug.WriteLine($"[BAN_SERVICE] Recuperate statistiche ban");
                        return stats;
                    }
                }

                Debug.WriteLine($"[BAN_SERVICE] Errore API get stats: {response.StatusCode}");
                return new BanStats();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore get ban stats: {ex.Message}");
                return new BanStats();
            }
        }

        public async Task<bool> CheckUserBanStatusAsync(long userId)
        {
            try
            {
                var userBan = await GetUserBanAsync(userId);
                // controlla se è attivo
                return userBan != null && userBan.IsActive;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore check ban status: {ex.Message}");
                return false;
            }
        }



        public void Dispose()
        {
            _client?.Dispose();
            Debug.WriteLine("[BAN_SERVICE] Service disposed");
        }

        ~BanService()
        {
            Dispose();
        }
    }
}