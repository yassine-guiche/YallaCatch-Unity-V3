using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace YallaCatch
{
    /// <summary>
    /// Gère la boutique in-game (marketplace)
    /// Intégration avec le module marketplace backend (8 endpoints)
    /// Permet d'acheter des items avec des points (power-ups, boosts, cosmétiques)
    /// Contrôlé par l'admin panel
    /// </summary>
    public class MarketplaceManager : MonoBehaviour
    {
        public static MarketplaceManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float cacheExpiration = 300f; // 5 minutes
        
        // État
        private List<MarketplaceItem> allItems = new List<MarketplaceItem>();
        private List<MarketplaceItem> featuredItems = new List<MarketplaceItem>();
        private List<MarketplaceCategory> categories = new List<MarketplaceCategory>();
        private List<MarketplacePurchase> purchaseHistory = new List<MarketplacePurchase>();
        private Dictionary<string, int> inventory = new Dictionary<string, int>(); // itemId -> quantity
        
        private float lastCacheUpdate = 0f;
        private bool isInitialized = false;

        // Events
        public delegate void OnItemPurchased(MarketplaceItem item, int quantity);
        public event OnItemPurchased OnItemPurchasedEvent;
        
        public delegate void OnInventoryUpdated();
        public event OnInventoryUpdated OnInventoryUpdatedEvent;

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
            StartCoroutine(Initialize());
        }

        /// <summary>
        /// Initialise le marketplace
        /// </summary>
        private IEnumerator Initialize()
        {
            // Charger les catégories
            yield return StartCoroutine(LoadCategories());
            
            // Charger tous les items
            yield return StartCoroutine(LoadAllItems());
            
            // Charger les items en vedette
            yield return StartCoroutine(LoadFeaturedItems());
            
            // Charger l'historique d'achats
            yield return StartCoroutine(LoadPurchaseHistory());
            
            isInitialized = true;
            Debug.Log("[MarketplaceManager] Initialized successfully");
        }

        #region API Calls

        /// <summary>
        /// GET /marketplace/ - Liste tous les items du marketplace
        /// </summary>
        public IEnumerator LoadAllItems(System.Action<bool> callback = null)
        {
            string endpoint = "/marketplace/";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    allItems = JsonConvert.DeserializeObject<List<MarketplaceItem>>(response.data.ToString());
                    lastCacheUpdate = Time.time;
                    
                    // Mettre à jour l'inventaire
                    UpdateInventoryFromItems();
                    
                    callback?.Invoke(true);
                    Debug.Log($"[MarketplaceManager] Loaded {allItems.Count} items");
                }
                else
                {
                    callback?.Invoke(false);
                }
            });
        }

        /// <summary>
        /// POST /marketplace/purchase - Acheter un item
        /// </summary>
        public IEnumerator PurchaseItem(string itemId, int quantity = 1, System.Action<PurchaseResult> callback = null)
        {
            var data = new
            {
                itemId = itemId,
                quantity = quantity,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            string endpoint = "/marketplace/purchase";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    var result = JsonConvert.DeserializeObject<PurchaseResult>(response.data.ToString());
                    
                    // Mettre à jour les points du joueur
                    GameManager.Instance.SetPoints(result.remainingPoints);
                    
                    // Mettre à jour l'inventaire
                    if (inventory.ContainsKey(itemId))
                    {
                        inventory[itemId] += quantity;
                    }
                    else
                    {
                        inventory[itemId] = quantity;
                    }
                    
                    // Ajouter à l'historique
                    purchaseHistory.Add(result.purchase);
                    
                    // Trouver l'item
                    MarketplaceItem item = allItems.Find(i => i.id == itemId);
                    
                    // Events
                    OnItemPurchasedEvent?.Invoke(item, quantity);
                    OnInventoryUpdatedEvent?.Invoke();
                    
                    callback?.Invoke(result);
                    
                    // Appliquer l'item si c'est un power-up consommable
                    if (item != null && item.type == "powerup" && item.autoApply)
                    {
                        ApplyPowerUp(item);
                    }
                    
                    Debug.Log($"[MarketplaceManager] Purchased: {item?.name} x{quantity}");
                }
                else
                {
                    callback?.Invoke(null);
                    UIManager.Instance.ShowMessage(response.message ?? "Purchase failed");
                }
            });
        }

        /// <summary>
        /// GET /marketplace/redemptions - Liste des achats (redemptions)
        /// </summary>
        public IEnumerator LoadRedemptions(System.Action<List<MarketplacePurchase>> callback = null)
        {
            string endpoint = "/marketplace/redemptions";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    var redemptions = JsonConvert.DeserializeObject<List<MarketplacePurchase>>(response.data.ToString());
                    callback?.Invoke(redemptions);
                }
                else
                {
                    callback?.Invoke(new List<MarketplacePurchase>());
                }
            });
        }

        /// <summary>
        /// POST /marketplace/redeem - Utiliser un item acheté
        /// </summary>
        public IEnumerator RedeemItem(string purchaseId, System.Action<bool> callback = null)
        {
            var data = new
            {
                purchaseId = purchaseId,
                timestamp = System.DateTime.UtcNow.ToString("o")
            };
            
            string endpoint = "/marketplace/redeem";
            
            yield return APIManager.Instance.Post(endpoint, data, (response) =>
            {
                if (response.success)
                {
                    callback?.Invoke(true);
                    UIManager.Instance.ShowMessage("Item redeemed successfully!");
                }
                else
                {
                    callback?.Invoke(false);
                    UIManager.Instance.ShowMessage("Failed to redeem item");
                }
            });
        }

        /// <summary>
        /// GET /marketplace/analytics - Analytics du marketplace (admin)
        /// </summary>
        public IEnumerator GetAnalytics(System.Action<MarketplaceAnalytics> callback = null)
        {
            string endpoint = "/marketplace/analytics";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    var analytics = JsonConvert.DeserializeObject<MarketplaceAnalytics>(response.data.ToString());
                    callback?.Invoke(analytics);
                }
                else
                {
                    callback?.Invoke(null);
                }
            });
        }

        /// <summary>
        /// GET /marketplace/categories - Liste des catégories
        /// </summary>
        public IEnumerator LoadCategories(System.Action<List<MarketplaceCategory>> callback = null)
        {
            string endpoint = "/marketplace/categories";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    categories = JsonConvert.DeserializeObject<List<MarketplaceCategory>>(response.data.ToString());
                    callback?.Invoke(categories);
                    
                    Debug.Log($"[MarketplaceManager] Loaded {categories.Count} categories");
                }
                else
                {
                    callback?.Invoke(new List<MarketplaceCategory>());
                }
            });
        }

        /// <summary>
        /// GET /marketplace/featured - Items en vedette
        /// </summary>
        public IEnumerator LoadFeaturedItems(System.Action<List<MarketplaceItem>> callback = null)
        {
            string endpoint = "/marketplace/featured";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    featuredItems = JsonConvert.DeserializeObject<List<MarketplaceItem>>(response.data.ToString());
                    callback?.Invoke(featuredItems);
                    
                    Debug.Log($"[MarketplaceManager] Loaded {featuredItems.Count} featured items");
                }
                else
                {
                    callback?.Invoke(new List<MarketplaceItem>());
                }
            });
        }

        /// <summary>
        /// GET /marketplace/history - Historique des achats
        /// </summary>
        public IEnumerator LoadPurchaseHistory(System.Action<List<MarketplacePurchase>> callback = null)
        {
            string endpoint = "/marketplace/history";
            
            yield return APIManager.Instance.Get(endpoint, (response) =>
            {
                if (response.success)
                {
                    purchaseHistory = JsonConvert.DeserializeObject<List<MarketplacePurchase>>(response.data.ToString());
                    callback?.Invoke(purchaseHistory);
                    
                    Debug.Log($"[MarketplaceManager] Loaded {purchaseHistory.Count} purchases");
                }
                else
                {
                    callback?.Invoke(new List<MarketplacePurchase>());
                }
            });
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Obtient tous les items
        /// </summary>
        public List<MarketplaceItem> GetAllItems()
        {
            return allItems;
        }

        /// <summary>
        /// Obtient les items par catégorie
        /// </summary>
        public List<MarketplaceItem> GetItemsByCategory(string categoryId)
        {
            return allItems.Where(i => i.categoryId == categoryId).ToList();
        }

        /// <summary>
        /// Obtient les items par type
        /// </summary>
        public List<MarketplaceItem> GetItemsByType(string type)
        {
            return allItems.Where(i => i.type == type).ToList();
        }

        /// <summary>
        /// Obtient les items en vedette
        /// </summary>
        public List<MarketplaceItem> GetFeaturedItems()
        {
            return featuredItems;
        }

        /// <summary>
        /// Obtient les catégories
        /// </summary>
        public List<MarketplaceCategory> GetCategories()
        {
            return categories;
        }

        /// <summary>
        /// Vérifie si le joueur peut acheter un item
        /// </summary>
        public bool CanAffordItem(MarketplaceItem item)
        {
            return GameManager.Instance.GetPoints() >= item.price;
        }

        /// <summary>
        /// Obtient la quantité d'un item dans l'inventaire
        /// </summary>
        public int GetItemQuantity(string itemId)
        {
            if (inventory.ContainsKey(itemId))
            {
                return inventory[itemId];
            }
            return 0;
        }

        /// <summary>
        /// Vérifie si le joueur possède un item
        /// </summary>
        public bool HasItem(string itemId)
        {
            return inventory.ContainsKey(itemId) && inventory[itemId] > 0;
        }

        /// <summary>
        /// Obtient l'inventaire complet
        /// </summary>
        public Dictionary<string, int> GetInventory()
        {
            return inventory;
        }

        /// <summary>
        /// Obtient l'historique des achats
        /// </summary>
        public List<MarketplacePurchase> GetPurchaseHistory()
        {
            return purchaseHistory;
        }

        /// <summary>
        /// Utilise un item de l'inventaire
        /// </summary>
        public bool UseItem(string itemId)
        {
            if (!HasItem(itemId))
            {
                return false;
            }
            
            // Trouver l'item
            MarketplaceItem item = allItems.Find(i => i.id == itemId);
            if (item == null)
            {
                return false;
            }
            
            // Appliquer l'effet
            bool success = ApplyPowerUp(item);
            
            if (success && item.consumable)
            {
                // Réduire la quantité
                inventory[itemId]--;
                
                if (inventory[itemId] <= 0)
                {
                    inventory.Remove(itemId);
                }
                
                OnInventoryUpdatedEvent?.Invoke();
            }
            
            return success;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Met à jour l'inventaire depuis les items
        /// </summary>
        private void UpdateInventoryFromItems()
        {
            // L'inventaire est géré côté backend, on le récupère depuis l'historique
            foreach (var purchase in purchaseHistory)
            {
                if (!purchase.isUsed)
                {
                    if (inventory.ContainsKey(purchase.itemId))
                    {
                        inventory[purchase.itemId] += purchase.quantity;
                    }
                    else
                    {
                        inventory[purchase.itemId] = purchase.quantity;
                    }
                }
            }
        }

        /// <summary>
        /// Applique l'effet d'un power-up
        /// </summary>
        private bool ApplyPowerUp(MarketplaceItem item)
        {
            if (PowerUpManager.Instance == null)
            {
                Debug.LogWarning("[MarketplaceManager] PowerUpManager not found!");
                return false;
            }
            
            switch (item.effectType)
            {
                case "radar":
                    PowerUpManager.Instance.ActivateRadar(item.effectDuration);
                    return true;
                    
                case "multiplier":
                    PowerUpManager.Instance.ActivatePointsMultiplier(item.effectValue, item.effectDuration);
                    return true;
                    
                case "magnet":
                    PowerUpManager.Instance.ActivateMagnet(item.effectValue, item.effectDuration);
                    return true;
                    
                case "speed":
                    PowerUpManager.Instance.ActivateSpeedBoost(item.effectValue, item.effectDuration);
                    return true;
                    
                case "xp_boost":
                    PowerUpManager.Instance.ActivateXPBoost(item.effectValue, item.effectDuration);
                    return true;
                    
                default:
                    Debug.LogWarning($"[MarketplaceManager] Unknown effect type: {item.effectType}");
                    return false;
            }
        }

        #endregion
    }

    #region Data Classes

    [System.Serializable]
    public class MarketplaceItem
    {
        public string id;
        public string name;
        public string description;
        public string image;
        public int price; // Prix en points
        public string categoryId;
        public string categoryName;
        public string type; // powerup, cosmetic, boost, bundle
        public bool isFeatured;
        public bool isLimited;
        public int stock;
        public string validUntil;
        public bool consumable;
        public bool autoApply;
        public string effectType; // radar, multiplier, magnet, speed, xp_boost
        public float effectValue;
        public float effectDuration; // en secondes
        public int purchaseCount;
        public float discount; // 0-1 (0.2 = 20% de réduction)
    }

    [System.Serializable]
    public class MarketplaceCategory
    {
        public string id;
        public string name;
        public string icon;
        public int itemCount;
    }

    [System.Serializable]
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

    [System.Serializable]
    public class PurchaseResult
    {
        public bool success;
        public string message;
        public MarketplacePurchase purchase;
        public int remainingPoints;
    }

    [System.Serializable]
    public class MarketplaceAnalytics
    {
        public int totalPurchases;
        public int totalRevenue; // en points
        public int uniqueBuyers;
        public List<TopItem> topItems;
        public Dictionary<string, int> purchasesByCategory;
    }

    [System.Serializable]
    public class TopItem
    {
        public string itemId;
        public string itemName;
        public int purchaseCount;
        public int revenue;
    }

    #endregion
}
