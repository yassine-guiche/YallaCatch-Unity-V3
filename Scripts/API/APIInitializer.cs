using UnityEngine;

namespace YallaCatch.API
{
    /// <summary>
    /// Initializes all API singletons on game start.
    /// Attach this to a GameObject in the first scene.
    /// </summary>
    public class APIInitializer : MonoBehaviour
    {
        [Header("API Configuration")]
        [SerializeField] private string baseUrl = "http://localhost:3000";

        private void Awake()
        {
            // Create API Client first
            CreateSingleton<APIClient>("APIClient", go =>
            {
                // Configure base URL if needed
            });

            // Create all API class singletons
            CreateSingleton<AuthAPI>("AuthAPI");
            CreateSingleton<GameAPI>("GameAPI");
            CreateSingleton<CaptureAPI>("CaptureAPI");
            CreateSingleton<RewardsAPI>("RewardsAPI");
            CreateSingleton<MarketplaceAPI>("MarketplaceAPI");
            CreateSingleton<SocialAPI>("SocialAPI");
            CreateSingleton<ARAPI>("ARAPI");
            CreateSingleton<AdMobAPI>("AdMobAPI");
            CreateSingleton<NotificationsAPI>("NotificationsAPI");
            CreateSingleton<OfflineAPI>("OfflineAPI");

            Debug.Log("[APIInitializer] All API singletons initialized");
        }

        private void CreateSingleton<T>(string name, System.Action<GameObject> configure = null) where T : MonoBehaviour
        {
            if (FindObjectOfType<T>() == null)
            {
                var go = new GameObject(name);
                go.AddComponent<T>();
                configure?.Invoke(go);
                DontDestroyOnLoad(go);
            }
        }
    }
}
