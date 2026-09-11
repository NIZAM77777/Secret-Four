# MN7 Store System — Secret Four Non-IAP Setup

A reusable Unity store system for MN7 Studios games.

For **Secret Four**, the store uses only:

- Free items
- Rewarded Ad purchases
- Permanent or consumable ownership
- Inventory
- Rewards
- Save/Load
- Transaction recovery for local rewarded/free fulfillment
- AdMob rewarded ads

**IAP / real-money purchasing is intentionally disabled for Secret Four.**

---

# 1. Secret Four Purchase Model

Secret Four does **not** use:

- Unity IAP
- Google Play Billing products for MN7 Store items
- IAP provider components
- IAP product IDs
- Currency-paid store items

Store items should use only:

```text
Free
RewardedAd
```

Do not create items using `IAP`.

---

# 2. Store Manager

Use one persistent `MN7StoreManager` in the game's bootstrap/main scene.

The manager automatically creates:

- Currency
- Inventory
- Purchases
- Rewards
- Transactions
- Recovery
- Ads

No separate IAP manager/provider is required for Secret Four.

---

# 3. Game Identity

The Secret Four store configuration uses the unique game ID:

```text
SECRET_FOUR
```

The store derives the save namespace from this Game ID so the store data is separated from other MN7 games.

Do not ship with the generic save namespace:

```text
MN7_STORE_DATA
```

---

# 4. Store Items

Create store items with:

```text
Create → MN7 Store → Store Item
```

For each item configure:

- Item ID
- Display Name
- Description
- Icon
- Purchase Type
- Rewarded Ad settings when applicable
- Rewards
- Ownership Type

### Free item

Set:

```text
Purchase Type = Free
```

The player receives the configured reward without payment.

### Rewarded Ad item

Set:

```text
Purchase Type = RewardedAd
```

Configure:

```text
Required Ads = number of rewarded ads required
Ad Placement ID = the rewarded-ad placement key
```

The player watches the required rewarded ads, then the reward is granted.

---

# 5. IAP Rules

For Secret Four:

```text
IAP = OFF
```

Never add:

```text
MN7UnityIAPProvider
```

Never configure an IAP product ID.

Never create an `IAP` store item.

There is nothing to configure for MN7 Store IAP in Google Play Console.

---

# 6. Rewarded Ads / AdMob

Secret Four uses AdMob for rewarded-ad store purchases.

Create/keep the store AdMob bootstrap object:

```text
MN7AdMob
└── MN7AdMobBootstrap
```

Assign the game's `MN7AdMobConfig`.

The rewarded placement ID configured on a store item must match a placement handled by the game's ad setup.

Use test ad IDs while developing and the game's production IDs for release.

---

# 7. Store UI

Recommended structure:

```text
Canvas
└── StorePanel
    ├── Header
    │   ├── CloseButton
    │   ├── StoreTitle
    │   └── CurrencyDisplay (optional)
    │
    └── Scroll View
        └── Viewport
            └── Content
```

Add `MN7StoreUI` to `StorePanel`.

Assign:

```text
Item Container → Content
Item Prefab    → MN7StoreItem.prefab
```

The store UI creates item cards from the configured store items.

---

# 8. Store Item Prefab

Use the reusable `MN7StoreItem` prefab.

Typical elements:

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

For Secret Four:

- Free items should expose the normal free-purchase action.
- Rewarded-Ad items should expose the Watch Ad action.
- IAP purchase controls should not be used.

---

# 9. Opening and Closing the Store

The game controls the `StorePanel`.

Example:

```text
Store Button
    ↓
StorePanel.SetActive(true)

Close Button
    ↓
StorePanel.SetActive(false)
```

The store UI handles the item list and purchase interaction.

---

# 10. Secret Four Configuration

The imported reusable package contains a store config under:

```text
Assets/MN7Store/Resources/MN7StoreConfig.asset
```

Its Game ID is configured for:

```text
SECRET_FOUR
```

The store item and currency lists should contain only the assets Secret Four actually uses.

Do not add IAP items.

---

# 11. Validation

In Unity, select `MN7StoreManager` and run:

```text
Validate Store Config
```

Fix every validation error.

Then run:

```text
Run Runtime Diagnostics
```

The store should initialize successfully.

For Secret Four, validation should be based on the non-IAP item types actually configured.

---

# 12. What Gets Saved

The store can persist locally:

- Store currency data, when used
- Inventory ownership/quantities
- Rewarded-ad progress
- Transaction state

The store uses the Secret Four Game ID to keep this data in the game's store namespace.

This is local persistence, not server-authoritative economy data.

---

# 13. Testing Checklist

Before release, test:

```text
[ ] Store opens
[ ] Store closes
[ ] All item cards appear correctly
[ ] Free item purchase works
[ ] Free reward is granted once as configured
[ ] Rewarded ad loads
[ ] Rewarded ad completion advances progress
[ ] Required number of ads is respected
[ ] Reward is granted after required ads
[ ] Inventory updates correctly
[ ] Inventory persists after restart
[ ] Rewarded-ad progress persists after restart
[ ] Failed/not-ready ad does not grant the reward
[ ] Duplicate purchase is blocked according to ownership rules
[ ] No IAP button/path is exposed
[ ] Android Back closes the store correctly
[ ] Real Android device test completed
```

---

# 14. Simple Secret Four Flow

```text
Secret Four
     ↓
MN7StoreManager
     ↓
MN7StoreConfig
     ↓
Store Catalog
     ↓
┌──────────────────────┐
│ Free Items           │
│ Rewarded-Ad Items    │
└──────────────────────┘
     ↓
MN7PurchaseManager
     ↓
Rewards + Inventory
     ↓
Local Store Save
```

Rewarded-ad path:

```text
Store Item
     ↓
Watch Ad
     ↓
MN7AdManager
     ↓
AdMob Rewarded Ad
     ↓
Progress Complete
     ↓
Reward Granted
     ↓
Inventory / Currency Updated
```

---

# 15. One Rule to Remember

**For Secret Four: MN7 Store = Free Items + Rewarded Ads only. No IAP, no Google Play store products, and no IAP provider.**
