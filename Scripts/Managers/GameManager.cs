using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.Models;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Main game manager that coordinates all game systems
    /// Uses new API layer (GameAPI, CaptureAPI, etc.)
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Configuration")]
        [SerializeField] private float prizeRefreshInterval = 30f;
        [SerializeField] private float prizeScanRadius = 500f;
        [SerializeField] private float prizeCaptureRadius = 50f;

        [Header("Player Data")]
        public int PlayerPoints { get; private set; }
        public int PlayerLevel { get; private set; }
        public string PlayerUsername { get; private set; }

        [Header("Game State")]
        public bool IsGameReady { get; private set; }
        public List<Prize> NearbyPrizes { get; private set; }
        public GameSession CurrentSession { get; private set; }

        // Events
        public event System.Action OnGameReady;
        public event System.Action OnPrizesUpdated;
        public event System.Action<int> OnPointsChanged;
        public event System.Action<CaptureResult> OnPrizeCaptured;

        private Coroutine prizeRefreshCoroutine;

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                NearbyPrizes = new List<Prize>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            StartCoroutine(InitializeGame());
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && CurrentSession != null)
            {
                EndGameSession();
            }
        }

        #endregion

        #region Initialization

        private IEnumerator InitializeGame()
        {
            Debug.Log("[GameManager] Initializing...");

            // Wait for GPS to initialize
            while (GPSManager.Instance == null || !GPSManager.Instance.IsInitialized)
            {
                yield return new WaitForSeconds(0.5f);
            }

            Debug.Log("[GameManager] GPS initialized");

            // Wait for API to be ready
            while (APIClient.Instance == null)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // If authenticated, load user profile
            if (APIClient.Instance.IsAuthenticated)
            {
                yield return StartCoroutine(LoadUserProfile());
            }

            // Start game session
            StartGameSession();

            // Start prize refresh coroutine
            prizeRefreshCoroutine = StartCoroutine(RefreshPrizesCoroutine());

            IsGameReady = true;
            OnGameReady?.Invoke();

            Debug.Log("[GameManager] Game ready!");
        }

        private IEnumerator LoadUserProfile()
        {
            bool loaded = false;

            AuthAPI.Instance.GetProfile(response =>
            {
                if (response.success && response.data != null)
                {
                    var user = response.data;
                    PlayerUsername = user.displayName;
                    PlayerPoints = user.AvailablePoints;
                    PlayerLevel = int.TryParse(user.level, out int lvl) ? lvl : 1;
                    OnPointsChanged?.Invoke(PlayerPoints);
                    Debug.Log($"[GameManager] Profile loaded: {PlayerUsername}, Points: {PlayerPoints}");
                }
                loaded = true;
            });

            while (!loaded) yield return null;
        }

        #endregion

        #region Game Session

        private void StartGameSession()
        {
            if (GameAPI.Instance == null) return;

            var request = new SessionStartRequest
            {
                deviceId = APIClient.Instance.GetDeviceId(),
                platform = APIClient.Instance.GetPlatform(),
                version = Application.version,
                deviceModel = SystemInfo.deviceModel,
                osVersion = SystemInfo.operatingSystem,
                appVersion = Application.version,
                location = new SessionLocation
                {
                    latitude = GPSManager.Instance.CurrentLatitude,
                    longitude = GPSManager.Instance.CurrentLongitude,
                    accuracy = GPSManager.Instance.CurrentAccuracy
                }
            };

            GameAPI.Instance.StartSession(request, response =>
            {
                if (response.success && response.data != null)
                {
                    CurrentSession = response.data;
                    Debug.Log($"[GameManager] Session started: {CurrentSession.sessionId}");
                }
            });
        }

        public void EndGameSession()
        {
            if (GameAPI.Instance == null || CurrentSession == null) return;

            GameAPI.Instance.EndSession(CurrentSession.sessionId, response =>
            {
                if (response.success && response.data != null)
                {
                    Debug.Log($"[GameManager] Session ended. Points earned: {response.data.pointsEarned}");
                    PlayerPoints += response.data.pointsEarned;
                    OnPointsChanged?.Invoke(PlayerPoints);
                }
                CurrentSession = null;
            });
        }

        #endregion

        #region Prize Management

        private IEnumerator RefreshPrizesCoroutine()
        {
            while (true)
            {
                RefreshNearbyPrizes();
                yield return new WaitForSeconds(prizeRefreshInterval);
            }
        }

        public void RefreshNearbyPrizes()
        {
            if (GameAPI.Instance == null || GPSManager.Instance == null) return;

            float lat = GPSManager.Instance.CurrentLatitude;
            float lng = GPSManager.Instance.CurrentLongitude;

            // Calculate bounds for map data
            float offset = prizeScanRadius / 111000f; // Rough conversion to degrees
            var bounds = new MapBounds
            {
                north = lat + offset,
                south = lat - offset,
                east = lng + offset,
                west = lng - offset
            };

            GameAPI.Instance.GetMapData(bounds, response =>
            {
                if (response.success && response.data != null)
                {
                    NearbyPrizes.Clear();
                    NearbyPrizes.AddRange(response.data.prizes);
                    OnPrizesUpdated?.Invoke();
                    Debug.Log($"[GameManager] Found {NearbyPrizes.Count} nearby prizes");
                }
            });
        }

        public bool CanCapturePrize(Prize prize)
        {
            if (prize == null || GPSManager.Instance == null) return false;

            return GPSManager.Instance.IsWithinRange(
                prize.Latitude,
                prize.Longitude,
                prizeCaptureRadius
            );
        }

        public void CapturePrize(Prize prize, System.Action<bool, string, int> callback)
        {
            if (!CanCapturePrize(prize))
            {
                callback?.Invoke(false, "Prize is too far away!", 0);
                return;
            }

            var request = new CaptureAttemptRequest
            {
                prizeId = prize._id,
                location = new CaptureLocation
                {
                    latitude = GPSManager.Instance.CurrentLatitude,
                    longitude = GPSManager.Instance.CurrentLongitude,
                    accuracy = GPSManager.Instance.CurrentAccuracy
                },
                deviceInfo = new CaptureDeviceInfo
                {
                    platform = APIClient.Instance.GetPlatform(),
                    deviceModel = SystemInfo.deviceModel,
                    osVersion = SystemInfo.operatingSystem,
                    appVersion = Application.version,
                    timestamp = System.DateTime.UtcNow.ToString("o")
                },
                captureMethod = "tap"
            };

            CaptureAPI.Instance.AttemptCapture(request, response =>
            {
                if (response.success && response.data != null)
                {
                    var result = response.data;
                    int points = result.content?.points ?? 0;
                    
                    PlayerPoints += points;
                    OnPointsChanged?.Invoke(PlayerPoints);

                    // Remove captured prize from list
                    NearbyPrizes.RemoveAll(p => p._id == prize._id);
                    OnPrizesUpdated?.Invoke();

                    // Confirm capture
                    if (!string.IsNullOrEmpty(result.claimId))
                    {
                        CaptureAPI.Instance.ConfirmCapture(result.claimId, _ => { });
                    }

                    OnPrizeCaptured?.Invoke(result);
                    callback?.Invoke(true, "Prize captured!", points);
                }
                else
                {
                    callback?.Invoke(false, response.message ?? "Capture failed", 0);
                }
            });
        }

        #endregion

        #region Location Updates

        public void UpdatePlayerLocation()
        {
            if (GameAPI.Instance == null || CurrentSession == null) return;

            var request = new LocationUpdateRequest
            {
                sessionId = CurrentSession.sessionId,
                location = new SessionLocation
                {
                    latitude = GPSManager.Instance.CurrentLatitude,
                    longitude = GPSManager.Instance.CurrentLongitude,
                    accuracy = GPSManager.Instance.CurrentAccuracy
                },
                timestamp = System.DateTime.UtcNow.ToString("o")
            };

            GameAPI.Instance.UpdateLocation(request, response =>
            {
                if (response.success && response.data != null && response.data.nearbyPrizes != null)
                {
                    // Update prizes from location response
                    NearbyPrizes.Clear();
                    NearbyPrizes.AddRange(response.data.nearbyPrizes);
                    OnPrizesUpdated?.Invoke();
                }
            });
        }

        #endregion

        #region Challenges

        public void GetDailyChallenges(System.Action<List<DailyChallenge>> callback)
        {
            GameAPI.Instance.GetChallenges(response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.challenges);
                }
                else
                {
                    callback?.Invoke(new List<DailyChallenge>());
                }
            });
        }

        public void CompleteChallenge(string challengeId, System.Action<bool, int> callback)
        {
            GameAPI.Instance.CompleteChallenge(challengeId, response =>
            {
                if (response.success && response.data != null)
                {
                    PlayerPoints += response.data.pointsEarned;
                    OnPointsChanged?.Invoke(PlayerPoints);
                    callback?.Invoke(true, response.data.pointsEarned);
                }
                else
                {
                    callback?.Invoke(false, 0);
                }
            });
        }

        #endregion

        #region Leaderboard

        public void GetLeaderboard(string type, System.Action<List<LeaderboardEntry>, LeaderboardEntry> callback)
        {
            GameAPI.Instance.GetLeaderboard(type, 50, response =>
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

        #region Utility

        public void AddPoints(int points)
        {
            PlayerPoints += points;
            OnPointsChanged?.Invoke(PlayerPoints);
        }

        public void SetPlayerData(string username, int points, int level)
        {
            PlayerUsername = username;
            PlayerPoints = points;
            PlayerLevel = level;
            OnPointsChanged?.Invoke(PlayerPoints);
        }

        #endregion
    }
}
