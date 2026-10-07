SECRET FOUR — TEXT AND LANGUAGE

Open GameTextCatalog.asset in Unity.
The Inspector has a search field and English, Spanish, French and Arabic text for each key.
Edit the wording, then enter Play mode to see the change.
Keep matching rich-text tags and any numbered placeholders, such as {0}, in all translations.
Blank translations fall back to English.

Language is selected in Settings and saved on this device.
The game title, menu labels, rules, settings, bot difficulty descriptions, shop labels,
item names and game messages use this catalog. Player names and room codes are live data.
A LocalizedText component connects each scene or prefab label to its catalog key.
Empty keys are reserved for live text (names, percentages and match messages).

The shared Latin, Arabic and title fonts are under Localization/Fonts.
Arabic uses the bundled RTLTMPro runtime for connected letters and right-to-left text.
Its MIT license and pinned upstream source are under Assets/ThirdParty/RTLTMPro.
Noto Sans Arabic and Cormorant Garamond font licenses are under Assets/Art/Fonts.

Settings contains master volume, music volume, sound-effects volume, and mute.
Values are applied immediately, saved when settings closes, and retained between scenes.
Opening in-game settings pauses a local match and preserves an already-paused state.
Online matches continue while settings is open.

Validation entry points:
SecretFourDesignValidation.RunBatch — all four languages, screen overflow, category filters,
audio mixing and settings behavior. Add -qaTall for the taller portrait layout.
SecretFourFlowValidation.RunBatch — menu, local match, three bot difficulties, pause,
win/draw, rematch and return flow, plus rules and scene wiring checks.
