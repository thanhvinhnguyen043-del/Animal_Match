# Selection highlight

The original frame PNG was generated with the built-in imagegen tool and saved as `Assets/Art/SelectionHighlight.png`. It is retained as an unused alternative. At the user's request, selection now gently lights the entire animal using `AnimalSelectionLight.shader` and its material. AnimalTile copies the current animal sprite to the Highlight Image on setup; the material uses that sprite's alpha to blend a white silhouette at 10% opacity. The overlay has the same dimensions as the animal. Deselecting hides it and restores the original appearance without changing the animal's colors or material.

## Final prompt

Use case: stylized-concept. Asset type: transparent PNG Unity match-2 game tile selection highlight. Create exactly one glossy golden-yellow rounded square OUTLINE frame, front-facing flat square composition, centered occupying 90% of square canvas. Bright lemon gold beveled rim with warm amber outside edge, little white specular gleam upper left and restrained soft golden outer glow. Rim thickness about 5% of image width, corners modestly rounded. Friendly polished candy cartoon mobile game style matching glossy animal icons and blue bubble buttons. The entire large central opening MUST be completely transparent alpha zero, as must the exterior background. No solid panel inside, no animal, no letters, no numbers, no objects or scattered sparkles, no background, no perspective. A clean reusable frame overlay which surrounds an animal without obscuring it.

## Stone size

BarrierOverlay uses stretch anchors with an additional 22 pixels in each dimension (122 x 122 on the default 100 x 100 tile), compensating for the transparent margin inside the stone artwork. This is slightly smaller than the earlier 128 x 128 version. Board spacing is unchanged. The existing water artwork uses a cool blue tint (0.8, 0.93, 1) and 0.78 image opacity to make it easier to recognize.

## Art recovery

The copied PNGs remain directly in Assets/Art. Restored 19 original GUIDs after GardenUI was deleted; metadata before recovery is backed up in Logs/ArtRecovery-2026-10-10. Future asset moves should be made by dragging them within Unity's Project window so Unity preserves their metadata.
