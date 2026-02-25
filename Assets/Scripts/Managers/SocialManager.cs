using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;
using YallaCatch.Models;
using YallaCatch.Core;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages friends, teams, and social features
    /// Uses SocialAPI
    /// </summary>
    public class SocialManager : MonoBehaviour
    {
        public static SocialManager Instance { get; private set; }

        [Header("Nearby Players Refresh")]
        [SerializeField] private bool autoRefreshNearbyPlayers = true;
        [SerializeField] private float nearbyPlayersRadiusKm = 5f;
        [SerializeField] private float nearbyPlayersRefreshIntervalSeconds = 10f;
        [SerializeField] private float nearbyPlayersRefreshDistanceThresholdMeters = 50f;

        // Runtime caches (Header attributes are only valid on fields).
        public List<Friend> Friends { get; private set; }
        public List<FriendRequest> PendingIncoming { get; private set; }
        public List<FriendRequest> PendingSent { get; private set; }
        public List<NearbyPlayer> NearbyPlayers { get; private set; }
        public List<Team> MyTeams { get; private set; }

        // Events
        public event System.Action OnFriendsUpdated;
        public event System.Action OnPendingRequestsUpdated;
        public event System.Action OnNearbyPlayersUpdated;

        private bool gpsLocationHooksRegistered;
        private bool nearbyRefreshInFlight;
        private bool hasNearbyRefreshAnchor;
        private float lastNearbyRefreshLat;
        private float lastNearbyRefreshLng;
        private float lastNearbyRefreshAt = -999f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Friends = new List<Friend>();
                PendingIncoming = new List<FriendRequest>();
                PendingSent = new List<FriendRequest>();
                NearbyPlayers = new List<NearbyPlayer>();
                MyTeams = new List<Team>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            TryRegisterGpsLocationHooks();
        }

        private void Update()
        {
            if (!gpsLocationHooksRegistered && GPSManager.Instance != null)
            {
                TryRegisterGpsLocationHooks();
            }

            if (autoRefreshNearbyPlayers &&
                !hasNearbyRefreshAnchor &&
                !nearbyRefreshInFlight &&
                GPSManager.Instance != null &&
                GPSManager.Instance.IsInitialized &&
                SocialAPI.Instance != null &&
                APIClient.Instance != null &&
                APIClient.Instance.IsAuthenticated)
            {
                RefreshNearbyPlayersInternal(force: false);
            }
        }

        private void OnDestroy()
        {
            UnregisterGpsLocationHooks();
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #region Friends

        public void LoadFriends(System.Action<bool> callback = null)
        {
            SocialAPI.Instance.GetFriends(response =>
            {
                if (response.success && response.data != null)
                {
                    Friends.Clear();
                    Friends.AddRange(response.data.friends);
                    OnFriendsUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void LoadPendingRequests(System.Action<bool> callback = null)
        {
            SocialAPI.Instance.GetPendingRequests(response =>
            {
                if (response.success && response.data != null)
                {
                    PendingIncoming.Clear();
                    PendingIncoming.AddRange(response.data.incoming);
                    PendingSent.Clear();
                    PendingSent.AddRange(response.data.outgoing);
                    OnPendingRequestsUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void SendFriendRequest(string userId, string message, System.Action<bool, string> callback)
        {
            SocialAPI.Instance.SendFriendRequest(userId, message, response =>
            {
                callback?.Invoke(response.success, response.data?.message ?? response.message);
            });
        }

        public void AcceptFriendRequest(string requestId, System.Action<bool> callback)
        {
            SocialAPI.Instance.AcceptFriendRequest(requestId, response =>
            {
                if (response.success)
                {
                    PendingIncoming.RemoveAll(r => r.requestId == requestId);
                    OnPendingRequestsUpdated?.Invoke();
                    LoadFriends(); // Reload friends list
                }
                callback?.Invoke(response.success);
            });
        }

        public void RejectFriendRequest(string requestId, System.Action<bool> callback)
        {
            SocialAPI.Instance.RejectFriendRequest(requestId, response =>
            {
                if (response.success)
                {
                    PendingIncoming.RemoveAll(r => r.requestId == requestId);
                    OnPendingRequestsUpdated?.Invoke();
                }
                callback?.Invoke(response.success);
            });
        }

        public void RemoveFriend(string friendId, System.Action<bool> callback)
        {
            SocialAPI.Instance.RemoveFriend(friendId, response =>
            {
                if (response.success)
                {
                    Friends.RemoveAll(f => f.friendId == friendId);
                    OnFriendsUpdated?.Invoke();
                }
                callback?.Invoke(response.success);
            });
        }

        public List<Friend> GetFriends()
        {
            return Friends != null ? new List<Friend>(Friends) : new List<Friend>();
        }

        public List<FriendRequest> GetPendingRequests()
        {
            return PendingIncoming != null ? new List<FriendRequest>(PendingIncoming) : new List<FriendRequest>();
        }

        #endregion

        #region Nearby Players

        public void RefreshNearbyPlayers(System.Action<bool> callback = null)
        {
            RefreshNearbyPlayersInternal(force: true, callback);
        }

        private void RefreshNearbyPlayersInternal(bool force, System.Action<bool> callback = null)
        {
            if (GPSManager.Instance == null || !GPSManager.Instance.IsInitialized || SocialAPI.Instance == null)
            {
                callback?.Invoke(false);
                return;
            }

            if (APIClient.Instance == null || !APIClient.Instance.IsAuthenticated)
            {
                callback?.Invoke(false);
                return;
            }

            float lat = GPSManager.Instance.CurrentLatitude;
            float lng = GPSManager.Instance.CurrentLongitude;
            if (!force && !ShouldAutoRefreshNearbyPlayers(lat, lng))
            {
                callback?.Invoke(false);
                return;
            }

            if (nearbyRefreshInFlight)
            {
                callback?.Invoke(false);
                return;
            }

            nearbyRefreshInFlight = true;

            var location = new Location
            {
                latitude = lat,
                longitude = lng
            };

            SocialAPI.Instance.GetNearbyPlayers(location, Mathf.Max(0.1f, nearbyPlayersRadiusKm), response =>
            {
                nearbyRefreshInFlight = false;

                if (response.success && response.data != null)
                {
                    NearbyPlayers.Clear();
                    NearbyPlayers.AddRange(response.data.players);
                    lastNearbyRefreshLat = lat;
                    lastNearbyRefreshLng = lng;
                    lastNearbyRefreshAt = Time.time;
                    hasNearbyRefreshAnchor = true;
                    OnNearbyPlayersUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        private bool ShouldAutoRefreshNearbyPlayers(float lat, float lng)
        {
            if (!autoRefreshNearbyPlayers)
            {
                return false;
            }

            if (!hasNearbyRefreshAnchor)
            {
                return true;
            }

            bool intervalElapsed = (Time.time - lastNearbyRefreshAt) >= Mathf.Max(1f, nearbyPlayersRefreshIntervalSeconds);
            bool distanceThresholdMet = GPSManager.CalculateDistance(lastNearbyRefreshLat, lastNearbyRefreshLng, lat, lng) >=
                Mathf.Max(1f, nearbyPlayersRefreshDistanceThresholdMeters);

            return intervalElapsed || distanceThresholdMet;
        }

        private void TryRegisterGpsLocationHooks()
        {
            if (gpsLocationHooksRegistered || GPSManager.Instance == null)
            {
                return;
            }

            GPSManager.Instance.OnLocationUpdated += HandleGpsLocationUpdated;
            gpsLocationHooksRegistered = true;
        }

        private void UnregisterGpsLocationHooks()
        {
            if (!gpsLocationHooksRegistered || GPSManager.Instance == null)
            {
                return;
            }

            GPSManager.Instance.OnLocationUpdated -= HandleGpsLocationUpdated;
            gpsLocationHooksRegistered = false;
        }

        private void HandleGpsLocationUpdated(float lat, float lng)
        {
            if (!autoRefreshNearbyPlayers)
            {
                return;
            }

            if (float.IsNaN(lat) || float.IsNaN(lng) || float.IsInfinity(lat) || float.IsInfinity(lng))
            {
                return;
            }

            RefreshNearbyPlayersInternal(force: false);
        }

        #endregion

        #region User Profiles

        public void GetUserProfile(string userId, System.Action<User, string> callback)
        {
            SocialAPI.Instance.GetUserProfile(userId, response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.user, response.data.friendshipStatus);
                }
                else
                {
                    callback?.Invoke(null, null);
                }
            });
        }

        public void GetPlayerProfile(string userId, System.Action<User, string> callback)
        {
            GetUserProfile(userId, callback);
        }

        #endregion

        #region Teams

        public void CreateTeam(string name, string description, bool isPublic, int maxMembers, System.Action<bool, Team> callback)
        {
            var request = new CreateTeamRequest
            {
                name = name,
                description = description,
                isPublic = isPublic,
                maxMembers = maxMembers
            };

            SocialAPI.Instance.CreateTeam(request, response =>
            {
                if (response.success && response.data != null)
                {
                    MyTeams.Add(response.data);
                    callback?.Invoke(true, response.data);
                }
                else
                {
                    callback?.Invoke(false, null);
                }
            });
        }

        #endregion

        #region Leaderboard

        public void GetLeaderboard(string type, string city, System.Action<List<LeaderboardEntry>, LeaderboardEntry> callback)
        {
            SocialAPI.Instance.GetLeaderboard(type, city, 50, 0, response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.leaderboard, response.data.userRank);
                }
                else
                {
                    callback?.Invoke(new List<LeaderboardEntry>(), null);
                }
            });
        }

        #endregion

        #region Share

        public void ShareCapture(string captureId, string platform, string message, System.Action<bool, string> callback)
        {
            SocialAPI.Instance.ShareCapture(captureId, platform, message, response =>
            {
                callback?.Invoke(response.success, response.data?.shareUrl);
            });
        }

        #endregion
    }
}
