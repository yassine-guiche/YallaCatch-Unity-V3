using UnityEngine;
using UnityEngine.XR.ARFoundation;
using Unity.XR.CoreUtils;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.UI;

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
        [SerializeField] private XROrigin arSessionOrigin;
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
        private System.DateTime currentSessionStartedAtUtc;
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

            if (APIManager.Instance == null)
            {
                callback?.Invoke(false);
                UIManager.Instance?.ShowMessage("AR dependencies are not ready");
                yield break;
            }
            
            // Backend schema expects prizeId + metadata
            var data = new
            {
                prizeId = prizeId,
                metadata = new
                {
                    deviceModel = SystemInfo.deviceModel,
                    osVersion = SystemInfo.operatingSystem,
                    arKitVersion = (string)null,
                    arCoreVersion = (string)null,
                    cameraPermission = Application.HasUserAuthorization(UserAuthorization.WebCam),
                    locationPermission = GPSManager.Instance != null
                }
            };
            
            string endpoint = "/ar/view/start";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    JObject payload = response.data as JObject ?? JObject.FromObject(response.data);
                    currentSessionId = payload["sessionId"]?.Value<string>();
                    if (string.IsNullOrEmpty(currentSessionId))
                    {
                        callback?.Invoke(false);
                        UIManager.Instance?.ShowMessage("Invalid AR session response");
                        return;
                    }
                    currentSessionStartedAtUtc = System.DateTime.UtcNow;
                    
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
                    UIManager.Instance?.ShowMessage("Failed to start AR session");
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

            if (APIManager.Instance == null)
            {
                callback?.Invoke(false);
                yield break;
            }
            
            int durationSeconds = 0;
            if (currentSessionStartedAtUtc != default)
            {
                durationSeconds = Mathf.Max(0, (int)(System.DateTime.UtcNow - currentSessionStartedAtUtc).TotalSeconds);
            }

            var data = new
            {
                sessionId = currentSessionId,
                duration = durationSeconds
            };
            
            string endpoint = "/ar/session/end";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    DeactivateAR();
                    
                    isARActive = false;
                    currentSessionId = null;
                    currentPrizeId = null;
                    currentSessionStartedAtUtc = default;
                    
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

            if (APIManager.Instance == null)
            {
                callback?.Invoke(null);
                UIManager.Instance?.ShowMessage("AR capture dependencies are not ready");
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
                screenshot = new
                {
                    base64 = base64Image,
                    location = GPSManager.Instance != null ? new
                    {
                        lat = GPSManager.Instance.CurrentLatitude,
                        lng = GPSManager.Instance.CurrentLongitude
                    } : null
                }
            };
            
            string endpoint = "/ar/capture";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    CaptureResult result = ParseCaptureResponse(response.data);
                    callback?.Invoke(result);
                    
                    // Backend AR capture returns screenshot metadata, not reward points.
                    if (CaptureAnimationController.Instance != null && !string.IsNullOrEmpty(currentPrizeId))
                    {
                        // Reconstruct a Prize object for animation from currentPrizeId
                        var animPrize = new YallaCatch.Models.Prize { _id = currentPrizeId, name = "AR Prize" };
                        CaptureAnimationController.Instance.PlayCaptureAnimation(animPrize, result?.pointsEarned ?? 0);
                    }
                    
                    Debug.Log("[ARManager] AR capture successful");
                }
                else
                {
                    callback?.Invoke(null);
                    UIManager.Instance?.ShowMessage("Capture failed");
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

            if (APIManager.Instance == null)
            {
                LoadDefaultModel();
                callback?.Invoke(currentARModel);
                yield break;
            }
            
            string endpoint = $"/ar/model/{prizeId}";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    ARModelData modelData = ParseARModelData(response.data, prizeId);
                    if (modelData == null)
                    {
                        LoadDefaultModel();
                        callback?.Invoke(currentARModel);
                        return;
                    }

                    if (string.IsNullOrEmpty(modelData.modelUrl))
                    {
                        GameObject placeholder = CreatePlaceholderModel(modelData);
                        modelCache[prizeId] = placeholder;
                        InstantiateARModel(placeholder);
                        callback?.Invoke(currentARModel);
                        return;
                    }
                    
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
            currentSessionStartedAtUtc = default;
            
            Debug.Log("[ARManager] AR deactivated");
        }

        private CaptureResult ParseCaptureResponse(object payloadObj)
        {
            try
            {
                JObject payload = payloadObj as JObject ?? JObject.FromObject(payloadObj);
                return new CaptureResult
                {
                    success = true,
                    captureId = payload["screenshotId"]?.Value<string>() ?? payload["screenshotUrl"]?.Value<string>(),
                    screenshotUrl = payload["screenshotUrl"]?.Value<string>(),
                    timestamp = payload["timestamp"]?.Value<string>(),
                    pointsEarned = payload["pointsEarned"]?.Value<int?>() ?? 0,
                    message = "AR screenshot captured"
                };
            }
            catch
            {
                return new CaptureResult
                {
                    success = true,
                    pointsEarned = 0,
                    message = "AR screenshot captured"
                };
            }
        }

        private ARModelData ParseARModelData(object payloadObj, string requestedPrizeId)
        {
            try
            {
                JObject payload = payloadObj as JObject ?? JObject.FromObject(payloadObj);
                JObject arModel = payload["arModel"] as JObject ?? payload;

                return new ARModelData
                {
                    prizeId = payload["prizeId"]?.Value<string>() ?? requestedPrizeId,
                    name = payload["name"]?.Value<string>() ?? "AR Prize",
                    modelUrl = arModel["modelUrl"]?.Value<string>(),
                    textureUrl = arModel["textureUrl"]?.Value<string>(),
                    scale = arModel["scale"]?.Value<float?>() ?? modelScale,
                };
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ARManager] Failed to parse AR model payload: {ex.Message}");
                return null;
            }
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
        public string screenshotUrl;
        public string timestamp;
        public int pointsEarned;
        public string message;
    }

    #endregion
}
