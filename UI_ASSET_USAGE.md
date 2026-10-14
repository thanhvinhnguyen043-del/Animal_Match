# Pastel UI asset usage

All 24 supplied files are copied unchanged into Assets/Art under English filenames. Icons are imported as transparent UI sprites (512px maximum); the landscape background uses a 2048px maximum. Original full-size PNGs are preserved. No generated replacements were made.

| Asset | Current use |
| --- | --- |
| PastelGarden.png | Default free garden background and shop thumbnail; purchased themes are preserved. |
| Add.png | Shop purchase actions. |
| Help.png | Imported and ready; not assigned because there is no help/tutorial screen yet. |
| Lock.png | Locked levels and gated shop background thumbnails. |
| Trophy.png | Win result only. |
| Coin.png | Menu, gameplay and shop wallets. |
| SoundOff.png | Master/SFX muted state. |
| Timer.png | Gameplay time bar. |
| MusicOff.png | Music muted state. |
| MusicOn.png | Music unmuted state. |
| SoundOn.png | Master/SFX unmuted state. |
| LevelSelect.png | Main menu Select Level. |
| NextLevel.png | Win result next-level button. |
| PastelContinue.png | Owned background use/equip action icon. |
| Close.png | Settings close button. |
| Back.png | Level-selection and shop Back buttons. |
| Shop.png | Main menu shop entry and shop header. |
| Settings.png | Main menu settings. |
| Hint.png | Gameplay booster and shop product artwork. |
| Shuffle.png | Gameplay booster and shop product artwork. |
| PastelRestart.png | Settings and result restart buttons. |
| Home.png | Settings/result return-home buttons. |
| Pause.png | Gameplay pause button; opens the existing settings panel and pauses gameplay. |
| PastelPlay.png | Main menu Play. |

## Artwork still needed

- Panel/window frames: settings, results and shop product cards.
- Blank wide button bodies: shop purchase/equip bars and the wallet pill.
- Slider track, fill and handle artwork; timer progress-bar track/fill.
- Level-number tile/button frames, level badge and booster quantity badges.
- UI text now uses individual PNG word art and PNG glyphs for changing values. ANIMAL MATCH has a PNG title.

These elements retain existing Unity images/rounded geometry and live text. Functional labels and prices remain English. The supplied Help icon is available in the art folder but has no new behavior.

Validation: Unity play checks verify menu navigation and real button raycasts, locked level/background icons, hint/shuffle quantities, all three audio mute/unmute icon transitions, purchases and equipped backgrounds, win-only trophy, next level and restart. Prices and progress rules remain unchanged. Tests restore currency, progress and audio preferences.

PastelUI has been flattened into Art while preserving every sprite GUID. Three filenames use a Pastel prefix to avoid replacing pre-existing Play/Restart/Continue artwork.
