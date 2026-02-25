using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YallaCatch.API;
using YallaCatch.Core;
using YallaCatch.UI;

namespace YallaCatch
{
    /// <summary>
    /// Gère les réclamations de récompenses physiques
    /// Intégration avec le module claims backend (5 endpoints)
    /// Génère des codes de retrait et QR codes pour récupérer les récompenses chez les partenaires
    /// Contrôlé par l'admin panel
    /// </summary>
    public class ClaimsManager : MonoBehaviour
    {
        public static ClaimsManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float qrCodeDisplayDuration = 30f;
        
        // État
        private List<Claim> myClaims = new List<Claim>();
        private ClaimStats myStats;
        
        public bool IsInitialized => isInitialized;
        private bool isInitialized = false;

        // Events
        public delegate void OnClaimCreated(Claim claim);
        public event OnClaimCreated OnClaimCreatedEvent;
        
        public delegate void OnClaimStatusChanged(Claim claim);
        public event OnClaimStatusChanged OnClaimStatusChangedEvent;

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

        void Start()
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                Debug.Log("[ClaimsManager] Skipping auto-initialize until player is authenticated.");
                return;
            }
            StartCoroutine(Initialize());
        }

        /// <summary>
        /// Initialise le système de réclamations
        /// </summary>
        private IEnumerator Initialize()
        {
            // Charger les réclamations existantes
            yield return StartCoroutine(LoadMyClaims());
            
            // Charger les statistiques
            yield return StartCoroutine(LoadMyStats());
            
            isInitialized = true;
            Debug.Log("[ClaimsManager] Initialized successfully");
        }

        #region API Calls

        /// <summary>
        /// POST /claims/ - Créer une réclamation pour une récompense
        /// </summary>
        public IEnumerator CreateClaim(string redemptionId, string partnerId, string locationId, System.Action<Claim> callback = null)
        {
            var data = new
            {
                redemptionId = redemptionId,
                partnerId = partnerId,
                locationId = locationId,
                timestamp = System.DateTime.UtcNow.ToString("o"),
                location = new
                {
                    lat = GPSManager.Instance.CurrentLatitude,
                    lng = GPSManager.Instance.CurrentLongitude
                }
            };
            
            string endpoint = "/claims/";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    var token = response.data as JToken ?? (response.data != null ? JToken.FromObject(response.data) : null);
                    var claimToken = token?["claim"] ?? token;
                    var claim = claimToken != null ? claimToken.ToObject<Claim>() : null;
                    if (claim == null)
                    {
                        callback?.Invoke(null);
                        UIManager.Instance.ShowMessage("Failed to create claim");
                        return;
                    }
                    
                    // Ajouter à la liste locale
                    myClaims.Add(claim);
                    
                    OnClaimCreatedEvent?.Invoke(claim);
                    
                    callback?.Invoke(claim);
                    
                    // Afficher le QR code
                    UIManager.Instance.ShowClaimQRCode(claim);
                    
                    Debug.Log($"[ClaimsManager] Claim created: {claim.id} (Display duration: {qrCodeDisplayDuration}s)");
                }
                else
                {
                    callback?.Invoke(null);
                    UIManager.Instance.ShowMessage(response.message ?? "Failed to create claim");
                }
            });
        }

        /// <summary>
        /// GET /claims/my-claims - Liste de mes réclamations
        /// </summary>
        public IEnumerator LoadMyClaims(System.Action<List<Claim>> callback = null)
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                myClaims = new List<Claim>();
                callback?.Invoke(myClaims);
                yield break;
            }
            string endpoint = "/claims/my-claims";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    var token = response.data as JToken ?? (response.data != null ? JToken.FromObject(response.data) : null);
                    var claimsToken = token;
                    if (token != null && token.Type == JTokenType.Object)
                    {
                        claimsToken = token["claims"] ?? token["items"] ?? token;
                    }
                    myClaims = claimsToken != null && claimsToken.Type == JTokenType.Array
                        ? (claimsToken.ToObject<List<Claim>>() ?? new List<Claim>())
                        : new List<Claim>();
                    callback?.Invoke(myClaims);
                    
                    Debug.Log($"[ClaimsManager] Loaded {myClaims.Count} claims");
                }
                else
                {
                    callback?.Invoke(new List<Claim>());
                }
            });
        }

        /// <summary>
        /// GET /claims/:claimId - Détails d'une réclamation
        /// </summary>
        public IEnumerator GetClaimDetails(string claimId, System.Action<Claim> callback)
        {
            string endpoint = $"/claims/{claimId}";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    var claim = JsonConvert.DeserializeObject<Claim>(response.data.ToString());
                    
                    // Mettre à jour dans la liste locale
                    int index = myClaims.FindIndex(c => c.id == claimId);
                    if (index >= 0)
                    {
                        string oldStatus = myClaims[index].status;
                        myClaims[index] = claim;
                        
                        // Si le statut a changé, déclencher l'event
                        if (oldStatus != claim.status)
                        {
                            OnClaimStatusChangedEvent?.Invoke(claim);
                        }
                    }
                    
                    callback?.Invoke(claim);
                }
                else
                {
                    callback?.Invoke(null);
                }
            });
        }

        /// <summary>
        /// GET /claims/my-stats - Statistiques de réclamations
        /// </summary>
        public IEnumerator LoadMyStats(System.Action<ClaimStats> callback = null)
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                callback?.Invoke(null);
                yield break;
            }
            string endpoint = "/claims/my-stats";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    var token = response.data as JToken ?? (response.data != null ? JToken.FromObject(response.data) : null);
                    var statsToken = token?["stats"] ?? token;
                    myStats = statsToken != null ? statsToken.ToObject<ClaimStats>() : null;
                    callback?.Invoke(myStats);
                    
                    Debug.Log($"[ClaimsManager] Loaded stats: {(myStats != null ? myStats.totalClaims : 0)} total claims");
                }
                else
                {
                    callback?.Invoke(null);
                }
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Obtient toutes les réclamations
        /// </summary>
        public List<Claim> GetMyClaims()
        {
            return myClaims;
        }

        /// <summary>
        /// Obtient les réclamations par statut
        /// </summary>
        public List<Claim> GetClaimsByStatus(string status)
        {
            return myClaims.FindAll(c => c.status == status);
        }

        /// <summary>
        /// Obtient les réclamations en attente
        /// </summary>
        public List<Claim> GetPendingClaims()
        {
            return GetClaimsByStatus("pending");
        }

        /// <summary>
        /// Obtient les réclamations validées
        /// </summary>
        public List<Claim> GetValidatedClaims()
        {
            return GetClaimsByStatus("validated");
        }

        /// <summary>
        /// Obtient les réclamations expirées
        /// </summary>
        public List<Claim> GetExpiredClaims()
        {
            return GetClaimsByStatus("expired");
        }

        /// <summary>
        /// Obtient les statistiques
        /// </summary>
        public ClaimStats GetMyStats()
        {
            return myStats;
        }

        /// <summary>
        /// Vérifie si une réclamation est expirée
        /// </summary>
        public bool IsClaimExpired(Claim claim)
        {
            if (string.IsNullOrEmpty(claim.expiresAt))
                return false;
            
            System.DateTime expiryDate;
            if (System.DateTime.TryParse(claim.expiresAt, out expiryDate))
            {
                return System.DateTime.UtcNow > expiryDate;
            }
            
            return false;
        }

        /// <summary>
        /// Obtient le temps restant avant expiration
        /// </summary>
        public System.TimeSpan GetTimeUntilExpiry(Claim claim)
        {
            if (string.IsNullOrEmpty(claim.expiresAt))
                return System.TimeSpan.Zero;
            
            System.DateTime expiryDate;
            if (System.DateTime.TryParse(claim.expiresAt, out expiryDate))
            {
                return expiryDate - System.DateTime.UtcNow;
            }
            
            return System.TimeSpan.Zero;
        }

        /// <summary>
        /// Rafraîchit le statut d'une réclamation
        /// </summary>
        public void RefreshClaimStatus(string claimId)
        {
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
                return;
            StartCoroutine(GetClaimDetails(claimId, null));
        }

        /// <summary>
        /// Génère un QR code pour une réclamation (utilise le QR code du backend)
        /// </summary>
        public Texture2D GenerateQRCodeTexture(Claim claim)
        {
            // Le QR code est déjà généré côté backend
            // On pourrait le télécharger ou utiliser une lib Unity pour le générer
            // Pour l'instant, on retourne null et on affiche l'URL du QR code
            
            if (!string.IsNullOrEmpty(claim.qrCodeUrl))
            {
                // Télécharger l'image du QR code depuis l'URL
                StartCoroutine(DownloadQRCodeImage(claim.qrCodeUrl, (texture) =>
                {
                    // Texture téléchargée
                }));
            }
            
            return null;
        }

        /// <summary>
        /// Télécharge l'image du QR code
        /// </summary>
        private IEnumerator DownloadQRCodeImage(string url, System.Action<Texture2D> callback)
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
                    Debug.LogError($"[ClaimsManager] Failed to download QR code: {request.error}");
                    callback?.Invoke(null);
                }
            }
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class Claim
    {
        public string id;
        public string userId;
        public string redemptionId;
        public string rewardId;
        public string rewardName;
        public string rewardImage;
        public string partnerId;
        public string partnerName;
        public string locationId;
        public string locationName;
        public string locationAddress;
        public string status; // pending, validated, expired, cancelled
        public string claimCode; // Code à présenter au partenaire
        public string qrCode; // Données du QR code
        public string qrCodeUrl; // URL de l'image du QR code
        public string createdAt;
        public string validatedAt;
        public string expiresAt;
        public string validatorId; // ID de l'employé du partenaire qui a validé
        public string notes;
    }

    [System.Serializable]
    public class ClaimStats
    {
        public int totalClaims;
        public int pendingClaims;
        public int validatedClaims;
        public int expiredClaims;
        public int cancelledClaims;
        public string mostClaimedReward;
        public string favoritePartner;
    }

    #endregion
}
