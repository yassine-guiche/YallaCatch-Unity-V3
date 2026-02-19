using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
            var body = new
            {
                notificationIds,
                all
            };
            StartCoroutine(APIClient.Instance.Put<MarkReadResponse>(APIEndpoints.NOTIFICATIONS_READ, body, callback));
        }

        #endregion

        #region Settings Methods

        /// <summary>
        /// Get notification settings
        /// </summary>
        public void GetSettings(Action<ApiResponse<NotificationSettingsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<NotificationSettingsResponse>(APIEndpoints.NOTIFICATIONS_SETTINGS, callback));
        }

        /// <summary>
        /// Update notification settings
        /// </summary>
        public void UpdateSettings(NotificationSettings settings, Action<ApiResponse<NotificationSettingsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Put<NotificationSettingsResponse>(APIEndpoints.NOTIFICATIONS_SETTINGS, settings, callback));
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
