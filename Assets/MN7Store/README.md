# MN7 Store — Reusable Unity Mobile Store

MN7 Store is a reusable, configuration-driven store system for Unity mobile games.

**System = how the store works. Data = what each game sells. Prefabs = how the store looks.**

Each game should provide its own Store Config, items, categories, currencies and optional Ads/IAP configuration.

---

## 1. Import

Copy/import the `MN7Store` folder into the Unity project.

From `Assets/MN7Store/Prefabs/`:

1. Add `MN7StoreSystem.prefab` to the game's first/bootstrap scene.
2. Add `MN7StorePanel.prefab` to the game's Canvas.
3. Keep only one `MN7StoreManager` active in the game.
4. Do not create a second EventSystem if the game already has one.
5. Store Panel, Category Bar and Item prefabs remain editable.

`MN7StoreSystem` keeps the manager alive between scenes.

---

## 2. Create the game's Store Config

Right-click → **Create → MN7 Store → Store Config**.

Set a unique Game ID, for example:

```text
Game ID = MY_GAME
```

Then select `MN7StoreSystem → MN7StoreManager` and assign that Store Config.

The Game ID creates an isolated store save namespace automatically. Never reuse another game's Game ID.

---

## 3. Currency

If the game uses coins, create:

**Create → MN7 Store → Currency Data**

Example:

```text
Currency ID     = soft_currency
Display Name    = Coins
Starting Amount = 3000
```

Add the currency to the game's Store Config.

If the game does not use coins, set **Coins = OFF** in Store Features. Do not leave currency rewards on items when Coins are disabled.

---

## 4. Categories

Categories are configured inside Store Config.

Set:

```text
Category ID
Display Name
Sort Order
```

`ALL` is automatic. Never add `__ALL__` manually.

Category appearance belongs to the shared `MN7StoreCategoryButton` prefab, not to individual category data.

---

## 5. Store Items

Create:

**Create → MN7 Store → Store Item**

Set the item's:

```text
Item ID
Display Name
Description
Icon
Category IDs
Purchase Type
Rewards
Ownership Type
```

Add each item to the Store Config.

### Free

```text
Purchase Type = Free
```

### Currency

```text
Purchase Type = Currency
Currency ID   = soft_currency
Currency Cost = 100
```

### Rewarded Ad

```text
Purchase Type    = Rewarded Ad
Required Ads     = 1
Ad Placement ID = store_reward
```

### Currency Or Ad

Configure the currency and/or rewarded-ad route. The active Store Features determine which route is available.

### IAP

```text
Purchase Type  = IAP
IAP Product ID = exact store product ID
```

---

## 6. Permanent skins / characters / colors

For an item that should remain owned after purchase:

```text
Ownership Type     = Permanent
Selectable         = ON
Selection Group ID = CharacterSkin
```

A purchasable permanent selectable item must reward itself:

```text
Rewards → Size = 1
Reward Type   = Item
Item ID       = exact same item ID
Quantity      = 1
```

Example:

```text
blue_skin → blue_skin × 1
```

This produces:

```text
Not owned → BUY
Purchase → SELECT
Select → SELECTED
```

For a starting item:

```text
Default Owned    = ON
Default Selected = ON
```

Only one item in a selection group should be Default Selected.

---

## 7. Store Features

On `MN7StoreManager` configure only what the game actually uses:

```text
Free Items = ON/OFF
Ad Rewards = ON/OFF
Coins      = ON/OFF
IAP        = ON/OFF
```

Disabled features do not initialize their managers. Their purchase routes are unavailable and configuration warnings/errors explain what needs attention.

---

## 8. AdMob

Only add/configure the AdMob provider when **Ad Rewards** is enabled.

Create:

**Create → MN7 Store → AdMob Config**

For each rewarded placement configure:

```text
Placement ID
Android Ad Unit ID
iOS Ad Unit ID
```

The Store Item's `Ad Placement ID` must exactly match an AdMob Config placement.

MN7 Store validates duplicate/empty placements and validates the platform ad unit ID at runtime.

The Google Mobile Ads SDK also requires the game's **AdMob App ID** in the Google Mobile Ads plugin settings. The App ID is separate from the MN7 Store rewarded placement IDs.

Use test ads/test devices during development and production IDs only for release builds.

---

## 9. IAP

If IAP is enabled:

1. Set `IAP = ON`.
2. Give every IAP item an exact product ID.
3. Configure the platform store product.
4. Register the game's Unity IAP provider.
5. Initialize IAP after the provider is configured.
6. Test purchases/restores on a real device.

