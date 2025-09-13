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

        public async Task<BanResponse> BanUserAsync(BanUserRequest banRequest)
        {
            try
            {
                Debug.WriteLine($"[BAN_SERVICE] Richiesta ban per user ID: {banRequest.UserId}");

                var json = JsonSerializer.Serialize(banRequest, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var request = CreateAuthenticatedRequest(HttpMethod.Post, $"{_baseUrl}/admin/ban/user");
                request.Content = content;

                var response = await _client.SendAsync(request);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var banResponse = JsonSerializer.Deserialize<BanResponse>(responseJson, _jsonOptions);
                    Debug.WriteLine($"[BAN_SERVICE] ✅ Utente {banRequest.UserId} bannato con successo");
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
                    Debug.WriteLine($"[BAN_SERVICE] ✅ Utente {userId} sbannato con successo");
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

                        Debug.WriteLine($"[BAN_SERVICE] ✅ Recuperati {bans?.Count ?? 0} ban attivi");
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

                        Debug.WriteLine($"[BAN_SERVICE] ✅ Recuperato ban per user {userId}");
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

                        Debug.WriteLine($"[BAN_SERVICE] ✅ Recuperate statistiche ban");
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
                return userBan != null && userBan.IsActive && !userBan.IsExpired;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[BAN_SERVICE] Errore check ban status: {ex.Message}");
                return false;
            }
        }

        // Metodi di utilità
        public static string GetBanReasonOptions()
        {
            return "Violazione regole comunità|Spam|Linguaggio inappropriato|Comportamento molesto|Contenuti offensivi|Account fake|Altro";
        }

        public static List<string> GetBanDurationOptions()
        {
            return new List<string>
            {
                "1 ora",
                "6 ore",
                "12 ore",
                "1 giorno",
                "3 giorni",
                "1 settimana",
                "2 settimane",
                "1 mese",
                "3 mesi",
                "6 mesi",
                "1 anno",
                "Permanente"
            };
        }

        public static DateTime? GetExpirationDateFromDuration(string duration)
        {
            var now = DateTime.Now;

            return duration switch
            {
                "1 ora" => now.AddHours(1),
                "6 ore" => now.AddHours(6),
                "12 ore" => now.AddHours(12),
                "1 giorno" => now.AddDays(1),
                "3 giorni" => now.AddDays(3),
                "1 settimana" => now.AddDays(7),
                "2 settimane" => now.AddDays(14),
                "1 mese" => now.AddMonths(1),
                "3 mesi" => now.AddMonths(3),
                "6 mesi" => now.AddMonths(6),
                "1 anno" => now.AddYears(1),
                "Permanente" => null,
                _ => now.AddDays(1)
            };
        }

        public static string GetBanTypeFromDuration(string duration)
        {
            return duration == "Permanente" ? "permanent" : "temporary";
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