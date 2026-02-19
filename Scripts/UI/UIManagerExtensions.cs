using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace YallaCatch.UI
{
    /// <summary>
    /// Extensions du UIManager pour les nouveaux managers
    /// Contient toutes les méthodes UI pour Rewards, Marketplace, Social, PowerUps, Challenges, Claims
    /// </summary>
    public partial class UIManager
    {
        [Header("Rewards UI")]
        [SerializeField] private GameObject rewardsPanel;
        [SerializeField] private GameObject rewardItemPrefab;
        [SerializeField] private Transform rewardsContainer;
        [SerializeField] private TMP_Dropdown categoryDropdown;
        [SerializeField] private Button favoritesButton;
        
        [Header("Marketplace UI")]
        [SerializeField] private GameObject marketplacePanel;
        [SerializeField] private GameObject marketplaceItemPrefab;
        [SerializeField] private Transform marketplaceContainer;
        [SerializeField] private Button inventoryButton;
        
        [Header("Social UI")]
        [SerializeField] private GameObject socialPanel;
        [SerializeField] private GameObject friendItemPrefab;
        [SerializeField] private Transform friendsContainer;
        [SerializeField] private GameObject friendRequestPrefab;
        [SerializeField] private Transform requestsContainer;
        [SerializeField] private Button leaderboardButton;
        
        [Header("PowerUps UI")]
        [SerializeField] private GameObject powerUpIndicator;
        [SerializeField] private TMP_Text powerUpTimerText;
        [SerializeField] private Image powerUpIcon;
        
        [Header("Challenges UI")]
        [SerializeField] private GameObject challengesPanel;
        [SerializeField] private GameObject challengeItemPrefab;
        [SerializeField] private Transform challengesContainer;
        [SerializeField] private TMP_Text challengesProgressText;
        
        [Header("Claims UI")]
        [SerializeField] private GameObject claimsPanel;
        [SerializeField] private GameObject claimItemPrefab;
        [SerializeField] private Transform claimsContainer;
        [SerializeField] private GameObject qrCodePanel;
        [SerializeField] private RawImage qrCodeImage;
        
        [Header("Report UI")]
        [SerializeField] private GameObject reportPanel;
        [SerializeField] private TMP_Dropdown reportTypeDropdown;
        [SerializeField] private TMP_InputField reportDescriptionInput;
        [SerializeField] private Button submitReportButton;

        #region Rewards UI

        /// <summary>
        /// Affiche le panneau des récompenses
        /// </summary>
        public void ShowRewardsPanel()
        {
            if (rewardsPanel != null)
            {
                rewardsPanel.SetActive(true);
                RefreshRewardsList();
            }
        }

        /// <summary>
        /// Rafraîchit la liste des récompenses
        /// </summary>
        public void RefreshRewardsList(string categoryFilter = null)
        {
            if (rewardsContainer == null || rewardItemPrefab == null)
                return;
            
            // Clear existing items
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get rewards
            var rewards = string.IsNullOrEmpty(categoryFilter)
                ? RewardsManager.Instance.GetAllRewards()
                : RewardsManager.Instance.GetRewardsByCategory(categoryFilter);
            
            // Create UI items
            foreach (var reward in rewards)
            {
                GameObject item = Instantiate(rewardItemPrefab, rewardsContainer);
                SetupRewardItem(item, reward);
            }
        }

        /// <summary>
        /// Configure un item de récompense
        /// </summary>
        private void SetupRewardItem(GameObject item, Reward reward)
        {
            // Set name
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = reward.name;
            
            // Set points cost
            TMP_Text pointsText = item.transform.Find("PointsText")?.GetComponent<TMP_Text>();
            if (pointsText != null)
                pointsText.text = $"{reward.pointsCost} pts";
            
            // Set image
            Image rewardImage = item.transform.Find("RewardImage")?.GetComponent<Image>();
            if (rewardImage != null && !string.IsNullOrEmpty(reward.image))
            {
                StartCoroutine(LoadImageFromURL(reward.image, rewardImage));
            }
            
            // Redeem button
            Button redeemButton = item.transform.Find("RedeemButton")?.GetComponent<Button>();
            if (redeemButton != null)
            {
                bool canAfford = RewardsManager.Instance.CanAffordReward(reward);
                redeemButton.interactable = canAfford && reward.stock > 0;
                redeemButton.onClick.AddListener(() => OnRedeemRewardClicked(reward));
            }
            
            // Favorite button
            Button favoriteButton = item.transform.Find("FavoriteButton")?.GetComponent<Button>();
            if (favoriteButton != null)
            {
                bool isFavorite = RewardsManager.Instance.IsFavorite(reward.id);
                favoriteButton.GetComponentInChildren<TMP_Text>().text = isFavorite ? "★" : "☆";
                favoriteButton.onClick.AddListener(() => ToggleFavorite(reward.id));
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton d'échange de récompense
        /// </summary>
        private void OnRedeemRewardClicked(Reward reward)
        {
            ShowConfirmDialog(
                $"Redeem {reward.name}?",
                $"This will cost {reward.pointsCost} points.",
                () => StartCoroutine(RewardsManager.Instance.RedeemReward(reward.id))
            );
        }

        /// <summary>
        /// Toggle favorite
        /// </summary>
        private void ToggleFavorite(string rewardId)
        {
            if (RewardsManager.Instance.IsFavorite(rewardId))
            {
                StartCoroutine(RewardsManager.Instance.RemoveFromFavorites(rewardId));
            }
            else
            {
                StartCoroutine(RewardsManager.Instance.AddToFavorites(rewardId));
            }
        }

        #endregion

        #region Marketplace UI

        /// <summary>
        /// Affiche le panneau marketplace
        /// </summary>
        public void ShowMarketplacePanel()
        {
            if (marketplacePanel != null)
            {
                marketplacePanel.SetActive(true);
                RefreshMarketplaceList();
            }
        }

        /// <summary>
        /// Rafraîchit la liste du marketplace
        /// </summary>
        public void RefreshMarketplaceList()
        {
            if (marketplaceContainer == null || marketplaceItemPrefab == null)
                return;
            
            // Clear existing items
            foreach (Transform child in marketplaceContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Get items
            var items = MarketplaceManager.Instance.GetAllItems();
            
            // Create UI items
            foreach (var item in items)
            {
                GameObject uiItem = Instantiate(marketplaceItemPrefab, marketplaceContainer);
                SetupMarketplaceItem(uiItem, item);
            }
        }

        /// <summary>
        /// Configure un item du marketplace
        /// </summary>
        private void SetupMarketplaceItem(GameObject uiItem, MarketplaceItem item)
        {
            // Similar to SetupRewardItem but for marketplace items
            TMP_Text nameText = uiItem.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = item.name;
            
            TMP_Text priceText = uiItem.transform.Find("PriceText")?.GetComponent<TMP_Text>();
            if (priceText != null)
                priceText.text = $"{item.price} pts";
            
            Button buyButton = uiItem.transform.Find("BuyButton")?.GetComponent<Button>();
            if (buyButton != null)
            {
                bool canAfford = MarketplaceManager.Instance.CanAffordItem(item);
                buyButton.interactable = canAfford;
                buyButton.onClick.AddListener(() => OnBuyItemClicked(item));
            }
        }

        /// <summary>
        /// Gère le clic sur le bouton d'achat
        /// </summary>
        private void OnBuyItemClicked(MarketplaceItem item)
        {
            ShowConfirmDialog(
                $"Buy {item.name}?",
                $"This will cost {item.price} points.",
                () => StartCoroutine(MarketplaceManager.Instance.PurchaseItem(item.id))
            );
        }

        #endregion

        #region Social UI

        /// <summary>
        /// Affiche le panneau social
        /// </summary>
        public void ShowSocialPanel()
        {
            if (socialPanel != null)
            {
                socialPanel.SetActive(true);
                RefreshFriendsList();
                RefreshFriendRequests();
            }
        }

        /// <summary>
        /// Rafraîchit la liste d'amis
        /// </summary>
        public void RefreshFriendsList()
        {
            if (friendsContainer == null || friendItemPrefab == null)
                return;
            
            foreach (Transform child in friendsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var friends = SocialManager.Instance.GetFriends();
            
            foreach (var friend in friends)
            {
                GameObject item = Instantiate(friendItemPrefab, friendsContainer);
                SetupFriendItem(item, friend);
            }
        }

        /// <summary>
        /// Configure un item d'ami
        /// </summary>
        private void SetupFriendItem(GameObject item, Friend friend)
        {
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = friend.displayName;
            
            TMP_Text statusText = item.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusText != null)
                statusText.text = friend.isOnline ? "Online" : "Offline";
            
            Button profileButton = item.transform.Find("ProfileButton")?.GetComponent<Button>();
            if (profileButton != null)
            {
                profileButton.onClick.AddListener(() => ShowPlayerProfile(friend.userId));
            }
        }

        /// <summary>
        /// Rafraîchit les demandes d'amis
        /// </summary>
        public void RefreshFriendRequests()
        {
            if (requestsContainer == null || friendRequestPrefab == null)
                return;
            
            foreach (Transform child in requestsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var requests = SocialManager.Instance.GetPendingRequests();
            
            foreach (var request in requests)
            {
                GameObject item = Instantiate(friendRequestPrefab, requestsContainer);
                SetupFriendRequestItem(item, request);
            }
        }

        /// <summary>
        /// Configure un item de demande d'ami
        /// </summary>
        private void SetupFriendRequestItem(GameObject item, FriendRequest request)
        {
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = request.fromUserName;
            
            Button acceptButton = item.transform.Find("AcceptButton")?.GetComponent<Button>();
            if (acceptButton != null)
            {
                acceptButton.onClick.AddListener(() =>
                {
                    StartCoroutine(SocialManager.Instance.AcceptFriendRequest(request.id));
                    Destroy(item);
                });
            }
            
            Button rejectButton = item.transform.Find("RejectButton")?.GetComponent<Button>();
            if (rejectButton != null)
            {
                rejectButton.onClick.AddListener(() =>
                {
                    StartCoroutine(SocialManager.Instance.RejectFriendRequest(request.id));
                    Destroy(item);
                });
            }
        }

        /// <summary>
        /// Affiche le profil d'un joueur
        /// </summary>
        public void ShowPlayerProfile(string userId)
        {
            StartCoroutine(SocialManager.Instance.GetPlayerProfile(userId, (profile) =>
            {
                if (profile != null)
                {
                    // Show profile panel with player data
                    // TODO: Implement profile panel UI
                }
            }));
        }

        #endregion

        #region PowerUps UI

        /// <summary>
        /// Affiche l'indicateur de power-up actif
        /// </summary>
        public void ShowPowerUpIndicator(string powerUpType, float duration)
        {
            if (powerUpIndicator != null)
            {
                powerUpIndicator.SetActive(true);
                StartCoroutine(UpdatePowerUpTimer(powerUpType, duration));
            }
        }

        /// <summary>
        /// Met à jour le timer du power-up
        /// </summary>
        private IEnumerator UpdatePowerUpTimer(string powerUpType, float duration)
        {
            float remaining = duration;
            
            while (remaining > 0)
            {
                if (powerUpTimerText != null)
                {
                    powerUpTimerText.text = $"{powerUpType}\n{remaining:F1}s";
                }
                
                remaining -= Time.deltaTime;
                yield return null;
            }
            
            if (powerUpIndicator != null)
            {
                powerUpIndicator.SetActive(false);
            }
        }

        #endregion

        #region Challenges UI

        /// <summary>
        /// Affiche le panneau des challenges
        /// </summary>
        public void ShowChallengesPanel()
        {
            if (challengesPanel != null)
            {
                challengesPanel.SetActive(true);
                RefreshChallengesList();
            }
        }

        /// <summary>
        /// Rafraîchit la liste des challenges
        /// </summary>
        public void RefreshChallengesList()
        {
            if (challengesContainer == null || challengeItemPrefab == null)
                return;
            
            foreach (Transform child in challengesContainer)
            {
                Destroy(child.gameObject);
            }
            
            var challenges = ChallengesManager.Instance.GetDailyChallenges();
            
            foreach (var challenge in challenges)
            {
                GameObject item = Instantiate(challengeItemPrefab, challengesContainer);
                SetupChallengeItem(item, challenge);
            }
            
            // Update progress text
            if (challengesProgressText != null)
            {
                int completed = ChallengesManager.Instance.GetCompletedChallengesCount();
                int total = ChallengesManager.Instance.GetTotalChallengesCount();
                challengesProgressText.text = $"{completed}/{total} Completed";
            }
        }

        /// <summary>
        /// Configure un item de challenge
        /// </summary>
        private void SetupChallengeItem(GameObject item, DailyChallenge challenge)
        {
            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            if (nameText != null)
                nameText.text = challenge.name;
            
            TMP_Text descText = item.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
            if (descText != null)
                descText.text = challenge.description;
            
            TMP_Text progressText = item.transform.Find("ProgressText")?.GetComponent<TMP_Text>();
            if (progressText != null)
                progressText.text = $"{challenge.currentProgress}/{challenge.targetValue}";
            
            Slider progressBar = item.transform.Find("ProgressBar")?.GetComponent<Slider>();
            if (progressBar != null)
            {
                progressBar.maxValue = challenge.targetValue;
                progressBar.value = challenge.currentProgress;
            }
            
            TMP_Text rewardText = item.transform.Find("RewardText")?.GetComponent<TMP_Text>();
            if (rewardText != null)
                rewardText.text = $"+{challenge.pointsReward} pts";
            
            // Checkmark si complété
            GameObject checkmark = item.transform.Find("Checkmark")?.gameObject;
            if (checkmark != null)
                checkmark.SetActive(challenge.isCompleted);
        }

        /// <summary>
        /// Affiche le popup de challenge complété
        /// </summary>
        public void ShowChallengeCompletePopup(DailyChallenge challenge, ChallengeCompletionResult result)
        {
            string message = $"🎉 Challenge Complete!\n\n{challenge.name}\n\n+{result.pointsReward} points";
            
            if (result.xpReward > 0)
            {
                message += $"\n+{result.xpReward} XP";
            }
            
            if (result.bonusPoints > 0)
            {
                message += $"\n\n🌟 Bonus: +{result.bonusPoints} points!";
            }
            
            ShowMessage(message);
        }

        #endregion

        #region Claims UI

        /// <summary>
        /// Affiche le panneau des réclamations
        /// </summary>
        public void ShowClaimsPanel()
        {
            if (claimsPanel != null)
            {
                claimsPanel.SetActive(true);
                RefreshClaimsList();
            }
        }

        /// <summary>
        /// Rafraîchit la liste des réclamations
        /// </summary>
        public void RefreshClaimsList()
        {
            if (claimsContainer == null || claimItemPrefab == null)
                return;
            
            foreach (Transform child in claimsContainer)
            {
                Destroy(child.gameObject);
            }
            
            var claims = ClaimsManager.Instance.GetMyClaims();
            
            foreach (var claim in claims)
            {
                GameObject item = Instantiate(claimItemPrefab, claimsContainer);
                SetupClaimItem(item, claim);
            }
        }

        /// <summary>
        /// Configure un item de réclamation
        /// </summary>
        private void SetupClaimItem(GameObject item, Claim claim)
        {
            TMP_Text rewardText = item.transform.Find("RewardText")?.GetComponent<TMP_Text>();
            if (rewardText != null)
                rewardText.text = claim.rewardName;
            
            TMP_Text statusText = item.transform.Find("StatusText")?.GetComponent<TMP_Text>();
            if (statusText != null)
                statusText.text = claim.status.ToUpper();
            
            TMP_Text codeText = item.transform.Find("CodeText")?.GetComponent<TMP_Text>();
            if (codeText != null)
                codeText.text = $"Code: {claim.claimCode}";
            
            Button viewQRButton = item.transform.Find("ViewQRButton")?.GetComponent<Button>();
            if (viewQRButton != null)
            {
                viewQRButton.onClick.AddListener(() => ShowClaimQRCode(claim));
            }
        }

        /// <summary>
        /// Affiche le QR code d'une réclamation
        /// </summary>
        public void ShowClaimQRCode(Claim claim)
        {
            if (qrCodePanel != null)
            {
                qrCodePanel.SetActive(true);
                
                if (qrCodeImage != null && !string.IsNullOrEmpty(claim.qrCodeUrl))
                {
                    StartCoroutine(LoadQRCodeImage(claim.qrCodeUrl));
                }
            }
        }

        /// <summary>
        /// Charge l'image du QR code
        /// </summary>
        private IEnumerator LoadQRCodeImage(string url)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    if (qrCodeImage != null)
                    {
                        qrCodeImage.texture = texture;
                    }
                }
            }
        }

        #endregion

        #region Report UI

        /// <summary>
        /// Affiche le dialogue de signalement
        /// </summary>
        public void ShowReportDialog(string captureId, Prize prize)
        {
            if (reportPanel != null)
            {
                reportPanel.SetActive(true);
                
                if (submitReportButton != null)
                {
                    submitReportButton.onClick.RemoveAllListeners();
                    submitReportButton.onClick.AddListener(() => SubmitReport(captureId));
                }
            }
        }

        /// <summary>
        /// Soumet un rapport
        /// </summary>
        private void SubmitReport(string captureId)
        {
            if (reportTypeDropdown == null || reportDescriptionInput == null)
                return;
            
            string issueType = reportTypeDropdown.options[reportTypeDropdown.value].text;
            string description = reportDescriptionInput.text;
            
            StartCoroutine(CaptureController.Instance.ReportCaptureIssue(captureId, issueType, description, (success) =>
            {
                if (success && reportPanel != null)
                {
                    reportPanel.SetActive(false);
                }
            }));
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Charge une image depuis une URL
        /// </summary>
        private IEnumerator LoadImageFromURL(string url, Image targetImage)
        {
            using (UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                yield return request.SendWebRequest();
                
                if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Texture2D texture = UnityEngine.Networking.DownloadHandlerTexture.GetContent(request);
                    targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        /// <summary>
        /// Affiche un dialogue de confirmation
        /// </summary>
        public void ShowConfirmDialog(string title, string message, System.Action onConfirm)
        {
            // TODO: Implement confirm dialog UI
            // For now, just execute the action
            onConfirm?.Invoke();
        }

        #endregion
    }
}
