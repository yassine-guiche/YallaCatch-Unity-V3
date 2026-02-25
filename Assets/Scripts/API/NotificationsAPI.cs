using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// Notifications API - Push and in-app notifications
    /// </summary>
    public class NotificationsAPI : MonoBehaviour
    {
        public static NotificationsAPI Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region List Methods

        /// <summary>
        /// Get user notifications
        /// </summary>
        public void GetNotifications(int page = 1, int limit = 20, bool unreadOnly = false, Action<ApiResponse<NotificationsListResponse>> callback = null)
        {
            string query = $"?page={page}&limit={limit}&unreadOnly={unreadOnly.ToString().ToLower()}";
            StartCoroutine(APIClient.Instance.Get<NotificationsListResponse>(APIEndpoints.NOTIFICATIONS_LIST + query, callback));
        }

        /// <summary>
        /// Mark notifications as read (backend uses PUT /notifications/read)
        /// </summary>
        public void MarkAsRead(List<string> notificationIds = null, bool all = false, Action<ApiResponse<MarkReadResponse>> callback = null)
        {
            object body = all
                ? new { all = true }
                : new { notificationIds = notificationIds ?? new List<string>() };
            StartCoroutine(APIClient.Instance.Put<MarkReadResponse>(APIEndpoints.NOTIFICATIONS_READ, body, callback));
        }

        #endregion

        #region Settings Methods

        /// <summary>
        /// Get notification settings
        /// </summary>
        public void GetSettings(Action<ApiResponse<NotificationSettingsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<JToken>(APIEndpoints.NOTIFICATIONS_SETTINGS, response =>
            {
                callback?.Invoke(NormalizeSettingsApiResponse(response));
            }));
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        public void UpdateSettings(NotificationSettings settings, Action<ApiResponse<NotificationSettingsResponse>> callback)
        {
            var payload = new NotificationSettings
            {
                push = settings?.push ?? true,
                email = settings?.email ?? true,
                sms = settings?.sms ?? false,
                inApp = settings?.inApp ?? true
            };

            StartCoroutine(APIClient.Instance.Put<JToken>(APIEndpoints.NOTIFICATIONS_SETTINGS, payload, response =>
            {
                callback?.Invoke(NormalizeSettingsApiResponse(response, payload));
            }));
        }

        #endregion

        #region Push Methods

        /// <summary>
        /// Subscribe to push notifications
        /// </summary>
        public void SubscribePush(PushSubscription subscription, Action<ApiResponse<PushSubscriptionResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<PushSubscriptionResponse>(APIEndpoints.NOTIFICATIONS_PUSH_SUBSCRIBE, subscription, callback));
        }

        /// <summary>
        /// Unsubscribe from push notifications (backend uses DELETE /push/unsubscribe)
        /// </summary>
        public void UnsubscribePush(string endpoint, Action<ApiResponse<PushSubscriptionResponse>> callback)
        {
            var body = new { endpoint };
            StartCoroutine(APIClient.Instance.Delete<PushSubscriptionResponse>(APIEndpoints.NOTIFICATIONS_PUSH_UNSUBSCRIBE, body, callback));
        }

        #endregion

        #region Stats Methods

        /// <summary>
        /// Get notification stats
        /// </summary>
        public void GetStats(Action<ApiResponse<NotificationStatsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<NotificationStatsResponse>(APIEndpoints.NOTIFICATIONS_STATS, callback));
        }

        #endregion

        private static ApiResponse<NotificationSettingsResponse> NormalizeSettingsApiResponse(
            ApiResponse<JToken> response,
            NotificationSettings fallbackSettings = null)
        {
            if (response == null)
            {
                return new ApiResponse<NotificationSettingsResponse>
                {
                    success = false,
                    error = "NULL_RESPONSE",
                    message = "No response received"
                };
            }

            var normalized = new ApiResponse<NotificationSettingsResponse>
            {
                success = response.success,
                error = response.error,
                message = response.message,
                timestamp = response.timestamp
            };

            if (!response.success)
            {
                return normalized;
            }

            if (response.data == null || response.data.Type == JTokenType.Null)
            {
                if (fallbackSettings != null)
                {
                    normalized.data = new NotificationSettingsResponse
                    {
                        settings = CloneSettings(fallbackSettings)
                    };
                }
                return normalized;
            }

            if (response.data.Type == JTokenType.Boolean)
            {
                bool ok = response.data.Value<bool>();
                if (ok)
                {
                    normalized.data = new NotificationSettingsResponse
                    {
                        settings = CloneSettings(fallbackSettings ?? new NotificationSettings
                        {
                            push = true,
                            email = true,
                            sms = false,
                            inApp = true
                        })
                    };
                }
                else
                {
                    normalized.success = false;
                    normalized.error = "INVALID_SETTINGS_RESPONSE";
                    normalized.message = "Backend returned false for notification settings update.";
                }
                return normalized;
            }

            if (response.data.Type == JTokenType.Object)
            {
                var obj = (JObject)response.data;
                NotificationSettingsResponse settingsResponse;

                if (obj["settings"] != null)
                {
                    settingsResponse = obj.ToObject<NotificationSettingsResponse>();
                }
                else
                {
                    var rawSettings = obj.ToObject<NotificationSettings>();
                    settingsResponse = new NotificationSettingsResponse
                    {
                        settings = rawSettings
                    };
                }

                settingsResponse?.NormalizeInPlace();
                normalized.data = settingsResponse;
                return normalized;
            }

            normalized.success = false;
            normalized.error = "INVALID_SETTINGS_RESPONSE";
            normalized.message = $"Unexpected notification settings response type: {response.data.Type}";
            return normalized;
        }

        private static NotificationSettings CloneSettings(NotificationSettings settings)
        {
            if (settings == null) return null;
            return new NotificationSettings
            {
                push = settings.push,
                email = settings.email,
                sms = settings.sms,
                inApp = settings.inApp
            };
        }
    }

    #region Notifications Request/Response Models

    [Serializable]
    public class NotificationsListResponse
    {
        public List<Notification> notifications;
        public Pagination pagination;
        public int unreadCount;
    }

    [Serializable]
    public class MarkReadResponse
    {
        public bool success;
        public int markedCount;
        public int remainingUnread;
    }

    [Serializable]
    public class NotificationSettings
    {
        public bool push;
        public bool email;
        public bool sms;
        public bool inApp;
    }

    [Serializable]
    public class NotificationSettingsResponse
    {
        public NotificationSettings settings;
        public bool push;
        public bool email;
        public bool sms;
        public bool inApp;

        public void NormalizeInPlace()
        {
            if (settings != null)
            {
                return;
            }

            // Backend currently returns the settings object directly in `data`.
            // Support both wrapped `{ settings: {...} }` and raw `{ push, email, ... }`.
            settings = new NotificationSettings
            {
                push = push,
                email = email,
                sms = sms,
                inApp = inApp
            };
        }
    }

    [Serializable]
    public class PushSubscription
    {
        public string endpoint;
        public PushKeys keys;
    }

    [Serializable]
    public class PushKeys
    {
        public string p256dh;
        public string auth;
    }

    [Serializable]
    public class PushSubscriptionResponse
    {
        public bool success;
        public string message;
    }

    [Serializable]
    public class NotificationStatsResponse
    {
        public int totalNotifications;
        public int unreadCount;
        public int todayCount;
        public Dictionary<string, int> byType;
    }

    #endregion
}
