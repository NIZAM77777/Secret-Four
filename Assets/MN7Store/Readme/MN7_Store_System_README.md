# MN7 Store System

A reusable Unity store system for games.

Supports:

- Free items
- Currency purchases
- Rewarded Ad purchases
- Currency OR Ad purchases
- Permanent items
- Consumable items
- Currency management
- Inventory
- Rewards
- Save/Load
- Transaction recovery
- AdMob rewarded ads

Real-money / IAP is not included.

---

# 1. Import

Copy the complete `MN7Store` folder into:

Assets/

The final structure should be:

Assets/
└── MN7Store/
    ├── Prefabs/
    ├── Resources/
    └── Scripts/
        ├── Ads/
        ├── Core/
        ├── Data/
        └── UI/

No external game scripts need to be placed inside MN7Store.

---

# 2. Create Store Data

In the Unity Project window, create:

## Currency

Right-click:

Create → MN7 Store → Currency Data

Example:

Currency ID:
soft_currency

Display Name:
Coins

Starting Amount:
1000

---

## Store Items

Create:

Create → MN7 Store → Store Item

Create one item for each thing you want to sell.

Set:

- Item ID
- Display Name
- Description
- Icon
- Purchase Type
- Currency ID / Cost
- Rewarded Ad settings
- Rewards
- Ownership Type

Every Item ID must be unique.

---

## Store Config

Create:

Create → MN7 Store → Store Config

Add:

### Currencies
Add all Currency Data assets.

### Store Items
Add all Store Item assets.

Set a unique Save Key.

Example:

MN7_STORE_DATA

---

# 3. Add Store Manager

In the game's main scene, create:

Empty GameObject
→ MN7StoreManager

Add:

MN7StoreManager

Assign:

Store Config

The Store Manager automatically creates and manages:

- Currency
- Inventory
- Purchases
- Rewards
- Transactions
- Recovery
- Ads

Do NOT create these managers manually.

---

# 4. Store UI

Create a Canvas.

Recommended structure:

Canvas
└── StorePanel
    ├── Header
    │   ├── CloseButton
    │   ├── StoreTitle
    │   └── CurrencyDisplay
    │
    └── Scroll View
        └── Viewport
            └── Content

Add:

### StorePanel

Add:

MN7StoreUI

Assign:

- Item Container → Content
- Item Prefab → MN7StoreItem prefab
- Items → Store Items

---

# 5. Store Item Prefab

Use:

MN7StoreItem

The prefab contains:

- Icon
- Name
- Description
- Price
- Status
- Buy Button
- Watch Ad Button

Add:

MN7StoreItemUI

Connect the corresponding UI objects in the Inspector.

The Item Data can be assigned automatically by MN7StoreUI.

---

# 6. Currency UI

For the currency display:

CurrencyDisplay
└── MN7CurrencyUI

Assign:

Currency ID:
soft_currency

Amount Text:
Your currency TMP text.

The ID must exactly match the Currency Data ID.

Example:

Currency Data:
soft_currency

Currency UI:
soft_currency

Store Item:
soft_currency

---

# 7. Rewarded Ads / AdMob

AdMob is optional.

If the game uses rewarded ads:

Create:

Empty GameObject
→ MN7AdMob

Add:

MN7AdMobBootstrap

Create:

Create → MN7 Store → AdMob Config

Add the rewarded ad placements and their Android/iOS Ad Unit IDs.

Assign the AdMob Config to:

MN7AdMobBootstrap

The AdMob object registers itself with MN7StoreManager automatically.

For games that do not use rewarded ads, MN7AdMob is not required.

---

# 8. Opening the Store

The game only needs to control the StorePanel.

Example:

Game Store Button
    ↓
StorePanel.SetActive(true)

Close Button
    ↓
StorePanel.SetActive(false)

MN7StoreUI handles the store contents automatically.

---

# 9. Required Scene Objects

For a normal store scene:

Main Camera
MN7StoreManager
Canvas
└── StorePanel
    └── Store UI

EventSystem

If using AdMob:

MN7AdMob

No other MN7 manager GameObjects are required.

---

# 10. Important Rules

### Unique IDs

Every ID must be unique.

Example:

soft_currency
item_coins_100
item_skin_blue

Do not use duplicate IDs.

### Currency ID

The Currency ID used by an item must exist in Store Config.

### Item ID

Every store item must have a valid unique Item ID.

### Save Key

Use a unique Save Key for each game if the same device can contain data from multiple games.

Example:

Secret Four:
SECRET_FOUR_STORE

Another Game:
MY_GAME_STORE

### Store Config

Always assign the Store Config to MN7StoreManager.

### Prefab

Use the provided MN7StoreItem prefab or a prefab containing MN7StoreItemUI.

---

# 11. Basic Connection

The complete connection is:

Store Config
     ↓
MN7StoreManager
     ↓
┌───────────────┐
│ Currency      │
│ Inventory     │
│ Purchases     │
│ Rewards       │
│ Transactions  │
│ Recovery      │
│ Ads           │
└───────────────┘

Store Config
     ↓
MN7StoreUI
     ↓
Content
     ↓
MN7StoreItem
     ↓
MN7StoreItemUI
     ↓
MN7PurchaseManager

For rewarded ads:

MN7AdMob
     ↓
MN7AdMobBootstrap
     ↓
MN7AdManager
     ↓
MN7PurchaseManager

---

# 12. Using in a New Game

For every new game:

1. Import `MN7Store`.
2. Create Currency Data.
3. Create Store Item Data.
4. Create Store Config.
5. Add currencies/items to Store Config.
6. Add `MN7StoreManager` to the game.
7. Assign Store Config.
8. Create the Store UI.
9. Assign `MN7StoreUI`.
10. Assign the Store Item prefab.
11. Add Currency UI if needed.
12. Add `MN7AdMob` only if rewarded ads are used.
13. Assign AdMob Config if using AdMob.
14. Connect the game's Store button to `StorePanel`.

Done.

---

# 13. Important

MN7Store is game-independent.

Do not modify the MN7Store scripts for individual games unless a system-level feature is being added.

Game-specific:

- UI design
- Store items
- Currency names
- Rewards
- Icons
- Prices
- Ad placements
- Store opening/closing

should be configured from the game's own data and UI.