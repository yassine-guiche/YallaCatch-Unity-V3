using System;
using System.Collections;
using UnityEngine;
using YallaCatch.Models;
using CaptureAttemptResult = YallaCatch.Models.CaptureResult;

namespace YallaCatch.API
{
    /// <summary>
    /// Capture API - AR prize capture flow
    /// </summary>
    public class CaptureAPI : MonoBehaviour
    {
        public static CaptureAPI Instance { get; private set; }

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

        #region Capture Methods

        /// <summary>
        /// Attempt to capture a prize
        /// </summary>
        public void AttemptCapture(CaptureAttemptRequest request, Action<ApiResponse<CaptureAttemptResult>> callback)
        {
            // Unity CaptureAttemptRequest already has the correct structure for the normalized backend
            StartCoroutine(APIClient.Instance.Post<CaptureAttemptResult>(APIEndpoints.CAPTURE_ATTEMPT, request, callback));
        }

        /// <summary>
        /// Pre-validate capture (UI feedback before attempting)
        /// </summary>
        public void ValidateCapture(string prizeId, Location location, bool preValidate, Action<ApiResponse<CaptureValidationResponse>> callback)
        {
            var body = new
            {
                prizeId,
                location = new
                {
                    latitude = location.latitude,
                    longitude = location.longitude
                },
                preValidate
            };
            StartCoroutine(APIClient.Instance.Post<CaptureValidationResponse>(APIEndpoints.CAPTURE_VALIDATE, body, callback));
        }

        /// <summary>
        /// Get box animation configuration for Unity
        /// </summary>
        public void GetAnimation(string prizeId, Action<ApiResponse<BoxAnimation>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.CAPTURE_ANIMATION, prizeId);
            StartCoroutine(APIClient.Instance.Get<BoxAnimation>(endpoint, callback));
        }

        /// <summary>
        /// Confirm capture after attempt succeeds
        /// </summary>
        public void ConfirmCapture(ConfirmCaptureRequest request, Action<ApiResponse<CaptureConfirmResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Post<CaptureConfirmResponse>(APIEndpoints.CAPTURE_CONFIRM, request, callback));
        }

        /// <summary>
        /// Legacy override for simple confirmations (not recommended for production anti-cheat)
        /// </summary>
        public void ConfirmCapture(string prizeId, Action<ApiResponse<CaptureConfirmResponse>> callback)
        {
            var body = new { prizeId };
            StartCoroutine(APIClient.Instance.Post<CaptureConfirmResponse>(APIEndpoints.CAPTURE_CONFIRM, body, callback));
        }

        #endregion
    }

    #region Capture Request/Response Models

    [Serializable]
    public class CaptureAttemptRequest
    {
        public string prizeId;
        public CaptureLocation location;
        public CaptureDeviceInfo deviceInfo;
        public CaptureARData deviceSignals;
        public string captureMethod; // tap, gesture, voice
    }

    [Serializable]
    public class CaptureLocation
    {
        public float latitude;
        public float longitude;
        public float accuracy;
        public float? altitude;
    }

    [Serializable]
    public class CaptureDeviceInfo
    {
        public string platform;
        public string deviceModel;
        public string osVersion;
        public string appVersion;
        public string timestamp;
    }

    [Serializable]
    public class CaptureARData
    {
        public CameraPosition cameraPosition;
        public CameraRotation cameraRotation;
        public float? lightEstimation;
        public string trackingState; // tracking, limited, not_tracking
    }

    [Serializable]
    public class CameraPosition
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class CameraRotation
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [Serializable]
    public class CaptureValidationResponse
    {
        public bool canCapture;
        public string reason;
        public float distance;
        public BoxAnimation animation;
        public EstimatedReward estimatedReward;
    }

    [Serializable]
    public class EstimatedReward
    {
        public int minPoints;
        public int maxPoints;
        public string rarity;
    }

    [Serializable]
    public class BoxAnimation
    {
        public string type; // mystery_box, treasure_chest, gift_box, energy_orb
        public string rarity; // common, uncommon, rare, epic, legendary
        public AnimationConfig animation;
        public AnimationEffects effects;
        public AnimationDuration duration;
    }

    [Serializable]
    public class AnimationConfig
    {
        public string approach;
        public string idle;
        public string opening;
        public string reveal;
    }

    [Serializable]
    public class AnimationEffects
    {
        public string[] particles;
        public string lighting;
        public string sound;
    }

    [Serializable]
    public class AnimationDuration
    {
        public float total;
        public float[] phases;
    }

    [Serializable]
    public class ConfirmCaptureRequest
    {
        public string prizeId;
        public CaptureLocation location;
        public DeviceSignals deviceSignals;
        public string idempotencyKey;
        public string platform = "mobile_unity";
    }

    [Serializable]
    public class DeviceSignals
    {
        public float speed;
        public bool mockLocation;
    }

    [Serializable]
    public class CaptureConfirmResponse
    {
        public bool confirmed;
        public string claimId;
        public string message;
    }

    #endregion
}
