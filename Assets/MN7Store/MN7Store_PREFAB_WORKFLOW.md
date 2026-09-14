# MN7 Store Prefab Workflow

The reusable UI is now prefab-first. Runtime code no longer creates category-bar UI objects or changes store-item visual dimensions.

## One-time editor generation

1. Open `Assets/MN7Store/MN7StoreSystem.unity` in Unity.
2. Let Unity finish compiling/importing.
3. Run **MN7 Store → Generate Reusable Prefabs**.
4. The editor tool updates the existing `MN7StoreItem.prefab`, creates/refreshes `MN7StoreCategoryButton.prefab`, creates `MN7StoreCategoryBar.prefab`, creates `MN7StorePanel.prefab` from the working StorePanel hierarchy, and creates `MN7StoreSystem.prefab` from the working store manager.
5. Save the generated assets and inspect the prefabs.

## Generated reusable prefabs

- `Assets/MN7Store/Prefabs/MN7StoreSystem.prefab`
  - Reusable store runtime manager root.
  - Does not include an EventSystem, camera, or ad provider.
- `Assets/MN7Store/Prefabs/MN7StorePanel.prefab`
  - Store UI hierarchy.
  - Contains the category-bar prefab instance and item grid.
- `Assets/MN7Store/Prefabs/MN7StoreCategoryBar.prefab`
  - Owns the horizontal ScrollRect, viewport, content layout, and category-button prefab reference.
- `Assets/MN7Store/Prefabs/MN7StoreCategoryButton.prefab`
  - Owns its Background, Icon, NameText, SelectionVisual, Button, and LayoutElement.
- `Assets/MN7Store/Prefabs/MN7StoreItem.prefab`
  - Owns its visual layout and explicit UI references.
  - Optional OwnedState, SelectionState, and LockedState hooks are present.

## Prefab-first rule

`MN7StoreUI` only controls data binding, filtering, sorting, and optional column-count behaviour. It does not set item width, item height, grid cell size, grid spacing, grid padding, category button size, or category-bar visual layout.

Change visual design in the prefab. For game-specific themes/layouts, use prefab variants.

## IAP flow

The public purchase path is:

`MN7StoreItemUI → MN7PurchaseManager.PurchaseWithIAP → MN7IAPManager → IMN7IAPProvider → Unity IAP`

`MN7PurchaseManager` forwards IAP success/failure events so existing item cards refresh through the same purchase event path as currency/free/ad purchases.

Pending Unity IAP orders are processed by transaction ID. The pending callback no longer attempts to acquire the same item lock that was already acquired by the original purchase request, so pending orders can be acknowledged and recovered correctly.

## Manual game setup after generation

For another game, add `MN7StoreSystem.prefab` and `MN7StorePanel.prefab` to the game scene, assign that game's `MN7StoreConfig` to the manager, and keep `MN7UnityIAPProvider` / ad provider components optional. Do not add an EventSystem from the store package if the game already has one.

If a game wants dedicated Select and Selected buttons, add those visual children to a prefab variant and assign their Button and TMP label references on `MN7StoreItemUI`.
