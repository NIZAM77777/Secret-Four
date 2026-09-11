# MN7 Store Setup Guide

## Runtime hierarchy

Use one persistent store root in the bootstrap scene:

```text
MN7StoreSystem
├── MN7StoreManager
├── MN7UnityIAPProvider          (when IAP is enabled)
├── MN7AdMob
├── Canvas
│   └── StorePanel
│       ├── Header
│       │   └── CloseButton
│       ├── StoreTitle
│       ├── CurrencyDisplay
│       └── Scroll View
│           └── Viewport
│               └── Content
└── EventSystem
```

`Scroll View/Viewport/Content` should start empty. `MN7StoreUI` creates the store item instances at runtime.

## StorePanel

Add `MN7StoreUI` to `StorePanel` and assign:

- `Item Container` -> `Scroll View/Viewport/Content`
- `Item Prefab` -> `MN7StoreItem.prefab`

Do not maintain a second item list on the UI. Items come from `MN7StoreConfig` through the central catalog.

## Store Config

Every game must have a unique `Game ID`, for example:

- `CONNECT_FOUR`
- `TIC_TAC_TOE`
- `MIRROR_ESCAPE`

The effective namespace becomes `MN7_STORE_<GAME_ID>`.

Never ship a game with the placeholder `MN7_STORE_DATA` namespace.

## Save data and migration

The current store save schema is version 2.

Currency, inventory and rewarded-ad progress now write a versioned envelope while still accepting the original unversioned payload. Transaction saves retain their version field and migrate from the previous version automatically.

The migration is intentionally non-destructive: legacy data is read, converted to the current format, and saved back under the same game-specific namespace.

## Item prefab

The reusable item prefab should contain the fields required by `MN7StoreItemUI`:

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

The prefab does not need a specific `MN7ItemData`; the UI assigns item data when it is spawned.

## IAP

Unity In-App Purchasing **5.4.3** is used by the repository.

Add `MN7UnityIAPProvider` to the persistent store bootstrap scene when IAP is enabled. The provider automatically finds `MN7StoreManager`, registers itself, and starts IAP initialization, so no manual provider hookup is required.

The IAP flow is designed around Unity IAP 5's pending-purchase model:

```text
Google Play purchase
        ↓
OnPurchasePending
        ↓
Google transaction ID becomes MN7 transaction ID
        ↓
PaymentCommitted
        ↓
Idempotent reward operations
        ↓
Completed
        ↓
ConfirmPurchase
```

If the app crashes before confirmation, Google Play can redeliver the same pending transaction after the next initialization. MN7 reuses the persisted store transaction ID instead of creating a second transaction, so rewards are not granted twice.

Confirmed non-consumable purchases are also surfaced during purchase fetching so permanent entitlements can be restored after reinstall.

Purchase failures/cancellations release the MN7 purchase lock and return the appropriate IAP result to the caller.

### IAP product rules

- Every IAP item needs a unique `iapProductId`.
- Permanent IAP items are registered as `NonConsumable`.
- Non-permanent IAP items are registered as `Consumable`.
- Never use a fake product ID in a production build.
- Configure the matching product in Google Play Console.

The core store remains provider-agnostic: another provider can implement `IMN7IAPProvider` without changing currency, inventory, rewards or transaction code.

## Runtime diagnostics

In the Unity Editor, select `MN7StoreManager` and use:

- **Validate Store Config**
- **Run Runtime Diagnostics**
- **Debug Print Currency**
- **Debug Print Inventory**
- **Reset Store Data**

These controls are editor-only and are not included as runtime context-menu controls in release builds.

## Release test checklist

1. Validate Store Config and fix every error.
2. Confirm the game has a unique Game ID.
3. Open the store and confirm all configured items spawn exactly once.
4. Test Free purchase.
5. Test Currency purchase.
6. Test CurrencyOrAd purchase using both paths.
7. Test RewardedAd with one required ad and multiple required ads.
8. Test rewarded-ad failure, skipped/not-ready ads and callback completion.
9. Force-close/relaunch around reward processing and verify transaction recovery.
10. Verify a recovered transaction grants each reward exactly once.
11. Verify permanent items cannot be purchased twice.
12. Verify consumables stack correctly.
13. Verify currency cannot overflow or become negative.
14. Verify legacy save data is retained after upgrading the package.
15. Verify two different games using the same package do not share PlayerPrefs data.
16. If IAP is enabled, test success, cancel, failure, pending purchase, crash-before-confirm and restart recovery.
17. Test permanent IAP restoration after clearing local app data/reinstalling the test app with the same Play account.
18. Build a release APK/AAB and test the real monetization providers on a physical device.

## Important limitations

PlayerPrefs is local persistence, not a secure server-authoritative economy. Do not treat client-side currency or inventory as tamper-proof for competitive or high-value economies.

Google Play purchases should ideally be verified server-side before granting high-value entitlements. This MN7 client provider performs local transaction coordination and idempotent fulfillment; it does not implement a secure backend receipt-verification service.
