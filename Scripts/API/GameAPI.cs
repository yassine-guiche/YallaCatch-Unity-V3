using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// Game API - Sessions, Location, Leaderboard, Challenges, Inventory
    /// </summary>
    public class GameAPI : MonoBehaviour
    {
        public static GameAPI Instance { get; private set; }

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

        #region Session Methods

        /// <summary>
        /// Start a new game session
        /// </summary>
        public void StartSession(SessionStartRequest request, Action<ApiResponse<GameSession>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<GameSession>(APIEndpoints.GAME_SESSION_START, request, callback));
        }

        /// <summary>
        /// End current game session
        /// </summary>
        public void EndSession(string sessionId, Action<ApiResponse<SessionEndResponse>> callback)
        {
            var body = new { sessionId };
            StartCoroutine(APIClient.Instance.Post<SessionEndResponse>(APIEndpoints.GAME_SESSION_END, body, callback));
        }

        #endregion

        #region Location Methods

        /// <summary>
        /// Update player location during game
        /// </summary>
        public void UpdateLocation(LocationUpdateRequest request, Action<ApiResponse<LocationUpdateResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<LocationUpdateResponse>(APIEndpoints.GAME_LOCATION, request, callback));
        }

        #endregion

        #region Map Methods

        /// <summary>
        /// Get map data with prizes for current viewport
        /// </summary>
        public void GetMapData(MapBounds bounds, Action<ApiResponse<MapDataResponse>> callback)
        {
            string query = $"?north={bounds.north}&south={bounds.south}&east={bounds.east}&west={bounds.west}";
            StartCoroutine(APIClient.Instance.Get<MapDataResponse>(APIEndpoints.GAME_MAP + query, callback));
        }

        #endregion

        #region Leaderboard Methods

        /// <summary>
        /// Get leaderboard
        /// </summary>
        public void GetLeaderboard(string type = "points", int limit = 50, Action<ApiResponse<LeaderboardResponse>> callback = null)
        {
            string query = $"?type={type}&limit={limit}";
            StartCoroutine(APIClient.Instance.Get<LeaderboardResponse>(APIEndpoints.GAME_LEADERBOARD + query, callback));
        }

        #endregion

        #region Power-Up Methods

        /// <summary>
        /// Use a power-up
        /// </summary>
        public void UsePowerUp(string powerUpId, Location location, Action<ApiResponse<PowerUpResult>> callback)
        {
            var body = new
            {
                powerUpId,
                location = new
                {
                    latitude = location.latitude,
                    longitude = location.longitude
                }
            };
            StartCoroutine(APIClient.Instance.Post<PowerUpResult>(APIEndpoints.GAME_POWERUP_USE, body, callback));
        }

        #endregion

        #region Challenge Methods

        /// <summary>
        /// Get daily challenges
        /// </summary>
        public void GetChallenges(Action<ApiResponse<ChallengesResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<ChallengesResponse>(APIEndpoints.GAME_CHALLENGES, callback));
        }

        /// <summary>
        /// Complete a challenge
        /// </summary>
        public void CompleteChallenge(string challengeId, Action<ApiResponse<ChallengeCompleteResponse>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.GAME_CHALLENGE_COMPLETE, challengeId);
            StartCoroutine(APIClient.Instance.Post<ChallengeCompleteResponse>(endpoint, null, callback));
        }

        #endregion

        #region Inventory Methods

        /// <summary>
        /// Get user inventory
        /// </summary>
        public void GetInventory(Action<ApiResponse<InventoryResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<InventoryResponse>(APIEndpoints.GAME_INVENTORY, callback));
        }

        #endregion
    }

    #region Game Request/Response Models

    [Serializable]
    public class SessionStartRequest
    {
        public string deviceId;
        public string platform;
        public string version;
        public string deviceModel;
        public string osVersion;
        public string appVersion;
        public SessionLocation location;
    }

    [Serializable]
    public class SessionEndResponse
    {
        public string sessionId;
        public int duration;
        public int prizesFound;
        public int pointsEarned;
        public float distanceTraveled;
        public SessionRewards rewards;
    }

    [Serializable]
    public class SessionRewards
    {
        public int bonusPoints;
        public string[] achievements;
    }

    [Serializable]
    public class LocationUpdateRequest
    {
        public string sessionId;
        public SessionLocation location;
        public DeviceInfo device;
        public string timestamp;
    }

    [Serializable]
    public class DeviceInfo
    {
        public string model;
        public string osVersion;
        public string appVersion;
    }

    [Serializable]
    public class LocationUpdateResponse
    {
        public bool valid;
        public List<Prize> nearbyPrizes;
        public int prizeCount;
    }

    [Serializable]
    public class MapBounds
    {
        public float north;
        public float south;
        public float east;
        public float west;
    }

    [Serializable]
    public class MapDataResponse
    {
        public List<Prize> prizes;
        public MapZones zones;
        public int totalPrizes;
    }

    [Serializable]
    public class MapZones
    {
        public List<HotZone> hotZones;
    }

    [Serializable]
    public class HotZone
    {
        public string id;
        public string name;
        public float lat;
        public float lng;
        public float radius;
        public int prizeCount;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public List<LeaderboardEntry> leaderboard;
        public LeaderboardEntry userRank;
        public string type;
    }

    [Serializable]
    public class PowerUpResult
    {
        public bool success;
        public string powerUpId;
        public int duration;
        public string effect;
        public string message;
    }

    [Serializable]
    public class ChallengesResponse
    {
        public List<DailyChallenge> challenges;
        public int completedToday;
        public int totalRewardsAvailable;
    }

    [Serializable]
    public class ChallengeCompleteResponse
    {
        public bool success;
        public string challengeId;
        public int pointsEarned;
        public string[] achievements;
    }

    [Serializable]
    public class InventoryResponse
    {
        public List<InventoryItem> items;
        public List<PowerUpItem> powerUps;
        public int totalItems;
    }

    [Serializable]
    public class InventoryItem
    {
        public string id;
        public string name;
        public string type;
        public int quantity;
        public string icon;
    }

    [Serializable]
    public class PowerUpItem
    {
        public string id;
        public string name;
        public string effect;
        public int duration;
        public int quantity;
    }

    #endregion
}
