using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json.Linq;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.Models;
using YallaCatch.UI;
using YallaCatch.Networking;
using CaptureAttemptResult = YallaCatch.Models.CaptureResult;

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
        [SerializeField] private float realtimeRefreshDebounceSeconds = 0.75f;
        [SerializeField] private float locationTelemetryIntervalSeconds = 5f;
        [SerializeField] private float locationTelemetryDistanceThresholdMeters = 15f;
        [SerializeField] private float movementMapRefreshDistanceThresholdMeters = 35f;
        [SerializeField] private float movementMapRefreshMinIntervalSeconds = 2f;
        [SerializeField] private bool subscribeToVisiblePartnerRealtimeRooms = true;

        // Runtime player data (Header attributes are only valid on fields).
        public int PlayerPoints { get; private set; }
        public int PlayerLevel { get; private set; }
        public string PlayerUsername { get; private set; }
        public string PlayerId { get; private set; }

        // Runtime game state (Header attributes are only valid on fields).
        public bool IsGameReady { get; private set; }
        public List<Prize> NearbyPrizes { get; private set; }
        public List<GameMapMarkerWire> NearbyPartnerMarkers { get; private set; }
        public GameSession CurrentSession { get; private set; }

        // Events
        public event System.Action OnGameReady;
        public event System.Action OnPrizesUpdated;
        public event System.Action<int> OnPointsChanged;

        private float lastRealtimeRefreshAt;
        public event System.Action<CaptureAttemptResult> OnPrizeCaptured;

        private Coroutine prizeRefreshCoroutine;
        private bool isInitializing;
        private bool gpsLocationHooksRegistered;
        private bool hasTelemetryAnchor;
        private bool hasMovementRefreshAnchor;
        private float lastTelemetryLat;
        private float lastTelemetryLng;
        private float lastMovementRefreshLat;
        private float lastMovementRefreshLng;
        private float lastLocationTelemetrySentAt = -999f;
        private float lastMovementRefreshAt = -999f;
        private readonly HashSet<string> subscribedPartnerRooms = new HashSet<string>(System.StringComparer.Ordinal);

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                NearbyPrizes = new List<Prize>();
                NearbyPartnerMarkers = new List<GameMapMarkerWire>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            TryInitializeGame();
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
            while (GPSManager.Instance == null || (!GPSManager.Instance.IsInitialized && !GPSManager.Instance.InitializationFailed))
            {
                yield return new WaitForSeconds(0.5f);
            }

            if (GPSManager.Instance != null && GPSManager.Instance.InitializationFailed)
            {
                isInitializing = false;
                Debug.LogError("[GameManager] GPS Initialization failed. Halting game initialization.");
                UIManager.Instance?.ShowMessage("<color=red>LOCATION REQUIRED</color>\nYallaCatch requires GPS to function. Please enable location services in your device settings and restart the app.");
                yield break;
            }

            Debug.Log("[GameManager] GPS initialized");
            RegisterGpsLocationCallbacks();

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

            if (APIClient.Instance == null || !APIClient.Instance.IsAuthenticated)
            {
                isInitializing = false;
                Debug.Log("[GameManager] Initialization halted: no valid authenticated session.");
                yield break;
            }

            // Start game session
            StartGameSession();

            // Start prize refresh coroutine (Fallback/Interval)
            prizeRefreshCoroutine = StartCoroutine(RefreshPrizesCoroutine());

            // Subscribe to real-time updates via SocketIO
            if (SocketIOClient.Instance != null)
            {
                SocketIOClient.Instance.OnEventReceived += HandleSocketEvent;
            }
            IsGameReady = true;
            isInitializing = false;
            OnGameReady?.Invoke();

            Debug.Log("[GameManager] Game ready with Real-Time sync!");
        }

        private void OnDestroy()
        {
            if (SocketIOClient.Instance != null)
            {
                SocketIOClient.Instance.OnEventReceived -= HandleSocketEvent;
            }

            foreach (string room in subscribedPartnerRooms)
            {
                SocketIOClient.Instance?.Unsubscribe(room);
            }
            subscribedPartnerRooms.Clear();
            UnregisterGpsLocationCallbacks();
        }

        public void InitializeAfterAuthentication()
        {
            TryInitializeGame();
        }

        private void TryInitializeGame()
        {
            if (IsGameReady || isInitializing)
            {
                return;
            }

            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                Debug.Log("[GameManager] Skipping game initialization until player is authenticated.");
                return;
            }

            isInitializing = true;
            StartCoroutine(InitializeGame());
        }

        private void RegisterGpsLocationCallbacks()
        {
            if (gpsLocationHooksRegistered || GPSManager.Instance == null)
            {
                return;
            }

            GPSManager.Instance.OnLocationUpdated += HandleGpsLocationUpdated;
            gpsLocationHooksRegistered = true;
            SeedLocationAnchors(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
        }

        private void UnregisterGpsLocationCallbacks()
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
            if (float.IsNaN(lat) || float.IsNaN(lng) || float.IsInfinity(lat) || float.IsInfinity(lng))
            {
                return;
            }

            if (!hasTelemetryAnchor || !hasMovementRefreshAnchor)
            {
                SeedLocationAnchors(lat, lng);
            }

            TrySendLocationTelemetry(lat, lng);
            TryRefreshMapOnMovement(lat, lng);
        }

        private void TrySendLocationTelemetry(float lat, float lng)
        {
            if (!IsGameReady || CurrentSession == null || GameAPI.Instance == null || GPSManager.Instance == null)
            {
                return;
            }

            bool intervalElapsed = (Time.time - lastLocationTelemetrySentAt) >= Mathf.Max(1f, locationTelemetryIntervalSeconds);
            bool distanceThresholdMet = hasTelemetryAnchor &&
                GPSManager.CalculateDistance(lastTelemetryLat, lastTelemetryLng, lat, lng) >= Mathf.Max(1f, locationTelemetryDistanceThresholdMeters);

            if (!intervalElapsed && !distanceThresholdMet)
            {
                return;
            }

            lastLocationTelemetrySentAt = Time.time;
            lastTelemetryLat = lat;
            lastTelemetryLng = lng;
            hasTelemetryAnchor = true;
            UpdatePlayerLocation();
        }

        private void TryRefreshMapOnMovement(float lat, float lng)
        {
            if (!IsGameReady || GameAPI.Instance == null || GPSManager.Instance == null)
            {
                return;
            }

            if (!hasMovementRefreshAnchor)
            {
                lastMovementRefreshLat = lat;
                lastMovementRefreshLng = lng;
                hasMovementRefreshAnchor = true;
                return;
            }

            float movedMeters = GPSManager.CalculateDistance(lastMovementRefreshLat, lastMovementRefreshLng, lat, lng);
            if (movedMeters < Mathf.Max(1f, movementMapRefreshDistanceThresholdMeters))
            {
                return;
            }

            if ((Time.time - lastMovementRefreshAt) < Mathf.Max(0.25f, movementMapRefreshMinIntervalSeconds))
            {
                return;
            }

            lastMovementRefreshAt = Time.time;
            lastMovementRefreshLat = lat;
            lastMovementRefreshLng = lng;
            RefreshNearbyPrizes();
        }

        private void SeedLocationAnchors(float lat, float lng)
        {
            if (float.IsNaN(lat) || float.IsNaN(lng) || float.IsInfinity(lat) || float.IsInfinity(lng))
            {
                return;
            }

            lastTelemetryLat = lat;
            lastTelemetryLng = lng;
            lastMovementRefreshLat = lat;
            lastMovementRefreshLng = lng;
            hasTelemetryAnchor = true;
            hasMovementRefreshAnchor = true;
        }

        private void HandleSocketEvent(string eventName, string data)
        {
            if (eventName == "game_event" || eventName == "room_event")
            {
                string derivedType = TryExtractRealtimeType(data);
                if (!string.IsNullOrWhiteSpace(derivedType))
                {
                    HandleRealtimeSignal(derivedType);
                }
            }

            HandleRealtimeSignal(eventName);
        }

        private void HandleRealtimeSignal(string eventName)
        {
            if (eventName == "game_update" ||
                eventName == "game_event" ||
                eventName == "room_event" ||
                eventName == "prize_deployed" ||
                eventName == "prize_update" ||
                eventName == "partner_update" ||
                eventName == "capture_created" ||
                eventName == "redemption_created")
            {
                if ((Time.time - lastRealtimeRefreshAt) < realtimeRefreshDebounceSeconds)
                {
                    return;
                }

                lastRealtimeRefreshAt = Time.time;
                Debug.Log($"[GameManager] Real-time signal received: {eventName}. Refreshing prizes...");
                RefreshNearbyPrizes();
            }
        }

        private string TryExtractRealtimeType(string data)
        {
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            try
            {
                var token = JToken.Parse(data);
                return token["type"]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private IEnumerator LoadUserProfile()
        {
            bool loaded = false;

            AuthAPI.Instance.GetProfile(response =>
            {
                if (response.success && response.data != null)
                {
                    var user = response.data;
                    PlayerId = user.Id;
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
            if (GameAPI.Instance == null || GPSManager.Instance == null) return;

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
                    SeedLocationAnchors(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
                    lastLocationTelemetrySentAt = Time.time;
                    lastMovementRefreshAt = Time.time;
                    if (string.IsNullOrWhiteSpace(PlayerId))
                    {
                        PlayerId = CurrentSession.userId;
                    }
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
                hasTelemetryAnchor = false;
                hasMovementRefreshAnchor = false;
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
                    ReplaceNearbyPrizes(response.data.prizes);
                    ReplaceNearbyPartnerMarkers(response.data);
                    SyncPartnerRealtimeSubscriptions(response.data);
                    OnPrizesUpdated?.Invoke();
                    Debug.Log($"[GameManager] Map refresh: {NearbyPrizes.Count} prizes, {NearbyPartnerMarkers.Count} partner markers");
                }
            });
        }

        /// <summary>
        /// Compatibility wrapper used by legacy managers (e.g. radar power-up) to force a nearby prize refresh.
        /// </summary>
        public void LoadNearbyPrizes(float scanRadius = -1f)
        {
            if (scanRadius <= 0f)
            {
                RefreshNearbyPrizes();
                return;
            }

            float previousRadius = prizeScanRadius;
            prizeScanRadius = scanRadius;
            try
            {
                RefreshNearbyPrizes();
            }
            finally
            {
                prizeScanRadius = previousRadius;
            }
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

                    // Confirm capture with anti-cheat telemetry
                    if (!string.IsNullOrEmpty(result.prizeId))
                    {
                        var confirmRequest = new ConfirmCaptureRequest
                        {
                            prizeId = result.prizeId,
                            location = new CaptureLocation
                            {
                                latitude = GPSManager.Instance.CurrentLatitude,
                                longitude = GPSManager.Instance.CurrentLongitude,
                                accuracy = GPSManager.Instance.CurrentAccuracy
                            },
                            deviceSignals = new DeviceSignals
                            {
                                speed = 0, // Speed can be calculated from GPS delta if needed
                                mockLocation = Application.isEditor // Basic check
                            },
                            idempotencyKey = $"CONFIRM_{prize._id}_{System.DateTime.UtcNow.Ticks}"
                        };
                        CaptureAPI.Instance.ConfirmCapture(confirmRequest, _ => { });
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
            if (GameAPI.Instance == null || CurrentSession == null || GPSManager.Instance == null) return;

            var request = new LocationUpdateRequest
            {
                sessionId = CurrentSession.sessionId,
                location = new SessionLocation
                {
                    latitude = GPSManager.Instance.CurrentLatitude,
                    longitude = GPSManager.Instance.CurrentLongitude,
                    accuracy = GPSManager.Instance.CurrentAccuracy
                },
                device = new DeviceInfo
                {
                    model = SystemInfo.deviceModel,
                    osVersion = SystemInfo.operatingSystem,
                    appVersion = Application.version
                },
                timestamp = System.DateTime.UtcNow.ToString("o")
            };

            GameAPI.Instance.UpdateLocation(request, response =>
            {
                if (response.success && response.data != null && response.data.nearbyPrizes != null)
                {
                    // Update prizes from location response
                    ReplaceNearbyPrizes(response.data.nearbyPrizes);
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

        /// <summary>
        /// Compatibility hook for managers that award XP separately.
        /// Current runtime model does not track XP independently, so this is a no-op placeholder.
        /// </summary>
        public void AddXP(int xp)
        {
            if (xp > 0)
            {
                Debug.Log($"[GameManager] XP awarded (compat placeholder): {xp}");
            }
        }

        public void SetPoints(int points)
        {
            PlayerPoints = points;
            OnPointsChanged?.Invoke(PlayerPoints);
        }

        public void SetPlayerData(string username, int points, int level)
        {
            PlayerUsername = username;
            PlayerPoints = points;
            PlayerLevel = level;
            OnPointsChanged?.Invoke(PlayerPoints);

            if (!IsGameReady && APIClient.Instance != null && APIClient.Instance.IsAuthenticated)
            {
                TryInitializeGame();
            }
        }

        private void ReplaceNearbyPrizes(List<GamePrizeWire> wirePrizes)
        {
            NearbyPrizes.Clear();
            if (wirePrizes == null) return;

            foreach (var wire in wirePrizes)
            {
                NearbyPrizes.Add(MapPrize(wire));
            }
        }

        private void ReplaceNearbyPartnerMarkers(MapDataResponse mapData)
        {
            NearbyPartnerMarkers.Clear();
            if (mapData == null)
            {
                return;
            }

            // Preferred path: backend already returns a flattened "markers" array with both prize and partner markers.
            if (mapData.markers != null && mapData.markers.Count > 0)
            {
                foreach (var marker in mapData.markers)
                {
                    if (marker == null || marker.position == null)
                    {
                        continue;
                    }

                    if (!string.Equals(marker.type, "partner", System.StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(marker.id))
                    {
                        continue;
                    }

                    NearbyPartnerMarkers.Add(marker);
                }

                return;
            }

            // Compatibility fallback for older payloads that only expose "partners" with nested locations.
            if (mapData.partners == null)
            {
                return;
            }

            foreach (var partner in mapData.partners)
            {
                if (partner?.locations == null)
                {
                    continue;
                }

                foreach (var loc in partner.locations)
                {
                    if (loc?.position == null || string.IsNullOrWhiteSpace(loc.id))
                    {
                        continue;
                    }

                    NearbyPartnerMarkers.Add(new GameMapMarkerWire
                    {
                        id = loc.id,
                        type = "partner",
                        title = !string.IsNullOrWhiteSpace(partner.name) ? partner.name : loc.name,
                        category = partner.category,
                        position = new GamePrizePosition
                        {
                            lat = loc.position.lat,
                            lng = loc.position.lng
                        }
                    });
                }
            }
        }

        private void SyncPartnerRealtimeSubscriptions(MapDataResponse mapData)
        {
            if (!subscribeToVisiblePartnerRealtimeRooms)
            {
                return;
            }

            // Requires partner IDs; if payload omits "partners", keep current subscriptions untouched.
            if (mapData?.partners == null)
            {
                return;
            }

            var desiredRooms = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var partner in mapData.partners)
            {
                if (partner == null || string.IsNullOrWhiteSpace(partner.id))
                {
                    continue;
                }

                desiredRooms.Add($"partner:{partner.id}");
            }

            var roomsToRemove = new List<string>();
            foreach (string room in subscribedPartnerRooms)
            {
                if (!desiredRooms.Contains(room))
                {
                    roomsToRemove.Add(room);
                }
            }

            foreach (string room in roomsToRemove)
            {
                SocketIOClient.Instance?.Unsubscribe(room);
                subscribedPartnerRooms.Remove(room);
            }

            foreach (string room in desiredRooms)
            {
                if (subscribedPartnerRooms.Add(room))
                {
                    SocketIOClient.Instance?.Subscribe(room);
                }
            }
        }

        private Prize MapPrize(GamePrizeWire wire)
        {
            if (wire == null)
            {
                return new Prize();
            }

            return new Prize
            {
                _id = wire.id,
                name = wire.title,
                description = wire.title,
                category = wire.category,
                rarity = wire.rarity,
                pointsValue = wire.points,
                location = new PrizeLocation
                {
                    lat = wire.position?.lat ?? 0f,
                    lng = wire.position?.lng ?? 0f
                },
                lat = wire.position?.lat ?? 0f,
                lng = wire.position?.lng ?? 0f,
                expiresAt = wire.expiresAt
            };
        }

        #endregion
    }
}
