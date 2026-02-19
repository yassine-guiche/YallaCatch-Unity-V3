using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YallaCatch.Managers;

namespace YallaCatch.Core
{
    /// <summary>
    /// Controls the interactive map display
    /// Handles map tiles, player marker, and prize markers
    /// Uses OpenStreetMap or Google Maps tiles
    /// </summary>
    public class MapController : MonoBehaviour
    {
    [Header("Map Configuration")]
    [SerializeField] private RawImage mapImage;
    [SerializeField] private int zoomLevel = 16;
    [SerializeField] private int tileSize = 256;
    [SerializeField] private string tileServerURL = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    
    [Header("Full Screen Mode")]
    [SerializeField] private bool isFullScreenMode = false;
    [SerializeField] private GameObject fullScreenMapContainer;
    [SerializeField] private Button centerOnPlayerButton;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;

    [Header("Markers")]
    [SerializeField] private GameObject playerMarkerPrefab;
    [SerializeField] private GameObject prizeMarkerPrefab;
    [SerializeField] private Transform markersContainer;
    
    [Header("Heatmap")]
    [SerializeField] private bool showHeatmap = false;
    [SerializeField] private RawImage heatmapOverlay;
    [SerializeField] private Gradient heatmapGradient;
    [SerializeField] private float heatmapRadius = 50f;

        private GameObject playerMarker;
        private Dictionary<string, GameObject> prizeMarkers = new Dictionary<string, GameObject>();
        
        private float currentLat;
        private float currentLon;
        private Texture2D mapTexture;
        private Texture2D heatmapTexture;
        private List<HeatmapPoint> heatmapData = new List<HeatmapPoint>();

        #region Unity Lifecycle

        private void Start()
        {
            // Subscribe to GPS updates
            GPSManager.Instance.OnLocationUpdated += OnPlayerLocationUpdated;
            
            // Subscribe to prize updates
            GameManager.Instance.OnPrizesUpdated += OnPrizesUpdated;

            // Create player marker
            CreatePlayerMarker();

            // Initial map load
            if (GPSManager.Instance.IsInitialized)
            {
                UpdateMap(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
            }
        }

        private void OnDestroy()
        {
            if (GPSManager.Instance != null)
            {
                GPSManager.Instance.OnLocationUpdated -= OnPlayerLocationUpdated;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPrizesUpdated -= OnPrizesUpdated;
            }
        }

        #endregion

        #region Map Updates

        private void OnPlayerLocationUpdated(float lat, float lon)
        {
            UpdateMap(lat, lon);
            UpdatePlayerMarkerPosition(lat, lon);
        }

        private void UpdateMap(float lat, float lon)
        {
            currentLat = lat;
            currentLon = lon;

            StartCoroutine(LoadMapTile(lat, lon, zoomLevel));
        }

        private IEnumerator LoadMapTile(float lat, float lon, int zoom)
        {
            // Convert lat/lon to tile coordinates
            int tileX = LonToTileX(lon, zoom);
            int tileY = LatToTileY(lat, zoom);

            string url = tileServerURL
                .Replace("{z}", zoom.ToString())
                .Replace("{x}", tileX.ToString())
                .Replace("{y}", tileY.ToString());

            using (WWW www = new WWW(url))
            {
                yield return www;

                if (string.IsNullOrEmpty(www.error))
                {
                    mapTexture = www.texture;
                    mapImage.texture = mapTexture;
                }
                else
                {
                    Debug.LogError($"Failed to load map tile: {www.error}");
                }
            }
        }

        #endregion

        #region Markers

        private void CreatePlayerMarker()
        {
            if (playerMarkerPrefab != null)
            {
                playerMarker = Instantiate(playerMarkerPrefab, markersContainer);
                playerMarker.name = "PlayerMarker";
            }
        }

        private void UpdatePlayerMarkerPosition(float lat, float lon)
        {
            if (playerMarker != null)
            {
                Vector2 screenPos = LatLonToScreenPosition(lat, lon);
                playerMarker.transform.localPosition = screenPos;
            }
        }

        private void OnPrizesUpdated()
        {
            // Clear old markers
            foreach (var marker in prizeMarkers.Values)
            {
                Destroy(marker);
            }
            prizeMarkers.Clear();

            // Create new markers
            foreach (var prize in GameManager.Instance.NearbyPrizes)
            {
                CreatePrizeMarker(prize);
            }
        }

        private void CreatePrizeMarker(Prize prize)
        {
            if (prizeMarkerPrefab != null && !prizeMarkers.ContainsKey(prize._id))
            {
                GameObject marker = Instantiate(prizeMarkerPrefab, markersContainer);
                marker.name = $"Prize_{prize.name}";

                Vector2 screenPos = LatLonToScreenPosition(prize.location.latitude, prize.location.longitude);
                marker.transform.localPosition = screenPos;

                // Add click handler
                Button button = marker.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnPrizeMarkerClicked(prize));
                }

                prizeMarkers[prize._id] = marker;
            }
        }

