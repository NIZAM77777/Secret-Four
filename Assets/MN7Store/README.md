# MN7 Store — Secret Four Integration

Secret Four uses the reusable MN7 Store architecture from `MN7-Store-Test-Project`, but this game intentionally exposes only the acquisition paths it needs.

## Secret Four store rules

- Free items can be claimed once and become permanently owned.
- Rewarded-ad items are unlocked by watching the configured number of rewarded ads.
- Owned selectable items can be equipped.
- The selected item is saved locally by the MN7 Store selection system.
- The selected `PLAYER` item is used as the Secret Four coin/player visual.
- Online placement sends the selected item ID so every client displays the correct player's selected skin.
- There is no currency wallet or currency purchase flow in Secret Four.
- There is no IAP or Google Play Billing flow in Secret Four.

## Store configuration

The Secret Four catalog is:

- Game ID: `SECRET_FOUR`
- Save Key: `SECRET_FOUR_STORE`
- Selection Group: `PLAYER`
- Category: `Skins`

Current catalog:

| Item | Acquisition | Ads | Default | Selection Group |
|---|---|---:|---|---|
| Red Skin | Free | 0 | Owned + Equipped | `PLAYER` |
| Blue Skin | Rewarded Ad | 1 | Locked | `PLAYER` |
| Green Skin | Rewarded Ad | 1 | Locked | `PLAYER` |

Every permanent selectable item rewards itself with quantity `1`.

## Runtime integration

`Assets/MN7Store/MN7StoreSystem.unity` contains the reusable Store Manager and AdMob bootstrap. `MainMenuManager` loads this scene additively from the Main Menu and keeps it loaded so Store ownership and selection persist into gameplay.

The Main Menu `StoreButton` is wired automatically by `MainMenuManager`. The Store panel is hidden during gameplay.

## AdMob

The `store_reward` placement currently uses Google's official rewarded-ad test IDs so the store can be tested safely during development.

Before publishing the game, replace the test IDs in:

`Assets/MN7Store/Resources/MN7AdMobConfig.asset`

with Secret Four's real AdMob rewarded ad unit ID. Google requires test ads during development and the sample IDs must be replaced before publishing.

## Secret Four gameplay bridge

`Assets/Scripts/Store/SecretFourStoreIntegration.cs` exposes the selected `PLAYER` item to gameplay.

`PlacementManager` uses the selected item's sprite when creating a coin visual.

`NetworkCoinPlacementManager` sends the selected item ID with each accepted placement. The host validates that the ID is a selectable `PLAYER` item in the Store catalog before broadcasting it.

## Important

Do not modify `MN7-Store-Test-Project` for Secret Four-specific behavior. Changes specific to this game belong in `Secret-Four` integration code and configuration.
