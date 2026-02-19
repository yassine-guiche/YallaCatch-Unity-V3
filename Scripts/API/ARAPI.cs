using System;
using System.Collections;
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
            StartCoroutine(APIClient.Instance.Post<ARSession>(APIEndpoints.AR_VIEW_START, request, callback));
        }

        /// <summary>
        /// Capture AR screenshot
        /// </summary>
        public void CaptureScreenshot(ARScreenshotRequest request, Action<ApiResponse<ARScreenshotResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<ARScreenshotResponse>(APIEndpoints.AR_CAPTURE, request, callback));
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
            StartCoroutine(APIClient.Instance.Get<ARModelResponse>(endpoint, callback));
        }

        #endregion
    }

    #region AR Request/Response Models

    [Serializable]
    public class StartARViewRequest
    {
        public string prizeId;
        public ARDeviceCapabilities deviceCapabilities;
        public ARStartLocation location;
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
        public string imageBase64;
        public ARScreenshotMetadata metadata;
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
    }

    [Serializable]
    public class ARSessionEndResponse
    {
        public bool success;
        public int totalDuration;
        public int screenshotsCount;
        public string sessionId;
    }

    [Serializable]
    public class ARModelResponse
    {
        public string modelUrl;
        public string modelFormat; // glb, gltf
        public ARModelConfig config;
        public ARModelAnimations animations;
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
