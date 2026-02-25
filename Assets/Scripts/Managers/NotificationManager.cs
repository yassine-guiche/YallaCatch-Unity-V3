using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YallaCatch.API;
using YallaCatch.Models;
using YallaCatch.Networking;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages push notifications and in-app notifications
    /// Uses NotificationsAPI
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        // Inspector headers only apply to fields; notification state is runtime data.
        public List<Notification> Notifications { get; private set; }
        public int UnreadCount { get; private set; }
        public NotificationSettings Settings { get; private set; }

        // Events
        public event System.Action OnNotificationsUpdated;
        public event System.Action<Notification> OnNewNotification;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Notifications = new List<Notification>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                Debug.Log("[NotificationManager] Skipping auto-load until player is authenticated.");
            }
            else
            {
                LoadNotifications();
                LoadSettings();
            }
            
            // Subscribe to real-time events
            if (SocketIOClient.Instance != null)
            {
                SocketIOClient.Instance.OnEventReceived += HandleSocketIOEvent;
            }
        }

        private void OnDestroy()
        {
            if (SocketIOClient.Instance != null)
            {
                SocketIOClient.Instance.OnEventReceived -= HandleSocketIOEvent;
            }
        }

        private void HandleSocketIOEvent(string type, string data)
        {
            if ((type == "notification" || type == "notification_update" || type == "notification_sent") && !string.IsNullOrWhiteSpace(data))
            {
                try
                {
                    var token = JToken.Parse(data);
                    if (token.Type == JTokenType.Object && token["notification"] != null)
                    {
                        token = token["notification"];
                    }

                    var notification = token?.ToObject<Notification>();
                    if (notification != null)
                    {
                        AddLocalNotification(notification);
                        Debug.Log($"[NotificationManager] Real-time notification received: {notification.title}");
                        
                        // Show immediate popup for real-time notifications
                        if (UI.UIManager.Instance != null)
                        {
                            UI.UIManager.Instance.ShowMessage($"<color=yellow>{notification.title}</color>\n{notification.message}");
                        }

                        // Check for ban/kick metadata
                        if (notification.metadata != null && notification.metadata.ContainsKey("action"))
                        {
                            if (notification.metadata["action"].ToString() == "ban")
                            {
                                Debug.LogWarning("[NotificationManager] BAN NOTIFICATION RECEIVED. Forcing logout.");
                                // Small delay to allow user to read before kick if they click OK, 
                                // but here we'll let UIManager handle the "OK" click to logout if it's a ban alert.
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[NotificationManager] Error parsing socket notification: {ex.Message}");
                }
            }
        }

        #region Load Notifications

        public void LoadNotifications(int page = 1, bool unreadOnly = false, System.Action<bool> callback = null)
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            NotificationsAPI.Instance.GetNotifications(page, 20, unreadOnly, response =>
            {
                if (response.success && response.data != null)
                {
                    if (page == 1)
                    {
                        Notifications.Clear();
                    }
                    Notifications.AddRange(response.data.notifications);
                    UnreadCount = response.data.unreadCount;
                    OnNotificationsUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        #endregion

        #region Mark as Read

        public void MarkAsRead(List<string> notificationIds, System.Action<bool> callback = null)
        {
            NotificationsAPI.Instance.MarkAsRead(notificationIds, false, response =>
            {
                if (response.success && response.data != null)
                {
                    UnreadCount = response.data.remainingUnread;
                    foreach (var id in notificationIds)
                    {
                        var notification = Notifications.Find(n => n._id == id);
                        if (notification != null)
                        {
                            notification.read = true;
                        }
                    }
                    OnNotificationsUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void MarkAllAsRead(System.Action<bool> callback = null)
        {
            NotificationsAPI.Instance.MarkAsRead(null, true, response =>
            {
                if (response.success)
                {
                    UnreadCount = 0;
                    foreach (var notification in Notifications)
                    {
                        notification.read = true;
                    }
                    OnNotificationsUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        #endregion

        #region Settings

        public void LoadSettings(System.Action<bool> callback = null)
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            NotificationsAPI.Instance.GetSettings(response =>
            {
                if (response.success && response.data != null)
                {
                    Settings = response.data.settings;
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[NotificationManager] Failed to load settings. error={response?.error} message={response?.message}");
                    callback?.Invoke(false);
                }
            });
        }

        public void UpdateSettings(NotificationSettings settings, System.Action<bool> callback = null)
        {
            NotificationsAPI.Instance.UpdateSettings(settings, response =>
            {
                if (response.success && response.data != null)
                {
                    Settings = response.data.settings;
                    callback?.Invoke(true);
                }
                else
                {
                    Debug.LogWarning($"[NotificationManager] Failed to update settings. error={response?.error} message={response?.message}");
                    callback?.Invoke(false);
                }
            });
        }

        #endregion

        #region Push Subscription

        public void SubscribeToPush(string endpoint, string p256dh, string auth, System.Action<bool> callback = null)
        {
            var subscription = new PushSubscription
            {
                endpoint = endpoint,
                keys = new PushKeys
                {
                    p256dh = p256dh,
                    auth = auth
                }
            };

            NotificationsAPI.Instance.SubscribePush(subscription, response =>
            {
                callback?.Invoke(response.success);
            });
        }

        public void UnsubscribeFromPush(string endpoint, System.Action<bool> callback = null)
        {
            NotificationsAPI.Instance.UnsubscribePush(endpoint, response =>
            {
                callback?.Invoke(response.success);
            });
        }

        #endregion

        #region Local Notifications

        public void NotifyAchievementUnlocked(string achievementName)
        {
            var notification = new Notification
            {
                _id = System.Guid.NewGuid().ToString("N"),
                title = "Achievement Unlocked",
                message = $"{achievementName}",
                type = "in_app",
                read = false,
                createdAt = System.DateTime.UtcNow,
                metadata = new Dictionary<string, object>
                {
                    { "category", "achievement" }
                }
            };

            AddLocalNotification(notification);
        }

        public void AddLocalNotification(Notification notification)
        {
            Notifications.Insert(0, notification);
            UnreadCount++;
            OnNewNotification?.Invoke(notification);
            OnNotificationsUpdated?.Invoke();
        }

        #endregion
    }
}
