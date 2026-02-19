using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace YallaCatch.Utils
{
    /// <summary>
    /// JSON serialization/deserialization helpers
    /// </summary>
    public static class JsonHelper
    {
        private static readonly JsonSerializerSettings DefaultSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc
        };

        #region Serialization

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        public static string ToJson<T>(T obj, bool prettyPrint = false)
        {
            try
            {
                return JsonConvert.SerializeObject(obj, 
                    prettyPrint ? Formatting.Indented : Formatting.None, 
                    DefaultSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] Serialization failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        public static T FromJson<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(json, DefaultSettings);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] Deserialization failed: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Try to deserialize JSON string
        /// </summary>
        public static bool TryFromJson<T>(string json, out T result)
        {
            result = default;
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                result = JsonConvert.DeserializeObject<T>(json, DefaultSettings);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Array Helpers

        /// <summary>
        /// Deserialize JSON array to List
        /// </summary>
        public static List<T> FromJsonArray<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return new List<T>();

            try
            {
                return JsonConvert.DeserializeObject<List<T>>(json, DefaultSettings) ?? new List<T>();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JsonHelper] Array deserialization failed: {ex.Message}");
                return new List<T>();
            }
        }

        /// <summary>
        /// Serialize List to JSON array string
        /// </summary>
        public static string ToJsonArray<T>(List<T> list)
        {
            return ToJson(list);
        }

        #endregion

        #region Dynamic Access

        /// <summary>
        /// Get a value from JSON by path (e.g., "data.user.name")
        /// </summary>
        public static T GetValue<T>(string json, string path, T defaultValue = default)
        {
            if (string.IsNullOrEmpty(json)) return defaultValue;

            try
            {
                JObject obj = JObject.Parse(json);
                JToken token = obj.SelectToken(path);
                return token != null ? token.ToObject<T>() : defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Check if JSON has a specific path
        /// </summary>
        public static bool HasPath(string json, string path)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                JObject obj = JObject.Parse(json);
                return obj.SelectToken(path) != null;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Check if string is valid JSON
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return false;

            try
            {
                JToken.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Merge

        /// <summary>
        /// Merge two JSON objects (second overwrites first)
        /// </summary>
        public static string Merge(string json1, string json2)
        {
            if (string.IsNullOrEmpty(json1)) return json2;
            if (string.IsNullOrEmpty(json2)) return json1;

            try
            {
                JObject obj1 = JObject.Parse(json1);
                JObject obj2 = JObject.Parse(json2);
                obj1.Merge(obj2, new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Replace
                });
                return obj1.ToString();
            }
            catch
            {
                return json1;
            }
        }

        #endregion
    }
}
