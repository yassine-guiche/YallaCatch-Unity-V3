using UnityEngine;

namespace YallaCatch.Utils
{
    /// <summary>
    /// Common utility functions
    /// </summary>
    public static class Utilities
    {
        #region Distance Calculations

        /// <summary>
        /// Calculate distance between two GPS coordinates in meters (Haversine formula)
        /// </summary>
        public static float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            const float R = 6371000f; // Earth's radius in meters

            float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
            float dLon = (lon2 - lon1) * Mathf.Deg2Rad;

            float a = Mathf.Sin(dLat / 2f) * Mathf.Sin(dLat / 2f) +
                      Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) *
                      Mathf.Sin(dLon / 2f) * Mathf.Sin(dLon / 2f);

            float c = 2f * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1f - a));

            return R * c;
        }

        /// <summary>
        /// Check if a point is within radius of another point
        /// </summary>
        public static bool IsWithinRadius(float lat1, float lon1, float lat2, float lon2, float radiusMeters)
        {
            return CalculateDistance(lat1, lon1, lat2, lon2) <= radiusMeters;
        }

        #endregion

        #region Formatting

        /// <summary>
        /// Format points with K/M suffix for large numbers
        /// </summary>
        public static string FormatPoints(int points)
        {
            if (points >= 1000000)
                return $"{points / 1000000f:F1}M";
            if (points >= 1000)
                return $"{points / 1000f:F1}K";
            return points.ToString();
        }

        /// <summary>
        /// Format distance for display
        /// </summary>
        public static string FormatDistance(float meters)
        {
            if (meters >= 1000)
                return $"{meters / 1000f:F1} km";
            return $"{Mathf.RoundToInt(meters)} m";
        }

        /// <summary>
        /// Format time duration
        /// </summary>
        public static string FormatDuration(int seconds)
        {
            if (seconds < 60)
                return $"{seconds}s";
            if (seconds < 3600)
                return $"{seconds / 60}m {seconds % 60}s";
            int hours = seconds / 3600;
            int minutes = (seconds % 3600) / 60;
            return $"{hours}h {minutes}m";
        }

        /// <summary>
        /// Format relative time (e.g., "2 hours ago")
        /// </summary>
        public static string FormatRelativeTime(System.DateTime dateTime)
        {
            var diff = System.DateTime.UtcNow - dateTime;

            if (diff.TotalMinutes < 1)
                return "Just now";
            if (diff.TotalMinutes < 60)
                return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24)
                return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7)
                return $"{(int)diff.TotalDays}d ago";
            if (diff.TotalDays < 30)
                return $"{(int)(diff.TotalDays / 7)}w ago";
            return dateTime.ToString("MMM dd");
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate email format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        public static (bool isValid, string message) ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return (false, "Password is required");
            if (password.Length < 8)
                return (false, "Password must be at least 8 characters");
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[A-Z]"))
                return (false, "Password must contain an uppercase letter");
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[a-z]"))
                return (false, "Password must contain a lowercase letter");
            if (!System.Text.RegularExpressions.Regex.IsMatch(password, @"[0-9]"))
                return (false, "Password must contain a number");
            return (true, "");
        }

        #endregion

        #region Device Info

        /// <summary>
        /// Get device category (phone, tablet, pc)
        /// </summary>
        public static string GetDeviceCategory()
        {
            #if UNITY_IOS || UNITY_ANDROID
            float diagonal = Mathf.Sqrt(Screen.width * Screen.width + Screen.height * Screen.height) / Screen.dpi;
            return diagonal >= 7f ? "tablet" : "phone";
            #else
            return "pc";
            #endif
        }

        /// <summary>
        /// Get platform string
        /// </summary>
        public static string GetPlatform()
        {
            #if UNITY_IOS
            return "iOS";
            #elif UNITY_ANDROID
            return "Android";
            #else
            return "Unity";
            #endif
        }

        #endregion

        #region Colors

        /// <summary>
        /// Get rarity color
        /// </summary>
        public static Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "common" => new Color(0.7f, 0.7f, 0.7f),
                "uncommon" => new Color(0.3f, 0.8f, 0.3f),
                "rare" => new Color(0.3f, 0.5f, 0.9f),
                "epic" => new Color(0.6f, 0.3f, 0.9f),
                "legendary" => new Color(1f, 0.8f, 0.2f),
                _ => Color.white
            };
        }

        #endregion
    }
}
