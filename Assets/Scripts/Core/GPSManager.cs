using System;
using System.Collections;
using UnityEngine;

namespace YallaCatch.Core
{
    /// <summary>
    /// Manages GPS location services for the game
    /// Provides real-time player location updates
    /// </summary>
    public class GPSManager : MonoBehaviour
    {
        public static GPSManager Instance { get; private set; }

        [Header("GPS Settings")]
        [SerializeField] private float updateInterval = 1f; // Update every second
        [SerializeField] private float desiredAccuracy = 10f; // 10 meters
        [SerializeField] private float distanceFilter = 5f; // Update when moved 5 meters

#if UNITY_EDITOR
        [Header("Editor Mock GPS")]
        [SerializeField] private bool useEditorMockLocationWhenGpsDisabled = true;
        [SerializeField] private float editorMockLatitude = 25.2048f;
        [SerializeField] private float editorMockLongitude = 55.2708f;
        [SerializeField] private float editorMockAccuracy = 5f;
#endif

        public float CurrentLatitude { get; private set; }
        public float CurrentLongitude { get; private set; }
        public float CurrentAccuracy { get; private set; }
        public bool IsLocationServiceEnabled { get; private set; }
        public bool IsInitialized { get; private set; }
        public bool InitializationFailed { get; private set; }

        public event Action<float, float> OnLocationUpdated;
        public event Action<string> OnLocationError;

        private Coroutine updateCoroutine;

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
            StartCoroutine(InitializeLocationService());
        }

        private void OnDestroy()
        {
            StopLocationService();
        }

        #endregion

        #region Location Service

        private IEnumerator InitializeLocationService()
        {
            // Check if user has location service enabled
            if (!Input.location.isEnabledByUser)
            {
#if UNITY_EDITOR
                if (useEditorMockLocationWhenGpsDisabled)
                {
                    Debug.LogWarning("Location service is not enabled by user (Editor). Initializing editor mock GPS location.");
                    ApplyEditorMockLocation();
                    yield break;
                }

                Debug.LogWarning("Location service is not enabled by user (Editor). Using mock/fallback map behavior if configured.");
#else
                Debug.LogError("Location service is not enabled by user");
#endif
                InitializationFailed = true;
                OnLocationError?.Invoke("Please enable location services in your device settings");
                yield break;
            }

            // Start location service
            Input.location.Start(desiredAccuracy, distanceFilter);

            // Wait until service initializes
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            // Service didn't initialize in 20 seconds
            if (maxWait < 1)
            {
                Debug.LogError("Location service initialization timed out");
                InitializationFailed = true;
                OnLocationError?.Invoke("Failed to initialize location services");
                yield break;
            }

            // Connection has failed
            if (Input.location.status == LocationServiceStatus.Failed)
            {
                Debug.LogError("Unable to determine device location");
                InitializationFailed = true;
                OnLocationError?.Invoke("Unable to determine your location");
                yield break;
            }

            // Success!
            IsLocationServiceEnabled = true;
            IsInitialized = true;
            
            // Get initial location
            UpdateLocation();
            
            Debug.Log($"Location initialized: {CurrentLatitude}, {CurrentLongitude}");

            // Start continuous updates
            updateCoroutine = StartCoroutine(ContinuousLocationUpdate());
        }

        private IEnumerator ContinuousLocationUpdate()
        {
            while (IsLocationServiceEnabled)
            {
                UpdateLocation();
                yield return new WaitForSeconds(updateInterval);
            }
        }

        private void UpdateLocation()
        {
            if (Input.location.status == LocationServiceStatus.Running)
            {
                CurrentLatitude = Input.location.lastData.latitude;
                CurrentLongitude = Input.location.lastData.longitude;
                CurrentAccuracy = Input.location.lastData.horizontalAccuracy;
                
                OnLocationUpdated?.Invoke(CurrentLatitude, CurrentLongitude);
            }
        }

        public void StopLocationService()
        {
            if (updateCoroutine != null)
            {
                StopCoroutine(updateCoroutine);
            }

            if (Input.location.isEnabledByUser)
            {
                Input.location.Stop();
            }

            IsLocationServiceEnabled = false;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculate distance between two GPS coordinates in meters
        /// Uses Haversine formula
        /// </summary>
        public static float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
        {
            const float R = 6371000f; // Earth radius in meters

            float dLat = Mathf.Deg2Rad * (lat2 - lat1);
            float dLon = Mathf.Deg2Rad * (lon2 - lon1);

            float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) +
                      Mathf.Cos(Mathf.Deg2Rad * lat1) * Mathf.Cos(Mathf.Deg2Rad * lat2) *
                      Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);

            float c = 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));

            return R * c;
        }

        /// <summary>
        /// Check if a prize is within capture range
        /// </summary>
        public bool IsWithinRange(float prizeLat, float prizeLon, float captureRadius)
        {
            float distance = CalculateDistance(CurrentLatitude, CurrentLongitude, prizeLat, prizeLon);
            return distance <= captureRadius;
        }

        #endregion

        #region Debug/Testing

        // For testing in Unity Editor without GPS
        public void SetMockLocation(float latitude, float longitude)
        {
            #if UNITY_EDITOR
            CurrentLatitude = latitude;
            CurrentLongitude = longitude;
            CurrentAccuracy = 5f;
            IsLocationServiceEnabled = true;
            IsInitialized = true;
            InitializationFailed = false;
            OnLocationUpdated?.Invoke(CurrentLatitude, CurrentLongitude);
            Debug.Log($"Mock location set: {latitude}, {longitude}");
            #endif
        }

#if UNITY_EDITOR
        private void ApplyEditorMockLocation()
        {
            CurrentLatitude = editorMockLatitude;
            CurrentLongitude = editorMockLongitude;
            CurrentAccuracy = editorMockAccuracy;
            IsLocationServiceEnabled = true;
            IsInitialized = true;
            InitializationFailed = false;
            OnLocationUpdated?.Invoke(CurrentLatitude, CurrentLongitude);
            Debug.Log($"[GPSManager] Editor mock GPS initialized: {editorMockLatitude}, {editorMockLongitude}");
        }
#endif

        #endregion
    }
}
