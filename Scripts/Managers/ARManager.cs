using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace YallaCatch
{
    /// <summary>
    /// Gère la réalité augmentée (AR)
    /// Intégration avec le module AR backend (4 endpoints)
    /// Charge les modèles 3D dynamiques depuis le backend
    /// Contrôlé par l'admin panel
    /// </summary>
    public class ARManager : MonoBehaviour
    {
        public static ARManager Instance { get; private set; }

        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARSessionOrigin arSessionOrigin;
        [SerializeField] private ARRaycastManager arRaycastManager;
        [SerializeField] private ARPlaneManager arPlaneManager;
        
        [Header("AR Settings")]
        [SerializeField] private float modelScale = 1f;
        [SerializeField] private float rotationSpeed = 50f;
        [SerializeField] private bool autoRotate = true;
        
        // État
        private bool isARActive = false;
        private string currentSessionId;
        private string currentPrizeId;
        private GameObject currentARModel;
        private Vector3 placementPosition;
        private bool isModelPlaced = false;
        
        // Cache des modèles 3D
        private Dictionary<string, GameObject> modelCache = new Dictionary<string, GameObject>();
        
        // Events
        public delegate void OnARSessionStarted();
        public event OnARSessionStarted OnARSessionStartedEvent;
        
        public delegate void OnARSessionEnded();
        public event OnARSessionEnded OnARSessionEndedEvent;
        
        public delegate void OnModelLoaded(GameObject model);
        public event OnModelLoaded OnModelLoadedEvent;

        void Awake()
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

        void Update()
        {
            if (!isARActive || currentARModel == null)
                return;
            
            // Auto-rotation du modèle
            if (autoRotate && isModelPlaced)
            {
                currentARModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            }
            
            // Détection de tap pour placer le modèle
            if (!isModelPlaced && Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                if (touch.phase == TouchPhase.Began)
                {
                    TryPlaceModel(touch.position);
                }
            }
        }

        #region AR Session Management

        /// <summary>
        /// Démarre une session AR
        /// </summary>
        public IEnumerator StartARSession(string prizeId, System.Action<bool> callback = null)
        {
            currentPrizeId = prizeId;
            
            // Appeler l'API backend pour démarrer la session
            var data = new
            {
                prizeId = prizeId,
                timestamp = System.DateTime.UtcNow.ToString("o"),
                location = new
                {
                    lat = GPSManager.Instance.GetLatitude(),
                    lng = GPSManager.Instance.GetLongitude()
                }
            };
            
            string endpoint = "/ar/view";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    var sessionData = JsonConvert.DeserializeObject<ARSessionData>(response.data.ToString());
                    currentSessionId = sessionData.sessionId;
                    
                    // Activer l'AR
                    ActivateAR();
                    
                    // Charger le modèle 3D
                    StartCoroutine(LoadARModel(prizeId));
                    
                    isARActive = true;
                    OnARSessionStartedEvent?.Invoke();
                    
                    callback?.Invoke(true);
                    
                    Debug.Log($"[ARManager] AR session started: {currentSessionId}");
                }
                else
                {
                    callback?.Invoke(false);
                    UIManager.Instance.ShowMessage("Failed to start AR session");
                }
            });
        }

        /// <summary>
        /// Termine une session AR
        /// </summary>
        public IEnumerator EndARSession(System.Action<bool> callback = null)
        {
            if (string.IsNullOrEmpty(currentSessionId))
            {
                callback?.Invoke(false);
                yield break;
            }
            
            var data = new
            {
                sessionId = currentSessionId,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            string endpoint = "/ar/end";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    DeactivateAR();
                    
                    isARActive = false;
                    currentSessionId = null;
                    currentPrizeId = null;
                    
                    OnARSessionEndedEvent?.Invoke();
                    
                    callback?.Invoke(true);
                    
                    Debug.Log("[ARManager] AR session ended");
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        /// <summary>
        /// Capture en AR
        /// </summary>
        public IEnumerator CaptureInAR(System.Action<CaptureResult> callback = null)
        {
            if (!isARActive || !isModelPlaced)
            {
                callback?.Invoke(null);
                yield break;
            }
            
            // Prendre un screenshot
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            
            // Convertir en base64
            byte[] bytes = screenshot.EncodeToPNG();
            string base64Image = System.Convert.ToBase64String(bytes);
            
            var data = new
            {
                sessionId = currentSessionId,
                prizeId = currentPrizeId,
                image = base64Image,
                timestamp = System.DateTime.UtcNow.ToString("o"),
                location = new
                {
                    lat = GPSManager.Instance.GetLatitude(),
                    lng = GPSManager.Instance.GetLongitude()
                }
            };
            
            string endpoint = "/ar/capture";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    var result = JsonConvert.DeserializeObject<CaptureResult>(response.data.ToString());
                    callback?.Invoke(result);
                    
                    // Afficher l'animation de capture
                    CaptureAnimationController.Instance?.PlayCaptureAnimation();
                    
                    Debug.Log("[ARManager] AR capture successful");
                }
                else
                {
                    callback?.Invoke(null);
                    UIManager.Instance.ShowMessage("Capture failed");
                }
            });
            
            // Nettoyer
            Destroy(screenshot);
        }

        #endregion

        #region AR Model Loading

        /// <summary>
        /// Charge un modèle 3D depuis le backend
        /// GET /ar/model/:prizeId
        /// </summary>
        public IEnumerator LoadARModel(string prizeId, System.Action<GameObject> callback = null)
        {
            // Vérifier si le modèle est déjà en cache
            if (modelCache.ContainsKey(prizeId))
            {
                InstantiateARModel(modelCache[prizeId]);
                callback?.Invoke(currentARModel);
                yield break;
            }
            
            string endpoint = $"/ar/model/{prizeId}";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    var modelData = JsonConvert.DeserializeObject<ARModelData>(response.data.ToString());
                    
                    // Télécharger le modèle 3D
                    StartCoroutine(DownloadAndLoadModel(modelData, (model) =>
                    {
                        if (model != null)
                        {
                            modelCache[prizeId] = model;
                            InstantiateARModel(model);
                            callback?.Invoke(currentARModel);
                        }
                        else
                        {
                            callback?.Invoke(null);
                        }
                    }));
                }
                else
                {
                    // Utiliser un modèle par défaut
                    LoadDefaultModel();
                    callback?.Invoke(currentARModel);
                }
            });
        }

        /// <summary>
        /// Télécharge et charge un modèle 3D
        /// </summary>
        private IEnumerator DownloadAndLoadModel(ARModelData modelData, System.Action<GameObject> callback)
        {
            // Télécharger le fichier du modèle (GLB, FBX, etc.)
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(modelData.modelUrl))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    byte[] modelBytes = request.downloadHandler.data;
                    
                    // Charger le modèle (nécessite un package comme GLTFUtility ou TriLib)
                    // Pour l'instant, on utilise un placeholder
                    GameObject model = CreatePlaceholderModel(modelData);
                    
                    callback?.Invoke(model);
                    
                    Debug.Log($"[ARManager] Model downloaded: {modelData.name}");
                }
                else
                {
                    Debug.LogError($"[ARManager] Failed to download model: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Crée un modèle placeholder
        /// </summary>
        private GameObject CreatePlaceholderModel(ARModelData modelData)
        {
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Cube);
            model.name = modelData.name;
            model.transform.localScale = Vector3.one * modelData.scale;
            
            // Appliquer la texture si disponible
            if (!string.IsNullOrEmpty(modelData.textureUrl))
            {
                StartCoroutine(LoadTexture(modelData.textureUrl, (texture) =>
                {
                    if (texture != null)
                    {
                        model.GetComponent<Renderer>().material.mainTexture = texture;
                    }
                }));
            }
            
            return model;
        }

        /// <summary>
        /// Charge une texture
        /// </summary>
        private IEnumerator LoadTexture(string url, System.Action<Texture2D> callback)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    callback?.Invoke(texture);
                }
                else
                {
                    callback?.Invoke(null);
                }
            }
        }

        /// <summary>
        /// Charge un modèle par défaut
        /// </summary>
        private void LoadDefaultModel()
        {
            GameObject model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.name = "DefaultPrize";
            model.transform.localScale = Vector3.one * modelScale;
            
            InstantiateARModel(model);
        }

        /// <summary>
        /// Instancie le modèle AR dans la scène
        /// </summary>
        private void InstantiateARModel(GameObject modelPrefab)
        {
            if (currentARModel != null)
            {
                Destroy(currentARModel);
            }
            
            currentARModel = Instantiate(modelPrefab);
            currentARModel.SetActive(false); // Caché jusqu'au placement
            
            OnModelLoadedEvent?.Invoke(currentARModel);
            
            Debug.Log("[ARManager] AR model instantiated");
        }

        #endregion

        #region AR Placement

        /// <summary>
        /// Essaie de placer le modèle à la position du tap
        /// </summary>
        private void TryPlaceModel(Vector2 screenPosition)
        {
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (arRaycastManager.Raycast(screenPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                
                placementPosition = hitPose.position;
                
                if (currentARModel != null)
                {
                    currentARModel.transform.position = placementPosition;
                    currentARModel.transform.rotation = hitPose.rotation;
                    currentARModel.SetActive(true);
                    
                    isModelPlaced = true;
                    
                    Debug.Log("[ARManager] Model placed at: " + placementPosition);
                }
            }
        }

        #endregion

        #region AR Activation/Deactivation

        /// <summary>
        /// Active l'AR
        /// </summary>
        private void ActivateAR()
        {
            if (arSession != null)
                arSession.enabled = true;
            
            if (arSessionOrigin != null)
                arSessionOrigin.gameObject.SetActive(true);
            
            if (arPlaneManager != null)
                arPlaneManager.enabled = true;
            
            Debug.Log("[ARManager] AR activated");
        }

        /// <summary>
        /// Désactive l'AR
        /// </summary>
        private void DeactivateAR()
        {
            if (currentARModel != null)
            {
                Destroy(currentARModel);
                currentARModel = null;
            }
            
            if (arPlaneManager != null)
                arPlaneManager.enabled = false;
            
            if (arSessionOrigin != null)
                arSessionOrigin.gameObject.SetActive(false);
            
            if (arSession != null)
                arSession.enabled = false;
            
            isModelPlaced = false;
            
            Debug.Log("[ARManager] AR deactivated");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Vérifie si l'AR est active
        /// </summary>
        public bool IsARActive()
        {
            return isARActive;
        }

        /// <summary>
        /// Vérifie si le modèle est placé
        /// </summary>
        public bool IsModelPlaced()
        {
            return isModelPlaced;
        }

        /// <summary>
        /// Obtient le modèle AR actuel
        /// </summary>
        public GameObject GetCurrentARModel()
        {
            return currentARModel;
        }

        /// <summary>
        /// Réinitialise le placement
        /// </summary>
        public void ResetPlacement()
        {
            if (currentARModel != null)
            {
                currentARModel.SetActive(false);
            }
            
            isModelPlaced = false;
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class ARSessionData
    {
        public string sessionId;
        public string prizeId;
        public string startTime;
    }

    [System.Serializable]
    public class ARModelData
    {
        public string prizeId;
        public string name;
        public string modelUrl; // URL du fichier 3D (GLB, FBX, etc.)
        public string textureUrl;
        public float scale;
        public string animationUrl; // URL des animations si disponibles
        public Dictionary<string, string> metadata;
    }

    [System.Serializable]
    public class CaptureResult
    {
        public bool success;
        public string captureId;
        public int pointsEarned;
        public string message;
    }

    #endregion
}
