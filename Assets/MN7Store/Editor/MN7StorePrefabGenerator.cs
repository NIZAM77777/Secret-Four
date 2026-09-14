#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace MN7.Store.Editor
{
    /// <summary>One-time migration tool that turns the working sample scene into reusable prefabs.</summary>
    public static class MN7StorePrefabGenerator
    {
        private const string PrefabFolder = "Assets/MN7Store/Prefabs";
        private const string ItemPath = PrefabFolder + "/MN7StoreItem.prefab";
        private const string ButtonPath = PrefabFolder + "/MN7StoreCategoryButton.prefab";
        private const string BarPath = PrefabFolder + "/MN7StoreCategoryBar.prefab";
        private const string PanelPath = PrefabFolder + "/MN7StorePanel.prefab";
        private const string SystemPath = PrefabFolder + "/MN7StoreSystem.prefab";
        private const string SampleScenePath = "Assets/MN7Store/MN7StoreSystem.unity";

        [MenuItem("MN7 Store/Generate Reusable Prefabs")]
        public static void GenerateAll()
        {
            EnsureFolder(PrefabFolder);
            Scene scene = EditorSceneManager.OpenScene(SampleScenePath, OpenSceneMode.Single);
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogError($"[MN7 Store] Could not open sample scene: {SampleScenePath}");
                return;
            }

            MN7StoreUI storeUI = FindSceneComponent<MN7StoreUI>(scene);
            MN7StoreManager storeManager = FindSceneComponent<MN7StoreManager>(scene);
            if (storeUI == null || storeManager == null)
            {
                Debug.LogError($"[MN7 Store] Could not find MN7StoreUI/MN7StoreManager in '{scene.path}'.");
                return;
            }

            EnsureItemPrefabReferences();
            EnsureCategoryButtonPrefab();
            EnsureCategoryBarPrefab();
            ConfigureStorePanel(storeUI);
            ConfigureStoreSystem(storeManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[MN7 Store] Reusable prefabs generated successfully.");
        }

        private static void ConfigureStorePanel(MN7StoreUI storeUI)
        {
            GameObject panel = storeUI.gameObject;
            MN7StoreCategoryBarUI categoryBar = panel.GetComponentInChildren<MN7StoreCategoryBarUI>(true);
            if (categoryBar == null)
            {
                MN7StoreCategoryBarUI prefab = AssetDatabase.LoadAssetAtPath<MN7StoreCategoryBarUI>(BarPath);
                if (prefab == null) throw new InvalidOperationException("MN7StoreCategoryBar.prefab could not be loaded.");
                categoryBar = PrefabUtility.InstantiatePrefab(prefab, panel.transform) as MN7StoreCategoryBarUI;
                categoryBar.name = "CategoryBar";
            }

            RectTransform content = FindDescendant<RectTransform>(panel.transform, "Content");
            if (content == null) throw new InvalidOperationException("Store Panel item Content was not found.");

            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>() ?? content.gameObject.AddComponent<GridLayoutGroup>();
            // Visual grid values live in the Store Panel prefab. Do not overwrite them during normal regeneration.
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            if (grid.constraintCount < 1)
                grid.constraintCount = 1;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>() ?? content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            SerializedObject so = new SerializedObject(storeUI);
            SetRef(so, "itemContainer", content);
            SetRef(so, "itemGrid", grid);
            SetRef(so, "itemContentFitter", fitter);
            SetRef(so, "itemPrefab", AssetDatabase.LoadAssetAtPath<MN7StoreItemUI>(ItemPath));
            SetRef(so, "categoryBar", categoryBar);
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(panel, PanelPath);
        }

        private static void ConfigureStoreSystem(MN7StoreManager manager)
        {
            GameObject wrapper = new GameObject("MN7StoreSystem");
            try
            {
                GameObject managerCopy = UnityEngine.Object.Instantiate(manager.gameObject, wrapper.transform);
                managerCopy.name = "MN7StoreManager";
                SavePrefab(wrapper, SystemPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(wrapper); }
        }

        private static void EnsureItemPrefabReferences()
        {
            if (AssetDatabase.LoadAssetAtPath<MN7StoreItemUI>(ItemPath) == null) return;
            GameObject root = PrefabUtility.LoadPrefabContents(ItemPath);
            try
            {
                EnsureState(root.transform, "OwnedState");
                EnsureState(root.transform, "SelectionState");
                EnsureState(root.transform, "LockedState");
                MN7StoreItemUI ui = root.GetComponent<MN7StoreItemUI>();
                if (ui == null) return;

                SerializedObject so = new SerializedObject(ui);
                Button buy = FindButton(root.transform, "BuyButton");
                Button ad = FindButton(root.transform, "AdButton");
                Button select = FindButton(root.transform, "SelectButton");
                Button selected = FindButton(root.transform, "SelectedButton");
                SetRef(so, "background", FindImage(root.transform, "BackGround", "Background"));
                SetRef(so, "icon", FindImage(root.transform, "Icon"));
                SetRef(so, "nameText", FindText(root.transform, "Name"));
                SetRef(so, "descriptionText", FindText(root.transform, "Description"));
                SetRef(so, "priceText", FindText(root.transform, "Price"));
                SetRef(so, "statusText", FindText(root.transform, "Status"));
                SetRef(so, "buyButton", buy);
                SetRef(so, "buyButtonLabel", FindButtonLabel(buy));
                SetRef(so, "watchAdButton", ad);
                SetRef(so, "watchAdButtonLabel", FindButtonLabel(ad));
                SetRef(so, "selectButton", select);
                SetRef(so, "selectButtonLabel", FindButtonLabel(select));
                SetRef(so, "selectedButton", selected);
                SetRef(so, "selectedButtonLabel", FindButtonLabel(selected));
                SetRef(so, "ownedState", root.transform.Find("OwnedState")?.gameObject);
                SetRef(so, "selectionState", root.transform.Find("SelectionState")?.gameObject);
                SetRef(so, "lockedState", root.transform.Find("LockedState")?.gameObject);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ItemPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void EnsureCategoryButtonPrefab()
        {
            MN7StoreCategoryButtonUI existing = AssetDatabase.LoadAssetAtPath<MN7StoreCategoryButtonUI>(ButtonPath);
            if (existing != null && FindDescendant<Transform>(existing.transform, "NameText") != null) return;

            GameObject root = new GameObject("MN7StoreCategoryButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement), typeof(MN7StoreCategoryButtonUI));
            RectTransform rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(160f, 52f);
            Image background = root.GetComponent<Image>();
            Button button = root.GetComponent<Button>();

            GameObject textObject = CreateChild(root.transform, "NameText");
            TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            SetFullRect(textObject.GetComponent<RectTransform>(), 10f, 4f, 10f, 4f);

            GameObject selectionObject = CreateChild(root.transform, "SelectionVisual");
            Image selectionImage = selectionObject.AddComponent<Image>();
            selectionImage.raycastTarget = false;
            SetFullRect(selectionObject.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            selectionObject.SetActive(false);

            LayoutElement layout = root.GetComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = 160f;
            layout.minHeight = layout.preferredHeight = 52f;

            SerializedObject so = new SerializedObject(root.GetComponent<MN7StoreCategoryButtonUI>());
            SetRef(so, "button", button);
            SetRef(so, "background", background);
            SetRef(so, "nameText", text);
            SetRef(so, "selectionVisual", selectionObject);
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(root, ButtonPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void EnsureCategoryBarPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<MN7StoreCategoryBarUI>(BarPath) != null) return;
            GameObject root = new GameObject("MN7StoreCategoryBar", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(MN7StoreCategoryBarUI));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 64f);

            GameObject viewportObject = CreateChild(root.transform, "Viewport");
            viewportObject.AddComponent<RectMask2D>();
            RectTransform viewport = viewportObject.GetComponent<RectTransform>();
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 0f);
            viewport.offsetMax = new Vector2(-8f, 0f);

            GameObject contentObject = CreateChild(viewportObject.transform, "Content");
            HorizontalLayoutGroup layout = contentObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            RectTransform content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, .5f);
            content.anchorMax = new Vector2(0f, .5f);
            content.pivot = new Vector2(0f, .5f);

            ScrollRect scroll = root.GetComponent<ScrollRect>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.inertia = true;
            scroll.scrollSensitivity = 20f;

            SerializedObject so = new SerializedObject(root.GetComponent<MN7StoreCategoryBarUI>());
            SetRef(so, "scrollRect", scroll);
            SetRef(so, "content", content);
            SetRef(so, "categoryButtonPrefab", AssetDatabase.LoadAssetAtPath<MN7StoreCategoryButtonUI>(ButtonPath));
            so.ApplyModifiedPropertiesWithoutUndo();
            SavePrefab(root, BarPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static T FindSceneComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }
            return null;
        }

        private static T FindDescendant<T>(Transform root, string objectName) where T : Component
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                if (child.name == objectName) return child.GetComponent<T>();
            return null;
        }

        private static T FindDescendantComponent<T>(Transform root, string objectName) where T : Component => FindDescendant<T>(root, objectName);

        private static GameObject CreateChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void EnsureState(Transform root, string name)
        {
            if (root.Find(name) != null) return;
            GameObject state = CreateChild(root, name);
            state.SetActive(false);
            SetFullRect(state.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
        }

        private static Image FindImage(Transform root, params string[] names)
        {
            foreach (string name in names)
            {
                Image image = root.Find(name)?.GetComponent<Image>();
                if (image != null) return image;
            }
            return null;
        }

        private static TMP_Text FindText(Transform root, string name) => root.Find(name)?.GetComponent<TMP_Text>();
        private static Button FindButton(Transform root, string name) => root.Find(name)?.GetComponent<Button>();
        private static TMP_Text FindButtonLabel(Button button) => button == null ? null : button.transform.Find("Text")?.GetComponent<TMP_Text>();

        private static void SetRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }

        private static void SetFullRect(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void SavePrefab(GameObject source, string path)
        {
            EnsureFolder(PrefabFolder);
            PrefabUtility.SaveAsPrefabAsset(source, path);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