The validator detects empty and duplicate IAP product IDs.

If IAP is not used, keep it OFF and do not configure IAP items/provider.

---

## 10. Store UI

`MN7StorePanel.prefab` contains the editable UI.

The runtime creates item cards from Store Config and creates category buttons from Store Config.

You can edit:

- Store Panel position/size
- Category Bar position/size
- Category button visuals
- Item card visuals
- Fonts
- Sprites
- Grid size/spacing
- Action button artwork

Do not manually create store item cards under Content.

The shared action buttons are bound by code. Their Unity `On Click()` lists intentionally do not need manually assigned functions.

The Store UI also validates its required Item Container, Grid, Content Size Fitter, Item Prefab and Category Bar references.

---

## 11. Connect the selected item to the actual game

MN7 Store does not assume how a game's character/skin system works.

For a UI sprite, add `MN7SelectedSpriteApplier` to the game's target object and set:

```text
Selection Group ID = CharacterSkin
Target Image       = game's Image
```

For a 2D SpriteRenderer, set the SpriteRenderer target.

For a 3D game, the game can read the selected item and apply its own model/material/visual logic:

```csharp
MN7StoreManager.Instance.Selection.GetSelectedItem("CharacterSkin");
```

---

## 12. Validation before shipping

Select `MN7StoreManager` and use:

**Inspector → ⋮ → Validate Store Config**

Fix every red error before shipping.

The validator checks, among other things:

- Game ID/save namespace
- duplicate IDs
- currencies
- categories
- item references
- rewards
- permanent selectable ownership
- selection defaults
- currency routes
- rewarded-ad routes
- IAP product IDs
- disabled feature conflicts

Then use:

**Run Runtime Diagnostics**

Runtime diagnostics are feature-aware: disabled Coins/Ads/IAP are not incorrectly reported as missing managers.

---

## 13. Safe reset for development

Store reset is intentionally Editor-only.

Use:

**MN7StoreManager → Inspector → ⋮ → Reset Store Data**

It resets MN7 Store data for the configured namespace. It does **not** call `PlayerPrefs.DeleteAll()` and therefore does not erase unrelated game data.

---

## 14. Crash-safe transactions

Local currency purchases persist a transaction before the debit and use an idempotent payment operation ID.

If the app stops after the debit but before `PaymentCommitted`, startup recovery can recognize the completed payment operation and continue reward processing.

If the payment operation was never recorded, recovery does not charge the player later.

Reward operations are also idempotent, preventing duplicate rewards during recovery.

This is local crash-window protection. PlayerPrefs is not a secure/server-authoritative economy.

---

## 15. Public API guidance

Use high-level purchase APIs for store purchases:

```text
MN7StoreManager.Instance.Purchases
```

Do not implement your own purchase flow by manually changing inventory/currency and then creating a fake completed transaction.

Use Currency/Inventory APIs for legitimate game systems that award or consume resources, and use the Purchase Manager for store purchases so transaction/reward protection remains intact.

---

## 16. Final mobile release checklist

Before shipping a game using MN7 Store:

```text
[ ] Unique Game ID
[ ] Store Config assigned
[ ] Currency IDs valid
[ ] Categories valid
[ ] Item IDs unique
[ ] Rewards valid
[ ] Permanent selectable items reward themselves ×1
[ ] Selection defaults valid
[ ] Features match the game's purchase routes
[ ] AdMob placements match item Placement IDs
[ ] Android/iOS ad unit IDs configured for the target platform
[ ] AdMob App ID configured in Google Mobile Ads settings
[ ] IAP product IDs exact and unique (if used)
[ ] Runtime Diagnostics pass
[ ] Store UI references pass
[ ] Store persists after app restart
[ ] Coin purchase tested
[ ] Free purchase tested
[ ] Rewarded-ad purchase tested
[ ] Selection tested
[ ] Category filtering tested
[ ] Insufficient currency tested
[ ] Ad-not-ready/failure tested
[ ] IAP purchase/restore tested if enabled
[ ] Android real-device build tested
[ ] iOS real-device build tested if supported
```

---

## Important reusable-asset rule

**MN7 Store provides the reusable system. Each game provides its own data and platform configuration.**

Never ship another game's production:

- Game ID
- save data
- AdMob App ID
- AdMob Ad Unit IDs
- IAP product IDs

into a different game.
