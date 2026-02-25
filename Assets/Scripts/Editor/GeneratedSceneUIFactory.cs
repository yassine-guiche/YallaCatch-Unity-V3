using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace YallaCatch.Editor
{
    /// <summary>
    /// Shared UI construction primitives for scene generators.
    /// Extracted from FullSceneGenerator to enable direct scene builders.
    /// </summary>
    internal static class GeneratedSceneUIFactory
    {
        internal static Canvas CreateCanvasSceneRoot(bool createEventSystem)
        {
            if (createEventSystem && Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<StandaloneInputModule>();
            }

            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        internal static GameObject CreatePanel(Transform parent, string name, Color? color = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color ?? new Color(0.08f, 0.08f, 0.12f, 0.95f);

            obj.AddComponent<CanvasGroup>();
            return obj;
        }

        internal static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 pos,
            Color color,
            Vector2? size = null,
            int fontSize = 30)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(500f, 80f);

            Image img = obj.AddComponent<Image>();
            img.color = color;

            Button btn = obj.AddComponent<Button>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontSize = fontSize;

            return btn;
        }

        internal static TextMeshProUGUI CreateText(
            Transform parent,
            string name,
            string text,
            Vector2 pos,
            Color color,
            int fontSize = 36,
            Vector2? size = null)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform rect = textObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(700f, 80f);

            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.fontSize = fontSize;
            return tmp;
        }

        internal static TMP_InputField CreateInputField(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2? size = null,
            string placeholder = "Enter text...")
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size ?? new Vector2(500f, 120f);

            Image img = obj.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f);

            TMP_InputField input = obj.AddComponent<TMP_InputField>();

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            RectTransform tRect = textObj.AddComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = new Vector2(-20f, -10f);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.color = Color.white;
            tmp.fontSize = 20f;

            GameObject phObj = new GameObject("Placeholder");
            phObj.transform.SetParent(obj.transform, false);
            RectTransform phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.sizeDelta = new Vector2(-20f, -10f);
            TextMeshProUGUI phtmp = phObj.AddComponent<TextMeshProUGUI>();
            phtmp.text = placeholder;
            phtmp.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            phtmp.fontSize = 20f;

            input.textComponent = tmp;
            input.placeholder = phtmp;
            input.lineType = TMP_InputField.LineType.MultiLineNewline;
            return input;
        }

        internal static TMP_Dropdown CreateTMPDropdown(
            Transform parent,
            string name,
            Vector2 pos,
            Vector2 size,
            List<string> options = null)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            RectTransform rootRect = root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 0.5f);
            rootRect.anchorMax = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = pos;
            rootRect.sizeDelta = size;

            Image bg = root.AddComponent<Image>();
            bg.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            TMP_Dropdown dropdown = root.AddComponent<TMP_Dropdown>();
            dropdown.targetGraphic = bg;

            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(20f, 8f);
            labelRect.offsetMax = new Vector2(-70f, -8f);
            TextMeshProUGUI label = labelObj.AddComponent<TextMeshProUGUI>();
            label.color = Color.white;
            label.fontSize = 26;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.text = options != null && options.Count > 0 ? options[0] : "Select";
            dropdown.captionText = label;

            GameObject arrowObj = new GameObject("Arrow");
            arrowObj.transform.SetParent(root.transform, false);
            RectTransform arrowRect = arrowObj.AddComponent<RectTransform>();
            arrowRect.anchorMin = new Vector2(1f, 0.5f);
            arrowRect.anchorMax = new Vector2(1f, 0.5f);
            arrowRect.pivot = new Vector2(1f, 0.5f);
            arrowRect.anchoredPosition = new Vector2(-18f, 0f);
            arrowRect.sizeDelta = new Vector2(36f, 36f);
            TextMeshProUGUI arrowText = arrowObj.AddComponent<TextMeshProUGUI>();
            arrowText.text = "v";
            arrowText.color = new Color(0.85f, 0.85f, 0.9f);
            arrowText.fontSize = 22;
            arrowText.alignment = TextAlignmentOptions.Center;

            GameObject templateObj = new GameObject("Template");
            templateObj.transform.SetParent(root.transform, false);
            RectTransform templateRect = templateObj.AddComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0f, 0f);
            templateRect.anchorMax = new Vector2(1f, 0f);
            templateRect.pivot = new Vector2(0.5f, 1f);
            templateRect.anchoredPosition = new Vector2(0f, -6f);
            templateRect.sizeDelta = new Vector2(0f, 260f);
            Image templateBg = templateObj.AddComponent<Image>();
            templateBg.color = new Color(0.09f, 0.09f, 0.13f, 0.98f);
            ScrollRect templateScroll = templateObj.AddComponent<ScrollRect>();
            templateScroll.horizontal = false;
            templateScroll.vertical = true;
            templateScroll.movementType = ScrollRect.MovementType.Clamped;

            GameObject viewportObj = new GameObject("Viewport");
            viewportObj.transform.SetParent(templateObj.transform, false);
            RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            Image viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.001f);
            Mask viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;
            templateScroll.viewport = viewportRect;

            GameObject contentObj = new GameObject("Content");
            contentObj.transform.SetParent(viewportObj.transform, false);
            RectTransform contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 40f);
            templateScroll.content = contentRect;

            GameObject itemObj = new GameObject("Item");
            itemObj.transform.SetParent(contentObj.transform, false);
            RectTransform itemRect = itemObj.AddComponent<RectTransform>();
            itemRect.anchorMin = new Vector2(0f, 1f);
            itemRect.anchorMax = new Vector2(1f, 1f);
            itemRect.pivot = new Vector2(0.5f, 1f);
            itemRect.sizeDelta = new Vector2(0f, 48f);
            Toggle itemToggle = itemObj.AddComponent<Toggle>();

            GameObject itemBgObj = new GameObject("Item Background");
            itemBgObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemBgRect = itemBgObj.AddComponent<RectTransform>();
            itemBgRect.anchorMin = Vector2.zero;
            itemBgRect.anchorMax = Vector2.one;
            itemBgRect.sizeDelta = Vector2.zero;
            Image itemBgImage = itemBgObj.AddComponent<Image>();
            itemBgImage.color = new Color(0.14f, 0.14f, 0.2f, 1f);

            GameObject itemCheckObj = new GameObject("Item Checkmark");
            itemCheckObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemCheckRect = itemCheckObj.AddComponent<RectTransform>();
            itemCheckRect.anchorMin = new Vector2(0f, 0.5f);
            itemCheckRect.anchorMax = new Vector2(0f, 0.5f);
            itemCheckRect.pivot = new Vector2(0f, 0.5f);
            itemCheckRect.anchoredPosition = new Vector2(12f, 0f);
            itemCheckRect.sizeDelta = new Vector2(18f, 18f);
            Image itemCheckImage = itemCheckObj.AddComponent<Image>();
            itemCheckImage.color = new Color(0.2f, 0.8f, 0.4f, 1f);

            GameObject itemLabelObj = new GameObject("Item Label");
            itemLabelObj.transform.SetParent(itemObj.transform, false);
            RectTransform itemLabelRect = itemLabelObj.AddComponent<RectTransform>();
            itemLabelRect.anchorMin = Vector2.zero;
            itemLabelRect.anchorMax = Vector2.one;
            itemLabelRect.offsetMin = new Vector2(40f, 4f);
            itemLabelRect.offsetMax = new Vector2(-12f, -4f);
            TextMeshProUGUI itemLabel = itemLabelObj.AddComponent<TextMeshProUGUI>();
            itemLabel.color = Color.white;
            itemLabel.fontSize = 22;
            itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
            itemLabel.text = "Option";

            itemToggle.targetGraphic = itemBgImage;
            itemToggle.graphic = itemCheckImage;
            itemToggle.isOn = true;

            dropdown.template = templateRect;
            dropdown.itemText = itemLabel;
            dropdown.ClearOptions();
            dropdown.AddOptions(options ?? new List<string> { "Select" });
            dropdown.RefreshShownValue();
            templateObj.SetActive(false);

            return dropdown;
        }

        internal static GameObject CreateScrollContainer(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject scrollObj = new GameObject(name);
            scrollObj.transform.SetParent(parent, false);

            RectTransform rect = scrollObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image bg = scrollObj.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.11f, 0.95f);

            ScrollRect scrollView = scrollObj.AddComponent<ScrollRect>();
            scrollView.horizontal = false;

            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vRect = viewport.AddComponent<RectTransform>();
            vRect.anchorMin = Vector2.zero;
            vRect.anchorMax = Vector2.one;
            vRect.sizeDelta = Vector2.zero;
            Image vImg = viewport.AddComponent<Image>();
            vImg.color = new Color(0f, 0f, 0f, 0.001f);
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRect = content.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollView.viewport = vRect;
            scrollView.content = cRect;
            return scrollObj;
        }

        internal static GameObject CreateBackground(Transform parent, string name, string resourcePath)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.SetAsFirstSibling(); // Ensure background is behind everything
            
            RectTransform rect = obj.AddComponent<RectTransform>();
            StretchToParent(rect);

            Image img = obj.AddComponent<Image>();
            Sprite bgSprite = Resources.Load<Sprite>(resourcePath);
            if (bgSprite != null)
            {
                img.sprite = bgSprite;
            }
            else
            {
                Debug.LogWarning($"[GeneratedSceneUIFactory] Background sprite not found at Resources/{resourcePath}");
                img.color = new Color(0.04f, 0.04f, 0.06f, 1f); // Fallback color
            }

            return obj;
        }

        internal static Image CreateImage(Transform parent, string name, Vector2 pos, Vector2 size, string resourcePath = null, Color? defaultColor = null)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            Image img = obj.AddComponent<Image>();
            
            if (!string.IsNullOrEmpty(resourcePath))
            {
                Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
                if (loadedSprite != null)
                {
                    img.sprite = loadedSprite;
                    img.color = Color.white; // Ensure tint isn't hiding it
                }
                else
                {
                    Debug.LogWarning($"[GeneratedSceneUIFactory] Sprite not found at Resources/{resourcePath}");
                    img.color = defaultColor ?? Color.white;
                }
            }
            else
            {
                img.color = defaultColor ?? Color.white;
            }

            return img;
        }

        internal static void AttachUIAnimator(GameObject panel, YallaCatch.UI.UIAnimationType type = YallaCatch.UI.UIAnimationType.ScaleBounce, float duration = 0.4f)
        {
            if (panel != null && panel.GetComponent<YallaCatch.UI.UIAnimator>() == null)
            {
                var anim = panel.AddComponent<YallaCatch.UI.UIAnimator>();
                anim.animationType = type;
                anim.duration = duration;
            }
        }

        internal static void StretchToParent(RectTransform rect)
        {
            if (rect == null) return;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