        private void OnPrizeMarkerClicked(Prize prize)
        {
            // Check if prize is in range
            bool inRange = GameManager.Instance.CanCapturePrize(prize);
            
            if (inRange)
            {
                // Show capture UI
                UIManager.Instance?.ShowCaptureDialog(prize);
            }
            else
            {
                // Show "too far" message
                float distance = GPSManager.CalculateDistance(
                    GPSManager.Instance.CurrentLatitude,
                    GPSManager.Instance.CurrentLongitude,
                    prize.location.latitude,
                    prize.location.longitude
                );
                UIManager.Instance?.ShowMessage($"Prize is {distance:F0}m away. Get closer to capture!");
            }
        }

        #endregion

        #region Coordinate Conversion

        private int LonToTileX(float lon, int zoom)
        {
            return (int)((lon + 180.0) / 360.0 * (1 << zoom));
        }

        private int LatToTileY(float lat, int zoom)
        {
            float latRad = lat * Mathf.Deg2Rad;
            return (int)((1.0 - Mathf.Log(Mathf.Tan(latRad) + 1.0 / Mathf.Cos(latRad)) / Mathf.PI) / 2.0 * (1 << zoom));
        }

        private Vector2 LatLonToScreenPosition(float lat, float lon)
        {
            // Convert to relative position on map
            float relX = (lon - currentLon) * 111320f * Mathf.Cos(currentLat * Mathf.Deg2Rad);
            float relY = (lat - currentLat) * 110540f;

            // Convert to screen coordinates
            float screenX = relX / 10f; // Scale factor
            float screenY = relY / 10f;

            return new Vector2(screenX, screenY);
        }

        #endregion

        #region Public Methods

