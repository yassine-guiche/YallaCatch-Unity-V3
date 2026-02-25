using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace YallaCatch.Models
{
    #region User Models

    /// <summary>
    /// User profile matching backend User model
    /// </summary>
    [Serializable]
    public class User
    {
        public string _id;
        [JsonProperty("id")]
        public string authId;
        public string displayName;
        [JsonProperty("username")]
        public string username;
        [JsonProperty("name")]
        public string name;
        public string email;
        public string role;
        public string level;
        public UserPoints points;
        public string avatar;
        public UserLocation location;
        public UserStats stats;
        [JsonProperty("preferences")]
        public UserSettings settings;
        public bool isOnline;
        public string lastIp;
        public string lastUserAgent;
        public DateTime lastActive;
        public DateTime createdAt;

        // Flattened for Unity Mandate
        public float lat;
        public float lng;

        // Convenience properties
        public string Id => !string.IsNullOrEmpty(_id) ? _id : authId;
        public int AvailablePoints => points?.available ?? 0;
        public int TotalPoints => points?.total ?? 0;
        public string DisplayNameSafe => !string.IsNullOrWhiteSpace(displayName)
            ? displayName
            : (!string.IsNullOrWhiteSpace(username) ? username : (!string.IsNullOrWhiteSpace(name) ? name : "Player"));
    }

    [Serializable]
    public class UserPoints
    {
        public int available;
        public int total;
        public int spent;
    }

    [Serializable]
    public class UserLocation
    {
        public string city;
        public string governorate;
        public string country;
    }

    [Serializable]
    public class UserStats
    {
        public int prizesFound;
        public int rewardsRedeemed;
        public int sessionsCount;
        public int totalPlayTime;
        public int longestStreak;
        public int currentStreak;
        public string favoriteCity;
        public int dailyClaimsCount;
    }

    [Serializable]
    public class UserSettings
    {
        public string language;
        public string theme;
        [JsonProperty("notifications")]
        public bool notificationsEnabled;
        public bool soundEnabled;
    }

    #endregion

    #region Prize Models

    /// <summary>
    /// Prize matching backend Prize model
    /// </summary>
    [Serializable]
    public class Prize
    {
        public string _id;
        public string name;
        public string description;
        public PrizeLocation location;
        public int pointsValue;
        public string imageUrl;
        public string displayType; // standard, mystery_box, treasure, bonus, special
        public string contentType; // points, reward, hybrid
        public string rarity; // common, uncommon, rare, epic, legendary
        public string status; // active, captured, expired, inactive
        public string category;
        public DateTime expiresAt;
        public PrizeContent content;

        // Flattened for Unity Mandate
        public float lat;
        public float lng;

        // Convenience
        public string Id => _id;
        public float Latitude => lat != 0 ? lat : (location?.lat ?? 0);
        public float Longitude => lng != 0 ? lng : (location?.lng ?? 0);
    }

    [Serializable]
    public class PrizeLocation
    {
        public float lat;
        public float lng;
        public string city;
    }

    [Serializable]
    public class PrizeContent
    {
        public int points;
        public string rewardId;
        public float rewardProbability;
    }

    #endregion

    #region Claim Models

    /// <summary>
    /// Claim/Capture result matching backend Claim model
    /// </summary>
    [Serializable]
    public class Claim
    {
        public string _id;
        public string prizeId;
        public string userId;
        public int pointsEarned;
        public string status; // pending, confirmed, failed
        public ClaimLocation location;
        public DateTime claimedAt;
        public string rewardGranted;
        public string rewardName;
        public string claimCode;
        public string qrCodeUrl;

        public string Id => _id;
    }

    [Serializable]
    public class ClaimLocation
    {
        public float lat;
        public float lng;
        public float accuracy;
    }

    #endregion

    #region Reward Models

    /// <summary>
    /// Reward matching backend Reward model
    /// </summary>
    [Serializable]
    public class Reward
    {
        public string _id;
        public string name;
        public string description;
        public int pointsCost;
        public string category; // voucher, gift_card, physical, digital, experience
        public List<string> images;
        
        // Stock management matching Backend
        public int stockQuantity;
        public int stockAvailable;
        public int stockReserved;
        
        public string partnerId;
        public string partnerName;
        public string partnerLogo;
        
        // Metadata matching Backend instead of flat terms
        public RewardMetadata metadata;
        
        public bool isPopular; // Matches backend instead of isFeatured
        public DateTime createdAt; // Replaces validUntil which is not strongly typed on root

        // Properties
        public string Id => _id;
        public string PrimaryImage => images?.Count > 0 ? images[0] : null;
        public int stock => stockAvailable; // Backwards compatibility for UI
    }

    [Serializable]
    public class RewardMetadata
    {
        // Backend sometimes returns `terms` as a plain string and sometimes as an object.
        // Keep this as object to avoid deserialization failures, and parse on demand if needed.
        public object terms;
        public int validityPeriod;
        public bool isSponsored;
    }

    [Serializable]
    public class RewardTerms
    {
        public string conditions;
        public string howToRedeem;
        public int validityDays;
    }

    #endregion

    #region Redemption Models

    /// <summary>
    /// Redemption matching backend Redemption model
    /// </summary>
    [Serializable]
    public class Redemption
    {
        public string _id;
        public string rewardId;
        public string userId;
        public string code;
        public string qrCode;
        public string status; // pending, fulfilled, cancelled, failed
        public int pointsSpent;
        public DateTime validUntil;
        public DateTime redeemedAt;
        public RedemptionReward reward;

        public string Id => _id;
    }

    [Serializable]
    public class RedemptionReward
    {
        public string name;
        public string description;
        public string partnerName;
    }

    #endregion

    #region Social Models

    /// <summary>
    /// Friend matching backend Friend model
    /// </summary>
    [Serializable]
    public class Friend
    {
        public string friendId;
        public string displayName;
        public string avatar;
        public string level;
        public int points;
        public string status; // pending, accepted, blocked
        public bool isOnline;
        public DateTime lastActive;
    }

    /// <summary>
    /// Team matching backend Team model
    /// </summary>
    [Serializable]
    public class Team
    {
        public string _id;
        public string name;
        public string description;
        public List<TeamMember> members;
        public int score;
        public int maxMembers;
        public bool isPublic;
        public string creatorId;

        public string Id => _id;
    }

    [Serializable]
    public class TeamMember
    {
        public string userId;
        public string displayName;
        public string role; // leader, member
        public int contribution;
    }

    /// <summary>
    /// Nearby player
    /// </summary>
    [Serializable]
    public class NearbyPlayer
    {
        public string userId;
        public string displayName;
        public string level;
        public string avatar;
        public NearbyPlayerPosition position;
        public float lat;
        public float lng;
        public float distance;
        public string activity;

        public float Latitude => lat != 0 ? lat : (position?.lat ?? 0f);
        public float Longitude => lng != 0 ? lng : (position?.lng ?? 0f);
        public bool HasCoordinates => position != null || !(Math.Abs(Latitude) < 0.000001f && Math.Abs(Longitude) < 0.000001f);
    }

    [Serializable]
    public class NearbyPlayerPosition
    {
        public float lat;
        public float lng;
    }

    /// <summary>
    /// Leaderboard entry
    /// </summary>
    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string odlUserId;
        public string displayName;
        public string avatar;
        public string level;
        public int points;
        public int prizesFound;
    }

    #endregion

    #region Game Session Models

    /// <summary>
    /// Game session data
    /// </summary>
    [Serializable]
    public class GameSession
    {
        public string sessionId;
        public string userId;
        public DateTime startTime;
        public DateTime? endTime;
        public int duration;
        public SessionLocation initialLocation;
        public SessionLocation lastLocation;
        public float distanceTraveled;
        public int prizesFound;
        public int claimsAttempted;
        public string status;
    }

    [Serializable]
    public class SessionLocation
    {
        [JsonProperty("latitude")]
        public float lat;
        [JsonProperty("longitude")]
        public float lng;
        public float accuracy;

        // Request/response aliases used across backend modules
        [JsonIgnore] public float latitude { get => lat; set => lat = value; }
        [JsonIgnore] public float longitude { get => lng; set => lng = value; }
    }

    /// <summary>
    /// Daily challenge
    /// </summary>
    [Serializable]
    public class DailyChallenge
    {
        public string id;
        public string title;
        public string description;
        public string type; // claims, distance, categories
        public int target;
        public int progress;
        public int reward;
        public bool completed;
        public DateTime? completedAt;
    }

    #endregion

    #region Notification Models

    /// <summary>
    /// Notification matching backend Notification model
    /// </summary>
    [Serializable]
    public class Notification
    {
        public string _id;
        public string id { set { if (string.IsNullOrEmpty(_id)) _id = value; } }
        public string title;
        public string message;
        public string type; // push, email, sms, in_app
        public bool read;
        public DateTime createdAt;
        public Dictionary<string, object> metadata;

        public string Id => _id;
    }

    #endregion

    #region AR Models

    /// <summary>
    /// AR session data
    /// </summary>
    [Serializable]
    public class ARSession
    {
        public string sessionId;
        public string prizeId;
        public string modelUrl;
        public ARModelConfig modelConfig;
    }

    [Serializable]
    public class ARModelConfig
    {
        public float scale;
        public string animationType;
        public string particleEffect;
    }

    /// <summary>
    /// Capture result from AR capture
    /// </summary>
    [Serializable]
    public class CaptureResult
    {
        public bool success;
        public string prizeId;
        public string claimId;
        public CaptureContent content;
        public CaptureAnimation animation;
        public CaptureStats stats;
    }

    [Serializable]
    public class CaptureContent
    {
        public string type; // mystery_box, direct_points, power_up, special_item
        public string animation; // standard, rare, epic, legendary
        public int points;
        public string rewardId;
    }

    [Serializable]
    public class CaptureAnimation
    {
        public string type;
        public string rarity;
        public float duration;
    }

    [Serializable]
    public class CaptureStats
    {
        public int totalPoints;
        public int streak;
        public string[] achievements;
    }

    #endregion

    #region Gamification Models

    [Serializable]
    public class Achievement
    {
        public string _id;
        public string name;
        public string description;
        public string icon;
        public string category;
        public string type; // condition type
        public float targetValue;
        public int rewardPoints;
        public bool isUnlocked;
        public DateTime? unlockedAt;
        public bool isHidden;

        public string Id => _id;
    }

    #endregion

    #region Common Models

    /// <summary>
    /// Pagination info
    /// </summary>
    [Serializable]
    public class Pagination
    {
        public int page;
        public int limit;
        public int total;
        public int totalPages;
    }

    /// <summary>
    /// Location coordinates
    /// </summary>
    [Serializable]
    public class Location
    {
        [JsonProperty("latitude")]
        public float lat;
        
        [JsonProperty("longitude")]
        public float lng;
        
        public float? accuracy;

        // Convenience properties for backend alignment
        [JsonIgnore] public float latitude { get => lat; set => lat = value; }
        [JsonIgnore] public float longitude { get => lng; set => lng = value; }
    }

    #endregion

    #region Marketplace Models

    [Serializable]
    public class MarketplaceFilters
    {
        public string category;
        public int? minPoints;
        public int? maxPoints;
        public string search;
        public bool? featured;
        public int page = 1;
        public int limit = 50;
    }

    [Serializable]
    public class MarketplaceResponse
    {
        public List<MarketplaceItem> items;
        public List<string> categories;
        public int totalItems;
        public MarketplaceFilterOptions filters;
        public MarketplaceUserInfo userInfo;
    }

    [Serializable]
    public class MarketplaceItem
    {
        public string id;
        public string title;
        public string name; 
        public string description;
        public string category;
        public string categoryId;
        public string categoryName;
        public int pointsCost;
        public int price; 
        public List<string> images;
        public string image; 
        public string partnerName;
        public string partnerLogo;
        public bool canAfford;
        public string stockStatus;
        public int? savings;
        public string type; 
        public bool isFeatured;
        public bool isLimited;
        public int stock;
        public string validUntil;
        public bool consumable;
        public bool autoApply;
        public string effectType; 
        public float effectValue;
        public float effectDuration;
        public int purchaseCount;
        public float discount;
    }

    [Serializable]
    public class MarketplaceCategory
    {
        public string id;
        public string name;
        public string icon;
        public int itemCount;
    }

    [Serializable]
    public class MarketplacePurchase
    {
        public string id;
        public string itemId;
        public string itemName;
        public string itemImage;
        public int quantity;
        public int pointsSpent;
        public string purchasedAt;
        public bool isUsed;
        public string usedAt;
    }

    [Serializable]
    public class PurchaseResult
    {
        public bool success;
        public string message;
        public MarketplacePurchase purchase;
        public int remainingPoints;
    }

    [Serializable]
    public class MarketplaceAnalytics
    {
        public int totalPurchases;
        public int totalRevenue;
        public int uniqueBuyers;
        public List<TopItem> topItems;
        public Dictionary<string, int> purchasesByCategory;
    }

    [Serializable]
    public class TopItem
    {
        public string itemId;
        public string itemName;
        public int purchaseCount;
        public int revenue;
    }

    [Serializable]
    public class MarketplaceFilterOptions
    {
        public List<PriceRange> priceRanges;
        public List<CategoryCount> categories;
        public List<PartnerCount> partners;
    }

    [Serializable]
    public class PriceRange
    {
        public int min;
        public int max;
        public string label;
    }

    [Serializable]
    public class CategoryCount
    {
        public string name;
        public int count;
    }

    [Serializable]
    public class PartnerCount
    {
        public string id;
        public string name;
        public int count;
    }

    [Serializable]
    public class MarketplaceUserInfo
    {
        public int currentPoints;
        public int canAfford;
        public int recentPurchases;
    }

    [Serializable]
    public class PurchaseResponse
    {
        public bool success;
        public PurchaseRedemption redemption;
        public UserBalance userBalance;
        public string message;
    }

    [Serializable]
    public class UserBalance
    {
        public int currentPoints;
        public int lifetimePoints;
    }

    [Serializable]
    public class PurchaseRedemption
    {
        public string id;
        public string code;
        public string qrCode;
        public PurchasedItemDetails item;
        public string validUntil;
        public string howToRedeem;
    }

    [Serializable]
    public class PurchasedItemDetails
    {
        public string title;
        public string description;
        public string partnerName;
        public float? originalValue;
        public string currency;
    }

    [Serializable]
    public class MarketplaceRedemptionsResponse
    {
        public List<Redemption> redemptions;
        public int totalCount;
        public Dictionary<string, int> summary;
    }

    [Serializable]
    public class PartnerRedeemResponse
    {
        public bool success;
        public string message;
        public MarketplacePartnerRedemption redemption;
        public PartnerRedeemCodeInfo code;
    }

    [Serializable]
    public class MarketplacePartnerRedemption
    {
        public string id;
        public string status;
        public string fulfilledAt;
        public string rewardId;
        public string rewardName;
        public string partnerName;
    }

    [Serializable]
    public class PartnerRedeemCodeInfo
    {
        public string redemptionCode;
        public bool verificationCodeAccepted;
    }

    #endregion
}
