using System;
using System.Collections;
using Newtonsoft.Json.Linq;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// AR API - AR sessions and 3D model loading
    /// </summary>
    public class ARAPI : MonoBehaviour
    {
        public static ARAPI Instance { get; private set; }

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
        /// Start AR view session
        /// </summary>
        public void StartARView(StartARViewRequest request, Action<ApiResponse<ARSession>> callback)
        {
            object body = BuildStartRequest(request);
            StartCoroutine(APIClient.Instance.Post<object>(APIEndpoints.AR_VIEW_START, body, response =>
            {
                var mapped = new ApiResponse<ARSession>
                {
                    success = response.success,
                    error = response.error,
                    message = response.message,
                    timestamp = response.timestamp
                };

                if (response.success && response.data != null)
                    mapped.data = ParseARSession(response.data);

                callback?.Invoke(mapped);
            }));
        }

        /// <summary>
        /// Capture AR screenshot
        /// </summary>
        public void CaptureScreenshot(ARScreenshotRequest request, Action<ApiResponse<ARScreenshotResponse>> callback)
        {
            object body = BuildScreenshotRequest(request);
            StartCoroutine(APIClient.Instance.Post<ARScreenshotResponse>(APIEndpoints.AR_CAPTURE, body, callback));
        }

        /// <summary>
        /// End AR session
        /// </summary>
        public void EndARSession(string sessionId, int duration, Action<ApiResponse<ARSessionEndResponse>> callback)
        {
            var body = new { sessionId, duration };
            StartCoroutine(APIClient.Instance.Post<ARSessionEndResponse>(APIEndpoints.AR_SESSION_END, body, callback));
        }

        #endregion

        #region Model Methods

        /// <summary>
        /// Get AR model for prize
        /// </summary>
        public void GetARModel(string prizeId, Action<ApiResponse<ARModelResponse>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.AR_MODEL, prizeId);
            StartCoroutine(APIClient.Instance.Get<ARModelResponse>(endpoint, response =>
            {
                if (response.success && response.data != null)
                {
                    TryNormalizeARModelResponse(response.data);
                }

                callback?.Invoke(response);
            }));
        }

        #endregion

        #region Normalizers

        private object BuildStartRequest(StartARViewRequest request)
        {
            if (request == null) return null;

            var metadata = request.metadata ?? new ARStartMetadata
            {
                deviceModel = SystemInfo.deviceModel,
                osVersion = SystemInfo.operatingSystem,
                cameraPermission = Application.HasUserAuthorization(UserAuthorization.WebCam),
                locationPermission = request.location != null
            };

            return new
            {
                prizeId = request.prizeId,
                metadata = new
                {
                    metadata.deviceModel,
                    metadata.osVersion,
                    metadata.arKitVersion,
                    metadata.arCoreVersion,
                    cameraPermission = metadata.cameraPermission,
                    locationPermission = metadata.locationPermission
                }
            };
        }

        private object BuildScreenshotRequest(ARScreenshotRequest request)
        {
            if (request == null) return null;

            ARScreenshotPayload screenshot = request.screenshot;
            if (screenshot == null)
            {
                screenshot = new ARScreenshotPayload
                {
                    base64 = request.imageBase64
                };
            }

            return new
            {
                sessionId = request.sessionId,
                screenshot = new
                {
                    base64 = screenshot.base64,
                    location = screenshot.location != null ? new { lat = screenshot.location.lat, lng = screenshot.location.lng } : null
                }
            };
        }

        private ARSession ParseARSession(object payloadObj)
        {
            try
            {
                JObject payload = payloadObj as JObject ?? JObject.FromObject(payloadObj);
                JObject arModel = payload["arModel"] as JObject;

                var session = new ARSession
                {
                    sessionId = payload["sessionId"]?.Value<string>(),
                    prizeId = payload["prizeId"]?.Value<string>(),
                    modelUrl = arModel?["modelUrl"]?.Value<string>(),
                };

                if (arModel != null)
                {
                    session.modelConfig = new ARModelConfig
                    {
                        scale = arModel["scale"]?.Value<float?>() ?? 1f
                    };
                }

                return session;
            }
            catch
            {
                return null;
            }
        }

        private void TryNormalizeARModelResponse(ARModelResponse response)
        {
            if (response == null || response.arModel == null) return;

            if (string.IsNullOrEmpty(response.modelUrl))
                response.modelUrl = response.arModel.modelUrl;

            if (response.config == null)
            {
                response.config = new ARModelConfig
                {
                    scale = response.arModel.scale
                };
            }
            else if (response.config.scale <= 0f && response.arModel.scale > 0f)
            {
                response.config.scale = response.arModel.scale;
            }
        }

        #endregion
    }

    #region AR Request/Response Models

    [Serializable]
    public class StartARViewRequest
    {
        public string prizeId;
        public ARStartMetadata metadata; // Backend-compatible schema
        public ARDeviceCapabilities deviceCapabilities;
        public ARStartLocation location;
    }

    [Serializable]
    public class ARStartMetadata
    {
        public string deviceModel;
        public string osVersion;
        public string arKitVersion;
        public string arCoreVersion;
        public bool cameraPermission = true;
        public bool locationPermission = true;
    }

    [Serializable]
    public class ARDeviceCapabilities
    {
        public bool hasARCore;
        public bool hasARKit;
        public bool supportsDepth;
        public bool supportsLiDAR;
    }

    [Serializable]
    public class ARStartLocation
    {
        public float lat;
        public float lng;
    }

    [Serializable]
    public class ARScreenshotRequest
    {
        public string sessionId;
        public ARScreenshotPayload screenshot; // Backend-compatible schema
        public string imageBase64;
        public ARScreenshotMetadata metadata;
    }

    [Serializable]
    public class ARScreenshotPayload
    {
        public string base64;
        public ARStartLocation location;
    }

    [Serializable]
    public class ARScreenshotMetadata
    {
        public float lightEstimation;
        public string trackingState;
        public CameraPosition cameraPosition;
    }

    [Serializable]
    public class ARScreenshotResponse
    {
        public bool success;
        public string screenshotUrl;
        public string screenshotId;
        public string timestamp;
    }

    [Serializable]
    public class ARSessionEndResponse
    {
        public bool success;
        public int duration;
        public int screenshots;
        public int totalDuration;
        public int screenshotsCount;
        public string sessionId;
    }

    [Serializable]
    public class ARModelResponse
    {
        public string prizeId;
        public BackendARModelPayload arModel;
        public string modelUrl;
        public string modelFormat; // glb, gltf
        public ARModelConfig config;
        public ARModelAnimations animations;
    }

    [Serializable]
    public class BackendARModelPayload
    {
        public string modelUrl;
        public string textureUrl;
        public float scale;
        public ARRotation rotation;
    }

    [Serializable]
    public class ARRotation
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class ARModelAnimations
    {
        public string idle;
        public string capture;
        public string celebration;
    }

    #endregion
}
