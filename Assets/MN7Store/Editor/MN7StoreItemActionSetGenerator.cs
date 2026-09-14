#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store.Editor
{
    /// <summary>
    /// Converts the existing Store Item buttons into one reusable Action Buttons prefab.
    /// Run once after importing/updating MN7 Store.
    /// </summary>
    public static class MN7StoreItemActionSetGenerator
    {
        private const string ItemPath = "Assets/MN7Store/Prefabs/MN7StoreItem.prefab";
        private const string ActionPath = "Assets/MN7Store/Prefabs/MN7StoreItemActions.prefab";

        [MenuItem("MN7 Store/Generate Item Action Buttons")]
        public static void Generate()
        {
            if (AssetDatabase.LoadAssetAtPath<MN7StoreItemUI>(ItemPath) == null)
            {
                Debug.LogError("[MN7 Store] MN7StoreItem.prefab was not found.");
                return;
            }

            GameObject itemRoot = PrefabUtility.LoadPrefabContents(ItemPath);
            try
            {
                MN7StoreItemActionSetUI existingActionSet = itemRoot.GetComponentInChildren<MN7StoreItemActionSetUI>(true);
                if (existingActionSet != null)
                {
                    PrefabUtility.SaveAsPrefabAsset(itemRoot, ItemPath);
                    Debug.Log("[MN7 Store] Item Action Buttons are already configured.");
                    return;
                }

                Transform oldBuy = itemRoot.transform.Find("BuyButton");
                Transform oldAd = itemRoot.transform.Find("AdButton");
                if (oldBuy == null || oldAd == null)
                {
                    Debug.LogError("[MN7 Store] Existing BuyButton and AdButton are required.");
                    return;
                }

                GameObject actionObject = new GameObject("ActionButtons", typeof(RectTransform), typeof(MN7StoreItemActionSetUI));
                actionObject.transform.SetParent(itemRoot.transform, false);
                RectTransform actionRect = actionObject.GetComponent<RectTransform>();
                actionRect.anchorMin = Vector2.zero;
                actionRect.anchorMax = Vector2.one;
                actionRect.offsetMin = Vector2.zero;
                actionRect.offsetMax = Vector2.zero;

                MoveChild(oldBuy, actionObject.transform);
                MoveChild(oldAd, actionObject.transform);

                GameObject select = Object.Instantiate(oldBuy.gameObject, actionObject.transform);
                select.name = "SelectButton";

                GameObject selected = Object.Instantiate(oldBuy.gameObject, actionObject.transform);
                selected.name = "SelectedButton";

                SetLabel(select, "SELECT");
                SetLabel(selected, "SELECTED");

                // Only one state is visible at runtime. Keep the default purchase buttons visible in the prefab for editing.
                select.SetActive(false);
                selected.SetActive(false);

                MN7StoreItemActionSetUI actionSet = actionObject.GetComponent<MN7StoreItemActionSetUI>();
                SerializedObject so = new SerializedObject(actionSet);
                SetRef(so, "buyButton", actionObject.transform.Find("BuyButton")?.GetComponent<Button>());
                SetRef(so, "buyLabel", FindLabel(actionObject.transform.Find("BuyButton")));
                SetRef(so, "buyWithAdButton", actionObject.transform.Find("AdButton")?.GetComponent<Button>());
                SetRef(so, "buyWithAdLabel", FindLabel(actionObject.transform.Find("AdButton")));
                SetRef(so, "selectButton", select.GetComponent<Button>());
                SetRef(so, "selectLabel", FindLabel(select.transform));
                SetRef(so, "selectedButton", selected.GetComponent<Button>());
                SetRef(so, "selectedLabel", FindLabel(selected.transform));
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(actionObject, ActionPath);

                // Replace the in-item buttons with the reusable action prefab instance.
                Object.DestroyImmediate(actionObject);
                GameObject actionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ActionPath);
                PrefabUtility.InstantiatePrefab(actionPrefab, itemRoot.transform);

                PrefabUtility.SaveAsPrefabAsset(itemRoot, ItemPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[MN7 Store] Item Action Buttons generated and connected successfully.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(itemRoot);
            }
        }

        private static void MoveChild(Transform child, Transform newParent)
        {
            child.SetParent(newParent, false);
        }

        private static TMP_Text FindLabel(Transform button)
        {
            return button == null ? null : button.GetComponentInChildren<TMP_Text>(true);
        }

        private static void SetLabel(GameObject button, string value)
        {
            TMP_Text label = FindLabel(button.transform);
            if (label != null) label.text = value;
        }

        private static void SetRef(SerializedObject so, string propertyName, Object value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null) property.objectReferenceValue = value;
        }
    }
}
#endif
