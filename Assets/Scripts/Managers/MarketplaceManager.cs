using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using YallaCatch.Models;
using YallaCatch.UI;
using YallaCatch.API;
using YallaCatch.Managers;

namespace YallaCatch
{
    /// <summary>
    /// GÃ¨re la boutique in-game (marketplace)
    /// IntÃ©gration avec le module marketplace backend (8 endpoints)
    /// Permet d'acheter des items avec des points (power-ups, boosts, cosmÃ©tiques)
    /// ContrÃ´lÃ© par l'admin panel
    /// </summary>
    public class MarketplaceManager : MonoBehaviour
    {
        public static MarketplaceManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private float cacheExpiration = 300f; // 5 minutes
        
        // Ã‰tat
        private List<MarketplaceItem> allItems = new List<MarketplaceItem>();
        private List<MarketplaceItem> featuredItems = new List<MarketplaceItem>();
        private List<MarketplaceCategory> categories = new List<MarketplaceCategory>();
        private List<MarketplacePurchase> purchaseHistory = new List<MarketplacePurchase>();
        private Dictionary<string, int> inventory = new Dictionary<string, int>(); // itemId -> quantity
        
        private float lastCacheUpdate = 0f;
        public bool IsInitialized => isInitialized;
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
            if (APIClient.Instance != null && !APIClient.Instance.IsAuthenticated)
            {
                Debug.Log("[MarketplaceManager] Skipping auto-initialize until player is authenticated.");
                return;
            }

            StartCoroutine(Initialize());
        }

        /// <summary>
        /// Initialise le marketplace
        /// </summary>
        private IEnumerator Initialize()
        {
            // Charger les catÃ©gories
            // Charger tous les items
            yield return StartCoroutine(LoadAllItems());
            if (categories == null || categories.Count == 0)
                RebuildCategoriesFromItems();
            
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
            if (allItems.Count > 0 && (Time.time - lastCacheUpdate) < cacheExpiration)
            {
                callback?.Invoke(true);
                yield break;
            }

            if (MarketplaceAPI.Instance == null)
            {
                callback?.Invoke(false);
                yield break;
            }

            bool done = false;
            bool success = false;

            MarketplaceAPI.Instance.GetMarketplace(new MarketplaceFilters { page = 1, limit = 100 }, (response) =>
            {
                if (response.success && response.data != null)
                {
                    allItems = response.data.items ?? new List<MarketplaceItem>();
                    categories = (response.data.categories ?? new List<string>())
                        .Select(c => new MarketplaceCategory
                        {
                            id = c,
                            name = c,
                            itemCount = allItems.Count(i => i.category == c || i.categoryId == c)
                        })
                        .ToList();
                    lastCacheUpdate = Time.time;
                    UpdateInventoryFromItems();
                    success = true;
                    Debug.Log($"[MarketplaceManager] Loaded {allItems.Count} items");
                }

                done = true;
            });

            while (!done) yield return null;
            callback?.Invoke(success);
        }

        /// <summary>
        /// Acheter un item via le MarketplaceAPI (Module modulaire)
        /// </summary>
        public IEnumerator PurchaseItem(string itemId, int quantity = 1, System.Action<PurchaseResult> callback = null)
        {
            // Note: Le backend supporte actuellement l'achat d'un seul item par requÃªte.
            // On utilise le MarketplaceAPI spÃ©cialisÃ© pour garantir l'envoi du bon payload (location, deviceInfo).
            
            bool isDone = false;
            PurchaseResult purchaseResult = null;

            MarketplaceAPI.Instance.PurchaseItem(itemId, null, (response) => {
                if (response.success && response.data != null)
                {
                    var result = response.data;
                    
                    // Mettre Ã  jour les points du joueur
                    if (GameManager.Instance != null)
                        GameManager.Instance.SetPoints(result.userBalance?.currentPoints ?? 0);
                    
                    // Mettre Ã  jour l'inventaire
                    if (inventory.ContainsKey(itemId))
                        inventory[itemId] += quantity;
                    else
                        inventory[itemId] = quantity;
                    
                    // Trouver l'item pour les events / pricing helpers
                    MarketplaceItem item = allItems.Find(i => i.id == itemId);

                    // Mapper PurchaseResponse.PurchaseRedemption vers MarketplacePurchase pour l'historique legacy
                    var legacyHistoryEntry = new MarketplacePurchase
                    {
                        id = result.redemption?.id,
                        itemId = itemId,
                        itemName = result.redemption?.item?.title,
                        quantity = quantity,
                        pointsSpent = item != null ? GetItemCost(item) : 0,
                        purchasedAt = System.DateTime.UtcNow.ToString("o")
                    };
                    purchaseHistory.Add(legacyHistoryEntry);
                    
                    // Events
                    OnItemPurchasedEvent?.Invoke(item, quantity);
                    OnInventoryUpdatedEvent?.Invoke();
                    
                    // Conversion result pour le callback legacy
                    purchaseResult = new PurchaseResult
                    {
                        success = true,
                        remainingPoints = result.userBalance?.currentPoints ?? 0,
                        purchase = legacyHistoryEntry
                    };
                    
                    callback?.Invoke(purchaseResult);
                    
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
                    if (UIManager.Instance != null)
                        UIManager.Instance.ShowMessage(response.message ?? response.error ?? "Purchase failed");
                }
                isDone = true;
            });

            // Attendre la fin de l'appel asynchrone (Transition Graduelle vers async/await/callbacks)
            while (!isDone) yield return null;
        }

