# MN7 Store — Use This In Every Game

MN7 Store is a reusable Unity store system for MN7 Studios games.

Use the **same MN7 Store code** in every game, but give every game its own configuration, IDs, store UI, ads and IAP products.

---

## 1. What you need to do for EVERY new game

Follow this checklist for every game that uses MN7 Store.

### A. Add the MN7 Store system

Copy/import the MN7 Store package into the game.

Use one persistent store root in the game's bootstrap/starting scene:

```text
MN7StoreSystem
├── MN7StoreManager
├── MN7UnityIAPProvider       ← ONLY when this game uses IAP
├── MN7AdMob                  ← when this game uses MN7/AdMob ads
├── Canvas
│   └── StorePanel
└── EventSystem
```

Do not create multiple `MN7StoreManager` objects.

### B. Create a NEW Store Config

Create a separate `MN7StoreConfig` asset for this game.

Every game must have its own:

- `MN7StoreConfig`
- `Game ID`
- currency definitions
- item definitions
- ad settings/placement configuration
- IAP product IDs (when IAP is used)
- store UI/prefab setup

Example Game IDs:

```text
CONNECT_FOUR
TIC_TAC_TOE
MIRROR_ESCAPE
```

### C. Set a unique Game ID

This is REQUIRED.

Example:

```text
Game ID = CONNECT_FOUR
```

Never leave it empty.

Never use the same Game ID for two different games.

The store uses the Game ID to create the game's save-data namespace, so currency, inventory, rewarded-ad progress and transactions stay separated between games.

### D. Do NOT use the default save key

Do not ship with:

```text
MN7_STORE_DATA
```

The Game ID is what makes the save namespace game-specific.

### E. Configure only the items that this game needs

In `MN7StoreConfig`, add the currencies and store items for that game.

For every item, check:

- Item ID is unique.
- Item has the correct reward.
- Purchase type is correct.
- Price/settings are correct.
- Permanent/consumable ownership is correct.
- IAP Product ID is set only when the purchase type is IAP.

### F. Configure the store UI

Add `MN7StoreUI` to the game's `StorePanel`.

Assign:

```text
Item Container → Scroll View/Viewport/Content
Item Prefab    → MN7StoreItem.prefab
```

The Content object should start empty. MN7 Store creates the item cards at runtime from the store config.

Typical prefab structure:

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

### G. Configure ads when the game uses ads

Set up the game's AdMob/advertising configuration separately from other games.

Do not reuse another game's production ad IDs.

Test rewarded ads on a real device before release.

### H. Configure IAP when the game uses IAP

For each IAP item:

1. Create the product in that game's Google Play Console.
2. Give it the exact product ID you want to use.
3. Put the same product ID in that game's `MN7StoreConfig`.
4. Make sure every IAP product ID is unique within the game.
5. Add `MN7UnityIAPProvider` to the persistent store bootstrap scene.
6. Test purchase, cancellation, failure, pending purchase and restart recovery on a real device.

Permanent items use `NonConsumable` IAP. Other supported item purchases use `Consumable` IAP.

### I. Validate before testing

In the Unity Editor, select `MN7StoreManager` and run:

```text
Validate Store Config
```

Fix every error before release.

Then use:

```text
Run Runtime Diagnostics
```

The store should initialize successfully.

---

# 2. IMPORTANT — If a game DOES NOT use IAP

You do **not** need to remove the entire MN7 Store system.

Keep the normal store systems such as:

- `MN7StoreManager`
- `MN7StoreConfig`
- Currency
- Inventory
- Rewards
- Transactions/recovery
- Store Catalog
- Store UI
- Ad system, if the game uses ads
- Free/Currency/Rewarded-Ad purchase types that the game actually uses

### Remove/skip these IAP-specific parts for that game

#### 1. Do NOT add `MN7UnityIAPProvider`

Remove it from the game's scene if it was copied from another game.

```text
MN7UnityIAPProvider  ← remove when IAP is not used
```

#### 2. Do NOT create IAP items in `MN7StoreConfig`

Do not create store items whose purchase type is:

```text
IAP
```

Use the purchase types the game actually needs, such as Free, Currency or Rewarded Ad.

#### 3. Do NOT enter Google Play IAP product IDs

No IAP products means no `iapProductId` setup is needed for that game.

