using System.Collections.Generic;
using UnityEngine;
using YallaCatch.API;
using YallaCatch.Models;

namespace YallaCatch.Managers
{
    /// <summary>
    /// Manages rewards browsing, redemption, and favorites
    /// Uses RewardsAPI and MarketplaceAPI
    /// </summary>
    public class RewardsManager : MonoBehaviour
    {
        public static RewardsManager Instance { get; private set; }

        [Header("Cache")]
        public List<Reward> CachedRewards { get; private set; }
        public List<Reward> FavoriteRewards { get; private set; }
        public List<Redemption> MyRedemptions { get; private set; }
        public List<string> Categories { get; private set; }

        // Events
        public event System.Action OnRewardsLoaded;
        public event System.Action OnFavoritesUpdated;
        public event System.Action<Redemption> OnRewardRedeemed;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                CachedRewards = new List<Reward>();
                FavoriteRewards = new List<Reward>();
                MyRedemptions = new List<Redemption>();
                Categories = new List<string>();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Browse Rewards

        public void LoadRewards(string category = null, int page = 1, int limit = 50, System.Action<bool> callback = null)
        {
            var filters = new RewardFilters
            {
                category = category,
                page = page,
                limit = limit
            };

            RewardsAPI.Instance.GetRewards(filters, response =>
            {
                if (response.success && response.data != null)
                {
                    if (page == 1)
                    {
                        CachedRewards.Clear();
                    }
                    CachedRewards.AddRange(response.data.rewards);
                    
                    if (response.data.categories != null)
                    {
                        Categories.Clear();
                        Categories.AddRange(response.data.categories);
                    }
                    
                    OnRewardsLoaded?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void SearchRewards(string query, System.Action<List<Reward>> callback)
        {
            RewardsAPI.Instance.SearchRewards(query, null, 20, response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.rewards);
                }
                else
                {
                    callback?.Invoke(new List<Reward>());
                }
            });
        }

        public void GetRewardDetails(string rewardId, System.Action<Reward> callback)
        {
            RewardsAPI.Instance.GetRewardDetails(rewardId, response =>
            {
                callback?.Invoke(response.success ? response.data : null);
            });
        }

        public void GetFeaturedRewards(System.Action<List<Reward>> callback)
        {
            RewardsAPI.Instance.GetFeatured(10, response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.rewards);
                }
                else
                {
                    callback?.Invoke(new List<Reward>());
                }
            });
        }

        public void GetCategories(System.Action<List<string>> callback)
        {
            RewardsAPI.Instance.GetCategories(response =>
            {
                if (response.success && response.data != null)
                {
                    Categories.Clear();
                    Categories.AddRange(response.data.categories);
                    callback?.Invoke(Categories);
                }
                else
                {
                    callback?.Invoke(Categories);
                }
            });
        }

        #endregion

        #region Redeem Rewards

        public void RedeemReward(string rewardId, System.Action<bool, Redemption, string> callback)
        {
            int pointsCost = CachedRewards.Find(r => r._id == rewardId)?.pointsCost ?? 0;
            
            if (GameManager.Instance.PlayerPoints < pointsCost)
            {
                callback?.Invoke(false, null, "Not enough points!");
                return;
            }

            RewardsAPI.Instance.RedeemReward(rewardId, null, response =>
            {
                if (response.success && response.data != null)
                {
                    // Update player points
                    GameManager.Instance.AddPoints(-response.data.userBalance.pointsSpent);
                    
                    MyRedemptions.Insert(0, response.data.redemption);
                    OnRewardRedeemed?.Invoke(response.data.redemption);
                    callback?.Invoke(true, response.data.redemption, "Reward redeemed successfully!");
                }
                else
                {
                    callback?.Invoke(false, null, response.message ?? "Redemption failed");
                }
            });
        }

        public void LoadMyRedemptions(int page = 1, System.Action<bool> callback = null)
        {
            RewardsAPI.Instance.GetMyRedemptions(page, 20, response =>
            {
                if (response.success && response.data != null)
                {
                    if (page == 1)
                    {
                        MyRedemptions.Clear();
                    }
                    MyRedemptions.AddRange(response.data.redemptions);
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        #endregion

        #region Favorites

        public void LoadFavorites(System.Action<bool> callback = null)
        {
            RewardsAPI.Instance.GetFavorites(response =>
            {
                if (response.success && response.data != null)
                {
                    FavoriteRewards.Clear();
                    FavoriteRewards.AddRange(response.data.rewards);
                    OnFavoritesUpdated?.Invoke();
                    callback?.Invoke(true);
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        public void AddToFavorites(string rewardId, System.Action<bool> callback = null)
        {
            RewardsAPI.Instance.AddToFavorites(rewardId, response =>
            {
                if (response.success)
                {
                    var reward = CachedRewards.Find(r => r._id == rewardId);
                    if (reward != null && !FavoriteRewards.Exists(r => r._id == rewardId))
                    {
                        FavoriteRewards.Add(reward);
                        OnFavoritesUpdated?.Invoke();
                    }
                }
                callback?.Invoke(response.success);
            });
        }

        public void RemoveFromFavorites(string rewardId, System.Action<bool> callback = null)
        {
            RewardsAPI.Instance.RemoveFromFavorites(rewardId, response =>
            {
                if (response.success)
                {
                    FavoriteRewards.RemoveAll(r => r._id == rewardId);
                    OnFavoritesUpdated?.Invoke();
                }
                callback?.Invoke(response.success);
            });
        }

        public bool IsFavorite(string rewardId)
        {
            return FavoriteRewards.Exists(r => r._id == rewardId);
        }

        #endregion

        #region Promo Codes

        public void RedeemPromoCode(string code, System.Action<bool, string, int> callback)
        {
            RewardsAPI.Instance.RedeemPromoCode(code, response =>
            {
                if (response.success && response.data != null)
                {
                    if (response.data.pointsAwarded > 0)
                    {
                        GameManager.Instance.AddPoints(response.data.pointsAwarded);
                    }
                    callback?.Invoke(true, response.data.message ?? "Promo code redeemed!", response.data.pointsAwarded);
                }
                else
                {
                    callback?.Invoke(false, response.message ?? "Invalid promo code", 0);
                }
            });
        }

        #endregion

        #region Partners

        public void GetPartners(System.Action<List<Partner>> callback)
        {
            RewardsAPI.Instance.GetPartners(response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.partners);
                }
                else
                {
                    callback?.Invoke(new List<Partner>());
                }
            });
        }

        public void GetPartnerLocations(string partnerId, System.Action<List<PartnerLocation>> callback)
        {
            RewardsAPI.Instance.GetPartnerLocations(partnerId, response =>
            {
                if (response.success && response.data != null)
                {
                    callback?.Invoke(response.data.locations);
                }
                else
                {
                    callback?.Invoke(new List<PartnerLocation>());
                }
            });
        }

        #endregion
    }
}
