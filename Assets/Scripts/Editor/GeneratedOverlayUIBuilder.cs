using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Shared overlay/dialog UI builder for OverlayScene and future direct Meta/Game scene generators.
    /// Keeps object names and structures consistent with UIManager/UIManagerExtensions expectations.
    /// </summary>
    internal static class GeneratedOverlayUIBuilder
    {
        internal static void BuildAll(Transform canvasT)
        {
            BuildCaptureDialog(canvasT);
            BuildMessageDialog(canvasT);
            BuildConfirmDialog(canvasT);
            BuildLoadingOverlay(canvasT);
            BuildReportPanel(canvasT);
            BuildQRCodePanel(canvasT);
            BuildCaptureRewardPopup(canvasT);
            BuildCaptureScreenFlash(canvasT);
        }

        internal static void BuildCaptureDialog(Transform canvasT)
        {
            GameObject captureDialog = GeneratedSceneUIFactory.CreatePanel(canvasT, "CaptureDialog", new Color(0.02f, 0.04f, 0.06f, 0.8f));
            GeneratedSceneUIFactory.AttachUIAnimator(captureDialog);

            GeneratedSceneUIFactory.CreateText(captureDialog.transform, "CaptureHeader", "TARGET ACQUIRED", new Vector2(0f, 700f), new Color(0.1f, 0.9f, 0.4f), 55);
            GeneratedSceneUIFactory.CreateText(captureDialog.transform, "PrizeNameText", "Prize Name", new Vector2(0f, 500f), Color.white, 48);
            GeneratedSceneUIFactory.CreateText(captureDialog.transform, "PrizeDescriptionText", "Description...", new Vector2(0f, 380f), new Color(0.7f, 0.7f, 0.75f), 28, new Vector2(900f, 80f));
            GeneratedSceneUIFactory.CreateText(captureDialog.transform, "PrizePointsText", "+100 pts", new Vector2(0f, -350f), new Color(1f, 0.8f, 0.2f), 42);

            GameObject prizeImageObj = new GameObject("PrizeImage");
            prizeImageObj.transform.SetParent(captureDialog.transform, false);
            RectTransform piRect = prizeImageObj.AddComponent<RectTransform>();
            piRect.anchorMin = new Vector2(0.5f, 0.5f);
            piRect.anchorMax = new Vector2(0.5f, 0.5f);
            piRect.anchoredPosition = new Vector2(0f, 30f);
            piRect.sizeDelta = new Vector2(350f, 350f);
            prizeImageObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f);

            GeneratedSceneUIFactory.CreateButton(captureDialog.transform, "CaptureButton", "INITIATE CAPTURE", new Vector2(0f, -550f), new Color(0.1f, 0.7f, 0.3f), new Vector2(850f, 140f), 40);
            GeneratedSceneUIFactory.CreateButton(captureDialog.transform, "CancelButton", "ABORT", new Vector2(0f, -750f), new Color(0.6f, 0.2f, 0.2f), new Vector2(400f, 100f), 32);

            captureDialog.SetActive(false);
        }

        internal static void BuildMessageDialog(Transform canvasT)
        {
            GameObject messageDialog = GeneratedSceneUIFactory.CreatePanel(canvasT, "MessageDialog", new Color(0.04f, 0.04f, 0.06f, 0.7f));
            GeneratedSceneUIFactory.AttachUIAnimator(messageDialog);
            GeneratedSceneUIFactory.CreateText(messageDialog.transform, "MessageHeader", "SYSTEM MESSAGE", new Vector2(0f, 300f), new Color(0.5f, 0.7f, 1f), 36);
            GeneratedSceneUIFactory.CreateText(messageDialog.transform, "MessageText", "Message Content...", new Vector2(0f, 100f), Color.white, 32, new Vector2(900f, 180f));
            GeneratedSceneUIFactory.CreateButton(messageDialog.transform, "MessageOkButton", "ACKNOWLEDGE", new Vector2(0f, -200f), new Color(0.2f, 0.4f, 0.8f), new Vector2(500f, 120f), 36);
            messageDialog.SetActive(false);
        }

        internal static void BuildConfirmDialog(Transform canvasT)
        {
            GameObject confirmDialog = GeneratedSceneUIFactory.CreatePanel(canvasT, "ConfirmDialog", new Color(0.04f, 0.04f, 0.06f, 0.7f));
            GeneratedSceneUIFactory.AttachUIAnimator(confirmDialog);
            GeneratedSceneUIFactory.CreateText(confirmDialog.transform, "ConfirmTitleText", "AUTHORIZATION REQUIRED", new Vector2(0f, 300f), new Color(1f, 0.6f, 0.1f), 36, new Vector2(950f, 80f));
            GeneratedSceneUIFactory.CreateText(confirmDialog.transform, "ConfirmMessageText", "Proceed?", new Vector2(0f, 100f), Color.white, 32, new Vector2(900f, 180f));
            GeneratedSceneUIFactory.CreateButton(confirmDialog.transform, "ConfirmYesButton", "AUTHORIZE", new Vector2(-250f, -200f), new Color(0.1f, 0.7f, 0.3f), new Vector2(400f, 120f), 36);
            GeneratedSceneUIFactory.CreateButton(confirmDialog.transform, "ConfirmNoButton", "CANCEL", new Vector2(250f, -200f), new Color(0.7f, 0.2f, 0.2f), new Vector2(400f, 120f), 36);
            confirmDialog.SetActive(false);
        }

        internal static void BuildLoadingOverlay(Transform canvasT)
        {
            GameObject loadingOverlay = GeneratedSceneUIFactory.CreatePanel(canvasT, "LoadingOverlay", new Color(0.02f, 0.02f, 0.03f, 0.95f));
            GeneratedSceneUIFactory.CreateText(loadingOverlay.transform, "LoadingText", "SYNCING...", Vector2.zero, new Color(0.4f, 0.8f, 0.5f), 32, new Vector2(900f, 120f));
            loadingOverlay.SetActive(false);
        }

        internal static void BuildReportPanel(Transform canvasT)
        {
            GameObject reportDialog = GeneratedSceneUIFactory.CreatePanel(canvasT, "ReportPanel", new Color(0.04f, 0.04f, 0.06f, 0.7f));
            GeneratedSceneUIFactory.AttachUIAnimator(reportDialog);
            GeneratedSceneUIFactory.CreateText(reportDialog.transform, "ReportTitle", "SUBMIT FIELD REPORT", new Vector2(0f, 400f), new Color(0.9f, 0.9f, 0.95f), 48);
            GeneratedSceneUIFactory.CreateTMPDropdown(
                reportDialog.transform,
                "ReportTypeDropdown",
                new Vector2(0f, 200f),
                new Vector2(600f, 80f),
                new List<string> { "Incorrect Prize Location", "Prize Not Visible", "Duplicate Prize", "Other" });
            GeneratedSceneUIFactory.CreateInputField(reportDialog.transform, "ReportDescriptionInput", new Vector2(0f, -50f), new Vector2(700f, 220f), "Describe the issue...");
            GeneratedSceneUIFactory.CreateButton(reportDialog.transform, "SubmitReportButton", "TRANSMIT REPORT", new Vector2(0f, -300f), new Color(0.8f, 0.3f, 0.2f), new Vector2(600f, 100f), 30);
            GeneratedSceneUIFactory.CreateButton(reportDialog.transform, "CloseReportButton", "CANCEL", new Vector2(0f, -450f), new Color(0.3f, 0.3f, 0.4f), new Vector2(400f, 80f), 28);
            reportDialog.SetActive(false);
        }

        internal static void BuildQRCodePanel(Transform canvasT)
        {
            GameObject qrPanel = GeneratedSceneUIFactory.CreatePanel(canvasT, "QRCodePanel", new Color(0.04f, 0.04f, 0.06f, 0.7f));
            GeneratedSceneUIFactory.AttachUIAnimator(qrPanel);
            GeneratedSceneUIFactory.CreateText(qrPanel.transform, "QRTitle", "SECURITY CLEARANCE CODE", new Vector2(0f, 350f), new Color(0.9f, 0.9f, 0.95f), 42, new Vector2(1000f, 80f));

            GameObject qrImgObj = new GameObject("QRCodeImage");
            qrImgObj.transform.SetParent(qrPanel.transform, false);
            RectTransform qrRect = qrImgObj.AddComponent<RectTransform>();
            qrRect.anchorMin = new Vector2(0.5f, 0.5f);
            qrRect.anchorMax = new Vector2(0.5f, 0.5f);
            qrRect.anchoredPosition = new Vector2(0f, 50f);
            qrRect.sizeDelta = new Vector2(400f, 400f);
            qrImgObj.AddComponent<RawImage>();

            GeneratedSceneUIFactory.CreateButton(qrPanel.transform, "CloseQRButton", "CLOSE TERMINAL", new Vector2(0f, -300f), new Color(0.3f, 0.3f, 0.4f), new Vector2(500f, 100f), 32);
            qrPanel.SetActive(false);
        }

        internal static void BuildCaptureRewardPopup(Transform canvasT)
        {
            GameObject rewardPopup = GeneratedSceneUIFactory.CreatePanel(canvasT, "CaptureRewardPopupPanel", new Color(0f, 0f, 0f, 0.55f));
            GeneratedSceneUIFactory.AttachUIAnimator(rewardPopup);

            GameObject rewardCard = GeneratedSceneUIFactory.CreatePanel(rewardPopup.transform, "RewardCard", new Color(0.06f, 0.07f, 0.1f, 0.97f));
            RectTransform cardRect = rewardCard.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(820f, 760f);

            GameObject rewardIconObj = new GameObject("RewardIcon");
            rewardIconObj.transform.SetParent(rewardCard.transform, false);
            RectTransform iconRect = rewardIconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -60f);
            iconRect.sizeDelta = new Vector2(180f, 180f);
            rewardIconObj.AddComponent<Image>().color = new Color(0.8f, 0.8f, 0.85f, 1f);

            GeneratedSceneUIFactory.CreateText(rewardCard.transform, "RewardNameText", "Reward Name", new Vector2(0f, 150f), Color.white, 40, new Vector2(700f, 80f));
            GeneratedSceneUIFactory.CreateText(rewardCard.transform, "RewardPointsText", "+0 Points", new Vector2(0f, 60f), new Color(1f, 0.82f, 0.2f), 34, new Vector2(700f, 80f));
            GeneratedSceneUIFactory.CreateText(rewardCard.transform, "RewardMessageText", "Prize captured!", new Vector2(0f, -20f), new Color(0.75f, 0.8f, 0.95f), 24, new Vector2(700f, 120f));
            GeneratedSceneUIFactory.CreateButton(rewardCard.transform, "RewardCloseButton", "CONTINUE", new Vector2(0f, -250f), new Color(0.22f, 0.58f, 0.25f), new Vector2(420f, 90f), 28);

            rewardPopup.SetActive(false);
        }

        internal static void BuildCaptureScreenFlash(Transform canvasT)
        {
            GameObject screenFlash = new GameObject("CaptureScreenFlash");
            screenFlash.transform.SetParent(canvasT, false);
            RectTransform flashRect = screenFlash.AddComponent<RectTransform>();
            GeneratedSceneUIFactory.StretchToParent(flashRect);
            Image flashImage = screenFlash.AddComponent<Image>();
            flashImage.color = new Color(1f, 1f, 1f, 0.5f);
            screenFlash.SetActive(false);
        }
    }
}
