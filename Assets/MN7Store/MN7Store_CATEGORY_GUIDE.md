# MN7 Store - Category and Grid Guide

## What is configurable

The category system is data-driven. Category order, display name, normal sprite, selected sprite, optional icon, item membership, button size, spacing, and category-bar placement are configurable without changing filtering code.

The permanent `ALL` category is generated automatically and cannot be deleted or moved. Its label and visual fields are exposed in the `ALL Category` section of `MN7StoreConfig`.

## Store Config

Create a `MN7StoreConfig` from:

`Assets > Create > MN7 Store > Store Config`

Set a unique `Game ID` for every game, for example:

- `CONNECT_FOUR`
- `TIC_TAC_TOE`
- `MIRROR_ESCAPE`

When `Game ID` is set, the effective save namespace becomes `MN7_STORE_<GAME_ID>`.

### ALL Category

- `All Category Display Name`
- `All Category Normal Sprite`
- `All Category Selected Sprite`
- `All Category Icon`

### Store Categories

Every category contains:

- `Category ID` - stable internal ID used by item assignments.
- `Display Name` - text shown to players.
- `Sort Order` - lower values appear first.
- `Normal Sprite` - unselected background.
- `Selected Sprite` - optional selected background.
- `Icon` - optional category icon.

`ALL` is not added to this list. It is generated automatically.

## Item assignment

Each `MN7ItemData` can belong to one or more categories through `Category Ids`.

Example:

```text
Special Skin
Category Ids:
- PLAYER_SKINS
- EFFECTS
```

The same item therefore appears in both category views without creating duplicate item entries.

## Category button prefab

Reusable prefab:

`Assets/MN7Store/Resources/MN7StoreCategoryButton.prefab`

Structure:

```text
CategoryButton
├── Background
├── Icon
├── NameText
└── SelectionVisual
```

`MN7StoreCategoryButtonUI` controls presentation and selection state. Artwork remains editable in the prefab and category data.

The Store UI automatically loads this prefab from `Resources` when the `Category Button Prefab` field is empty. It can also be assigned manually in the `MN7StoreUI` Inspector.

## Category bar layout

The category bar is a sibling of the item ScrollView, not a child of the scrolling item `Content`.

```text
Store Header
     |
Category Bar  <- stays visible
     |
Item ScrollView
     |
Item Viewport
     |
Item Content -> GridLayoutGroup
```

Default behavior automatically places the bar above the item ScrollView and reserves space for it. For manual placement, assign `Category Bar Parent`, disable `Auto Position Category Bar`, then use `Category Bar Manual Offset` and `Category Bar Height`.

The bar uses `ScrollRect` + `HorizontalLayoutGroup` + `ContentSizeFitter`, so additional categories can be horizontally swiped on Android.

## Item grid

`MN7StoreUI` exposes:

- `Columns` or automatic column calculation.
- `Item Width`
- `Item Height`
- horizontal/vertical spacing
- content padding
- selected-first and owned-first sorting
- manual sort priority controls

The item `Content` is sized vertically from the grid's preferred height.

## Reusable item prefab

The reusable item prefab is:

`Assets/MN7Store/Prefabs/MN7StoreItem.prefab`

The prefab contains the visual and interaction hooks used by `MN7StoreItemUI`. It does not contain game-specific item data; the Store UI supplies the item data at runtime.

## Production integration

The package is provider-agnostic. A game can use only free/currency purchases, add rewarded ads, add Unity IAP, or combine them.

For rewarded ads, the game must provide its own AdMob configuration and real placement/unit IDs.

For IAP, the game must create/configure its own Unity IAP products and assign matching `iapProductId` values on the store items.

Store save data is isolated by `Game ID`; do not use the fallback `MN7_STORE_DATA` namespace in a shipped game.

## Release validation

Before shipping a game that uses MN7 Store:

1. Validate the Store Config and fix every error.
2. Confirm the Game ID is unique.
3. Verify all configured items and category IDs.
4. Test the purchase methods actually enabled by the game.
5. Test selection and persistence when selectable items are used.
6. Test rewarded-ad success/failure when ads are used.
7. Test IAP success/pending/failure/recovery when IAP is used.
8. Test a release build on a physical device.
