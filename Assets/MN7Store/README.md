# MN7 Store

**Reusable Unity store system for mobile games.**

MN7 Store gives you one reusable store system that can be used in many games.

You decide what each game needs:

- Free items
- Rewarded ads
- Coins / in-game currency
- In-App Purchases (IAP)
- Any combination of the above

You normally configure the store in the Unity Inspector. You do **not** need to change the MN7 Store source code for each game.

---

# 1. What you need

## Basic store

For a normal store using free items and/or coins, you need:

- Unity
- TextMeshPro / Unity UI
- MN7 Store

No AdMob SDK is required if ads are not used.
No Unity IAP package is required if IAP is not used.

## If the game uses rewarded ads

You also need:

- Google Mobile Ads Unity SDK
- Your game's AdMob App ID
- Your game's rewarded Ad Unit IDs

## If the game uses IAP

You also need:

- Unity In-App Purchasing package
- Products configured in the target platform store
- Matching product IDs in MN7 Store

**Only install/configure the SDKs that the game actually uses.**

---

# 2. Add MN7 Store to the game

From:

`Assets/MN7Store/Prefabs/`

Add these prefabs to the game:

### `MN7StoreSystem.prefab`

Add it to the game's first/bootstrap scene.

This contains the store manager and keeps it alive between scenes.

### `MN7StorePanel.prefab`

Add it under the game's Canvas.

If the game already has an EventSystem, **do not create another one**.

Keep only **one MN7StoreSystem / MN7StoreManager** active for the game.

---

# 3. Create a Store Config for the game

In Unity:

**Right-click in Project → Create → MN7 Store → Store Config**

Create one config for every game.

Example:

```text
Game ID = SECRET_FOUR
```

Then select:

`MN7StoreSystem → MN7StoreManager`

and assign the game's Store Config.

## Important: Game ID

The Game ID separates save data between games.

For example:

```text
Game A → GAME_A
Game B → GAME_B
Game C → SECRET_FOUR
```

Never use another game's Game ID.

For a released game, always use a unique Game ID.

---

# 4. Choose what the game uses

Select `MN7StoreSystem` and find **Store Features** on `MN7StoreManager`.

You will see:

```text
Free Items = ON / OFF
Ad Rewards = ON / OFF
Coins      = ON / OFF
IAP        = ON / OFF
```

Turn ON only the features used by that game.

### Example 1 — Ads only

```text
Free Items = ON
Ad Rewards = ON
Coins      = OFF
IAP        = OFF
```

### Example 2 — Coins + Ads

```text
Free Items = ON
Ad Rewards = ON
Coins      = ON
IAP        = OFF
```

### Example 3 — Coins + Ads + IAP

```text
Free Items = ON
Ad Rewards = ON
Coins      = ON
IAP        = ON
```

### Example 4 — IAP only

```text
Free Items = OFF
Ad Rewards = OFF
Coins      = OFF
IAP        = ON
```

Disabled features do not need to be configured for that game.

---

# 5. Add coins (only if the game uses coins)

If **Coins = ON**:

Create:

**Right-click → Create → MN7 Store → Currency Data**

Example:

```text
Currency ID     = soft_currency
Display Name    = Coins
Starting Amount = 3000
```

Add the Currency Data to the game's Store Config.

If the game does not use coins:

```text
Coins = OFF
```

Do not configure currency purchase/reward routes for a game that has Coins disabled.

---

# 6. Add categories

Categories are created inside the Store Config.

For each category, set:

```text
Category ID
Display Name
Sort Order
```

Example:

```text
Category ID  = SKINS
Display Name = Skins
Sort Order   = 0
```

## ALL category

You do **not** create the ALL category yourself.

MN7 Store creates it automatically.

Do not add `__ALL__` as a normal category or item category.

---

# 7. Create store items

Create:

**Right-click → Create → MN7 Store → Store Item**

Set the basic information:

```text
Item ID
Display Name
Description
Icon
Category IDs
```

Then choose how the player gets the item.

---

# 8. Free item

Use this when the player gets an item for free.

```text
Purchase Type = Free
```

Make sure:

```text
Free Items = ON
```

---

# 9. Coin item

Use this when the player pays with in-game currency.

```text
Purchase Type = Currency
Currency ID   = soft_currency
Currency Cost = 100
```

Make sure:

```text
Coins = ON
```

The player must have enough currency before the purchase can complete.

---

# 10. Rewarded-ad item

Use this when the player watches an ad to receive the item.

Example:

```text
Purchase Type    = Rewarded Ad
Required Ads     = 1
Ad Placement ID = store_reward
```

Make sure:

```text
Ad Rewards = ON
```

The `Ad Placement ID` must match a placement in the MN7 Store AdMob Config.

---

# 11. Currency OR Ad item

Use this when the player can obtain an item using currency, an ad, or both.

Configure the routes you want on the item.

Then the Store Features decide which routes are available:

```text
Coins      = ON → currency route can be used
Ad Rewards = ON → rewarded-ad route can be used
```

If a route's feature is OFF, that route is not available.

