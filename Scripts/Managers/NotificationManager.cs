using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;
using YallaCatch.Models;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages push notifications and in-app notifications
    /// Uses NotificationsAPI
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        [Header("Notifications")]
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
            LoadNotifications();
            LoadSettings();
        }

        #region Load Notifications

        public void LoadNotifications(int page = 1, bool unreadOnly = false, System.Action<bool> callback = null)
        {
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
            NotificationsAPI.Instance.GetSettings(response =>
            {
                if (response.success && response.data != null)
                {
                    Settings = response.data.settings;
                    callback?.Invoke(true);
                }
                else
                {
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