        /// <summary>
        /// GET /marketplace/redemptions - Liste des achats (redemptions)
        /// </summary>
        public IEnumerator LoadRedemptions(System.Action<List<MarketplacePurchase>> callback = null)
        {
            if (MarketplaceAPI.Instance == null)
            {
                callback?.Invoke(new List<MarketplacePurchase>());
                yield break;
            }

            bool done = false;
            List<MarketplacePurchase> mapped = new List<MarketplacePurchase>();

            MarketplaceAPI.Instance.GetRedemptions(null, 1, 50, (response) =>
            {
                if (response.success && response.data != null)
                {
                    mapped = MapRedemptions(response.data.redemptions);
                }
                done = true;
            });

            while (!done) yield return null;
            callback?.Invoke(mapped);
        }

        /// <summary>
        /// GET /marketplace/categories - Liste des catÃ©gories
        /// </summary>
        public IEnumerator LoadCategories(System.Action<List<MarketplaceCategory>> callback = null)
        {
            RebuildCategoriesFromItems();
            callback?.Invoke(categories ?? new List<MarketplaceCategory>());
            yield break;
        }

        /// <summary>
        /// GET /marketplace/featured - Items en vedette
        /// </summary>
        public IEnumerator LoadFeaturedItems(System.Action<List<MarketplaceItem>> callback = null)
        {
            if (MarketplaceAPI.Instance == null)
            {
                callback?.Invoke(new List<MarketplaceItem>());
                yield break;
            }

            bool done = false;
            List<MarketplaceItem> items = new List<MarketplaceItem>();

            MarketplaceAPI.Instance.GetMarketplace(new MarketplaceFilters { featured = true, page = 1, limit = 20 }, (response) =>
            {
                if (response.success && response.data != null)
                {
                    items = response.data.items ?? new List<MarketplaceItem>();
                    featuredItems = items;
                    Debug.Log($"[MarketplaceManager] Loaded {featuredItems.Count} featured items");
                }
                done = true;
            });

            while (!done) yield return null;
            callback?.Invoke(items);
        }

        /// <summary>
        /// GET /marketplace/history - Historique des achats
        /// </summary>
        public IEnumerator LoadPurchaseHistory(System.Action<List<MarketplacePurchase>> callback = null)
        {
            if (MarketplaceAPI.Instance == null)
            {
                callback?.Invoke(new List<MarketplacePurchase>());
                yield break;
            }

            bool done = false;

            MarketplaceAPI.Instance.GetRedemptions(null, 1, 100, (response) =>
            {
                if (response.success && response.data != null)
                {
                    purchaseHistory = MapRedemptions(response.data.redemptions);
                    UpdateInventoryFromItems();
                    Debug.Log($"[MarketplaceManager] Loaded {purchaseHistory.Count} purchases");
                }
                else
                {
                    purchaseHistory = new List<MarketplacePurchase>();
                }
                done = true;
            });

            while (!done) yield return null;
            callback?.Invoke(purchaseHistory ?? new List<MarketplacePurchase>());
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
        /// Obtient les items par catÃ©gorie
        /// </summary>
        public List<MarketplaceItem> GetItemsByCategory(string categoryId)
        {
            return allItems.Where(i => i.categoryId == categoryId || i.category == categoryId).ToList();
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
        /// Obtient les catÃ©gories
        /// </summary>
        public List<MarketplaceCategory> GetCategories()
        {
            return categories;
        }

        /// <summary>
        /// VÃ©rifie si le joueur peut acheter un item
        /// </summary>
        public bool CanAffordItem(MarketplaceItem item)
        {
            if (GameManager.Instance == null || item == null) return false;
            return GameManager.Instance.PlayerPoints >= GetItemCost(item);
        }

        /// <summary>
        /// Obtient la quantitÃ© d'un item dans l'inventaire
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
        /// VÃ©rifie si le joueur possÃ¨de un item
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
                // RÃ©duire la quantitÃ©
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
        /// Met Ã  jour l'inventaire depuis les items
        /// </summary>
        private void UpdateInventoryFromItems()
        {
            inventory.Clear(); // Clear before rebuild
            
            // L'inventaire est gÃ©rÃ© cÃ´tÃ© backend, on le rÃ©cupÃ¨re depuis l'historique
            if (purchaseHistory == null) return;
            
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

        private int GetItemCost(MarketplaceItem item)
        {
            if (item == null) return 0;
            return item.pointsCost > 0 ? item.pointsCost : item.price;
        }

        private void RebuildCategoriesFromItems()
        {
            categories = (allItems ?? new List<MarketplaceItem>())
                .Where(i => !string.IsNullOrEmpty(i.category))
                .GroupBy(i => i.category)
                .Select(g => new MarketplaceCategory
                {
                    id = g.Key,
                    name = g.Key,
                    itemCount = g.Count()
                })
                .OrderBy(c => c.name)
                .ToList();
        }

        private List<MarketplacePurchase> MapRedemptions(List<Redemption> redemptions)
        {
            if (redemptions == null) return new List<MarketplacePurchase>();

            return redemptions.Select(r => new MarketplacePurchase
            {
                id = r._id,
                itemId = r.rewardId,
                itemName = r.reward?.name,
                quantity = 1,
                pointsSpent = r.pointsSpent,
                purchasedAt = (r.redeemedAt == default ? System.DateTime.UtcNow : r.redeemedAt).ToString("o"),
                isUsed = r.status == "fulfilled",
                usedAt = r.status == "fulfilled" ? r.redeemedAt.ToString("o") : null
            }).ToList();
        }

        #endregion
    }

    // Data Classes moved to Models.cs definition
}