---

# 12. IAP item

Use this when the player buys the item with a platform store purchase.

Set:

```text
Purchase Type = IAP
IAP Product ID = your_exact_product_id
```

Make sure:

```text
IAP = ON
```

The product ID in MN7 Store must exactly match the product ID configured in Google Play / App Store / the target platform.

### IAP setup

1. Install/enable Unity In-App Purchasing.
2. Set `IAP = ON`.
3. Configure the product in the platform store.
4. Put the exact product ID on the MN7 Store item.
5. Configure the MN7 Store Unity IAP provider.
6. Initialize the IAP system.
7. Test on a real device using the platform's testing system.

If the game does not use IAP:

```text
IAP = OFF
```

You do not need the IAP provider for that game.

---

# 13. Skins, characters and colors

For something that should stay owned after purchase, use:

```text
Ownership Type     = Permanent
Selectable         = ON
Selection Group ID = CharacterSkin
```

A purchasable permanent selectable item should reward itself:

```text
Rewards → Size = 1
Reward Type    = Item
Item ID        = same item ID
Quantity       = 1
```

Example:

```text
blue_skin → blue_skin × 1
```

The normal flow becomes:

```text
Not owned → BUY
Purchased → SELECT
Selected  → SELECTED
```

For a default/free starting skin:

```text
Default Owned    = ON
Default Selected = ON
```

Only one item in the same selection group should normally be the default selected item.

---

# 14. Reward items and currency

Rewards are configured on the Store Item.

You can reward items and, when Coins are enabled, currency.

Examples:

```text
Item reward:
Item ID  = blue_skin
Quantity = 1
```

```text
Currency reward:
Currency ID = soft_currency
Amount      = 100
```

If `Coins = OFF`, do not configure currency rewards for the store.

---

# 15. Rewarded Ads / AdMob setup

Only do this section if:

```text
Ad Rewards = ON
```

## Required SDK

Install the **Google Mobile Ads Unity SDK** in the game.

The SDK is separate from MN7 Store.

## AdMob App ID

Configure the game's AdMob **App ID** in the Google Mobile Ads plugin settings.

This is different from an Ad Unit ID.

## MN7 Store AdMob Config

Create/configure an MN7 Store AdMob Config.

For each rewarded placement set:

```text
Placement ID
Android Ad Unit ID
iOS Ad Unit ID
```

Example:

```text
Placement ID       = store_reward
Android Ad Unit ID = your Android rewarded ID
iOS Ad Unit ID     = your iOS rewarded ID
```

Then an item using ads must have:

```text
Ad Placement ID = store_reward
```

The names must match exactly.

### Important

- Use test ads/test devices while developing.
- Use production Ad Unit IDs only for release builds.
- Do not put one game's production AdMob IDs into another game.
- The MN7 Store package should remain reusable; each game supplies its own AdMob configuration.

If the game does not use ads:

```text
Ad Rewards = OFF
```

No AdMob configuration is needed for the store.

---

# 16. IAP setup in simple terms

Only do this section if:

```text
IAP = ON
```

MN7 Store does not create your Google Play/App Store products for you.

You create the product in the platform store first, for example:

```text
Platform product ID = remove_ads
```

Then use exactly the same ID in the MN7 Store item:

```text
IAP Product ID = remove_ads
```

The IDs must match exactly.

MN7 Store handles the store purchase flow through its IAP provider, while the platform handles the actual payment.

Test IAP with the platform's official test account/test track/sandbox setup before release.

---

# 17. Store UI

The main reusable UI prefabs are in:

`Assets/MN7Store/Prefabs/`

Important prefabs:

```text
MN7StoreSystem.prefab
MN7StorePanel.prefab
MN7StoreCategoryBar.prefab
MN7StoreCategoryButton.prefab
MN7StoreItem.prefab
MN7StoreItemActions.prefab
```

You can edit the prefabs to match your game's design:

- Backgrounds
- Icons
- Fonts
- Text sizes
- Buttons
- Item card layout
- Category button design
- Grid spacing
- Store panel size

The store creates item cards and category buttons from your Store Config at runtime.

**Do not manually create one item card for every store item.**

The action buttons are connected by MN7 Store code at runtime. You normally do not need to add functions to their Unity `On Click()` events.

---

# 18. Connect selected skins/characters to your game

MN7 Store manages which item is selected. Your game decides how that selected item changes the actual character/visual.

For a UI Image or 2D SpriteRenderer, you can use:

`MN7SelectedSpriteApplier`

Set the same selection group, for example:

```text
Selection Group ID = CharacterSkin
```

For a 3D game, your own game code can read the selected item and apply the correct model/material/visual.

Example:

```csharp
MN7StoreManager.Instance.Selection.GetSelectedItem("CharacterSkin");
```

---

# 19. Validate the store before testing

After configuration:

1. Select `MN7StoreSystem` in the Hierarchy.
2. On `MN7StoreManager`, open the component menu `⋮`.
3. Select **Validate Store Config**.
4. Fix every red error.

