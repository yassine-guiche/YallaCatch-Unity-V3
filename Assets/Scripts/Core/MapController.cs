using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YallaCatch.API;
using YallaCatch.Managers;
using YallaCatch.Models;
using YallaCatch.UI;
using Newtonsoft.Json;

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
    [SerializeField] private bool autoLoadFallbackMapWithoutGps = true;
    [SerializeField] private float fallbackLatitude = 25.2048f;   // Dubai (configurable in Inspector)
    [SerializeField] private float fallbackLongitude = 55.2708f;
    [SerializeField] private bool useEditorMockGpsFallback = true;
    
    [Header("Full Screen Mode")]
    [SerializeField] private bool isFullScreenMode = false;
    [SerializeField] private GameObject fullScreenMapContainer;
    [SerializeField] private Button centerOnPlayerButton;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button zoomOutButton;

    [Header("Markers")]
    [SerializeField] private GameObject playerMarkerPrefab;
    [SerializeField] private GameObject prizeMarkerPrefab;
    [SerializeField] private GameObject partnerMarkerPrefab;
    [SerializeField] private GameObject nearbyPlayerMarkerPrefab;
    [SerializeField] private Transform markersContainer;
    
    [Header("Heatmap")]
    [SerializeField] private bool showHeatmap = false;
    [SerializeField] private RawImage heatmapOverlay;
    [SerializeField] private Gradient heatmapGradient;
    [SerializeField] private float heatmapRadius = 50f;

        private GameObject playerMarker;
        private Dictionary<string, GameObject> prizeMarkers = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> partnerMarkers = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> nearbyPlayerMarkers = new Dictionary<string, GameObject>();
        
        private float currentLat;
        private float currentLon;
        private Texture2D mapTexture;
        private Texture2D heatmapTexture;
        private List<HeatmapPoint> heatmapData = new List<HeatmapPoint>();
        private Coroutine mapLoadCoroutine;
        private bool mapTileLoadedAtLeastOnce;
        private string lastRequestedTileKey;
        private bool fallbackLocationApplied;
        private static Sprite fallbackUiSprite;

        #region Unity Lifecycle

        private void Start()
        {
            if (mapImage == null)
            {
                Debug.LogWarning("[MapController] mapImage is not assigned. Map tiles cannot be displayed.");
            }

            // Subscribe to GPS updates
            if (GPSManager.Instance != null)
            {
                GPSManager.Instance.OnLocationUpdated += OnPlayerLocationUpdated;
                GPSManager.Instance.OnLocationError += OnLocationError;
            }
            else
            {
                Debug.LogWarning("[MapController] GPSManager instance not found at startup.");
            }
            
            // Subscribe to prize updates
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPrizesUpdated += OnPrizesUpdated;
            }
            else
            {
                Debug.LogWarning("[MapController] GameManager instance not found at startup.");
            }

            if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnNearbyPlayersUpdated += OnNearbyPlayersUpdated;
            }
            else
            {
                Debug.LogWarning("[MapController] SocialManager instance not found at startup.");
            }

            // Create player marker
            CreatePlayerMarker();
            OnNearbyPlayersUpdated();

            // Initial map load
            if (GPSManager.Instance != null && GPSManager.Instance.IsInitialized)
            {
                UpdateMap(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
            }
            else if (autoLoadFallbackMapWithoutGps)
            {
                StartCoroutine(EnsureMapVisibleWithoutGps());
            }
        }

        private void OnDestroy()
        {
            if (GPSManager.Instance != null)
            {
                GPSManager.Instance.OnLocationUpdated -= OnPlayerLocationUpdated;
                GPSManager.Instance.OnLocationError -= OnLocationError;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPrizesUpdated -= OnPrizesUpdated;
            }

            if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnNearbyPlayersUpdated -= OnNearbyPlayersUpdated;
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
            if (float.IsNaN(lat) || float.IsNaN(lon) || float.IsInfinity(lat) || float.IsInfinity(lon))
            {
                Debug.LogWarning("[MapController] Ignoring invalid coordinates for map update.");
                return;
            }

            currentLat = lat;
            currentLon = lon;
            RepositionMapMarkers();

            if (mapLoadCoroutine != null)
            {
                StopCoroutine(mapLoadCoroutine);
            }
            mapLoadCoroutine = StartCoroutine(LoadMapTile(lat, lon, zoomLevel));
        }

        private IEnumerator LoadMapTile(float lat, float lon, int zoom)
        {
            if (mapImage == null)
            {
                yield break;
            }

            // Convert lat/lon to tile coordinates
            int tileX = LonToTileX(lon, zoom);
            int tileY = LatToTileY(lat, zoom);
            string tileKey = $"{zoom}/{tileX}/{tileY}";

            if (mapTileLoadedAtLeastOnce && string.Equals(lastRequestedTileKey, tileKey, StringComparison.Ordinal))
            {
                mapLoadCoroutine = null;
                yield break;
            }

            string url = tileServerURL
                .Replace("{z}", zoom.ToString())
                .Replace("{x}", tileX.ToString())
                .Replace("{y}", tileY.ToString());

            // Note: tileSize is kept for Inspector visibility and future logic
            if (tileSize <= 0) tileSize = 256; 

            using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                www.SetRequestHeader("User-Agent", "YallaCatchUnity/1.0");
                yield return www.SendWebRequest();

                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    mapTexture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(www);
                    mapImage.texture = mapTexture;
                    mapTileLoadedAtLeastOnce = true;
                    lastRequestedTileKey = tileKey;
                    Debug.Log($"[MapController] Map tile loaded: {tileKey}");
                }
                else
                {
                    Debug.LogWarning($"[MapController] Failed to load map tile {tileKey}: {www.error}");
                }
            }

            mapLoadCoroutine = null;
        }

        #endregion

        #region Markers

        private void CreatePlayerMarker()
        {
            if (playerMarkerPrefab != null)
            {
                playerMarker = Instantiate(playerMarkerPrefab, markersContainer);
                playerMarker.name = "PlayerMarker";
                EnsureMarkerVisual(playerMarker, new Color(0.2f, 0.5f, 1f, 1f), new Vector2(22f, 22f));
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
            if (GameManager.Instance == null || GameManager.Instance.NearbyPrizes == null)
            {
                return;
            }

            // Clear old markers
            foreach (var marker in prizeMarkers.Values)
            {
                Destroy(marker);
            }
            prizeMarkers.Clear();
            foreach (var marker in partnerMarkers.Values)
            {
                Destroy(marker);
            }
            partnerMarkers.Clear();

            // Create new markers
            foreach (var prize in GameManager.Instance.NearbyPrizes)
            {
                CreatePrizeMarker(prize);
            }

            if (GameManager.Instance.NearbyPartnerMarkers != null)
            {
                foreach (var partnerMarker in GameManager.Instance.NearbyPartnerMarkers)
                {
                    CreatePartnerMarker(partnerMarker);
                }
            }
        }

        private void OnNearbyPlayersUpdated()
        {
            if (SocialManager.Instance == null || SocialManager.Instance.NearbyPlayers == null)
            {
                return;
            }

            List<string> visibleIds = new List<string>();
            foreach (var nearbyPlayer in SocialManager.Instance.NearbyPlayers)
            {
                if (nearbyPlayer == null || string.IsNullOrWhiteSpace(nearbyPlayer.userId) || !nearbyPlayer.HasCoordinates)
                {
                    continue;
                }

                visibleIds.Add(nearbyPlayer.userId);
                if (!nearbyPlayerMarkers.ContainsKey(nearbyPlayer.userId))
                {
                    CreateNearbyPlayerMarker(nearbyPlayer);
                }
            }

            List<string> removeIds = new List<string>();
            foreach (var kvp in nearbyPlayerMarkers)
            {
                if (!visibleIds.Contains(kvp.Key))
                {
                    if (kvp.Value != null)
                    {
                        Destroy(kvp.Value);
                    }
                    removeIds.Add(kvp.Key);
                }
            }

            foreach (string id in removeIds)
            {
                nearbyPlayerMarkers.Remove(id);
            }

            RepositionMapMarkers();
        }

        private void CreatePrizeMarker(Prize prize)
        {
            if (prize == null || string.IsNullOrWhiteSpace(prize._id))
            {
                return;
            }

            if (prizeMarkerPrefab != null && !prizeMarkers.ContainsKey(prize._id))
            {
                GameObject marker = Instantiate(prizeMarkerPrefab, markersContainer);
                marker.name = $"Prize_{prize.name}";

                Vector2 screenPos = LatLonToScreenPosition(prize.Latitude, prize.Longitude);
                marker.transform.localPosition = screenPos;

                // Add click handler
                Button button = marker.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnPrizeMarkerClicked(prize));
                }

                EnsureMarkerVisual(marker, new Color(1f, 0.85f, 0.2f, 1f), new Vector2(28f, 28f));

                prizeMarkers[prize._id] = marker;
            }
        }

        private void CreatePartnerMarker(GameMapMarkerWire partnerMarker)
        {
            if (partnerMarker == null || string.IsNullOrWhiteSpace(partnerMarker.id))
            {
                return;
            }

            GameObject sourcePrefab = partnerMarkerPrefab != null ? partnerMarkerPrefab : prizeMarkerPrefab;
            if (sourcePrefab == null || partnerMarkers.ContainsKey(partnerMarker.id))
            {
                return;
            }

            GameObject marker = Instantiate(sourcePrefab, markersContainer);
            marker.name = $"Partner_{partnerMarker.title}";

            float lat = partnerMarker.position?.lat ?? 0f;
            float lng = partnerMarker.position?.lng ?? 0f;
            marker.transform.localPosition = LatLonToScreenPosition(lat, lng);

            Button button = marker.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnPartnerMarkerClicked(partnerMarker));
            }

            EnsureMarkerVisual(marker, new Color(0.1f, 0.9f, 0.95f, 1f), new Vector2(24f, 24f));
            partnerMarkers[partnerMarker.id] = marker;
        }

        private void CreateNearbyPlayerMarker(NearbyPlayer nearbyPlayer)
        {
            if (nearbyPlayer == null || string.IsNullOrWhiteSpace(nearbyPlayer.userId))
            {
                return;
            }

            GameObject sourcePrefab = nearbyPlayerMarkerPrefab != null
                ? nearbyPlayerMarkerPrefab
                : (partnerMarkerPrefab != null ? partnerMarkerPrefab : prizeMarkerPrefab);
            if (sourcePrefab == null || nearbyPlayerMarkers.ContainsKey(nearbyPlayer.userId))
            {
                return;
            }

            GameObject marker = Instantiate(sourcePrefab, markersContainer);
            marker.name = $"NearbyPlayer_{nearbyPlayer.displayName}";
            marker.transform.localPosition = LatLonToScreenPosition(nearbyPlayer.Latitude, nearbyPlayer.Longitude);

            if (marker.TryGetComponent<Button>(out var button))
            {
                button.onClick.AddListener(() => OnNearbyPlayerMarkerClicked(nearbyPlayer));
            }

            EnsureMarkerVisual(marker, new Color(0.45f, 1f, 0.45f, 1f), new Vector2(20f, 20f));
            nearbyPlayerMarkers[nearbyPlayer.userId] = marker;
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
                    prize.Latitude,
                    prize.Longitude
                );
                UIManager.Instance?.ShowMessage($"Prize is {distance:F0}m away. Get closer to capture!");
            }
        }

        private void OnPartnerMarkerClicked(GameMapMarkerWire partnerMarker)
        {
            if (partnerMarker == null)
            {
                return;
            }

            string label = string.IsNullOrWhiteSpace(partnerMarker.title) ? "Partner" : partnerMarker.title;
            string category = string.IsNullOrWhiteSpace(partnerMarker.category) ? "" : $"\n{partnerMarker.category}";
            UIManager.Instance?.ShowMessage($"<color=#3DEAF2>{label}</color>{category}");
        }

        private void OnNearbyPlayerMarkerClicked(NearbyPlayer nearbyPlayer)
        {
            if (nearbyPlayer == null)
            {
                return;
            }

            string name = string.IsNullOrWhiteSpace(nearbyPlayer.displayName) ? "Nearby Player" : nearbyPlayer.displayName;
            string distanceText = nearbyPlayer.distance > 0f && !float.IsInfinity(nearbyPlayer.distance)
                ? $"\n{nearbyPlayer.distance:F0}m"
                : string.Empty;
            UIManager.Instance?.ShowMessage($"<color=#7BFF7B>{name}</color>{distanceText}");
        }

        private void RepositionMapMarkers()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            if (prizeMarkers.Count > 0 && GameManager.Instance.NearbyPrizes != null)
            {
                foreach (var kvp in prizeMarkers)
                {
                    Prize prize = GameManager.Instance.NearbyPrizes.Find(p => p != null && p._id == kvp.Key);
                    if (prize == null || kvp.Value == null)
                    {
                        continue;
                    }

                    kvp.Value.transform.localPosition = LatLonToScreenPosition(prize.Latitude, prize.Longitude);
                }
            }

            if (partnerMarkers.Count > 0 && GameManager.Instance.NearbyPartnerMarkers != null)
            {
                foreach (var kvp in partnerMarkers)
                {
                    GameMapMarkerWire partner = GameManager.Instance.NearbyPartnerMarkers.Find(m => m != null && m.id == kvp.Key);
                    if (partner?.position == null || kvp.Value == null)
                    {
                        continue;
                    }

                    kvp.Value.transform.localPosition = LatLonToScreenPosition(partner.position.lat, partner.position.lng);
                }
            }

            if (nearbyPlayerMarkers.Count > 0 && SocialManager.Instance != null && SocialManager.Instance.NearbyPlayers != null)
            {
                foreach (var kvp in nearbyPlayerMarkers)
                {
                    NearbyPlayer nearbyPlayer = SocialManager.Instance.NearbyPlayers.Find(p => p != null && p.userId == kvp.Key);
                    if (nearbyPlayer == null || !nearbyPlayer.HasCoordinates || kvp.Value == null)
                    {
                        continue;
                    }

                    kvp.Value.transform.localPosition = LatLonToScreenPosition(nearbyPlayer.Latitude, nearbyPlayer.Longitude);
                }
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
            return (int)((1f - Mathf.Log(Mathf.Tan(latRad) + 1f / Mathf.Cos(latRad)) / Mathf.PI) / 2f * (1 << zoom));
        }

        private Vector2 LatLonToScreenPosition(float lat, float lon)
        {
            // Approximate local mercator scaling around the current center and adapt to the actual map panel size.
            float relX = (lon - currentLon) * 111320f * Mathf.Cos(currentLat * Mathf.Deg2Rad);
            float relY = (lat - currentLat) * 110540f;

            float cosLat = Mathf.Max(0.1f, Mathf.Cos(currentLat * Mathf.Deg2Rad));
            float metersPerTilePixel = (156543.03392f * cosLat) / Mathf.Pow(2f, zoomLevel);
            metersPerTilePixel = Mathf.Max(0.01f, metersPerTilePixel);

            float uiPixelsPerTilePixelX = 1f;
            float uiPixelsPerTilePixelY = 1f;
            if (mapImage != null && mapImage.rectTransform != null)
            {
                Rect mapRect = mapImage.rectTransform.rect;
                if (mapRect.width > 1f)
                {
                    uiPixelsPerTilePixelX = mapRect.width / Mathf.Max(1f, tileSize);
                }
                if (mapRect.height > 1f)
                {
                    uiPixelsPerTilePixelY = mapRect.height / Mathf.Max(1f, tileSize);
                }
            }

            float screenX = (relX / metersPerTilePixel) * uiPixelsPerTilePixelX;
            float screenY = (relY / metersPerTilePixel) * uiPixelsPerTilePixelY;

            return new Vector2(screenX, screenY);
        }

        #endregion

        #region Public Methods

        public void CenterOnPlayer()
        {
            if (GPSManager.Instance != null && GPSManager.Instance.IsInitialized)
            {
                UpdateMap(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
                return;
            }

            ApplyFallbackMapCenter(forceReload: true);
        }

        public void SetZoomLevel(int zoom)
        {
            zoomLevel = Mathf.Clamp(zoom, 10, 18);

            if (HasMapCoordinates())
            {
                UpdateMap(currentLat, currentLon);
            }
            else
            {
                ApplyFallbackMapCenter(forceReload: true);
            }
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
            if (HasMapCoordinates())
            {
                UpdateMap(currentLat, currentLon);
            }
            else
            {
                ApplyFallbackMapCenter(forceReload: true);
            }
            
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

        public Texture2D CurrentMapTexture => mapTexture;
        
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
            if (APIClient.Instance == null)
            {
                yield break;
            }

            string city = ConfigManager.Instance?.GetCurrentCity() ?? "";
            string endpoint = string.IsNullOrEmpty(city) ? APIEndpoints.REWARDS_HEATMAP : $"{APIEndpoints.REWARDS_HEATMAP}/{city}";
            
            yield return APIClient.Instance.Get<List<HeatmapPoint>>(endpoint, (response) =>
            {
                if (response.success)
                {
                    heatmapData = response.data;
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

        #region Fallback / Diagnostics

        private IEnumerator EnsureMapVisibleWithoutGps()
        {
            // Give GPS init a short window before falling back.
            yield return new WaitForSeconds(0.5f);

            if (GPSManager.Instance != null && GPSManager.Instance.IsInitialized)
            {
                UpdateMap(GPSManager.Instance.CurrentLatitude, GPSManager.Instance.CurrentLongitude);
                yield break;
            }

            ApplyFallbackMapCenter(forceReload: !mapTileLoadedAtLeastOnce);
        }

        private void OnLocationError(string message)
        {
            if (!autoLoadFallbackMapWithoutGps)
            {
                return;
            }

            Debug.Log($"[MapController] GPS unavailable, using fallback map center. Reason: {message}");
            ApplyFallbackMapCenter(forceReload: !mapTileLoadedAtLeastOnce);
        }

        private void ApplyFallbackMapCenter(bool forceReload)
        {
            if (!autoLoadFallbackMapWithoutGps)
            {
                return;
            }

#if UNITY_EDITOR
            if (useEditorMockGpsFallback && GPSManager.Instance != null && !GPSManager.Instance.IsInitialized)
            {
                GPSManager.Instance.SetMockLocation(fallbackLatitude, fallbackLongitude);
                fallbackLocationApplied = true;
                return; // SetMockLocation triggers OnLocationUpdated -> UpdateMap
            }
#endif

            if (!forceReload && fallbackLocationApplied && mapTileLoadedAtLeastOnce)
            {
                return;
            }

            fallbackLocationApplied = true;
            UpdateMap(fallbackLatitude, fallbackLongitude);
            Debug.Log($"[MapController] Fallback map center applied: {fallbackLatitude}, {fallbackLongitude}");
        }

        private bool HasMapCoordinates()
        {
            return Mathf.Abs(currentLat) > 0.0001f || Mathf.Abs(currentLon) > 0.0001f;
        }

        private void EnsureMarkerVisual(GameObject marker, Color color, Vector2 size)
        {
            if (marker == null)
            {
                return;
            }

            var image = marker.GetComponent<Image>();
            if (image == null)
            {
                image = marker.AddComponent<Image>();
            }

            if (image.sprite == null)
            {
                image.sprite = GetFallbackUISprite();
                if (image.sprite != null)
                {
                    image.type = Image.Type.Sliced;
                }
            }

            image.color = color;

            if (marker.TryGetComponent<RectTransform>(out var rect))
            {
                if (rect.sizeDelta == Vector2.zero)
                {
                    rect.sizeDelta = size;
                }
            }
        }

        private static Sprite GetFallbackUISprite()
        {
            if (fallbackUiSprite != null)
            {
                return fallbackUiSprite;
            }

            try
            {
                var tex = Texture2D.whiteTexture;
                if (tex == null)
                {
                    return null;
                }

                fallbackUiSprite = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f),
                    100f
                );
                fallbackUiSprite.name = "YallaCatch_FallbackUISprite";
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MapController] Failed to create fallback UI sprite: {ex.Message}");
            }

            return fallbackUiSprite;
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
