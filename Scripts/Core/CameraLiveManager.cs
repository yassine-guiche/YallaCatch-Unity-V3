using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace YallaCatch
{
    /// <summary>
    /// Gère le mode caméra live avec mini-map circulaire en haut à droite
    /// Mode de jeu préféré où le joueur voit le monde réel avec overlay AR
    /// </summary>
    public class CameraLiveManager : MonoBehaviour
    {
        public static CameraLiveManager Instance { get; private set; }

        [Header("Camera Settings")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private RawImage cameraFeed; // Pour afficher la caméra si pas AR natif
        private WebCamTexture webCamTexture;
        
        [Header("Mini-Map Settings")]
        [SerializeField] private RawImage miniMapImage;
        [SerializeField] private RectTransform miniMapContainer;
        [SerializeField] private Image miniMapBorder; // Bordure circulaire
        [SerializeField] private float miniMapSize = 150f; // Taille en pixels
        [SerializeField] private Vector2 miniMapPosition = new Vector2(-20, -20); // Offset depuis top-right
        [SerializeField] private Image playerDot; // Point représentant le joueur
        [SerializeField] private Transform prizeMarkersContainer; // Container pour les markers de prizes
        
        [Header("Prize Tracking")]
        [SerializeField] private GameObject prizeMarkerPrefab; // Prefab pour marker de prize sur mini-map
        [SerializeField] private float miniMapRadius = 100f; // Rayon en mètres représenté par la mini-map
        [SerializeField] private Color nearPrizeColor = Color.green; // Couleur pour prizes proches (<50m)
        [SerializeField] private Color farPrizeColor = Color.yellow; // Couleur pour prizes loin (>50m)
        
        [Header("Catch Button")]
        [SerializeField] private GameObject catchButtonObject;
        [SerializeField] private Button catchButton;
        [SerializeField] private Text catchButtonText;
        [SerializeField] private float catchDistance = 5f; // Distance en mètres pour activer le bouton
        [SerializeField] private Image catchButtonGlow; // Effet de glow quand prize très proche
        
        [Header("Prize Overlay")]
        [SerializeField] private GameObject prizeOverlayPrefab; // Prefab pour afficher le prize en 3D/AR
        [SerializeField] private Transform prizeOverlayContainer;
        
        // État
        private bool isActive = false;
        private bool isCameraReady = false;
        private List<Prize> nearbyPrizes = new List<Prize>();
        private Dictionary<string, GameObject> prizeMarkers = new Dictionary<string, GameObject>();
        private Prize closestPrize = null;
        private GameObject currentPrizeOverlay = null;
        
        // Données GPS
        private float playerLat = 0f;
        private float playerLon = 0f;
        private float playerHeading = 0f; // Direction du joueur (0-360)

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            SetupMiniMap();
            SetupCatchButton();
        }

        void Update()
        {
            if (!isActive || !isCameraReady) return;

            UpdatePlayerPosition();
            UpdateMiniMap();
            UpdatePrizeTracking();
            UpdateCatchButton();
        }

        /// <summary>
        /// Active le mode caméra live
        /// </summary>
        public void ActivateCameraMode()
        {
            isActive = true;
            StartCamera();
            miniMapContainer.gameObject.SetActive(true);
            
            // Charger les prizes à proximité
            StartCoroutine(LoadNearbyPrizes());
            
            Debug.Log("[CameraLiveManager] Camera mode activated");
        }

        /// <summary>
        /// Désactive le mode caméra live
        /// </summary>
        public void DeactivateCameraMode()
        {
            isActive = false;
            StopCamera();
            miniMapContainer.gameObject.SetActive(false);
            catchButtonObject.SetActive(false);
            
            // Nettoyer les overlays
            if (currentPrizeOverlay != null)
            {
                Destroy(currentPrizeOverlay);
            }
            
            Debug.Log("[CameraLiveManager] Camera mode deactivated");
        }

        /// <summary>
        /// Démarre la caméra du device
        /// </summary>
        private void StartCamera()
        {
            #if UNITY_ANDROID || UNITY_IOS
            // Utiliser la caméra native
            if (WebCamTexture.devices.Length > 0)
            {
                webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name);
                cameraFeed.texture = webCamTexture;
                webCamTexture.Play();
                isCameraReady = true;
                
                Debug.Log($"[CameraLiveManager] Camera started: {webCamTexture.deviceName}");
            }
            else
            {
                Debug.LogError("[CameraLiveManager] No camera found on device!");
                // Fallback: afficher un fond noir ou une image de placeholder
                isCameraReady = false;
            }
            #else
            // Mode éditeur: simuler avec une texture
            Debug.LogWarning("[CameraLiveManager] Camera simulation in editor mode");
            isCameraReady = true;
            #endif
        }

        /// <summary>
        /// Arrête la caméra
        /// </summary>
        private void StopCamera()
        {
            if (webCamTexture != null && webCamTexture.isPlaying)
            {
                webCamTexture.Stop();
            }
            isCameraReady = false;
        }

        /// <summary>
        /// Configure la mini-map circulaire
        /// </summary>
        private void SetupMiniMap()
        {
            // Positionner en haut à droite
            miniMapContainer.anchorMin = new Vector2(1, 1);
            miniMapContainer.anchorMax = new Vector2(1, 1);
            miniMapContainer.pivot = new Vector2(1, 1);
            miniMapContainer.anchoredPosition = miniMapPosition;
            miniMapContainer.sizeDelta = new Vector2(miniMapSize, miniMapSize);
            
            // Rendre circulaire avec un mask
            Image maskImage = miniMapContainer.GetComponent<Image>();
            if (maskImage != null)
            {
                maskImage.enabled = true;
            }
            
            // Ajouter un Mask component pour rendre circulaire
            Mask mask = miniMapContainer.GetComponent<Mask>();
            if (mask == null)
            {
                mask = miniMapContainer.gameObject.AddComponent<Mask>();
            }
            mask.showMaskGraphic = false;
            
            // Bordure circulaire
            if (miniMapBorder != null)
            {
                miniMapBorder.color = new Color(1f, 1f, 1f, 0.8f);
            }
            
            // Point du joueur au centre
            if (playerDot != null)
            {
                playerDot.rectTransform.anchoredPosition = Vector2.zero;
                playerDot.color = Color.blue;
            }
            
            miniMapContainer.gameObject.SetActive(false);
            
            Debug.Log("[CameraLiveManager] Mini-map setup complete");
        }

        /// <summary>
        /// Configure le bouton CATCH
        /// </summary>
        private void SetupCatchButton()
        {
            catchButtonObject.SetActive(false);
            
            if (catchButton != null)
            {
                catchButton.onClick.AddListener(OnCatchButtonClicked);
            }
            
            if (catchButtonText != null)
            {
                catchButtonText.text = "CATCH!";
            }
            
            // Effet de glow désactivé par défaut
            if (catchButtonGlow != null)
            {
                catchButtonGlow.enabled = false;
            }
        }

        /// <summary>
        /// Met à jour la position du joueur depuis GPSManager
        /// </summary>
        private void UpdatePlayerPosition()
        {
            if (GPSManager.Instance != null)
            {
                playerLat = GPSManager.Instance.GetLatitude();
                playerLon = GPSManager.Instance.GetLongitude();
                playerHeading = Input.compass.trueHeading; // Direction du device
            }
        }

        /// <summary>
        /// Met à jour la mini-map avec les prizes
        /// </summary>
        private void UpdateMiniMap()
        {
            // Rotation de la mini-map selon la direction du joueur
            if (miniMapImage != null)
            {
                miniMapImage.rectTransform.rotation = Quaternion.Euler(0, 0, -playerHeading);
            }
            
            // Mettre à jour les positions des markers de prizes
            foreach (var kvp in prizeMarkers)
            {
                string prizeId = kvp.Key;
                GameObject marker = kvp.Value;
                
                // Trouver le prize correspondant
                Prize prize = nearbyPrizes.Find(p => p.id == prizeId);
                if (prize == null) continue;
                
                // Calculer la position relative au joueur
                float distance = GPSManager.Instance.CalculateDistance(
                    playerLat, playerLon,
                    prize.location.coordinates[1], prize.location.coordinates[0]
                );
                
                // Calculer l'angle relatif
                float angle = CalculateBearing(
                    playerLat, playerLon,
                    prize.location.coordinates[1], prize.location.coordinates[0]
                ) - playerHeading;
                
                // Convertir en position sur la mini-map
                float normalizedDistance = Mathf.Clamp01(distance / miniMapRadius);
                float markerRadius = (miniMapSize / 2f) * normalizedDistance;
                
                Vector2 markerPos = new Vector2(
                    markerRadius * Mathf.Sin(angle * Mathf.Deg2Rad),
                    markerRadius * Mathf.Cos(angle * Mathf.Deg2Rad)
                );
                
                RectTransform markerRect = marker.GetComponent<RectTransform>();
                markerRect.anchoredPosition = markerPos;
                
                // Changer la couleur selon la distance
                Image markerImage = marker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = distance < 50f ? nearPrizeColor : farPrizeColor;
                }
                
                // Changer la taille selon la distance (plus proche = plus grand)
                float scale = Mathf.Lerp(1.5f, 0.5f, normalizedDistance);
                markerRect.localScale = Vector3.one * scale;
            }
        }

        /// <summary>
        /// Met à jour le tracking des prizes et trouve le plus proche
        /// </summary>
        private void UpdatePrizeTracking()
        {
            if (nearbyPrizes.Count == 0) return;
            
            Prize newClosestPrize = null;
            float closestDistance = float.MaxValue;
            
            foreach (Prize prize in nearbyPrizes)
            {
                float distance = GPSManager.Instance.CalculateDistance(
                    playerLat, playerLon,
                    prize.location.coordinates[1], prize.location.coordinates[0]
                );
                
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    newClosestPrize = prize;
                }
            }
            
            // Si le prize le plus proche a changé
            if (newClosestPrize != closestPrize)
            {
                closestPrize = newClosestPrize;
                
                // Afficher l'overlay du prize en AR si < 20m
                if (closestDistance < 20f)
                {
                    ShowPrizeOverlay(closestPrize, closestDistance);
                }
                else
                {
                    HidePrizeOverlay();
                }
            }
        }

        /// <summary>
        /// Met à jour le bouton CATCH
        /// </summary>
        private void UpdateCatchButton()
        {
            if (closestPrize == null)
            {
                catchButtonObject.SetActive(false);
                return;
            }
            
            float distance = GPSManager.Instance.CalculateDistance(
                playerLat, playerLon,
                closestPrize.location.coordinates[1], closestPrize.location.coordinates[0]
            );
            
            // Activer le bouton si distance < catchDistance (5m par défaut)
            if (distance <= catchDistance)
            {
                catchButtonObject.SetActive(true);
                
                // Mettre à jour le texte avec la distance
                if (catchButtonText != null)
                {
                    catchButtonText.text = $"CATCH!\n{distance:F1}m";
                }
                
                // Activer l'effet de glow si très proche (<2m)
                if (catchButtonGlow != null)
                {
                    catchButtonGlow.enabled = distance < 2f;
                    
                    // Animation de pulsation
                    if (distance < 2f)
                    {
                        float pulse = (Mathf.Sin(Time.time * 5f) + 1f) / 2f;
                        catchButtonGlow.color = new Color(1f, 1f, 0f, pulse * 0.5f);
                    }
                }
            }
            else
            {
                catchButtonObject.SetActive(false);
            }
        }

        /// <summary>
        /// Affiche l'overlay du prize en AR
        /// </summary>
        private void ShowPrizeOverlay(Prize prize, float distance)
        {
            // Détruire l'ancien overlay
            if (currentPrizeOverlay != null)
            {
                Destroy(currentPrizeOverlay);
            }
            
            // Créer le nouvel overlay
            if (prizeOverlayPrefab != null)
            {
                currentPrizeOverlay = Instantiate(prizeOverlayPrefab, prizeOverlayContainer);
                
                // Positionner l'overlay dans la direction du prize
                float angle = CalculateBearing(
                    playerLat, playerLon,
                    prize.location.coordinates[1], prize.location.coordinates[0]
                );
                
                // Positionner à une distance proportionnelle (entre 2m et 10m dans la scène)
                float sceneDistance = Mathf.Lerp(2f, 10f, Mathf.Clamp01(distance / 20f));
                
                Vector3 direction = Quaternion.Euler(0, angle - playerHeading, 0) * Vector3.forward;
                currentPrizeOverlay.transform.position = mainCamera.transform.position + direction * sceneDistance;
                
                // Faire face à la caméra
                currentPrizeOverlay.transform.LookAt(mainCamera.transform);
                currentPrizeOverlay.transform.Rotate(0, 180, 0);
                
                // Configurer le visuel du prize
                // TODO: Charger l'icône du prize, afficher le nom, etc.
                
                Debug.Log($"[CameraLiveManager] Prize overlay shown: {prize.name} at {distance:F1}m");
            }
        }

        /// <summary>
        /// Cache l'overlay du prize
        /// </summary>
        private void HidePrizeOverlay()
        {
            if (currentPrizeOverlay != null)
            {
                Destroy(currentPrizeOverlay);
                currentPrizeOverlay = null;
            }
        }

        /// <summary>
        /// Appelé quand le joueur clique sur CATCH
        /// </summary>
        private void OnCatchButtonClicked()
        {
            if (closestPrize == null) return;
            
            // Vérifier la distance une dernière fois
            float distance = GPSManager.Instance.CalculateDistance(
                playerLat, playerLon,
                closestPrize.location.coordinates[1], closestPrize.location.coordinates[0]
            );
            
            if (distance <= catchDistance)
            {
                // Lancer l'animation de capture
                StartCoroutine(CapturePrizeAnimation(closestPrize));
            }
            else
            {
                UIManager.Instance.ShowMessage("Too far! Get closer to catch this prize.");
            }
        }

        /// <summary>
        /// Animation de capture du prize
        /// </summary>
        private IEnumerator CapturePrizeAnimation(Prize prize)
        {
            // Désactiver le bouton pendant l'animation
            catchButtonObject.SetActive(false);
            
            // Animation: Le prize vole vers le joueur
            if (currentPrizeOverlay != null)
            {
                float duration = 1f;
                float elapsed = 0f;
                Vector3 startPos = currentPrizeOverlay.transform.position;
                Vector3 endPos = mainCamera.transform.position;
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    
                    // Courbe d'animation (ease-in-out)
                    t = t * t * (3f - 2f * t);
                    
                    currentPrizeOverlay.transform.position = Vector3.Lerp(startPos, endPos, t);
                    currentPrizeOverlay.transform.localScale = Vector3.one * (1f - t * 0.5f);
                    
                    // Rotation
                    currentPrizeOverlay.transform.Rotate(0, Time.deltaTime * 360f, 0);
                    
                    yield return null;
                }
                
                // Détruire l'overlay
                Destroy(currentPrizeOverlay);
            }
            
            // Effet de particules
            // TODO: Ajouter des particules d'étoiles, confettis, etc.
            
            // Son de capture
            if (SoundManager.Instance != null)
            {
                SoundManager.Instance.PlayPrizeCapture();
            }
            
            // Appeler l'API pour capturer le prize
            yield return StartCoroutine(CapturePrizeAPI(prize));
        }

        /// <summary>
        /// Appelle l'API pour capturer le prize
        /// </summary>
        private IEnumerator CapturePrizeAPI(Prize prize)
        {
            // Préparer les données de capture
            var captureData = new
            {
                prizeId = prize.id,
                location = new
                {
                    type = "Point",
                    coordinates = new float[] { playerLon, playerLat }
                },
                timestamp = System.DateTime.UtcNow.ToString("o"),
                deviceInfo = new
                {
                    platform = Application.platform.ToString(),
                    model = SystemInfo.deviceModel
                }
            };
            
            // Appeler l'API
            bool success = false;
            int pointsEarned = 0;
            string rewardMessage = "";
            
            yield return APIManager.Instance.Post("/capture", captureData, (response) =>
            {
                if (response.success)
                {
                    success = true;
                    pointsEarned = response.data.points;
                    rewardMessage = response.data.message;
                    
                    // Mettre à jour les points du joueur
                    GameManager.Instance.AddPoints(pointsEarned);
                    
                    // Retirer le prize de la liste
                    nearbyPrizes.Remove(prize);
                    
                    // Retirer le marker
                    if (prizeMarkers.ContainsKey(prize.id))
                    {
                        Destroy(prizeMarkers[prize.id]);
                        prizeMarkers.Remove(prize.id);
                    }
                    
                    closestPrize = null;
                }
            });
            
            // Afficher le résultat
            if (success)
            {
                UIManager.Instance.ShowRewardPopup(prize.name, pointsEarned, prize.icon);
                
                // Vibration
                Handheld.Vibrate();
                
                // Achievement
                if (AchievementManager.Instance != null)
                {
                    AchievementManager.Instance.OnPrizeCaptured(prize.type);
                }
            }
            else
            {
                UIManager.Instance.ShowMessage("Failed to capture prize. Try again!");
            }
        }

        /// <summary>
        /// Charge les prizes à proximité depuis l'API
        /// </summary>
        private IEnumerator LoadNearbyPrizes()
        {
            // Obtenir le rayon depuis la config
            float radius = ConfigManager.Instance != null 
                ? ConfigManager.Instance.GetScanRadius() 
                : 500f;
            
            string endpoint = $"/prizes/nearby?lat={playerLat}&lon={playerLon}&radius={radius}";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    nearbyPrizes = response.data.prizes;
                    
                    // Créer les markers sur la mini-map
                    foreach (Prize prize in nearbyPrizes)
                    {
                        CreatePrizeMarker(prize);
                    }
                    
                    Debug.Log($"[CameraLiveManager] Loaded {nearbyPrizes.Count} nearby prizes");
                }
            });
        }

        /// <summary>
        /// Crée un marker de prize sur la mini-map
        /// </summary>
        private void CreatePrizeMarker(Prize prize)
        {
            if (prizeMarkerPrefab == null || prizeMarkersContainer == null) return;
            
            GameObject marker = Instantiate(prizeMarkerPrefab, prizeMarkersContainer);
            marker.name = $"Marker_{prize.id}";
            
            prizeMarkers[prize.id] = marker;
        }

        /// <summary>
        /// Calcule le bearing (angle) entre deux points GPS
        /// </summary>
        private float CalculateBearing(float lat1, float lon1, float lat2, float lon2)
        {
            float dLon = (lon2 - lon1) * Mathf.Deg2Rad;
            lat1 = lat1 * Mathf.Deg2Rad;
            lat2 = lat2 * Mathf.Deg2Rad;
            
            float y = Mathf.Sin(dLon) * Mathf.Cos(lat2);
            float x = Mathf.Cos(lat1) * Mathf.Sin(lat2) - 
                      Mathf.Sin(lat1) * Mathf.Cos(lat2) * Mathf.Cos(dLon);
            
            float bearing = Mathf.Atan2(y, x) * Mathf.Rad2Deg;
            return (bearing + 360f) % 360f;
        }

        /// <summary>
        /// Rafraîchit la liste des prizes (appelé périodiquement)
        /// </summary>
        public void RefreshPrizes()
        {
            StartCoroutine(LoadNearbyPrizes());
        }

        void OnDestroy()
        {
            StopCamera();
        }
    }

    // Classes de données
    [System.Serializable]
    public class Prize
    {
        public string id;
        public string name;
        public string type;
        public string icon;
        public Location location;
        public int points;
        public string rarity;
    }

    [System.Serializable]
    public class Location
    {
        public string type;
        public float[] coordinates; // [longitude, latitude]
    }
}