        public void CenterOnPlayer()
        {
            UpdateMap(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
        }

        public void SetZoomLevel(int zoom)
        {
            zoomLevel = Mathf.Clamp(zoom, 10, 18);
            UpdateMap(currentLat, currentLon);
        }
        
        /// <summary>
        /// Active le mode plein écran (mode map alternatif)
        /// </summary>
        public void ActivateFullScreenMode()
        {
            isFullScreenMode = true;
            
            if (fullScreenMapContainer != null)
            {
                fullScreenMapContainer.SetActive(true);
            }
            
            // Setup UI controls
            SetupFullScreenControls();
            
            // Refresh map
            UpdateMap(currentLat, currentLon);
            
            Debug.Log("[MapController] Full screen mode activated");
        }
        
        /// <summary>
        /// Désactive le mode plein écran
        /// </summary>
        public void DeactivateFullScreenMode()
        {
            isFullScreenMode = false;
            
            if (fullScreenMapContainer != null)
            {
                fullScreenMapContainer.SetActive(false);
            }
            
            Debug.Log("[MapController] Full screen mode deactivated");
        }
        
        /// <summary>
        /// Configure les contrôles du mode plein écran
        /// </summary>
        private void SetupFullScreenControls()
        {
            if (centerOnPlayerButton != null)
            {
                centerOnPlayerButton.onClick.RemoveAllListeners();
                centerOnPlayerButton.onClick.AddListener(CenterOnPlayer);
            }
            
            if (zoomInButton != null)
            {
                zoomInButton.onClick.RemoveAllListeners();
                zoomInButton.onClick.AddListener(() => SetZoomLevel(zoomLevel + 1));
            }
            
            if (zoomOutButton != null)
            {
                zoomOutButton.onClick.RemoveAllListeners();
                zoomOutButton.onClick.AddListener(() => SetZoomLevel(zoomLevel - 1));
            }
        }
        
        /// <summary>
        /// Vérifie si on est en mode plein écran
        /// </summary>
        public bool IsFullScreenMode()
        {
            return isFullScreenMode;
        }
        
        /// <summary>
        /// Active/désactive la heatmap
        /// </summary>
        public void ToggleHeatmap(bool enabled)
        {
            showHeatmap = enabled;
            
            if (showHeatmap)
            {
                StartCoroutine(LoadHeatmapData());
            }
            else
            {
                if (heatmapOverlay != null)
                {
                    heatmapOverlay.gameObject.SetActive(false);
                }
            }
        }
        
        /// <summary>
        /// Charge les données de heatmap depuis le backend
        /// GET /prizes/heatmap/:city?
        /// </summary>
        private IEnumerator LoadHeatmapData()
        {
            string city = ConfigManager.Instance?.GetCurrentCity() ?? "";
            string endpoint = string.IsNullOrEmpty(city) ? "/prizes/heatmap" : $"/prizes/heatmap/{city}";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    heatmapData = JsonConvert.DeserializeObject<List<HeatmapPoint>>(response.data.ToString());
                    GenerateHeatmapTexture();
                    
                    Debug.Log($"[MapController] Loaded {heatmapData.Count} heatmap points");
                }
            });
        }
        
        /// <summary>
        /// Génère la texture de heatmap
        /// </summary>
        private void GenerateHeatmapTexture()
        {
            if (heatmapData.Count == 0)
                return;
            
            int width = 512;
            int height = 512;
            
            heatmapTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            
            // Initialiser avec transparent
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            // Dessiner chaque point de chaleur
            foreach (var point in heatmapData)
            {
                Vector2 screenPos = LatLonToScreenPosition(point.latitude, point.longitude);
                
                // Convertir en coordonnées de texture
                int centerX = (int)((screenPos.x / 1000f + 0.5f) * width);
                int centerY = (int)((screenPos.y / 1000f + 0.5f) * height);
                
                // Dessiner un gradient circulaire
                int radius = (int)(heatmapRadius * point.intensity);
                
                for (int y = -radius; y <= radius; y++)
                {
                    for (int x = -radius; x <= radius; x++)
                    {
                        int px = centerX + x;
                        int py = centerY + y;
                        
                        if (px >= 0 && px < width && py >= 0 && py < height)
                        {
                            float distance = Mathf.Sqrt(x * x + y * y);
                            
                            if (distance <= radius)
                            {
                                float t = 1f - (distance / radius);
                                Color heatColor = heatmapGradient.Evaluate(t * point.intensity);
                                
                                int index = py * width + px;
                                pixels[index] = Color.Lerp(pixels[index], heatColor, 0.5f);
                            }
                        }
                    }
                }
            }
            
            heatmapTexture.SetPixels(pixels);
            heatmapTexture.Apply();
            
            // Afficher la heatmap
            if (heatmapOverlay != null)
            {
                heatmapOverlay.texture = heatmapTexture;
                heatmapOverlay.gameObject.SetActive(true);
            }
        }

        #endregion
    }
    
    #region Data Classes
    
    [System.Serializable]
    public class HeatmapPoint
    {
        public float latitude;
        public float longitude;
        public float intensity; // 0-1
        public int prizeCount;
    }
    
    #endregion
}
