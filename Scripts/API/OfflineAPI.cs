using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// Offline API - Sync and offline data management
    /// </summary>
    public class OfflineAPI : MonoBehaviour
    {
        public static OfflineAPI Instance { get; private set; }

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

        #region Sync Methods

        /// <summary>
        /// Sync offline actions to server
        /// </summary>
        public void SyncActions(SyncActionsRequest request, Action<ApiResponse<SyncActionsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<SyncActionsResponse>(APIEndpoints.OFFLINE_SYNC, request, callback));
        }

        /// <summary>
        /// Get sync status
        /// </summary>
        public void GetSyncStatus(string since = null, Action<ApiResponse<SyncStatusResponse>> callback = null)
        {
            string query = !string.IsNullOrEmpty(since) ? $"?since={Uri.EscapeDataString(since)}" : "";
            StartCoroutine(APIClient.Instance.Get<SyncStatusResponse>(APIEndpoints.OFFLINE_STATUS + query, callback));
        }

        #endregion

        #region Data Methods

        /// <summary>
        /// Get offline data package
        /// </summary>
        public void GetOfflinePackage(Action<ApiResponse<OfflinePackageResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<OfflinePackageResponse>(APIEndpoints.OFFLINE_PACKAGE, callback));
        }

        /// <summary>
        /// Get offline capabilities
        /// </summary>
        public void GetCapabilities(Action<ApiResponse<OfflineCapabilitiesResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<OfflineCapabilitiesResponse>(APIEndpoints.OFFLINE_CAPABILITIES, callback));
        }

        #endregion

        #region Retry Methods

        /// <summary>
        /// Retry failed actions
        /// </summary>
        public void RetryFailed(Action<ApiResponse<RetryFailedResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<RetryFailedResponse>(APIEndpoints.OFFLINE_RETRY, new { }, callback));
        }

        #endregion
    }

    #region Offline Request/Response Models

    [Serializable]
    public class SyncActionsRequest
    {
        public List<OfflineAction> actions;
        public OfflineDeviceInfo device;
        public string conflictResolution; // server_wins, client_wins, merge, manual
    }

    [Serializable]
    public class OfflineAction
    {
        public string actionId;
        public string actionType; // CLAIM, PROFILE_UPDATE, FRIEND_REQUEST, etc.
        public string data; // JSON stringified action data
        public string timestamp;
        public string checksum;
    }

    [Serializable]
    public class OfflineDeviceInfo
    {
        public string platform;
        public string appVersion;
        public string lastOnline;
        public int offlineDuration;
    }

    [Serializable]
    public class SyncActionsResponse
    {
        public bool success;
        public List<ActionResult> results;
        public int successCount;
        public int failedCount;
        public List<SyncConflict> conflicts;
    }

    [Serializable]
    public class ActionResult
    {
        public string actionId;
        public bool success;
        public string error;
        public object result;
    }

    [Serializable]
    public class SyncConflict
    {
        public string actionId;
        public string conflictType;
        public object serverData;
        public object clientData;
    }

    [Serializable]
    public class SyncStatusResponse
    {
        public bool hasChanges;
        public string lastSyncTime;
        public int pendingActions;
        public int failedActions;
        public List<string> changedEntities;
    }

    [Serializable]
    public class OfflinePackageResponse
    {
        public List<Prize> prizes;
        public List<DailyChallenge> challenges;
        public List<LeaderboardEntry> leaderboard;
        public User userProfile;
        public string packageVersion;
        public string expiresAt;
        public int maxOfflineDuration;
    }

    [Serializable]
    public class OfflineCapabilitiesResponse
    {
        public int maxOfflineActions;
        public int maxOfflineDurationHours;
        public List<string> supportedActionTypes;
        public bool canClaimOffline;
        public bool canUpdateProfileOffline;
        public int offlinePointsLimit;
    }

    [Serializable]
    public class RetryFailedResponse
    {
        public bool success;
        public int retriedCount;
        public int successCount;
        public int stillFailedCount;
        public List<ActionResult> results;
    }

    #endregion
}
