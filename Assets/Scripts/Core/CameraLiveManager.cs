using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using YallaCatch.Core;
using YallaCatch.Models;
using YallaCatch.Managers;
using YallaCatch.API;
using YallaCatch.UI;

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
        [SerializeField] private GameObject partnerMarkerPrefab; // Optional prefab for partner markers
        [SerializeField] private GameObject nearbyPlayerMarkerPrefab; // Optional prefab for nearby players
        [SerializeField] private float miniMapRadius = 100f; // Rayon en mètres représenté par la mini-map
        [SerializeField] private Color nearPrizeColor = Color.green; // Couleur pour prizes proches (<50m)
        [SerializeField] private Color farPrizeColor = Color.yellow; // Couleur pour prizes loin (>50m)
        [SerializeField] private Color partnerMarkerColor = new Color(0.1f, 0.9f, 0.95f, 1f);
        [SerializeField] private Color nearbyPlayerMarkerColor = new Color(0.45f, 1f, 0.45f, 1f);
        
        [Header("Catch Button")]
        [SerializeField] private GameObject catchButtonObject;
        [SerializeField] private Button catchButton;
        [SerializeField] private TMP_Text catchButtonText;
        [SerializeField] private float catchDistance = 5f; // Distance en mètres pour activer le bouton
        [SerializeField] private Image catchButtonGlow; // Effet de glow quand prize très proche
        
        [Header("Prize Overlay")]
        [SerializeField] private GameObject prizeOverlayPrefab; // Prefab pour afficher le prize en 3D/AR
        [SerializeField] private Transform prizeOverlayContainer;
        
        // État
        private bool isActive = false;
        private bool isCameraReady = false;
        private List<Prize> nearbyPrizes = new List<Prize>();
        private List<GameMapMarkerWire> nearbyPartnerMarkers = new List<GameMapMarkerWire>();
        private List<NearbyPlayer> nearbyPlayers = new List<NearbyPlayer>();
        private Dictionary<string, GameObject> prizeMarkers = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> partnerMarkers = new Dictionary<string, GameObject>();
        private Dictionary<string, GameObject> nearbyPlayerMarkers = new Dictionary<string, GameObject>();
        private Prize closestPrize = null;
        private GameObject currentPrizeOverlay = null;
        private MapController cachedMapController;
        private static Sprite fallbackUiSprite;
        
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
            
            // Listen to game manager updates
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPrizesUpdated += RefreshPrizes;
                RefreshPrizes(); // Load existing
            }

            if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnNearbyPlayersUpdated += RefreshNearbyPlayersMarkers;
                RefreshNearbyPlayersMarkers();
            }
            
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
            
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPrizesUpdated -= RefreshPrizes;
            }

            if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnNearbyPlayersUpdated -= RefreshNearbyPlayersMarkers;
            }
            
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
            if (miniMapContainer == null)
            {
                Debug.LogWarning("[CameraLiveManager] Mini-map container is not assigned.");
                return;
            }

            // Positionner en haut à droite
            miniMapContainer.anchorMin = new Vector2(1, 1);
            miniMapContainer.anchorMax = new Vector2(1, 1);
            miniMapContainer.pivot = new Vector2(1, 1);
            if (miniMapContainer.anchoredPosition.sqrMagnitude <= 0.01f)
            {
                miniMapContainer.anchoredPosition = miniMapPosition;
            }
            else
            {
                // Preserve generated scene placement and keep runtime calculations in sync with it.
                miniMapPosition = miniMapContainer.anchoredPosition;
            }

            // Respect generated scene sizing if present; only force a size when the container has none.
            if (miniMapContainer.sizeDelta.x <= 0f || miniMapContainer.sizeDelta.y <= 0f)
            {
                miniMapContainer.sizeDelta = new Vector2(miniMapSize, miniMapSize);
            }

            miniMapSize = GetMiniMapDisplaySize();
            
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
                EnsureImageHasDefaultSprite(miniMapBorder);
                miniMapBorder.color = new Color(1f, 1f, 1f, 0.8f);
            }
            
            // Point du joueur au centre
            if (playerDot != null)
            {
                EnsureImageHasDefaultSprite(playerDot);
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
                playerLat = GPSManager.Instance.CurrentLatitude;
                playerLon = GPSManager.Instance.CurrentLongitude;
                playerHeading = Input.compass.trueHeading; // Direction du device
            }
        }

        /// <summary>
        /// Met à jour la mini-map avec les prizes
        /// </summary>
        private void UpdateMiniMap()
        {
            TrySyncMiniMapTexture();

            float currentMiniMapSize = GetMiniMapDisplaySize();

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
                Prize prize = nearbyPrizes.Find(p => p._id == prizeId);
                if (prize == null) continue;
                
                // Calculer la position relative au joueur
                float distance = GPSManager.CalculateDistance(
                    playerLat, playerLon,
                    prize.Latitude, prize.Longitude
                );
                
                // Calculer l'angle relatif
                float angle = CalculateBearing(
                    playerLat, playerLon,
                    prize.Latitude, prize.Longitude
                ) - playerHeading;
                
                // Convertir en position sur la mini-map
                float normalizedDistance = Mathf.Clamp01(distance / miniMapRadius);
                float markerRadius = (currentMiniMapSize / 2f) * normalizedDistance;
                
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

            foreach (var kvp in partnerMarkers)
            {
                string markerId = kvp.Key;
                GameObject marker = kvp.Value;
                GameMapMarkerWire partnerMarker = nearbyPartnerMarkers.Find(m => m != null && m.id == markerId);
                if (partnerMarker?.position == null || marker == null) continue;

                float distance = GPSManager.CalculateDistance(
                    playerLat, playerLon,
                    partnerMarker.position.lat, partnerMarker.position.lng
                );

                float angle = CalculateBearing(
                    playerLat, playerLon,
                    partnerMarker.position.lat, partnerMarker.position.lng
                ) - playerHeading;

                float normalizedDistance = Mathf.Clamp01(distance / miniMapRadius);
                float markerRadius = (currentMiniMapSize / 2f) * normalizedDistance;

                Vector2 markerPos = new Vector2(
                    markerRadius * Mathf.Sin(angle * Mathf.Deg2Rad),
                    markerRadius * Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                RectTransform markerRect = marker.GetComponent<RectTransform>();
                markerRect.anchoredPosition = markerPos;
                markerRect.localScale = Vector3.one * Mathf.Lerp(1.1f, 0.6f, normalizedDistance);

                Image markerImage = marker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = partnerMarkerColor;
                }
            }

            foreach (var kvp in nearbyPlayerMarkers)
            {
                string markerId = kvp.Key;
                GameObject marker = kvp.Value;
                NearbyPlayer nearbyPlayer = nearbyPlayers.Find(p => p != null && p.userId == markerId);
                if (nearbyPlayer == null || !nearbyPlayer.HasCoordinates || marker == null) continue;

                float distance = GPSManager.CalculateDistance(
                    playerLat, playerLon,
                    nearbyPlayer.Latitude, nearbyPlayer.Longitude
                );

                float angle = CalculateBearing(
                    playerLat, playerLon,
                    nearbyPlayer.Latitude, nearbyPlayer.Longitude
                ) - playerHeading;

                float normalizedDistance = Mathf.Clamp01(distance / miniMapRadius);
                float markerRadius = (currentMiniMapSize / 2f) * normalizedDistance;

                Vector2 markerPos = new Vector2(
                    markerRadius * Mathf.Sin(angle * Mathf.Deg2Rad),
                    markerRadius * Mathf.Cos(angle * Mathf.Deg2Rad)
                );

                RectTransform markerRect = marker.GetComponent<RectTransform>();
                markerRect.anchoredPosition = markerPos;
                markerRect.localScale = Vector3.one * Mathf.Lerp(1.0f, 0.55f, normalizedDistance);

                Image markerImage = marker.GetComponent<Image>();
                if (markerImage != null)
                {
                    markerImage.color = nearbyPlayerMarkerColor;
                }
            }
        }

        private void TrySyncMiniMapTexture()
        {
            if (miniMapImage == null)
            {
                return;
            }

            if (cachedMapController == null)
            {
                cachedMapController = FindObjectOfType<MapController>();
            }

            if (cachedMapController != null && cachedMapController.CurrentMapTexture != null && miniMapImage.texture != cachedMapController.CurrentMapTexture)
            {
                miniMapImage.texture = cachedMapController.CurrentMapTexture;
            }
        }

        private float GetMiniMapDisplaySize()
        {
            if (miniMapContainer != null)
            {
                float rectSize = Mathf.Max(miniMapContainer.rect.width, miniMapContainer.rect.height);
                if (rectSize > 1f)
                {
                    miniMapSize = rectSize;
                    return rectSize;
                }

                float sizeDeltaSize = Mathf.Max(miniMapContainer.sizeDelta.x, miniMapContainer.sizeDelta.y);
                if (sizeDeltaSize > 1f)
                {
                    miniMapSize = sizeDeltaSize;
                    return sizeDeltaSize;
                }
            }

            return Mathf.Max(miniMapSize, 1f);
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
                float distance = GPSManager.CalculateDistance(
                    playerLat, playerLon,
                    prize.Latitude, prize.Longitude
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
            
            float distance = GPSManager.CalculateDistance(
                playerLat, playerLon,
                closestPrize.Latitude, closestPrize.Longitude
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
                if (currentPrizeOverlay != null && !currentPrizeOverlay.activeSelf)
                {
                    currentPrizeOverlay.SetActive(true);
                }
                
                // Positionner l'overlay dans la direction du prize
                float angle = CalculateBearing(
                    playerLat, playerLon,
                    prize.Latitude, prize.Longitude
                );
                
                // Positionner à une distance proportionnelle (entre 2m et 10m dans la scène)
                float sceneDistance = Mathf.Lerp(2f, 10f, Mathf.Clamp01(distance / 20f));
                
                Vector3 direction = Quaternion.Euler(0, angle - playerHeading, 0) * Vector3.forward;
                currentPrizeOverlay.transform.position = mainCamera.transform.position + direction * sceneDistance;
                
                // Faire face à la caméra
                currentPrizeOverlay.transform.LookAt(mainCamera.transform);
                currentPrizeOverlay.transform.Rotate(0, 180, 0);
                
                // Configure le visuel du prize
                TMPro.TMP_Text overlayName = currentPrizeOverlay.GetComponentInChildren<TMPro.TMP_Text>();
                if (overlayName != null)
                {
                    overlayName.text = $"{prize.name}\n{distance:F0}m";
                }
                
                // Apply rarity color
                Renderer overlayRenderer = currentPrizeOverlay.GetComponentInChildren<Renderer>();
                if (overlayRenderer != null)
                {
                    Color rarityColor = GetRarityTint(prize.rarity);
                    overlayRenderer.material.color = rarityColor;
                }
                
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
            float distance = GPSManager.CalculateDistance(
                playerLat, playerLon,
                closestPrize.Latitude, closestPrize.Longitude
            );
            
            if (distance <= catchDistance)
            {
                // Lancer l'animation de capture
                StartCoroutine(CapturePrizeAnimation(closestPrize));
            }
            else
            {
                UIManager.Instance?.ShowMessage("Too far! Get closer to catch this prize.");
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
            
            // Effet de particules via CaptureAnimationController
            if (CaptureAnimationController.Instance != null)
            {
                CaptureAnimationController.Instance.PlayQuickCaptureEffect(
                    mainCamera.transform.position + mainCamera.transform.forward * 2f
                );
            }
            
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
            bool captureDone = false;

            GameManager.Instance.CapturePrize(prize, (success, message, pointsEarned) =>
            {
                if (success)
                {
                    // Retirer le prize de la liste
                    nearbyPrizes.Remove(prize);
                    
                    // Retirer le marker
                    if (prizeMarkers.ContainsKey(prize._id))
                    {
                        Destroy(prizeMarkers[prize._id]);
                        prizeMarkers.Remove(prize._id);
                    }
                    
                    closestPrize = null;
                    
                    // Afficher le résultat
                    UI.UIManager.Instance?.ShowMessage($"{prize.name} captured!\n+{pointsEarned} points");
                    
                    // Vibration
                    Handheld.Vibrate();
                    
                    // Achievement
                    if (AchievementManager.Instance != null)
                    {
                        AchievementManager.Instance.OnPrizeCaptured(prize.displayType);
                    }
                }
                else
                {
                    UI.UIManager.Instance?.ShowMessage(message ?? "Failed to capture prize. Try again!");
                }
                
                captureDone = true;
            });

            while (!captureDone) yield return null;
        }

        /// <summary>
        /// Rafraîchit la liste des prizes depuis le GameManager
        /// </summary>
        public void RefreshPrizes()
        {
            if (GameManager.Instance == null || nearbyPrizes == null) return;

            // Retirer les anciens markers qui ne sont plus dans la nouvelle liste
            var newPrizes = GameManager.Instance.NearbyPrizes;
            if (newPrizes == null) return;

            List<string> newIds = new List<string>();
            foreach(var p in newPrizes) newIds.Add(p._id);

            List<string> keysToRemove = new List<string>();
            foreach(var kvp in prizeMarkers)
            {
                if (!newIds.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach(var k in keysToRemove)
            {
                prizeMarkers.Remove(k);
            }

            nearbyPrizes = new List<Prize>(newPrizes);

            // Ajouter les markers pour les nouveaux prizes
            foreach (Prize prize in nearbyPrizes)
            {
                if (!prizeMarkers.ContainsKey(prize._id))
                {
                    CreatePrizeMarker(prize);
                }
            }

            RefreshPartnerMarkers();
            RefreshNearbyPlayersMarkers();
        }

        /// <summary>
        /// Crée un marker de prize sur la mini-map
        /// </summary>
        private void CreatePrizeMarker(Prize prize)
        {
            if (prizeMarkerPrefab == null || prizeMarkersContainer == null) return;
            
            GameObject marker = Instantiate(prizeMarkerPrefab, prizeMarkersContainer);
            marker.name = $"Marker_{prize._id}";
            if (marker.TryGetComponent<Image>(out var markerImage))
            {
                EnsureImageHasDefaultSprite(markerImage);
            }
            
            prizeMarkers[prize._id] = marker;
        }

        private void RefreshPartnerMarkers()
        {
            if (GameManager.Instance == null)
            {
                return;
            }

            var newPartnerMarkers = GameManager.Instance.NearbyPartnerMarkers ?? new List<GameMapMarkerWire>();
            List<string> newIds = new List<string>();
            foreach (var marker in newPartnerMarkers)
            {
                if (marker != null && !string.IsNullOrWhiteSpace(marker.id))
                {
                    newIds.Add(marker.id);
                }
            }

            List<string> keysToRemove = new List<string>();
            foreach (var kvp in partnerMarkers)
            {
                if (!newIds.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                partnerMarkers.Remove(key);
            }

            nearbyPartnerMarkers = new List<GameMapMarkerWire>(newPartnerMarkers);

            foreach (var partner in nearbyPartnerMarkers)
            {
                if (partner == null || string.IsNullOrWhiteSpace(partner.id) || partner.position == null)
                {
                    continue;
                }

                if (!partnerMarkers.ContainsKey(partner.id))
                {
                    CreatePartnerMarker(partner);
                }
            }
        }

        private void CreatePartnerMarker(GameMapMarkerWire partner)
        {
            if (prizeMarkersContainer == null || partner == null || string.IsNullOrWhiteSpace(partner.id))
            {
                return;
            }

            GameObject sourcePrefab = partnerMarkerPrefab != null ? partnerMarkerPrefab : prizeMarkerPrefab;
            if (sourcePrefab == null)
            {
                return;
            }

            GameObject marker = Instantiate(sourcePrefab, prizeMarkersContainer);
            marker.name = $"PartnerMarker_{partner.id}";

            if (marker.TryGetComponent<Image>(out var markerImage))
            {
                EnsureImageHasDefaultSprite(markerImage);
                markerImage.color = partnerMarkerColor;
            }

            partnerMarkers[partner.id] = marker;
        }

        public void RefreshNearbyPlayersMarkers()
        {
            if (SocialManager.Instance == null)
            {
                return;
            }

            var sourcePlayers = SocialManager.Instance.NearbyPlayers ?? new List<NearbyPlayer>();
            List<string> newIds = new List<string>();
            foreach (var player in sourcePlayers)
            {
                if (player != null && !string.IsNullOrWhiteSpace(player.userId) && player.HasCoordinates)
                {
                    newIds.Add(player.userId);
                }
            }

            List<string> keysToRemove = new List<string>();
            foreach (var kvp in nearbyPlayerMarkers)
            {
                if (!newIds.Contains(kvp.Key))
                {
                    Destroy(kvp.Value);
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                nearbyPlayerMarkers.Remove(key);
            }

            nearbyPlayers = new List<NearbyPlayer>();
            foreach (var player in sourcePlayers)
            {
                if (player != null && !string.IsNullOrWhiteSpace(player.userId) && player.HasCoordinates)
                {
                    nearbyPlayers.Add(player);
                    if (!nearbyPlayerMarkers.ContainsKey(player.userId))
                    {
                        CreateNearbyPlayerMarker(player);
                    }
                }
            }
        }

        private void CreateNearbyPlayerMarker(NearbyPlayer nearbyPlayer)
        {
            if (prizeMarkersContainer == null || nearbyPlayer == null || string.IsNullOrWhiteSpace(nearbyPlayer.userId))
            {
                return;
            }

            GameObject sourcePrefab = nearbyPlayerMarkerPrefab != null
                ? nearbyPlayerMarkerPrefab
                : (partnerMarkerPrefab != null ? partnerMarkerPrefab : prizeMarkerPrefab);
            if (sourcePrefab == null)
            {
                return;
            }

            GameObject marker = Instantiate(sourcePrefab, prizeMarkersContainer);
            marker.name = $"NearbyPlayerMarker_{nearbyPlayer.userId}";

            if (marker.TryGetComponent<Image>(out var markerImage))
            {
                EnsureImageHasDefaultSprite(markerImage);
                markerImage.color = nearbyPlayerMarkerColor;
            }

            nearbyPlayerMarkers[nearbyPlayer.userId] = marker;
        }

        private void EnsureImageHasDefaultSprite(Image image)
        {
            if (image == null || image.sprite != null)
            {
                return;
            }

            image.sprite = GetFallbackUISprite();
            if (image.sprite != null)
            {
                image.type = Image.Type.Sliced;
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
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CameraLiveManager] Failed to create fallback UI sprite: {ex.Message}");
            }

            return fallbackUiSprite;
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
        /// Returns a tint color based on prize rarity
        /// </summary>
        private Color GetRarityTint(string rarity)
        {
            switch (rarity?.ToLower())
            {
                case "common": return new Color(0.7f, 0.7f, 0.7f);
                case "uncommon": return new Color(0.2f, 0.8f, 0.3f);
                case "rare": return new Color(0.2f, 0.5f, 1.0f);
                case "epic": return new Color(0.6f, 0.2f, 0.9f);
                case "legendary": return new Color(1.0f, 0.84f, 0.0f);
                default: return Color.white;
            }
        }

        void OnDestroy()
        {
            StopCamera();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnPrizesUpdated -= RefreshPrizes;
            }
            if (SocialManager.Instance != null)
            {
                SocialManager.Instance.OnNearbyPlayersUpdated -= RefreshNearbyPlayersMarkers;
            }
        }
    }
}

