# Secret Four — MN7 Store

This is the Secret Four-specific store implementation.

The store provides only:

- Free items
- Rewarded-ad items
- Permanent or consumable ownership
- Item inventory
- Item rewards
- Rewarded-ad progress
- AdMob rewarded ads
- Local save/load
- Transaction tracking and recovery

## Purchase model

Every store item uses one of these purchase types:

```text
Free
RewardedAd
```

### Free item

The player selects the item and receives its configured item reward immediately.

### Rewarded-ad item

The player watches the required number of rewarded ads. Progress is saved between sessions. When the required number is reached, the configured item reward is granted and progress resets.

## Secret Four identity

The store uses:

```text
Game ID: SECRET_FOUR
Save namespace: MN7_STORE_SECRET_FOUR
```

This keeps Secret Four store data isolated from other MN7 Studios games.

## Store manager

Use one persistent `MN7StoreManager` in the game's bootstrap/main scene.

It creates and manages:

```text
MN7StoreManager
├── Store Catalog
├── Purchase Manager
├── Inventory Manager
├── Reward Manager
├── Rewarded-Ad Progress
├── Ad Manager
├── Transaction Manager
└── Transaction Recovery
```

Do not create these managers manually.

## AdMob

For rewarded store items, keep the AdMob bootstrap configured for Secret Four:

```text
MN7AdMob
└── MN7AdMobBootstrap
```

Assign the Secret Four `MN7AdMobConfig` and use the appropriate rewarded-ad placement IDs.

Use test ad IDs during development and production IDs only for the release build.

## Store UI

Recommended structure:

```text
Canvas
└── StorePanel
    ├── Header
    │   ├── CloseButton
    │   └── StoreTitle
    │
    └── Scroll View
        └── Viewport
            └── Content
```

Add `MN7StoreUI` to `StorePanel` and assign:

```text
Item Container → Content
Item Prefab    → MN7StoreItem
```

## Store item card

The item prefab can contain:

```text
MN7StoreItem
├── Icon
├── NameText
├── DescriptionText
├── PriceText
├── StatusText
├── BuyButton
└── WatchAdButton
```

The UI automatically shows the appropriate action for the configured purchase type.

## Inventory

Items are saved locally using the Secret Four store namespace.

Permanent items can only be owned once.

Consumable items can be granted repeatedly when configured as consumable.

## Transactions and recovery

Free and rewarded-ad fulfillment uses local transactions so a reward can be recovered safely if the application closes during fulfillment.

Reward operations are idempotent, preventing the same transaction reward from being granted twice.

## Opening the store

The game controls the store panel:

```text
Store Button
    ↓
StorePanel.SetActive(true)

Close Button
    ↓
StorePanel.SetActive(false)
```

## Validation

In Unity, select `MN7StoreManager` and run:

```text
Validate Store Config
```

Then run:

```text
Run Runtime Diagnostics
```

The store should initialize successfully.

## Testing checklist

```text
[ ] Store opens
[ ] Store closes
[ ] Item cards appear correctly
[ ] Free item works
[ ] Free reward is granted correctly
[ ] Rewarded ad loads
[ ] Rewarded ad completion advances progress
[ ] Required ad count is respected
[ ] Reward is granted after the required ads
[ ] Inventory updates
[ ] Inventory persists after restart
[ ] Ad progress persists after restart
[ ] Failed/not-ready ads grant nothing
[ ] Duplicate permanent ownership is blocked
[ ] Android Back closes the store correctly
[ ] Real Android device tested
```

## Final architecture

```text
Secret Four
     ↓
MN7StoreManager
     ↓
MN7StoreConfig
     ↓
Store Catalog
     ↓
┌───────────────────┐
│ Free Items        │
│ Rewarded-Ad Items │
└───────────────────┘
     ↓
MN7PurchaseManager
     ↓
MN7RewardManager
     ↓
MN7InventoryManager
     ↓
Local Save
```

**Secret Four's store is intentionally limited to free items and rewarded-ad items.**
