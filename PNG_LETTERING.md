# PNG lettering and default background editing

The visible UI uses rounded white PNG lettering with black outlines and light blue lower shading, matching SettingsTitleEnglish.png.

- Fixed labels are separate Text_*.png sprites in Assets/Art.
- Changing coin/score/level values, prices, percentages and feedback use the character sprites from Assets/Art/LetteringGlyphs.png. The source TextMeshPro components remain as data references for existing game code; their normal font rendering is disabled by PngTextView.
- PngLettering.asset stores glyph rectangles and fixed-label sprites. PngTextGraphic renders the changing values, including text wrapping and animation alpha.
- Fixed label images are under the WordPng child. Replacing that child's Sprite is preserved when entering Play while its label text remains unchanged.
- All former PastelUI images now live directly in Art. Asset metadata/GUIDs were moved with their PNGs, preserving scene, catalog and prefab references.

## Changing the default game background

While outside Play Mode, select the existing background Image referenced by ShopUI.Background Targets and drag a new PNG into its Sprite field. DefaultBackgroundSync detects the Inspector change, saves that sprite as the garden/default product in EconomyConfig, and selects the default background in the local profile. This prevents ShopUI from restoring the previous sprite on entering Play. Purchased backgrounds and coins are retained; selecting another owned theme in the shop still equips it normally.

## Art generation

Built-in image_gen was used with SettingsTitleEnglish.png as the style reference. Final prompt: create a transparent 6-column by 8-row isolated glyph sheet with rounded bold white letters, thick black outlines and pale blue bevels; exact rows ABCDEF / GHIJKL / MNOPQR / STUVWX / YZ0123 / 456789 / +:%!?. / -/()×•; uniform cap height, no labels, background, grid or extra characters. Unity imports the glyph rectangles and exports the fixed UI labels into individual transparent PNG assets.

## Verification

Unity checks simulate an actual Inspector Sprite edit using Undo modifications, confirm the default background is synchronized and retained on Play, and restore the original catalog/profile after testing. Play checks verify all visible labels use the PNG renderer, dynamic inventory values, menu button raycasts, shop purchase/equip, level locks, audio toggles, next level and restart. Screenshots are saved in GardenPreviews/png-*.png.
