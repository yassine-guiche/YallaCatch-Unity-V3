using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace YallaCatch.Utils
{
    /// <summary>
    /// Secure storage for sensitive data like tokens
    /// Uses PlayerPrefs with encryption for sensitive values
    /// </summary>
    public static class SecureStorage
    {
        private static readonly byte[] Key = Encoding.UTF8.GetBytes("YallaCatch2024Key!"); // 16 bytes
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("YallaCatchIV2024"); // 16 bytes

        #region Public Methods

        /// <summary>
        /// Save a value securely
        /// </summary>
        public static void SetSecure(string key, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                PlayerPrefs.DeleteKey(key);
                return;
            }

            try
            {
                string encrypted = Encrypt(value);
                PlayerPrefs.SetString(key, encrypted);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SecureStorage] Failed to save: {ex.Message}");
                // Fallback to plain storage
                PlayerPrefs.SetString(key, value);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Get a securely stored value
        /// </summary>
        public static string GetSecure(string key, string defaultValue = "")
        {
            string stored = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(stored)) return defaultValue;

            try
            {
                return Decrypt(stored);
            }
            catch
            {
                // If decryption fails, might be plain text
                return stored;
            }
        }

        /// <summary>
        /// Delete a secure value
        /// </summary>
        public static void DeleteSecure(string key)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Check if a key exists
        /// </summary>
        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(key);
        }

        #endregion

        #region Encryption

        private static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor();
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        private static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor();
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                return Encoding.UTF8.GetString(decryptedBytes);
            }
        }

        #endregion

        #region Token Helper Methods

        private const string ACCESS_TOKEN_KEY = "yc_at";
        private const string REFRESH_TOKEN_KEY = "yc_rt";
        private const string USER_ID_KEY = "yc_uid";

        public static void SaveTokens(string accessToken, string refreshToken, string userId)
        {
            SetSecure(ACCESS_TOKEN_KEY, accessToken);
            SetSecure(REFRESH_TOKEN_KEY, refreshToken);
            SetSecure(USER_ID_KEY, userId);
        }

        public static (string accessToken, string refreshToken, string userId) GetTokens()
        {
            return (
                GetSecure(ACCESS_TOKEN_KEY),
                GetSecure(REFRESH_TOKEN_KEY),
                GetSecure(USER_ID_KEY)
            );
        }

        public static void ClearTokens()
        {
            DeleteSecure(ACCESS_TOKEN_KEY);
            DeleteSecure(REFRESH_TOKEN_KEY);
            DeleteSecure(USER_ID_KEY);
        }

        public static bool HasValidTokens()
        {
            return !string.IsNullOrEmpty(GetSecure(ACCESS_TOKEN_KEY));
        }

        #endregion
    }
}
