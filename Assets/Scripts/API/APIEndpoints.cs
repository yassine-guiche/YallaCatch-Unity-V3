namespace YallaCatch.API
{
    /// <summary>
    /// All API endpoint constants matching backend routes.
    /// Base URL: /api/v1
    /// </summary>
    public static class APIEndpoints
    {
        private const string API_VERSION = "/api/v1";

        #region Auth Endpoints

        public const string AUTH_LOGIN = API_VERSION + "/auth/login";
        public const string AUTH_REGISTER = API_VERSION + "/auth/register";
        public const string AUTH_GUEST = API_VERSION + "/auth/guest";
        public const string AUTH_REFRESH = API_VERSION + "/auth/refresh";
        public const string AUTH_LOGOUT = API_VERSION + "/auth/logout";
        public const string AUTH_ME = API_VERSION + "/auth/me"; // Deprecated — use USER_PROFILE

        // Canonical user endpoints (replaces deprecated /auth/me)
        public const string USER_PROFILE = API_VERSION + "/users/profile";
        public const string USER_STATS = API_VERSION + "/users/stats";
        public const string USER_LEADERBOARD = API_VERSION + "/users/leaderboard";

        #endregion

        #region Game Endpoints

        public const string GAME_SESSION_START = API_VERSION + "/game/session/start";
        public const string GAME_SESSION_END = API_VERSION + "/game/session/end";
        public const string GAME_LOCATION = API_VERSION + "/game/location";
        public const string GAME_LEADERBOARD = API_VERSION + "/game/leaderboard";
        public const string GAME_MAP = API_VERSION + "/game/map";
        public const string GAME_POWERUP_USE = API_VERSION + "/game/powerup/use";
        public const string GAME_CHALLENGES = API_VERSION + "/game/challenges";
        public const string GAME_CHALLENGE_COMPLETE = API_VERSION + "/game/challenges/{0}/complete"; // {0} = challengeId
        public const string GAME_INVENTORY = API_VERSION + "/game/inventory";
        public const string GAME_CONFIG = API_VERSION + "/game/config";

        #endregion

        #region Capture Endpoints

        public const string CAPTURE_NEARBY = API_VERSION + "/capture/nearby";
        public const string CAPTURE_ATTEMPT = API_VERSION + "/capture/attempt";
        public const string CAPTURE_VALIDATE = API_VERSION + "/capture/validate";
        public const string CAPTURE_ANIMATION = API_VERSION + "/capture/animation/{0}"; // {0} = prizeId
        public const string CAPTURE_CONFIRM = API_VERSION + "/capture/confirm";

        #endregion

        #region Rewards Endpoints

        public const string REWARDS_LIST = API_VERSION + "/rewards";
        public const string REWARDS_SEARCH = API_VERSION + "/rewards/search";
        public const string REWARDS_DETAILS = API_VERSION + "/rewards/{0}"; // {0} = rewardId
        public const string REWARDS_REDEEM = API_VERSION + "/rewards/redeem";
        public const string REWARDS_MY_REDEMPTIONS = API_VERSION + "/rewards/history"; // Backend uses /history
        public const string REWARDS_CATEGORIES = API_VERSION + "/rewards/categories";
        public const string REWARDS_FEATURED = API_VERSION + "/rewards/featured";
        public const string REWARDS_FAVORITES = API_VERSION + "/rewards/favorites";
        public const string REWARDS_FAVORITES_ADD = API_VERSION + "/rewards/favorites/{0}"; // {0} = rewardId (POST)
        public const string REWARDS_FAVORITES_REMOVE = API_VERSION + "/rewards/favorites/{0}"; // {0} = rewardId (DELETE)
        public const string REWARDS_HISTORY = API_VERSION + "/rewards/history";
        public const string REWARDS_QR_SCAN = API_VERSION + "/rewards/scan"; // Backend uses /scan
        public const string REWARDS_PROMO = API_VERSION + "/rewards/promo";
        public const string REWARDS_HEATMAP = API_VERSION + "/prizes/heatmap"; // Centralized heatmap endpoint

        #endregion

        #region Marketplace Endpoints

        public const string MARKETPLACE_LIST = API_VERSION + "/marketplace/items";
        public const string MARKETPLACE_PURCHASE = API_VERSION + "/marketplace/purchase";
        public const string MARKETPLACE_REDEMPTIONS = API_VERSION + "/marketplace/redemptions";
        public const string MARKETPLACE_REDEEM = API_VERSION + "/marketplace/redeem";

        #endregion

        #region Social Endpoints

        public const string SOCIAL_FRIEND_SEND = API_VERSION + "/social/friends/request";
        public const string SOCIAL_FRIEND_RESPOND = API_VERSION + "/social/friends/respond";
        public const string SOCIAL_FRIEND_REMOVE = API_VERSION + "/social/friends/{0}"; // {0} = friendId
        public const string SOCIAL_FRIENDS_LIST = API_VERSION + "/social/friends";
        public const string SOCIAL_FRIENDS_PENDING = API_VERSION + "/social/friends/requests/pending";
        public const string SOCIAL_NEARBY = API_VERSION + "/social/nearby";
        public const string SOCIAL_PROFILE = API_VERSION + "/social/profile/{0}"; // {0} = userId
        public const string SOCIAL_TEAMS = API_VERSION + "/social/teams";
        public const string SOCIAL_LEADERBOARD = API_VERSION + "/social/leaderboard";
        public const string SOCIAL_SHARE = API_VERSION + "/social/share";

        #endregion

        #region AR Endpoints

        public const string AR_VIEW_START = API_VERSION + "/ar/view/start";
        public const string AR_CAPTURE = API_VERSION + "/ar/capture";
        public const string AR_SESSION_END = API_VERSION + "/ar/session/end";
        public const string AR_MODEL = API_VERSION + "/ar/model/{0}"; // {0} = prizeId

        #endregion

        #region AdMob Endpoints

        public const string ADMOB_CONFIG = API_VERSION + "/admob/available"; // Backend uses /available
        public const string ADMOB_REWARD = API_VERSION + "/admob/reward";
        public const string ADMOB_STATS = API_VERSION + "/admob/stats";

        #endregion

        #region Notifications Endpoints

        public const string NOTIFICATIONS_LIST = API_VERSION + "/notifications";
        public const string NOTIFICATIONS_READ = API_VERSION + "/notifications/read";
        public const string NOTIFICATIONS_SETTINGS = API_VERSION + "/notifications/settings";
        public const string NOTIFICATIONS_PUSH_SUBSCRIBE = API_VERSION + "/notifications/push/subscribe";
        public const string NOTIFICATIONS_PUSH_UNSUBSCRIBE = API_VERSION + "/notifications/push/unsubscribe";
        public const string NOTIFICATIONS_STATS = API_VERSION + "/notifications/stats";

        #endregion

        #region Offline Endpoints

        public const string OFFLINE_SYNC = API_VERSION + "/offline/sync";
        public const string OFFLINE_STATUS = API_VERSION + "/offline/status";
        public const string OFFLINE_PACKAGE = API_VERSION + "/offline/package";
        public const string OFFLINE_CAPABILITIES = API_VERSION + "/offline/capabilities";
        public const string OFFLINE_RETRY = API_VERSION + "/offline/retry";

        #endregion

        #region Utility

        /// <summary>
        /// Format endpoint with parameters
        /// Example: Format(REWARDS_DETAILS, rewardId)
        /// </summary>
        public static string Format(string endpoint, params object[] args)
        {
            return string.Format(endpoint, args);
        }

        #endregion
    }
}
