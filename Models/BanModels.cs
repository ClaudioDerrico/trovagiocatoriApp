using System;
using System.Text.Json.Serialization;

namespace trovagiocatoriApp.Models
{
    public class BanInfo
    {
        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        [JsonPropertyName("banned_at")]
        public DateTime BannedAt { get; set; }

        [JsonPropertyName("ban_type")]
        public string BanType { get; set; } = "permanent";

        [JsonPropertyName("is_permanent")]
        public bool IsPermanent { get; set; } = true;

        // Proprietà computed per il display
        public string BanTypeDisplay => "Permanente";

        public string StatusDisplay => "Ban Permanente";

        public Color StatusColor => Colors.Red;
    }

    public class LoginResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }

        [JsonPropertyName("ban_info")]
        public BanInfo BanInfo { get; set; }
    }

    public class UserBan
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("banned_by_admin_id")]
        public long BannedByAdminId { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        [JsonPropertyName("banned_at")]
        public DateTime BannedAt { get; set; }

        [JsonPropertyName("unbanned_at")]
        public DateTime? UnbannedAt { get; set; }

        [JsonPropertyName("unbanned_by_admin_id")]
        public long? UnbannedByAdminId { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }

        [JsonPropertyName("ban_type")]
        public string BanType { get; set; } = "permanent";

        [JsonPropertyName("notes")]
        public string Notes { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("admin_username")]
        public string AdminUsername { get; set; }

        // Proprietà computed
        public string BanStatusDisplay => IsActive ? "Attivo" : "Inattivo";
        public Color BanStatusColor => IsActive ? Colors.Red : Colors.Gray;
        public string BannedAtFormatted => BannedAt.ToString("dd/MM/yyyy HH:mm");
        public bool IsPermanent => true;
    }

    public class BanUserRequest
    {
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("reason")]
        public string Reason { get; set; }

        [JsonPropertyName("ban_type")]
        public string BanType { get; set; } = "permanent";

        [JsonPropertyName("notes")]
        public string Notes { get; set; }
    }

    public class BanResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("error")]
        public string Error { get; set; }
    }

    public class BanStats
    {
        [JsonPropertyName("active_bans")]
        public int ActiveBans { get; set; }

        [JsonPropertyName("total_bans")]
        public int TotalBans { get; set; }

        [JsonPropertyName("permanent_bans")]
        public int PermanentBans { get; set; }
    }
}