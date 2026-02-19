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
        public string displayName;
        public string email;
        public string role;
        public string level;
        public UserPoints points;
        public string avatar;
        public UserLocation location;
        public UserStats stats;
        public UserSettings settings;
        public bool isOnline;
        public DateTime lastActive;
        public DateTime createdAt;

        // Convenience properties
        public string Id => _id;
        public int AvailablePoints => points?.available ?? 0;
        public int TotalPoints => points?.total ?? 0;
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
        public string displayType; // standard, mystery_box, treasure, bonus, special
        public string contentType; // points, reward, hybrid
        public string rarity; // common, uncommon, rare, epic, legendary
        public string status; // active, captured, expired, inactive
        public string category;
        public DateTime expiresAt;
        public PrizeContent content;

        // Convenience
        public string Id => _id;
        public float Latitude => location?.lat ?? 0;
        public float Longitude => location?.lng ?? 0;
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

        public string Id => _id;
    }

    [Serializable]
    public class ClaimLocation
    {
        public float latitude;
        public float longitude;
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
        public int stock;
        public string partnerId;
        public string partnerName;
        public string partnerLogo;
        public RewardTerms terms;
        public bool isFeatured;
        public DateTime validUntil;

        public string Id => _id;
        public string PrimaryImage => images?.Count > 0 ? images[0] : null;
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
        public float distance;
        public string activity;
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
        public float latitude;
        public float longitude;
        public float accuracy;
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
        public float latitude;
        public float longitude;
        public float? accuracy;
    }

    #endregion
}
