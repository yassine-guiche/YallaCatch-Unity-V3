using System;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;
using YallaCatch.Models;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages offline actions and sync with server
    /// Uses OfflineAPI
    /// </summary>
    public class OfflineManager : MonoBehaviour
    {
        public static OfflineManager Instance { get; private set; }

        [Header("Offline State")]
        public bool IsOnline { get; private set; } = true;
        public List<OfflineAction> PendingActions { get; private set; }
        public OfflinePackageResponse CachedData { get; private set; }
        public OfflineCapabilitiesResponse Capabilities { get; private set; }

        // Events
        public event System.Action<bool> OnConnectivityChanged;
        public event System.Action OnSyncCompleted;
        public event System.Action<int, int> OnSyncProgress; // success, failed

        private const string PENDING_ACTIONS_KEY = "yallacatch_pending_actions";
        private const string CACHED_DATA_KEY = "yallacatch_offline_data";
        private float lastOnlineCheck = 0f;
        private float onlineCheckInterval = 5f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                PendingActions = new List<OfflineAction>();
                LoadPendingActions();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            LoadCapabilities();
            CheckConnectivity();
        }

        private void Update()
        {
            // Periodically check connectivity
            if (Time.time - lastOnlineCheck > onlineCheckInterval)
            {
                lastOnlineCheck = Time.time;
                CheckConnectivity();
            }
        }

        #region Connectivity

        private void CheckConnectivity()
        {
            bool wasOnline = IsOnline;
            IsOnline = Application.internetReachability != NetworkReachability.NotReachable;

            if (IsOnline != wasOnline)
            {
                OnConnectivityChanged?.Invoke(IsOnline);
                
                if (IsOnline && PendingActions.Count > 0)
                {
                    SyncPendingActions();
                }
            }
        }

        #endregion

        #region Offline Actions

        public void QueueAction(string actionType, object data)
        {
            if (PendingActions.Count >= (Capabilities?.maxOfflineActions ?? 100))
            {
                Debug.LogWarning("[OfflineManager] Max offline actions reached");
                return;
            }

            var action = new OfflineAction
            {
                actionId = Guid.NewGuid().ToString(),
                actionType = actionType,
                data = Newtonsoft.Json.JsonConvert.SerializeObject(data),
                timestamp = DateTime.UtcNow.ToString("o"),
                checksum = ComputeChecksum(data)
            };

            PendingActions.Add(action);
            SavePendingActions();

            Debug.Log($"[OfflineManager] Queued action: {actionType}");
        }

        public void SyncPendingActions(System.Action<bool, int, int> callback = null)
        {
            if (!IsOnline || PendingActions.Count == 0)
            {
                callback?.Invoke(true, 0, 0);
                return;
            }

            var request = new SyncActionsRequest
            {
                actions = PendingActions,
                device = new OfflineDeviceInfo
                {
                    platform = APIClient.Instance.GetPlatform(),
                    appVersion = Application.version,
                    lastOnline = DateTime.UtcNow.ToString("o"),
                    offlineDuration = 0
                },
                conflictResolution = "server_wins"
            };

            OfflineAPI.Instance.SyncActions(request, response =>
            {
                if (response.success && response.data != null)
                {
                    int successCount = response.data.successCount;
                    int failedCount = response.data.failedCount;

                    // Remove successful actions
                    foreach (var result in response.data.results)
                    {
                        if (result.success)
                        {
                            PendingActions.RemoveAll(a => a.actionId == result.actionId);
                        }
                    }

                    SavePendingActions();
                    OnSyncProgress?.Invoke(successCount, failedCount);
                    OnSyncCompleted?.Invoke();
                    callback?.Invoke(true, successCount, failedCount);
                }
                else
                {
                    callback?.Invoke(false, 0, PendingActions.Count);
                }
            });
        }

        public void RetryFailedActions(System.Action<bool> callback = null)
        {
            OfflineAPI.Instance.RetryFailed(response =>
            {
                callback?.Invoke(response.success);
            });
        }

        #endregion

        #region Offline Data

        public void DownloadOfflinePackage(System.Action<bool> callback = null)
        {
            OfflineAPI.Instance.GetOfflinePackage(response =>
            {
                if (response.success && response.data != null)
                {
                    CachedData = response.data;
                    SaveCachedData();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void LoadCapabilities(System.Action<bool> callback = null)
        {
            OfflineAPI.Instance.GetCapabilities(response =>
            {
                if (response.success && response.data != null)
                {
                    Capabilities = response.data;
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void GetSyncStatus(System.Action<SyncStatusResponse> callback)
        {
            OfflineAPI.Instance.GetSyncStatus(null, response =>
            {
                callback?.Invoke(response.success ? response.data : null);
            });
        }

        #endregion

        #region Persistence

        private void SavePendingActions()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(PendingActions);
            PlayerPrefs.SetString(PENDING_ACTIONS_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadPendingActions()
        {
            string json = PlayerPrefs.GetString(PENDING_ACTIONS_KEY, "[]");
            try
            {
                PendingActions = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OfflineAction>>(json) ?? new List<OfflineAction>();
            }
            catch
            {
                PendingActions = new List<OfflineAction>();
            }
        }

        private void SaveCachedData()
        {
            if (CachedData == null) return;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(CachedData);
            PlayerPrefs.SetString(CACHED_DATA_KEY, json);
            PlayerPrefs.Save();
        }

        private void LoadCachedData()
        {
            string json = PlayerPrefs.GetString(CACHED_DATA_KEY, "");
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    CachedData = Newtonsoft.Json.JsonConvert.DeserializeObject<OfflinePackageResponse>(json);
                }
                catch
                {
                    CachedData = null;
                }
            }
        }

        private string ComputeChecksum(object data)
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return json.GetHashCode().ToString("X8");
        }

        #endregion

        #region Utility

        public int GetPendingActionsCount() => PendingActions.Count;

        public bool CanPerformActionOffline(string actionType)
        {
            return Capabilities?.supportedActionTypes?.Contains(actionType) ?? false;
        }

        #endregion
    }
}
