using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages offline queue for actions when no internet connection
    /// Syncs with backend when connection is restored
    /// Coherent with backend /api/v1/offline endpoints
    /// </summary>
    public class OfflineQueueManager : MonoBehaviour
    {
        public static OfflineQueueManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float syncInterval = 30f; // Try to sync every 30 seconds
        [SerializeField] private int maxQueueSize = 100;

        private List<OfflineAction> actionQueue = new List<OfflineAction>();
        private bool isSyncing = false;
        private Coroutine syncCoroutine;

        public bool IsOnline { get; private set; } = true;
        public int QueuedActionsCount => actionQueue.Count;

        public event Action<bool> OnConnectionStatusChanged;
        public event Action<int> OnQueueSynced;

        #region Unity Lifecycle

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

        private void Start()
        {
            LoadQueueFromDisk();
            syncCoroutine = StartCoroutine(SyncCoroutine());
        }

        private void OnDestroy()
        {
            SaveQueueToDisk();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveQueueToDisk();
            }
        }

        #endregion

        #region Queue Management

        public void QueueAction(string actionType, string actionData)
        {
            if (actionQueue.Count >= maxQueueSize)
            {
                Debug.LogWarning("Offline queue is full!");
                return;
            }

            OfflineAction action = new OfflineAction
            {
                id = Guid.NewGuid().ToString(),
                actionType = actionType,
                actionData = actionData,
                timestamp = DateTime.UtcNow.ToString("o"),
                retryCount = 0
            };

            actionQueue.Add(action);
            SaveQueueToDisk();

            Debug.Log($"Action queued: {actionType}");
        }

        public void QueueCapture(string prizeId, float latitude, float longitude)
        {
            string data = JsonUtility.ToJson(new
            {
                prizeId = prizeId,
                latitude = latitude,
                longitude = longitude
            });

            QueueAction("capture", data);
        }

        public void QueueClaim(string rewardId)
        {
            string data = JsonUtility.ToJson(new { rewardId = rewardId });
            QueueAction("claim", data);
        }

        #endregion

        #region Sync

        private IEnumerator SyncCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(syncInterval);

                if (actionQueue.Count > 0 && !isSyncing)
                {
                    yield return StartCoroutine(SyncQueue());
                }
            }
        }

        public IEnumerator SyncQueue()
        {
            if (isSyncing || actionQueue.Count == 0)
                yield break;

            isSyncing = true;
            Debug.Log($"Syncing {actionQueue.Count} offline actions...");

            // Check connection
            yield return StartCoroutine(CheckConnection());

            if (!IsOnline)
            {
                Debug.LogWarning("No internet connection, sync postponed");
                isSyncing = false;
                yield break;
            }

            // Send queue to backend
            OfflineQueueData queueData = new OfflineQueueData
            {
                actions = actionQueue.ToArray()
            };

            string jsonData = JsonUtility.ToJson(queueData);

            yield return StartCoroutine(APIManager.Instance.SyncOfflineQueue(jsonData, (success, syncedCount, failedActions) =>
            {
                if (success)
                {
                    Debug.Log($"Successfully synced {syncedCount} actions");

                    // Remove synced actions
                    actionQueue.Clear();

                    // Re-add failed actions
                    if (failedActions != null && failedActions.Length > 0)
                    {
                        foreach (var action in failedActions)
                        {
                            action.retryCount++;
                            
                            // Remove if too many retries
                            if (action.retryCount < 5)
                            {
                                actionQueue.Add(action);
                            }
                            else
                            {
                                Debug.LogWarning($"Action {action.id} failed after 5 retries, discarding");
                            }
                        }
                    }

                    SaveQueueToDisk();
                    OnQueueSynced?.Invoke(syncedCount);
                }
                else
                {
                    Debug.LogError("Failed to sync offline queue");
                }
            }));

            isSyncing = false;
        }

        private IEnumerator CheckConnection()
        {
            bool previousStatus = IsOnline;

            // Simple ping to check connection
            using (WWW www = new WWW(APIManager.Instance.GetBaseURL() + "/health"))
            {
                yield return www;
                IsOnline = string.IsNullOrEmpty(www.error);
            }

            if (previousStatus != IsOnline)
            {
                Debug.Log($"Connection status changed: {(IsOnline ? "Online" : "Offline")}");
                OnConnectionStatusChanged?.Invoke(IsOnline);
            }
        }

        #endregion

        #region Persistence

        private void SaveQueueToDisk()
        {
            try
            {
                OfflineQueueData data = new OfflineQueueData
                {
                    actions = actionQueue.ToArray()
                };

                string json = JsonUtility.ToJson(data);
                PlayerPrefs.SetString("OfflineQueue", json);
                PlayerPrefs.Save();

                Debug.Log($"Saved {actionQueue.Count} actions to disk");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save offline queue: {e.Message}");
            }
        }

        private void LoadQueueFromDisk()
        {
            try
            {
                if (PlayerPrefs.HasKey("OfflineQueue"))
                {
                    string json = PlayerPrefs.GetString("OfflineQueue");
                    OfflineQueueData data = JsonUtility.FromJson<OfflineQueueData>(json);

                    if (data != null && data.actions != null)
                    {
                        actionQueue = new List<OfflineAction>(data.actions);
                        Debug.Log($"Loaded {actionQueue.Count} actions from disk");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load offline queue: {e.Message}");
                actionQueue.Clear();
            }
        }

        public void ClearQueue()
        {
            actionQueue.Clear();
            SaveQueueToDisk();
            Debug.Log("Offline queue cleared");
        }

        #endregion
    }

    #region Data Classes

    [Serializable]
    public class OfflineAction
    {
        public string id;
        public string actionType;
        public string actionData;
        public string timestamp;
        public int retryCount;
    }

    [Serializable]
    public class OfflineQueueData
    {
        public OfflineAction[] actions;
    }

    #endregion
}
