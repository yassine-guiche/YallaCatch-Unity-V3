using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YallaCatch.Models;

namespace YallaCatch.API
{
    /// <summary>
    /// Marketplace API - Browse and purchase items
    /// </summary>
    public class MarketplaceAPI : MonoBehaviour
    {
        public static MarketplaceAPI Instance { get; private set; }
        public static bool PartnerRedeemRouteEnabled = true;

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
        /// Get marketplace items with filters
        /// </summary>
        public void GetMarketplace(MarketplaceFilters filters, Action<ApiResponse<MarketplaceResponse>> callback)
        {
            string query = BuildQueryString(filters);
            StartCoroutine(APIClient.Instance.Get<MarketplaceResponse>(APIEndpoints.MARKETPLACE_LIST + query, callback));
        }

        #endregion

        #region Purchase Methods

        /// <summary>
        /// Purchase an item
        /// </summary>
        public void PurchaseItem(string itemId, Location location = null, Action<ApiResponse<PurchaseResponse>> callback = null)
        {
            var body = new
            {
                itemId,
                location = location != null ? new
                {
                    latitude = location.latitude,
                    longitude = location.longitude
                } : null,
                deviceInfo = new
                {
                    platform = APIClient.Instance.GetPlatform(),
                    version = Application.version
                }
            };
            StartCoroutine(APIClient.Instance.Post<PurchaseResponse>(APIEndpoints.MARKETPLACE_PURCHASE, body, callback));
        }

        /// <summary>
        /// Get purchase/redemption history
        /// </summary>
        public void GetRedemptions(string status = null, int page = 1, int limit = 20, Action<ApiResponse<MarketplaceRedemptionsResponse>> callback = null)
        {
            string query = $"?page={page}&limit={limit}";
            if (!string.IsNullOrEmpty(status)) query += $"&status={status}";
            StartCoroutine(APIClient.Instance.Get<MarketplaceRedemptionsResponse>(APIEndpoints.MARKETPLACE_REDEMPTIONS + query, callback));
        }

        /// <summary>
        /// Redeem item at partner location
        /// </summary>
        public void RedeemAtPartner(string redemptionCode, Location location, string verificationCode = null, Action<ApiResponse<PartnerRedeemResponse>> callback = null)
        {
            if (!PartnerRedeemRouteEnabled)
            {
                callback?.Invoke(new ApiResponse<PartnerRedeemResponse>
                {
                    success = false,
                    error = "ENDPOINT_NOT_IMPLEMENTED",
                    message = "Partner redeem endpoint is not enabled on backend yet."
                });
                return;
            }

            var body = new
            {
                redemptionCode,
                location = new
                {
                    latitude = location.latitude,
                    longitude = location.longitude
                },
                verificationCode
            };
            StartCoroutine(APIClient.Instance.Post<PartnerRedeemResponse>(APIEndpoints.MARKETPLACE_REDEEM, body, callback));
        }

        #endregion

        #region Utility

        private string BuildQueryString(MarketplaceFilters filters)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(filters.category)) parts.Add($"category={Uri.EscapeDataString(filters.category)}");
            if (filters.minPoints.HasValue) parts.Add($"minPoints={filters.minPoints}");
            if (filters.maxPoints.HasValue) parts.Add($"maxPoints={filters.maxPoints}");
            if (!string.IsNullOrEmpty(filters.search)) parts.Add($"search={Uri.EscapeDataString(filters.search)}");
            if (filters.featured.HasValue) parts.Add($"featured={filters.featured.Value.ToString().ToLower()}");
            if (filters.page > 0) parts.Add($"page={filters.page}");
            if (filters.limit > 0) parts.Add($"limit={filters.limit}");
            return parts.Count > 0 ? "?" + string.Join("&", parts) : "";
        }

        #endregion
    }

    // Models moved to Models.cs
}
