using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MN7.Store
{
    /// <summary>Reusable store presentation controller. Visual layout belongs to the Store Panel and its prefabs.</summary>
    public class MN7StoreUI : MonoBehaviour
    {
        [Header("Store References")]
        [SerializeField] private RectTransform itemContainer;
        [SerializeField] private GridLayoutGroup itemGrid;
        [SerializeField] private ContentSizeFitter itemContentFitter;
        [SerializeField] private MN7StoreItemUI itemPrefab;
        [SerializeField] private MN7StoreCategoryBarUI categoryBar;
        [Header("Grid Behaviour")]
        [SerializeField, Min(1)] private int columns = 3;
        [SerializeField] private bool autoColumns;
        [Header("Item Sorting")]
        [Tooltip("Move the selected item to the beginning of the visible list.")][SerializeField] private bool selectedFirst = true;
        [Tooltip("Move owned items before unowned items.")][SerializeField] private bool ownedFirst = true;
        [SerializeField] private bool useManualSortPriority = true;
        [SerializeField] private bool manualPriorityDescending;
        private readonly List<MN7StoreItemUI> spawnedItems = new();
        private Coroutine waitForStoreCoroutine;
        private Coroutine refreshLayoutCoroutine;
        private string selectedCategoryId = MN7StoreConfig.AllCategoryId;

        private void Awake(){CacheReferences();ValidateReferences();}
        private void OnEnable()=>BeginStoreInitializationWait();
        public void Close()=>gameObject.SetActive(false);
        private void OnDisable(){UnsubscribeFromStore();if(waitForStoreCoroutine!=null){StopCoroutine(waitForStoreCoroutine);waitForStoreCoroutine=null;}if(refreshLayoutCoroutine!=null){StopCoroutine(refreshLayoutCoroutine);refreshLayoutCoroutine=null;}}
        private void BeginStoreInitializationWait(){if(waitForStoreCoroutine!=null)StopCoroutine(waitForStoreCoroutine);waitForStoreCoroutine=StartCoroutine(WaitForStoreManager());}
        private IEnumerator WaitForStoreManager(){while(MN7StoreManager.Instance==null)yield return null;MN7StoreManager store=MN7StoreManager.Instance;if(store.IsReady)BuildStore();else{store.OnStoreInitialized-=HandleStoreInitialized;store.OnStoreInitialized+=HandleStoreInitialized;}waitForStoreCoroutine=null;}
        private void HandleStoreInitialized()=>BuildStore();
        private void SubscribeToStoreEvents(MN7StoreManager store){if(store==null)return;if(store.Inventory!=null){store.Inventory.OnItemAdded-=HandleInventoryChanged;store.Inventory.OnItemRemoved-=HandleInventoryChanged;store.Inventory.OnItemQuantityChanged-=HandleInventoryQuantityChanged;store.Inventory.OnItemAdded+=HandleInventoryChanged;store.Inventory.OnItemRemoved+=HandleInventoryChanged;store.Inventory.OnItemQuantityChanged+=HandleInventoryQuantityChanged;}if(store.Selection!=null){store.Selection.OnSelectionChanged-=HandleSelectionChanged;store.Selection.OnSelectionChanged+=HandleSelectionChanged;}}
        private void UnsubscribeFromStore(){MN7StoreManager store=MN7StoreManager.Instance;if(store==null)return;store.OnStoreInitialized-=HandleStoreInitialized;if(store.Inventory!=null){store.Inventory.OnItemAdded-=HandleInventoryChanged;store.Inventory.OnItemRemoved-=HandleInventoryChanged;store.Inventory.OnItemQuantityChanged-=HandleInventoryQuantityChanged;}if(store.Selection!=null)store.Selection.OnSelectionChanged-=HandleSelectionChanged;}
        private void HandleInventoryChanged(string itemId)=>RefreshCurrentFilterAndSort();
        private void HandleInventoryQuantityChanged(string itemId,int quantity)=>RefreshCurrentFilterAndSort();
        private void HandleSelectionChanged(string groupId,string itemId)=>RefreshCurrentFilterAndSort();
        private void RefreshCurrentFilterAndSort(){if(isActiveAndEnabled&&spawnedItems.Count>0)ApplyCategoryFilter(selectedCategoryId);}
        public void BuildStore(){ClearStore();CacheReferences();MN7StoreManager store=MN7StoreManager.Instance;if(store==null||!store.IsReady||itemContainer==null||itemGrid==null||itemPrefab==null||categoryBar==null||store.Catalog==null){Debug.LogError("[MN7 Store] Store UI cannot build. Assign Item Container, Item Grid, Content Size Fitter, Item Prefab and Category Bar on the Store Panel prefab.");return;}SubscribeToStoreEvents(store);ConfigureItemGrid();categoryBar.Initialize(store.Config,selectedCategoryId,SelectCategory);foreach(MN7ItemData item in store.Catalog.GetItems()){if(item==null)continue;MN7StoreItemUI card=Instantiate(itemPrefab,itemContainer);if(card==null)continue;card.SetItem(item);spawnedItems.Add(card);}ApplyCategoryFilter(selectedCategoryId);if(refreshLayoutCoroutine!=null)StopCoroutine(refreshLayoutCoroutine);refreshLayoutCoroutine=StartCoroutine(RefreshLayoutNextFrame());}
        private IEnumerator RefreshLayoutNextFrame(){yield return null;Canvas.ForceUpdateCanvases();ConfigureItemGrid();refreshLayoutCoroutine=null;}
        private void ConfigureItemGrid(){if(itemGrid==null)return;if(autoColumns){ScrollRect itemScroll=itemContainer!=null?itemContainer.GetComponentInParent<ScrollRect>():null;RectTransform viewport=itemScroll!=null?itemScroll.viewport:null;float width=viewport!=null?viewport.rect.width:itemContainer.rect.width;float cellWidth=Mathf.Max(1f,itemGrid.cellSize.x);float spacing=Mathf.Max(0f,itemGrid.spacing.x);float available=Mathf.Max(1f,width-itemGrid.padding.left-itemGrid.padding.right+spacing);itemGrid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;itemGrid.constraintCount=Mathf.Max(1,Mathf.FloorToInt(available/(cellWidth+spacing)));}else{itemGrid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;itemGrid.constraintCount=Mathf.Max(1,columns);}if(itemContentFitter!=null)itemContentFitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;}
        private void SelectCategory(string categoryId){selectedCategoryId=string.IsNullOrWhiteSpace(categoryId)?MN7StoreConfig.AllCategoryId:categoryId.Trim();ApplyCategoryFilter(selectedCategoryId);}
        private void ApplyCategoryFilter(string categoryId){MN7StoreManager store=MN7StoreManager.Instance;if(store==null||store.Config==null)return;List<MN7StoreItemUI> visible=new();foreach(MN7StoreItemUI card in spawnedItems){if(card==null||card.ItemData==null)continue;bool matches=IsAllCategory(categoryId)||ContainsCategory(card.ItemData,categoryId);card.gameObject.SetActive(matches);if(matches)visible.Add(card);}SortVisibleItems(visible,store);for(int i=0;i<visible.Count;i++)visible[i].transform.SetSiblingIndex(i);}
        private static bool ContainsCategory(MN7ItemData item,string categoryId){if(item?.categoryIds==null||string.IsNullOrWhiteSpace(categoryId))return false;foreach(string id in item.categoryIds)if(string.Equals(id?.Trim(),categoryId.Trim(),StringComparison.OrdinalIgnoreCase))return true;return false;}
        private static bool IsAllCategory(string categoryId)=>string.Equals(categoryId,MN7StoreConfig.AllCategoryId,StringComparison.OrdinalIgnoreCase);
        private void SortVisibleItems(List<MN7StoreItemUI> items,MN7StoreManager store){items.Sort((a,b)=>{MN7ItemData ia=a.ItemData,ib=b.ItemData;bool ao=store.Inventory!=null&&store.Inventory.IsOwned(ia.itemId),bo=store.Inventory!=null&&store.Inventory.IsOwned(ib.itemId);bool ass=ao&&ia.selectable&&store.Selection!=null&&store.Selection.IsSelected(ia),bss=bo&&ib.selectable&&store.Selection!=null&&store.Selection.IsSelected(ib);if(selectedFirst&&ass!=bss)return ass?-1:1;if(ownedFirst&&ao!=bo)return ao?-1:1;if(useManualSortPriority&&ia.sortPriority!=ib.sortPriority)return manualPriorityDescending?ib.sortPriority.CompareTo(ia.sortPriority):ia.sortPriority.CompareTo(ib.sortPriority);if(ia.customSortWeight!=ib.customSortWeight)return ib.customSortWeight.CompareTo(ia.customSortWeight);return string.Compare(ia.displayName,ib.displayName,StringComparison.OrdinalIgnoreCase);});}
        public void RefreshStore(){if(MN7StoreManager.Instance==null||!MN7StoreManager.Instance.IsReady)return;BuildStore();}
        private void ClearStore(){foreach(MN7StoreItemUI card in spawnedItems)if(card!=null)Destroy(card.gameObject);spawnedItems.Clear();}
        private void CacheReferences(){if(itemContainer==null)itemContainer=transform.Find("Scroll View/Viewport/Content") as RectTransform;if(itemGrid==null&&itemContainer!=null)itemGrid=itemContainer.GetComponent<GridLayoutGroup>();if(itemContentFitter==null&&itemContainer!=null)itemContentFitter=itemContainer.GetComponent<ContentSizeFitter>();if(itemPrefab==null)itemPrefab=Resources.Load<MN7StoreItemUI>("MN7StoreItem");if(categoryBar==null)categoryBar=transform.Find("CategoryBar")?.GetComponent<MN7StoreCategoryBarUI>();if(categoryBar==null)categoryBar=transform.Find("Scroll View/CategoryBar")?.GetComponent<MN7StoreCategoryBarUI>();}
        private void ValidateReferences(){if(itemContainer==null)Debug.LogError("[MN7 Store] Store Panel is missing Item Container.");if(itemGrid==null)Debug.LogError("[MN7 Store] Store Panel Item Container is missing GridLayoutGroup.");if(itemContentFitter==null)Debug.LogError("[MN7 Store] Store Panel Item Container is missing ContentSizeFitter.");if(itemPrefab==null)Debug.LogError("[MN7 Store] Store Panel is missing Item Prefab.");if(categoryBar==null)Debug.LogError("[MN7 Store] Store Panel is missing Category Bar.");}
    }
}
