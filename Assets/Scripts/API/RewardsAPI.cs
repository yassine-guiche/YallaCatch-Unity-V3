using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json.Linq;
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
            StartCoroutine(APIClient.Instance.Get<JToken>(APIEndpoints.REWARDS_LIST + query, response =>
            {
                callback?.Invoke(NormalizeRewardsListResponse(response));
            }));
        }

        /// <summary>
        /// Search rewards
        /// </summary>
        public void SearchRewards(string searchQuery, string category = null, int limit = 20, Action<ApiResponse<RewardsListResponse>> callback = null)
        {
            string query = $"?query={Uri.EscapeDataString(searchQuery)}&limit={limit}";
            if (!string.IsNullOrEmpty(category)) query += $"&category={category}";
            StartCoroutine(APIClient.Instance.Get<JToken>(APIEndpoints.REWARDS_SEARCH + query, response =>
            {
                callback?.Invoke(NormalizeRewardsListResponse(response));
            }));
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
            StartCoroutine(APIClient.Instance.Get<object>(APIEndpoints.REWARDS_CATEGORIES, response =>
            {
                if (!response.success)
                {
                    callback?.Invoke(new ApiResponse<CategoriesResponse>
                    {
                        success = false,
                        error = response.error,
                        message = response.message,
                        timestamp = response.timestamp
                    });
                    return;
                }

                var categories = new List<string>();
                try
                {
                    JToken token = response.data as JToken ?? (response.data != null ? JToken.FromObject(response.data) : null);
                    if (token != null)
                    {
                        if (token.Type == JTokenType.Array)
                        {
                            categories = token.ToObject<List<string>>() ?? new List<string>();
                        }
                        else if (token.Type == JTokenType.Object)
                        {
                            categories = (token["categories"]?.ToObject<List<string>>() ?? new List<string>());
                        }
                    }
                }
                catch (Exception ex)
                {
                    callback?.Invoke(new ApiResponse<CategoriesResponse>
                    {
                        success = false,
                        error = "PARSE_ERROR",
                        message = ex.Message,
                        timestamp = response.timestamp
                    });
                    return;
                }

                callback?.Invoke(new ApiResponse<CategoriesResponse>
                {
                    success = true,
                    data = new CategoriesResponse { categories = categories },
                    message = response.message,
                    timestamp = response.timestamp
                });
            }));
        }

        /// <summary>
        /// Get featured rewards
        /// </summary>
        public void GetFeatured(int limit = 10, Action<ApiResponse<RewardsListResponse>> callback = null)
        {
            string query = $"?limit={limit}";
            StartCoroutine(APIClient.Instance.Get<JToken>(APIEndpoints.REWARDS_FEATURED + query, response =>
            {
                callback?.Invoke(NormalizeRewardsListResponse(response));
            }));
        }

        /// <summary>
        /// Get partner list (partner data is available on Reward objects via partnerId populate)
        /// Use the partner info returned in reward listings instead of a separate endpoint
        /// </summary>
        public void GetPartners(Action<ApiResponse<PartnersResponse>> callback)
        {
            var filters = new RewardFilters { page = 1, limit = 200 };
            GetRewards(filters, response =>
            {
                if (!response.success || response.data == null)
                {
                    callback?.Invoke(new ApiResponse<PartnersResponse>
                    {
                        success = false,
                        message = response.message ?? "Failed to load rewards for partner list",
                        error = response.error
                    });
                    return;
                }

                var partners = (response.data.rewards ?? new List<Reward>())
                    .Where(r => !string.IsNullOrWhiteSpace(r.partnerId) || !string.IsNullOrWhiteSpace(r.partnerName))
                    .GroupBy(r => !string.IsNullOrWhiteSpace(r.partnerId) ? r.partnerId : r.partnerName)
                    .Select(group =>
                    {
                        var first = group.First();
                        return new Partner
                        {
                            _id = first.partnerId,
                            name = first.partnerName,
                            logo = first.partnerLogo,
                            rewardsCount = group.Count()
                        };
                    })
                    .OrderBy(p => p.name)
                    .ToList();

                callback?.Invoke(new ApiResponse<PartnersResponse>
                {
                    success = true,
                    data = new PartnersResponse { partners = partners },
                    message = "Partners derived from rewards list"
                });
            });
        }

        /// <summary>
        /// Compatibility fallback. Backend no longer exposes a dedicated partner-locations endpoint in the Unity-facing rewards API.
        /// Returns an empty list so legacy UI can degrade gracefully.
        /// </summary>
        public void GetPartnerLocations(string partnerId, Action<ApiResponse<PartnerLocationsResponse>> callback)
        {
            callback?.Invoke(new ApiResponse<PartnerLocationsResponse>
            {
                success = true,
                data = new PartnerLocationsResponse { locations = new List<PartnerLocation>() },
                message = "Partner locations endpoint not available; returning empty list"
            });
        }

        #endregion

        #region Redemption Methods

        /// <summary>
        /// Redeem a reward
        /// </summary>
        public void RedeemReward(string rewardId, string idempotencyKey, Location location = null, Action<ApiResponse<RedemptionResponse>> callback = null)
        {
            // Endpoint is /rewards/redeem
            var body = new
            {
                rewardId,
                idempotencyKey,
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
            StartCoroutine(APIClient.Instance.Get<JToken>(APIEndpoints.REWARDS_FAVORITES, response =>
            {
                callback?.Invoke(NormalizeRewardsListResponse(response));
            }));
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

        private ApiResponse<RewardsListResponse> NormalizeRewardsListResponse(ApiResponse<JToken> response)
        {
            if (response == null)
            {
                return new ApiResponse<RewardsListResponse>
                {
                    success = false,
                    error = "NULL_RESPONSE",
                    message = "No response received"
                };
            }

            var normalized = new ApiResponse<RewardsListResponse>
            {
                success = response.success,
                error = response.error,
                message = response.message,
                timestamp = response.timestamp
            };

            if (!response.success)
            {
                return normalized;
            }

            if (response.data == null || response.data.Type == JTokenType.Null)
            {
                normalized.data = new RewardsListResponse
                {
                    rewards = new List<Reward>(),
                    categories = new List<string>()
                };
                return normalized;
            }

            try
            {
                if (response.data.Type == JTokenType.Array)
                {
                    normalized.data = new RewardsListResponse
                    {
                        rewards = response.data.ToObject<List<Reward>>() ?? new List<Reward>(),
                        categories = new List<string>()
                    };
                    return normalized;
                }

                if (response.data.Type == JTokenType.Object)
                {
                    JObject obj = (JObject)response.data;

                    // Standard backend shape: { rewards, pagination, categories }
                    if (obj["rewards"] != null)
                    {
                        normalized.data = obj.ToObject<RewardsListResponse>() ?? new RewardsListResponse();
                    }
                    else
                    {
                        // Fallback: object may itself be a single reward or alternate wrapper
                        normalized.data = new RewardsListResponse
                        {
                            rewards = new List<Reward>(),
                            categories = new List<string>()
                        };
                    }

                    if (normalized.data.rewards == null)
                    {
                        normalized.data.rewards = new List<Reward>();
                    }
                    if (normalized.data.categories == null)
                    {
                        normalized.data.categories = new List<string>();
                    }
                    return normalized;
                }

                normalized.success = false;
                normalized.error = "INVALID_REWARDS_RESPONSE";
                normalized.message = $"Unexpected rewards response type: {response.data.Type}";
                return normalized;
            }
            catch (Exception ex)
            {
                return new ApiResponse<RewardsListResponse>
                {
                    success = false,
                    error = "PARSE_ERROR",
                    message = ex.Message,
                    timestamp = response.timestamp
                };
            }
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
    public class RewardResponse
    {
        public bool success;
        public Reward reward;
        public string qrPayload; // Added for QR display consistency
        public UserBalance userBalance;
    }

    [Serializable]
    public class PromoCodeResponse
    {
        public bool success;
        public string type; // points, reward
        public int pointsAwarded;
        public Reward reward;
        public string qr; // Added for reward-based promo codes
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
