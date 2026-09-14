#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store.Editor
{
    /// <summary>
    /// Creates the shared item action-button prefab from the current Store Item button artwork
    /// and replaces the old direct action buttons inside MN7StoreItem.prefab.
    /// </summary>
    public static class MN7StoreItemActionPrefabGenerator
    {
        private const string PrefabFolder = "Assets/MN7Store/Prefabs";
        private const string ItemPath = PrefabFolder + "/MN7StoreItem.prefab";
        private const string ActionPath = PrefabFolder + "/MN7StoreItemActions.prefab";

        [MenuItem("MN7 Store/Generate Shared Item Action Prefab")]
        public static void Generate()
        {
            EnsureFolder(PrefabFolder);
            GameObject itemRoot = PrefabUtility.LoadPrefabContents(ItemPath);
            if (itemRoot == null)
            {
                Debug.LogError($"[MN7 Store] Could not open {ItemPath}.");
                return;
            }

            try
            {
                Transform oldBuy = FindDirect(itemRoot.transform, "BuyButton");
                Transform oldAd = FindDirect(itemRoot.transform, "AdButton");
                Transform oldWatchAd = FindDirect(itemRoot.transform, "WatchAdButton");
                Transform oldSelect = FindDirect(itemRoot.transform, "SelectButton");
                Transform oldSelected = FindDirect(itemRoot.transform, "SelectedButton");

                Button buySource = GetButton(oldBuy);
                Button adSource = GetButton(oldAd != null ? oldAd : oldWatchAd);
                Button selectSource = GetButton(oldSelect);
                Button selectedSource = GetButton(oldSelected);

                if (buySource == null && adSource == null)
                {
                    Debug.LogError("[MN7 Store] MN7StoreItem needs an existing BuyButton or AdButton to seed the shared action prefab.");
                    return;
                }

                GameObject actionRoot = new GameObject("MN7StoreItemActions", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(MN7StoreItemActionSetUI));
                try
                {
                    RectTransform actionRect = actionRoot.GetComponent<RectTransform>();
                    actionRect.sizeDelta = new Vector2(304f, 48f);

                    HorizontalLayoutGroup layout = actionRoot.GetComponent<HorizontalLayoutGroup>();
                    layout.spacing = 6f;
                    layout.childAlignment = TextAnchor.MiddleCenter;
                    layout.childControlWidth = true;
                    layout.childControlHeight = true;
                    layout.childForceExpandWidth = false;
                    layout.childForceExpandHeight = false;

                    GameObject buy = CloneButton(buySource ?? adSource, actionRoot.transform, "BuyButton", "BUY");
                    GameObject buyWithAd = CloneButton(adSource ?? buySource, actionRoot.transform, "BuyWithAdButton", "WATCH AD");
                    GameObject select = CloneButton(selectSource ?? buySource, actionRoot.transform, "SelectButton", "SELECT");
                    GameObject selected = CloneButton(selectedSource ?? buySource, actionRoot.transform, "SelectedButton", "SELECTED");

                    buyWithAd.SetActive(true);
                    select.SetActive(true);
                    selected.SetActive(true);

                    ConfigureLayoutElement(buy);
                    ConfigureLayoutElement(buyWithAd);
                    ConfigureLayoutElement(select);
                    ConfigureLayoutElement(selected);

                    MN7StoreItemActionSetUI actionSet = actionRoot.GetComponent<MN7StoreItemActionSetUI>();
                    SerializedObject actionSO = new SerializedObject(actionSet);
                    SetRef(actionSO, "buyButton", buy.GetComponent<Button>());
                    SetRef(actionSO, "buyLabel", FindLabel(buy.transform));
                    SetRef(actionSO, "buyWithAdButton", buyWithAd.GetComponent<Button>());
                    SetRef(actionSO, "buyWithAdLabel", FindLabel(buyWithAd.transform));
                    SetRef(actionSO, "selectButton", select.GetComponent<Button>());
                    SetRef(actionSO, "selectLabel", FindLabel(select.transform));
                    SetRef(actionSO, "selectedButton", selected.GetComponent<Button>());
                    SetRef(actionSO, "selectedLabel", FindLabel(selected.transform));
                    actionSO.ApplyModifiedPropertiesWithoutUndo();

                    PrefabUtility.SaveAsPrefabAsset(actionRoot, ActionPath);
                }
                finally
                {
                    Object.DestroyImmediate(actionRoot);
                }

                MN7StoreItemActionSetUI actionPrefab = AssetDatabase.LoadAssetAtPath<MN7StoreItemActionSetUI>(ActionPath);
                if (actionPrefab == null)
                {
                    Debug.LogError("[MN7 Store] Shared action prefab was not created.");
                    return;
                }

                Transform existingActions = FindDirect(itemRoot.transform, "Actions");
                if (existingActions != null)
                    Object.DestroyImmediate(existingActions.gameObject);

                Object.DestroyImmediate(oldBuy?.gameObject);
                Object.DestroyImmediate(oldAd?.gameObject);
                Object.DestroyImmediate(oldWatchAd?.gameObject);
                Object.DestroyImmediate(oldSelect?.gameObject);
                Object.DestroyImmediate(oldSelected?.gameObject);

                MN7StoreItemActionSetUI instance = PrefabUtility.InstantiatePrefab(actionPrefab, itemRoot.transform) as MN7StoreItemActionSetUI;
                if (instance == null)
                {
                    Debug.LogError("[MN7 Store] Could not add shared action prefab to MN7StoreItem.prefab.");
                    return;
                }
                instance.name = "Actions";

                RectTransform itemRect = itemRoot.GetComponent<RectTransform>();
                RectTransform instanceRect = instance.GetComponent<RectTransform>();
                instanceRect.anchorMin = new Vector2(0.5f, 0f);
                instanceRect.anchorMax = new Vector2(0.5f, 0f);
                instanceRect.pivot = new Vector2(0.5f, 0f);
                instanceRect.anchoredPosition = new Vector2(0f, 12f);
                instanceRect.sizeDelta = new Vector2(Mathf.Max(160f, itemRect != null ? itemRect.rect.width - 20f : 284f), 48f);

                MN7StoreItemUI itemUI = itemRoot.GetComponent<MN7StoreItemUI>();
                SerializedObject itemSO = new SerializedObject(itemUI);
                SetRef(itemSO, "actionSet", instance);
                SetRef(itemSO, "ownedState", FindDirect(itemRoot.transform, "OwnedState")?.gameObject);
                SetRef(itemSO, "selectionState", FindDirect(itemRoot.transform, "SelectionState")?.gameObject ?? FindDirect(itemRoot.transform, "Selected")?.gameObject);
                SetRef(itemSO, "lockedState", FindDirect(itemRoot.transform, "LockedState")?.gameObject);
                itemSO.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(itemRoot, ItemPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[MN7 Store] Shared item action prefab created and assigned successfully.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(itemRoot);
            }
        }

        private static GameObject CloneButton(Button source, Transform parent, string name, string label)
        {
            GameObject result;
            if (source != null)
            {
                result = Object.Instantiate(source.gameObject, parent);
                result.name = name;
            }
            else
            {
                result = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                result.transform.SetParent(parent, false);
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(result.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
            }

            TMP_Text text = FindLabel(result.transform);
            if (text == null)
            {
                GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(result.transform, false);
                RectTransform textRect = textObject.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                text = textObject.GetComponent<TMP_Text>();
            }
            text.text = label;
            text.raycastTarget = false;
            return result;
        }

        private static void ConfigureLayoutElement(GameObject button)
        {
            LayoutElement element = button.GetComponent<LayoutElement>() ?? button.AddComponent<LayoutElement>();
            element.preferredWidth = 140f;
            element.preferredHeight = 44f;
            element.minWidth = 0f;
            element.minHeight = 0f;
        }

        private static Transform FindDirect(Transform root, string name)
        {
            return root.Find(name);
        }

        private static Button GetButton(Transform transform)
        {
            return transform == null ? null : transform.GetComponent<Button>();
        }

        private static TMP_Text FindLabel(Transform root)
        {
            if (root == null) return null;
            TMP_Text direct = root.Find("Text")?.GetComponent<TMP_Text>();
            if (direct != null) return direct;
            return root.GetComponentInChildren<TMP_Text>(true);
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
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
