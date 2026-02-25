using UnityEngine;
using YallaCatch.Managers;
using YallaCatch.Networking;

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
                APIClient.Instance.SetBaseUrl(baseUrl);
            });

            // Create all API class singletons
            CreateSingleton<AuthAPI>("AuthAPI");
            CreateSingleton<GameAPI>("GameAPI");
            CreateSingleton<CaptureAPI>("CaptureAPI");
            CreateSingleton<RewardsAPI>("RewardsAPI");
            CreateSingleton<MarketplaceAPI>("MarketplaceAPI");
            CreateSingleton<SocialAPI>("SocialAPI");
            CreateSingleton<GamificationAPI>("GamificationAPI");
            CreateSingleton<ARAPI>("ARAPI");
            CreateSingleton<AdMobAPI>("AdMobAPI");
            CreateSingleton<NotificationsAPI>("NotificationsAPI");
            CreateSingleton<OfflineAPI>("OfflineAPI");
            
            // Realtime networking (Socket.IO is the canonical path)
            CreateSingleton<SocketIOClient>("SocketIOClient");

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

