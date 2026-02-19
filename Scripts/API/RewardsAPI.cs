using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// Rewards API - Browse, redeem, favorites, promo codes
    /// </summary>
    public class RewardsAPI : MonoBehaviour
    {
        public static RewardsAPI Instance { get; private set; }

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

        #region Browse Methods

        /// <summary>
        /// Get available rewards with filters
        /// </summary>
        public void GetRewards(RewardFilters filters, Action<ApiResponse<RewardsListResponse>> callback)
        {
            string query = BuildQueryString(filters);
            StartCoroutine(APIClient.Instance.Get<RewardsListResponse>(APIEndpoints.REWARDS_LIST + query, callback));
        }

        /// <summary>
        /// Search rewards
        /// </summary>
        public void SearchRewards(string searchQuery, string category = null, int limit = 20, Action<ApiResponse<RewardsListResponse>> callback = null)
        {
            string query = $"?query={Uri.EscapeDataString(searchQuery)}&limit={limit}";
            if (!string.IsNullOrEmpty(category)) query += $"&category={category}";
            StartCoroutine(APIClient.Instance.Get<RewardsListResponse>(APIEndpoints.REWARDS_SEARCH + query, callback));
        }

        /// <summary>
        /// Get reward details
        /// </summary>
        public void GetRewardDetails(string rewardId, Action<ApiResponse<Reward>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.REWARDS_DETAILS, rewardId);
            StartCoroutine(APIClient.Instance.Get<Reward>(endpoint, callback));
        }

        /// <summary>
        /// Get reward categories
        /// </summary>
        public void GetCategories(Action<ApiResponse<CategoriesResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<CategoriesResponse>(APIEndpoints.REWARDS_CATEGORIES, callback));
        }

        /// <summary>
        /// Get featured rewards
        /// </summary>
        public void GetFeatured(int limit = 10, Action<ApiResponse<RewardsListResponse>> callback = null)
        {
            string query = $"?limit={limit}";
            StartCoroutine(APIClient.Instance.Get<RewardsListResponse>(APIEndpoints.REWARDS_FEATURED + query, callback));
        }

        /// <summary>
        /// Get partner list (partner data is available on Reward objects via partnerId populate)
        /// Use the partner info returned in reward listings instead of a separate endpoint
        /// </summary>

        #endregion

        #region Redemption Methods

        /// <summary>
        /// Redeem a reward
        /// </summary>
        public void RedeemReward(string rewardId, Location location = null, Action<ApiResponse<RedemptionResponse>> callback = null)
        {
            // Endpoint is just /rewards/redeem now
            var body = new
            {
                rewardId,
                location = location != null ? new
                {
                    latitude = location.latitude,
                    longitude = location.longitude
                } : null
            };
            StartCoroutine(APIClient.Instance.Post<RedemptionResponse>(APIEndpoints.REWARDS_REDEEM, body, callback));
        }

        /// <summary>
        /// Get user redemptions
        /// </summary>
        public void GetMyRedemptions(int page = 1, int limit = 20, Action<ApiResponse<RedemptionsListResponse>> callback = null)
        {
            string query = $"?page={page}&limit={limit}";
            StartCoroutine(APIClient.Instance.Get<RedemptionsListResponse>(APIEndpoints.REWARDS_MY_REDEMPTIONS + query, callback));
        }

        /// <summary>
        /// Get full redemption history
        /// </summary>
        public void GetHistory(int page = 1, int limit = 50, Action<ApiResponse<RedemptionsListResponse>> callback = null)
        {
            string query = $"?page={page}&limit={limit}";
            StartCoroutine(APIClient.Instance.Get<RedemptionsListResponse>(APIEndpoints.REWARDS_HISTORY + query, callback));
        }

        /// <summary>
        /// Scan QR code for redemption
        /// </summary>
        public void ScanQRCode(string code, Location location, Action<ApiResponse<QRScanResponse>> callback)
        {
            var body = new
            {
                code,
                location = new
                {
                    latitude = location.latitude,
                    longitude = location.longitude
                }
            };
            StartCoroutine(APIClient.Instance.Post<QRScanResponse>(APIEndpoints.REWARDS_QR_SCAN, body, callback));
        }

        /// <summary>
        /// Redeem promo code
        /// </summary>
        public void RedeemPromoCode(string code, Action<ApiResponse<PromoCodeResponse>> callback)
        {
            var body = new { code };
            StartCoroutine(APIClient.Instance.Post<PromoCodeResponse>(APIEndpoints.REWARDS_PROMO, body, callback));
        }

        #endregion

        #region Favorites Methods

        /// <summary>
        /// Add reward to favorites (uses path param to match backend POST /favorites/:id)
        /// </summary>
        public void AddToFavorites(string rewardId, Action<ApiResponse<FavoriteResponse>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.REWARDS_FAVORITES_ADD, rewardId);
            StartCoroutine(APIClient.Instance.Post<FavoriteResponse>(endpoint, new {}, callback));
        }

        /// <summary>
        /// Remove from favorites
        /// </summary>
        public void RemoveFromFavorites(string rewardId, Action<ApiResponse<FavoriteResponse>> callback)
        {
            string endpoint = APIEndpoints.Format(APIEndpoints.REWARDS_FAVORITES_REMOVE, rewardId);
            StartCoroutine(APIClient.Instance.Delete<FavoriteResponse>(endpoint, callback));
        }

        /// <summary>
        /// Get favorite rewards
        /// </summary>
        public void GetFavorites(Action<ApiResponse<RewardsListResponse>> callback)
        {
            StartCoroutine(APIClient.Instance.Get<RewardsListResponse>(APIEndpoints.REWARDS_FAVORITES, callback));
        }

        #endregion

        #region Utility

        private string BuildQueryString(RewardFilters filters)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(filters.category)) parts.Add($"category={Uri.EscapeDataString(filters.category)}");
            if (filters.page > 0) parts.Add($"page={filters.page}");
            if (filters.limit > 0) parts.Add($"limit={filters.limit}");
            if (!string.IsNullOrEmpty(filters.sort)) parts.Add($"sort={filters.sort}");
            if (!string.IsNullOrEmpty(filters.listingType)) parts.Add($"listingType={filters.listingType}");
            return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        }

        #endregion
    }

    #region Rewards Request/Response Models

    [Serializable]
    public class RewardFilters
    {
        public string category;
        public int page = 1;
        public int limit = 50;
        public string sort = "pointsCost"; // pointsCost, name, popularity
        public string listingType;
    }

    [Serializable]
    public class RewardsListResponse
    {
        public List<Reward> rewards;
        public Pagination pagination;
        public List<string> categories;
    }

    [Serializable]
    public class CategoriesResponse
    {
        public List<string> categories;
    }

    [Serializable]
    public class PartnersResponse
    {
        public List<Partner> partners;
    }

    [Serializable]
    public class Partner
    {
        public string _id;
        public string name;
        public string logo;
        public string description;
        public int rewardsCount;

        public string Id => _id;
    }

    [Serializable]
    public class PartnerLocationsResponse
    {
        public List<PartnerLocation> locations;
    }

    [Serializable]
    public class PartnerLocation
    {
        public string id;
        public string name;
        public string address;
        public float lat;
        public float lng;
        public string phone;
        public string openingHours;
    }

    [Serializable]
    public class RedemptionResponse
    {
        public bool success;
        public Redemption redemption;
        public UserBalance userBalance;
        public string message;
    }

    [Serializable]
    public class UserBalance
    {
        public int previousPoints;
        public int pointsSpent;
        public int remainingPoints;
    }

    [Serializable]
    public class RedemptionsListResponse
    {
        public List<Redemption> redemptions;
        public Pagination pagination;
        public RedemptionSummary summary;
    }

    [Serializable]
    public class RedemptionSummary
    {
        public int totalRedeemed;
        public int pendingCount;
        public int fulfilledCount;
    }

    [Serializable]
    public class QRScanResponse
    {
        public bool success;
        public string message;
        public Redemption redemption;
    }

    [Serializable]
    public class PromoCodeResponse
    {
        public bool success;
        public string type; // points, reward
        public int pointsAwarded;
        public Reward reward;
        public string message;
    }

    [Serializable]
    public class FavoriteResponse
    {
        public bool success;
        public string message;
        public int favoritesCount;
    }

    #endregion
}
