using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using YallaCatch.Managers;

namespace YallaCatch.Core
{
    /// <summary>
    /// Controls AR capture experience
    /// Handles AR session, plane detection, and prize placement in AR
    /// Falls back to simple tap capture if AR not available
    /// </summary>
    public class CaptureController : MonoBehaviour
    {
        [Header("AR Components")]
        [SerializeField] private ARSession arSession;
        [SerializeField] private ARSessionOrigin arSessionOrigin;
        [SerializeField] private ARPlaneManager arPlaneManager;
        [SerializeField] private ARRaycastManager arRaycastManager;

        [Header("Capture Settings")]
        [SerializeField] private GameObject prizePrefab;
        [SerializeField] private float captureAnimationDuration = 2f;
        [SerializeField] private ParticleSystem captureParticles;

        [Header("UI")]
        [SerializeField] private GameObject arInstructions;
        [SerializeField] private GameObject captureButton;
        [SerializeField] private Button reportIssueButton;

        private Prize currentPrize;
        private GameObject spawnedPrize;
        private bool isARSupported = false;
        private bool isPrizeSpawned = false;
        private string lastCaptureId;

        #region Unity Lifecycle

        private void Start()
        {
            CheckARSupport();
        }

        #endregion

        #region AR Support

        private void CheckARSupport()
        {
            StartCoroutine(CheckARSupportCoroutine());
        }

        private IEnumerator CheckARSupportCoroutine()
        {
            if (ARSession.state == ARSessionState.None || ARSession.state == ARSessionState.CheckingAvailability)
            {
                yield return ARSession.CheckAvailability();
            }

            if (ARSession.state == ARSessionState.Unsupported)
            {
                Debug.LogWarning("AR is not supported on this device");
                isARSupported = false;
            }
            else
            {
                Debug.Log("AR is supported!");
                isARSupported = true;
                
                if (arSession != null)
                {
                    arSession.enabled = true;
                }
            }
        }

        #endregion

        #region Capture Flow

        public void StartCapture(Prize prize)
        {
            currentPrize = prize;
            isPrizeSpawned = false;

            if (isARSupported)
            {
                StartARCapture();
            }
            else
            {
                StartSimpleCapture();
            }
        }

        private void StartARCapture()
        {
            Debug.Log("Starting AR capture experience");
            
            // Enable AR components
            if (arPlaneManager != null)
                arPlaneManager.enabled = true;

            if (arRaycastManager != null)
                arRaycastManager.enabled = true;

            // Show AR instructions
            if (arInstructions != null)
                arInstructions.SetActive(true);
        }

        private void StartSimpleCapture()
        {
            Debug.Log("Starting simple tap capture");
            
            // Show simple capture UI
            if (captureButton != null)
                captureButton.SetActive(true);
        }

        #endregion

        #region AR Interaction

        private void Update()
        {
            if (!isARSupported || currentPrize == null || isPrizeSpawned)
                return;

            // Detect touch
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                if (touch.phase == TouchPhase.Began)
                {
                    TrySpawnPrizeInAR(touch.position);
                }
            }