The validator checks things such as:

- Game ID
- Duplicate IDs
- Currency setup
- Categories
- Item references
- Rewards
- Selection setup
- Currency purchase routes
- Ad routes
- IAP product IDs
- Disabled-feature conflicts

Warnings may be acceptable depending on the game, but **red errors should be fixed before release**.

---

# 20. Run Runtime Diagnostics

Press **Play** and wait for MN7 Store to initialize.

Then on `MN7StoreManager`:

**Component menu `⋮` → Run Runtime Diagnostics**

This checks that the runtime setup is working.

It understands disabled features, so a game with Coins OFF does not need a Currency manager and a game with Ads OFF does not need an Ad manager.

---

# 21. Test the features you enabled

Test only the systems used by the game.

### If Free Items are ON

- Free item purchase
- Item ownership
- Store refresh

### If Coins are ON

- Enough coins
- Not enough coins
- Successful purchase
- Balance after purchase
- Balance after restarting the game

### If Ads are ON

- Ad loads
- Successful rewarded ad
- Ad not ready
- Ad failure
- Reward is granted once

### If IAP is ON

- Successful purchase
- Pending purchase
- Failed purchase
- Restore/recovery
- Item is granted once

### If selectable items are used

- Purchase item
- Select item
- Selected state
- Restart game
- Confirm selection remains

---

# 22. Reset store data during development

If you need to start the store again from its defaults, use:

**MN7StoreManager → component menu `⋮` → Reset Store Data**

This is an **Editor-only** development feature.

It resets MN7 Store data for the current game's store namespace.

It does **not** use `PlayerPrefs.DeleteAll()`, so it does not intentionally erase unrelated game PlayerPrefs.

Do not use this as a player-facing reset button in a released game.

---

# 23. Important: what belongs to MN7 Store and what belongs to the game?

## MN7 Store provides

- Store logic
- Purchase flow
- Currency handling
- Inventory handling
- Selection handling
- Reward handling
- Transaction/recovery handling
- Store UI
- Category system
- Optional Ads integration
- Optional IAP integration
- Validation and diagnostics

## The game provides

- Its own Store Config
- Its own Game ID
- Its own items
- Its own currencies
- Its own categories
- Its own artwork
- Its own AdMob App ID / Ad Unit IDs
- Its own IAP product IDs
- Its own game-specific character/skin application logic

This is what makes MN7 Store reusable.

---

# 24. Quick setup examples

## Game A — Ads only

```text
Free Items = ON
Ad Rewards = ON
Coins      = OFF
IAP        = OFF
```

Needs:

```text
Google Mobile Ads SDK
AdMob App ID
Rewarded Ad Unit IDs
```

Does not need:

```text
Currency setup
Unity IAP
```

## Game B — Coins + Ads

```text
Free Items = ON
Ad Rewards = ON
Coins      = ON
IAP        = OFF
```

Needs:

```text
Currency Data
Google Mobile Ads SDK
AdMob configuration
```

## Game C — Coins + Ads + IAP

```text
Free Items = ON
Ad Rewards = ON
Coins      = ON
IAP        = ON
```

Needs:

```text
Currency Data
Google Mobile Ads SDK
AdMob configuration
Unity IAP
Platform store products
```

## Game D — IAP only

```text
Free Items = OFF
Ad Rewards = OFF
Coins      = OFF
IAP        = ON
```

Needs:

```text
Unity IAP
Platform store products
```

Does not need:

```text
AdMob SDK
AdMob configuration
```

---

# 25. Final release checklist

Before publishing a game using MN7 Store:

```text
[ ] Unique Game ID
[ ] Correct Store Config assigned
[ ] Features match the game
[ ] Currency setup checked (if Coins are ON)
[ ] Categories checked
[ ] Item IDs are unique
[ ] Rewards are valid
[ ] Selectable items tested (if used)
[ ] AdMob configured (if Ads are ON)
[ ] AdMob App ID configured
[ ] Ad Unit IDs configured for the target platform
[ ] IAP configured (if IAP is ON)
[ ] IAP product IDs exactly match the platform store
[ ] Validate Store Config passes
[ ] Runtime Diagnostics pass
[ ] Purchase flows tested
[ ] App restart/persistence tested
[ ] Physical Android device tested
[ ] Physical iOS device tested if the game supports iOS
```

---

# 26. One important rule

**Do not put game-specific production settings inside the reusable MN7 Store asset.**

Never reuse another game's:

- Game ID
- Store save data
- AdMob App ID
- AdMob Ad Unit IDs
- IAP product IDs

Each game should have its own configuration.

---

## In short

```text
1. Add MN7StoreSystem
2. Add MN7StorePanel
3. Create Store Config
4. Give the game a unique Game ID
5. Turn ON the features the game needs
6. Add currencies/categories/items
7. Configure AdMob only if Ads are used
8. Configure Unity IAP only if IAP is used
9. Validate
10. Run diagnostics
11. Test
12. Build and release
```

**MN7 Store = reusable system.**
**Each game = its own data + its own platform configuration.**