#### 4. Do NOT create Google Play products for MN7 Store

There is nothing to configure in Play Console for MN7 Store IAP when that game does not sell IAP items.

#### 5. IAP testing can be skipped

You do not need to test:

- IAP purchase success
- IAP cancellation
- IAP failure
- pending IAP recovery
- IAP restoration

You still need to test all non-IAP store features that the game actually uses.

### Keep the IAP package/code or remove it?

For a normal Unity project, the safest reusable setup is to leave the MN7 Store package code available but simply **not add/use `MN7UnityIAPProvider` and not configure any IAP items**.

If you intentionally want a smaller game/package, IAP support can be removed from the project, but then the Unity Purchasing package and any IAP-specific assembly/reference setup must also be removed consistently. Do not delete only one IAP script while leaving broken references behind.

---

# 3. What is different for every game?

| Setting | Same across games? | New for every game? |
|---|---|---|
| MN7 Store code | Yes | No |
| `MN7StoreManager` code | Yes | No |
| `MN7StoreConfig` asset | No | **Yes** |
| Game ID | No | **Yes** |
| Save namespace | No | **Yes, automatically from Game ID** |
| Currencies/items | No | **Yes** |
| Store UI | Can be reused | Usually **game-specific** |
| AdMob IDs/config | No | **Yes** |
| IAP product IDs | No | **Yes, when IAP is used** |
| Google Play products | No | **Yes, when IAP is used** |
| Game scenes | No | **Yes** |

---

# 4. Simple setup flow for a new game

Use this order every time:

```text
1. Add MN7 Store
        ↓
2. Add one MN7StoreManager
        ↓
3. Create a NEW MN7StoreConfig
        ↓
4. Set a UNIQUE Game ID
        ↓
5. Add this game's currencies/items
        ↓
6. Set up the Store UI
        ↓
7. Set up Ads (if used)
        ↓
8. Set up IAP (only if used)
        ↓
9. Validate Store Config
        ↓
10. Run Runtime Diagnostics
        ↓
11. Test purchases + persistence + recovery
        ↓
12. Test on a real Android device
        ↓
13. Build release APK/AAB
```

---

# 5. Before publishing each game

Check all of these:

```text
[ ] Unique Game ID
[ ] No default MN7_STORE_DATA save namespace
[ ] Correct currencies
[ ] Correct inventory items
[ ] Correct purchase types
[ ] Correct rewards
[ ] Store UI works
[ ] No duplicate store items
[ ] Currency persistence works
[ ] Inventory persistence works
[ ] Rewarded-ad progress works (if used)
[ ] Transaction recovery works
[ ] AdMob IDs are this game's IDs (if ads are used)
[ ] IAP products match Play Console (if IAP is used)
[ ] IAP success/cancel/failure works (if IAP is used)
[ ] IAP pending/recovery/restoration tested (if IAP is used)
[ ] Physical-device test completed
[ ] Release build tested
```

---

# 6. Important save-data rule

Never give two games the same Game ID.

For example, do NOT use:

```text
Game A → CONNECT_FOUR
Game B → CONNECT_FOUR
```

Use:

```text
Game A → CONNECT_FOUR
Game B → MIRROR_ESCAPE
```

This keeps each game's MN7 Store PlayerPrefs namespace separate.

---

# 7. Important security note

MN7 Store uses local PlayerPrefs persistence. PlayerPrefs is not a secure server-authoritative economy.

For normal casual game stores it can be used as local persistence, but high-value or competitive economies should use server-side validation/authority.

Google Play IAP should ideally be verified server-side before granting high-value entitlements. MN7 Store currently handles local transaction coordination, recovery and idempotent fulfillment; it does not provide a secure backend receipt-verification service.

---

# 8. Files you normally work with for each game

You normally configure these game-side assets/components:

```text
MN7StoreConfig
MN7StoreManager
MN7StoreUI
MN7StoreItem.prefab
MN7AdMob / ad provider configuration (when ads are used)
MN7UnityIAPProvider (when IAP is used)
```

The core MN7 Store scripts should normally remain unchanged when creating another game. Change the **game configuration**, not the shared store logic.

---

# 9. One sentence to remember

**Same MN7 Store code, new Game ID + new Store Config + game-specific ads/IAP/UI for every game.**
