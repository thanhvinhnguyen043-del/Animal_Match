# Coin, inventory and shop prototype

- Every completed win gives 100 coins, including replay wins. Duplicate result calls in the same run do not give another reward because GameManager only publishes one GameEnded event.
- A new profile starts with 0 coins, 3 hints and 3 shuffles. Inventory persists across restarts, levels and game launches.
- Hints cost 75 coins each; shuffles cost 100 coins each. A hint is consumed only when a valid pair is found. Repeated clicks during booster cooldown do not consume extra items. Empty stock shows a brief gameplay notice and does not open the shop.
- Levels 3, 6 and 9 give one additional hint. Levels 5 and 10 give one additional shuffle. Bonus fields are editable in Tools > Level Database.
- The default garden is owned for free. The sunset garden costs 2000 coins, is purchased once and can then be equipped or switched back to the default garden.
- The shop is a separate full-screen page opened only from the main menu. Opening hides the menu; Back restores it immediately. Gameplay and settings have no shop entry. The shop shows inventory, products, prices and purchase feedback; it contains no win-reward guide.
- The wallet and purchased backgrounds are stored together in PlayerPrefs under `Economy.Profile.v1`. This is local prototype storage, not server-backed currency.

Prices, starting inventory, coins per win and background products are configurable in `Assets/Resources/EconomyConfig.asset`.

## Background art

Generated with the built-in imagegen tool, using `Assets/Art/GardenBackground.png` as the style and composition reference. Output: `Assets/Art/SunsetGardenBackground.png`.

Final prompt: Use case: stylized-concept. Asset type: landscape 16:9 Unity mobile puzzle game cosmetic background. Reference image is style and composition reference. Create a sibling sunset version of this animal-free candy cartoon garden: glossy rounded lush bushes and flowers frame the left and right edges, rolling gentle hills, small distant lake, spacious clear center for a game board. Warm peach and lavender sky with fluffy rounded clouds, golden sunset illumination, glowing amber grass and soft teal foliage, bright readable inviting palette. Keep the center quiet and low detail. No animals, characters, text, logos, UI, buttons, or frame. Full opaque background, polished smooth hand-painted casual game art.


## Background catalog and level gates

All six cafe backgrounds are imported in Assets/Art and offered by the scrollable menu-only shop. Each costs 2000 coins once. Existing garden products and ownership stay intact.

| Product | Completed level required |
| --- | --- |
| Cozy Cafe | None |
| Winter Cafe | 10 |
| Vietnamese Tet Cafe | 20 |
| Halloween Cafe | 30 |
| Beach Cafe | 40 |
| Jungle Cafe | 50 |

The required milestone opens permission to purchase; it does not grant the background for free. Transactions reject locked products even if invoked directly. Owned backgrounds can always be equipped without another charge.

LevelProgress stores the highest completed level under Progress.HighestCompletedLevel independently from the clamped next playable level. Beating level 10 in a ten-level database correctly unlocks the level-10 product. Existing progress falls back to the previously unlocked-level value until another win records completion. Currently there are ten playable levels; later milestones become reachable after more levels are authored. Replaying earlier levels does not lower the milestone.

All in-game labels, shop names, purchase/lock feedback, settings, booster notices and results use English. SettingsTitleEnglish.png replaces the visible Vietnamese settings word art; the original asset is retained unused.

The cafe artwork uses the approved generated previews. The SETTINGS title was edited with the built-in image_gen tool using the original title as reference. Final prompt: Change Vietnamese wording to the exact all-capitals word SETTINGS; preserve chunky rounded white lettering, subtle pale blue lower bevels, thick black outline and tiny shadow; one horizontal centered line, no extra text, decoration or panel; real transparent alpha background.

Validation: Unity model and play checks cover all five milestone boundaries, locked purchases without coin loss, prices, duplicate purchases, equipment persistence, scroll wheel and drag, last-level completion, menu-only shop navigation, English UI and application of an owned background during gameplay.
