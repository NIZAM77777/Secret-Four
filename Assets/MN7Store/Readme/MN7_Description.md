# Store Resources / Data Assets

MN7 Store uses several ScriptableObject assets to define how the store works.

These assets contain the store's configuration and item data.
They do not need to be added to the Unity Hierarchy.

---

# 1. Currency Data

Create:

Create → MN7 Store → Currency Data

### What is it?

Currency Data defines a currency that the game uses.

For example:

- Coins
- Gems
- Stars
- Tickets

It tells MN7 Store:

- What the currency is called
- What ID the currency uses
- How much the player starts with

### Example

Currency ID:
soft_currency

Display Name:
Coins

Starting Amount:
1000

### Important

The Currency ID is the unique identifier used by the store system.

If an item costs:

soft_currency

there must be a Currency Data asset with:

soft_currency

The ID must match exactly.

---

# 2. Store Item

Create:

Create → MN7 Store → Store Item

### What is it?

A Store Item defines something that the player can obtain from the store.

It contains:

- Item ID
- Display name
- Description
- Icon
- Purchase method
- Price
- Rewarded Ad settings
- IAP product ID (not used if IAP is not implemented)
- Rewards
- Ownership type

### Example

Item ID:
coin_pack_100

Display Name:
100 Coins

Description:
Get 100 coins.

Icon:
Coin icon

Purchase Type:
Currency

Currency ID:
soft_currency

Currency Cost:
50

Rewards:
100 soft_currency

Ownership:
Consumable

### Purchase Types

#### Free

The player gets the item for free.

#### Currency

The player spends an in-game currency.

Example:

50 Coins → 100 Coins reward

#### Rewarded Ad

The player watches rewarded ads to obtain the item.

Example:

Watch 3 ads → 100 Coins

#### Currency Or Ad

The player can choose either:

- Spend currency
OR
- Watch the required ads

#### IAP

Real-money purchase.

MN7 Store currently does not use IAP.

---

# 3. Reward Data

Reward Data defines what the player receives after a successful purchase.

A Store Item can contain one or multiple rewards.

### Reward Types

#### Currency

Gives the player currency.

Example:

100 soft_currency

#### Item

Gives the player another inventory item.

Example:

1 Blue Skin

### Example

Store Item:

"100 Coins"

Reward:

Reward Type:
Currency

Currency ID:
soft_currency

Currency Amount:
100

Another example:

Store Item:

"Starter Pack"

Rewards:

- 500 Coins
- 1 Blue Skin
- 3 Tickets

A single store item can therefore give multiple rewards.

---

# 4. Store Config

Create:

Create → MN7 Store → Store Config

### What is it?

Store Config is the main configuration asset for the store.

It tells MN7 Store which currencies and store items belong to the game.

It contains:

- Save Key
- Currencies
- Store Items

### Currencies

Add all Currency Data assets used by the game.

Example:

Currencies:

- Coins
- Gems

### Store Items

Add all Store Item assets that should appear in the store.

Example:

Store Items:

- 100 Coins
- 500 Coins
- Blue Skin
- Free Reward
- Ad Reward

### Save Key

The Save Key identifies the store's saved data.

Example:

MY_GAME_STORE

Each game should use its own unique save key.

---

# 5. AdMob Config

Create:

Create → MN7 Store → AdMob Config

### What is it?

AdMob Config contains the AdMob rewarded-ad information used by MN7 Store.

It connects the store's ad placement IDs to the actual AdMob Ad Unit IDs.

It contains rewarded ad placements for:

- Android
- iOS

### Example

Placement ID:

store_reward

Android Ad Unit ID:

your Android rewarded Ad Unit ID

iOS Ad Unit ID:

your iOS rewarded Ad Unit ID

### Important

The Placement ID must match the placement ID used by the Store Item.

Example:

Store Item:

Ad Placement ID:
store_reward

AdMob Config:

Placement ID:
store_reward

The IDs must match.

---

# 6. How the Data Assets Connect

The basic relationship is:

Currency Data
      ↓
Store Config
      ↓
MN7StoreManager

Store Item
      ↓
Store Config
      ↓
MN7StoreManager

Reward Data
      ↓
Store Item

AdMob Config
      ↓
MN7AdMob
      ↓
MN7AdManager
      ↓
Store Item

---

# 7. Simple Example

For a game that uses Coins:

### Currency Data

```text
Currency ID: soft_currency
Display Name: Coins
Starting Amount: 1000