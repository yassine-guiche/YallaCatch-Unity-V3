using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// Social API - Friends, Teams, Leaderboard, Nearby players
    /// </summary>
    public class SocialAPI : MonoBehaviour
    {
        public static SocialAPI Instance { get; private set; }

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

        #region Friend Methods

        /// <summary>
        /// Send friend request
        /// </summary>
        public void SendFriendRequest(string targetUserId, string message = null, Action<ApiResponse<FriendRequestResponse>> callback = null)
        {
            var body = new { targetUserId, message };
            StartCoroutine(APIClient.Instance.Post<FriendRequestResponse>(APIEndpoints.SOCIAL_FRIEND_SEND, body, callback));
        }

        /// <summary>
         /// Accept friend request
         /// </summary>
        /// <summary>
        /// Accept friend request
        /// </summary>
        public void AcceptFriendRequest(string userId, Action<ApiResponse<FriendActionResponse>> callback)
        {
            var body = new { fromUserId = userId, action = "accept" };
            StartCoroutine(APIClient.Instance.Post<FriendActionResponse>(APIEndpoints.SOCIAL_FRIEND_RESPOND, body, callback));
        }

        /// <summary>
        /// Reject friend request
        /// </summary>
        public void RejectFriendRequest(string userId, Action<ApiResponse<FriendActionResponse>> callback)
        {
            var body = new { fromUserId = userId, action = "reject" };
            StartCoroutine(APIClient.Instance.Post<FriendActionResponse>(APIEndpoints.SOCIAL_FRIEND_RESPOND, body, callback));
        }

        /// <summary>
        /// Remove friend
        /// </summary>
        public void RemoveFriend(string friendId, Action<ApiResponse<FriendActionResponse>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.SOCIAL_FRIEND_REMOVE, friendId);
            StartCoroutine(APIClient.Instance.Delete<FriendActionResponse>(endpoint, callback));
        }

        /// <summary>
        /// Get friends list
        /// </summary>
        public void GetFriends(Action<ApiResponse<FriendsListResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<FriendsListResponse>(APIEndpoints.SOCIAL_FRIENDS_LIST, callback));
        }

        /// <summary>
        /// Get pending friend requests
        /// </summary>
        public void GetPendingRequests(Action<ApiResponse<PendingRequestsResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<PendingRequestsResponse>(APIEndpoints.SOCIAL_FRIENDS_PENDING, callback));
        }

        #endregion

        #region Nearby Methods

        /// <summary>
        /// Get nearby players
        /// </summary>
        public void GetNearbyPlayers(Location location, float radiusKm = 5f, Action<ApiResponse<NearbyPlayersResponse>> callback = null)
        {
            string query = $"?latitude={location.latitude}&longitude={location.longitude}&radius={radiusKm}";
            StartCoroutine(APIClient.Instance.Get<NearbyPlayersResponse>(APIEndpoints.SOCIAL_NEARBY + query, callback));
        }

        #endregion

        #region Profile Methods

        /// <summary>
        /// Get user profile
        /// </summary>
        public void GetUserProfile(string userId, Action<ApiResponse<UserProfileResponse>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.SOCIAL_PROFILE, userId);
            StartCoroutine(APIClient.Instance.Get<UserProfileResponse>(endpoint, callback));
        }

        #endregion

        #region Team Methods

        /// <summary>
        /// Create team
        /// </summary>
        public void CreateTeam(CreateTeamRequest request, Action<ApiResponse<Team>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<Team>(APIEndpoints.SOCIAL_TEAMS, request, callback));
        }

        #endregion

        #region Leaderboard Methods

        /// <summary>
        /// Get leaderboard
        /// </summary>
        public void GetLeaderboard(string type = "global", string city = null, int limit = 50, int offset = 0, Action<ApiResponse<SocialLeaderboardResponse>> callback = null)
        {
            string query = $"?type={type}&limit={limit}&offset={offset}";
            if (!string.IsNullOrEmpty(city)) query += $"&city={Uri.EscapeDataString(city)}";
            StartCoroutine(APIClient.Instance.Get<SocialLeaderboardResponse>(APIEndpoints.SOCIAL_LEADERBOARD + query, callback));
        }

        #endregion

        #region Share Methods

        /// <summary>
        /// Share capture to social platform
        /// </summary>
        public void ShareCapture(string captureId, string platform, string message = null, Action<ApiResponse<ShareResponse>> callback = null)
        {
            var body = new { captureId, platform, message };
            StartCoroutine(APIClient.Instance.Post<ShareResponse>(APIEndpoints.SOCIAL_SHARE, body, callback));
        }

        #endregion
    }

    #region Social Request/Response Models

    [Serializable]
    public class FriendRequestResponse
    {
        public bool success;
        public string requestId;
        public string message;
    }

    [Serializable]
    public class FriendActionResponse
    {
        public bool success;
        public string message;
    }

    [Serializable]
    public class FriendsListResponse
    {
        public List<Friend> friends;
        public int onlineCount;
        public int totalCount;
    }

    [Serializable]
    public class PendingRequestsResponse
    {
        public List<FriendRequest> incoming;
        public List<FriendRequest> outgoing;
    }

    [Serializable]
    public class FriendRequest
    {
        public string requestId;
        public string userId;
        public string displayName;
        public string avatar;
        public string message;
        public DateTime createdAt;
    }

    [Serializable]
    public class NearbyPlayersResponse
    {
        public List<NearbyPlayer> players;
        public int count;
    }

    [Serializable]
    public class UserProfileResponse
    {
        public User user;
        public string friendshipStatus; // none, pending_sent, pending_received, friends, blocked
        public bool canSendRequest;
    }

    [Serializable]
    public class CreateTeamRequest
    {
        public string name;
        public string description;
        public bool isPublic;
        public int maxMembers;
    }

    [Serializable]
    public class SocialLeaderboardResponse
    {
        public List<LeaderboardEntry> leaderboard;
        public LeaderboardEntry userRank;
        public string type;
        public string city;
    }

    [Serializable]
    public class ShareResponse
    {
        public bool success;
        public string shareUrl;
        public string message;
    }

    #endregion
}
