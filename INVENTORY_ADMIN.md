# Inventory Admin

Open **Tools → Inventory Admin** in Unity. Enter an amount beside Coins, Hint or Shuffle, then press **Add** or **Remove**.

The window edits the local saved inventory in Edit mode or Play mode. Changes persist after stopping Play mode or restarting Unity. During Play mode, the wallet, booster counts and shop update immediately.

Counts stay between 0 and 2,147,483,647. Background ownership, equipped background, level progress and audio settings are preserved. Starting amounts in EconomyConfig are separate defaults for a new save.

The window and inventory adjustment API are Editor-only and are excluded from game builds.

Use **Reset purchased backgrounds** to prepare a purchase demo. This removes ownership of purchased backgrounds and equips the free Day Garden. It preserves coins, boosters and level progress; purchases are not refunded. Backgrounds still require their original level unlocks and coin prices. The reset happens only when the button is pressed.