            // For testing in editor
            #if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                TrySpawnPrizeInAR(Input.mousePosition);
            }
            #endif
        }

        private void TrySpawnPrizeInAR(Vector2 touchPosition)
        {
            if (arRaycastManager == null)
                return;

            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            
            if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                SpawnPrizeAt(hitPose.position, hitPose.rotation);
            }
        }

        private void SpawnPrizeAt(Vector3 position, Quaternion rotation)
        {
            if (prizePrefab != null)
            {
                spawnedPrize = Instantiate(prizePrefab, position, rotation);
                isPrizeSpawned = true;

                // Hide AR instructions
                if (arInstructions != null)
                    arInstructions.SetActive(false);

                // Show capture button
                if (captureButton != null)
                    captureButton.SetActive(true);

                Debug.Log("Prize spawned in AR!");
            }
        }

        #endregion

        #region Capture Execution

        public void OnCaptureButtonClicked()
        {
            if (currentPrize == null)
                return;

            StartCoroutine(ExecuteCapture());
        }

        private IEnumerator ExecuteCapture()
        {
            // Disable capture button
            if (captureButton != null)
                captureButton.SetActive(false);

            // Play capture animation
            if (isARSupported && spawnedPrize != null)
            {
                yield return StartCoroutine(PlayCaptureAnimation());
            }

            // Play particles
            if (captureParticles != null)
            {
                captureParticles.Play();
            }

            // Call backend to capture prize
            yield return StartCoroutine(GameManager.Instance.CapturePrize(currentPrize, (success, message, points) =>
            {
                if (success)
                {
                    OnCaptureSuccess(points);
                }
                else
                {
                    OnCaptureFailed(message);
                }
            }));

            // Cleanup
            EndCapture();
        }

        private IEnumerator PlayCaptureAnimation()
        {
            if (spawnedPrize == null)
                yield break;

            float elapsed = 0f;
            Vector3 startPos = spawnedPrize.transform.position;
            Vector3 endPos = Camera.main.transform.position;
            Vector3 startScale = spawnedPrize.transform.localScale;

            while (elapsed < captureAnimationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / captureAnimationDuration;

                // Move towards camera
                spawnedPrize.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // Shrink
                spawnedPrize.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                // Rotate
                spawnedPrize.transform.Rotate(Vector3.up, 360f * Time.deltaTime);

                yield return null;
            }

            Destroy(spawnedPrize);
        }

        #endregion

        #region Capture Result

        private void OnCaptureSuccess(int points)
        {
            Debug.Log($"Capture successful! +{points} points");
            
            // Show success UI
            UI.UIManager.Instance?.ShowMessage($"🎉 Prize captured!\n+{points} points");
            
            // Play success sound
            // AudioManager.Instance?.PlaySound("capture_success");
        }

        private void OnCaptureFailed(string message)
        {
            Debug.LogWarning($"Capture failed: {message}");
            
            // Show error UI
            UI.UIManager.Instance?.ShowMessage(message);
            
            // Re-enable capture button
            if (captureButton != null)
                captureButton.SetActive(true);
        }

        #endregion

        #region Cleanup

        private void EndCapture()
        {
            currentPrize = null;
            isPrizeSpawned = false;

            if (spawnedPrize != null)
            {
                Destroy(spawnedPrize);
            }

            // Disable AR components
            if (arPlaneManager != null)
                arPlaneManager.enabled = false;

            if (arRaycastManager != null)
                arRaycastManager.enabled = false;

            // Hide UI
            if (arInstructions != null)
                arInstructions.SetActive(false);

            if (captureButton != null)
                captureButton.SetActive(false);
        }

        public void CancelCapture()
        {
            EndCapture();
        }

        #endregion
        
        #region Report System
        
        /// <summary>
        /// Ouvre le dialogue de signalement de problème
        /// POST /capture/report
        /// </summary>
        public void OpenReportDialog()
        {
            if (string.IsNullOrEmpty(lastCaptureId))
            {
                UIManager.Instance?.ShowMessage("No recent capture to report");
                return;
            }
            
            UIManager.Instance?.ShowReportDialog(lastCaptureId, currentPrize);
        }
        
        /// <summary>
        /// Envoie un rapport de problème
        /// </summary>
        public IEnumerator ReportCaptureIssue(string captureId, string issueType, string description, System.Action<bool> callback = null)
        {
            var data = new
            {
                captureId = captureId,
                issueType = issueType, // gps_incorrect, prize_invalid, capture_failed, other
                description = description,
                timestamp = System.DateTime.UtcNow.ToString("o"),
                location = new
                {
                    lat = GPSManager.Instance.GetLatitude(),
                    lng = GPSManager.Instance.GetLongitude()
                }
            };
            
            string endpoint = "/capture/report";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    UIManager.Instance?.ShowMessage("Issue reported. Thank you!");
                    callback?.Invoke(true);
                    
                    Debug.Log($"[CaptureController] Issue reported: {issueType}");
                }
                else
                {
                    UIManager.Instance?.ShowMessage("Failed to report issue");
                    callback?.Invoke(false);
                }
            });
        }
        
        /// <summary>
        /// Définit l'ID de la dernière capture
        /// </summary>
        public void SetLastCaptureId(string captureId)
        {
            lastCaptureId = captureId;
            
            // Activer le bouton de report
            if (reportIssueButton != null)
            {
                reportIssueButton.gameObject.SetActive(true);
                reportIssueButton.onClick.RemoveAllListeners();
                reportIssueButton.onClick.AddListener(OpenReportDialog);
            }
        }
        
        #endregion
    }
}
