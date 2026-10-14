# Collider input

Animal tiles use `BoxCollider2D` on the `AnimalTileHits` layer. The collider follows the tile's RectTransform, including movement, hint scaling and layout size changes. Tiles have no Rigidbody2D and no physics-driven falling.

`TileColliderInput` on BoardContainer receives mouse/touch events through Unity's EventSystem. It converts the screen position to the board plane and queries tile colliders with Physics2D. A click selects a tile only when the press and release both hit the same tile. When animated tiles overlap, the last rendered sibling is selected.

Hidden and Barrier states disable the hitbox. Ice remains selectable. Matched tiles unregister their hitbox immediately before deferred destruction. Restart unregisters the old board first. Menu, Shop, Settings and result screens do not accept board input.

UI buttons remain responsible for menus, settings and boosters. Per-tile UI Button input and graphic raycast targets are disabled, so a tile click is not processed twice. Matching rules, score, inventory and DOTween animations continue to use their existing systems.

## Performance

There is no Update or FixedUpdate loop in the input component. Collider queries and transform synchronization run only on pointer press/release. Results and pointer records reuse allocated buffers; the layer filter excludes unrelated colliders. Buffers grow only when a larger level is loaded.

Unity 2022.3 Editor checks passed for screen-space overlay and camera canvases, 1920×1080 and 1280×720 layouts, match resolution, state gating, restart, hint and shuffle. A synthetic 400-tile board used appropriately resized tile graphics/hitboxes. After warming up, 5,000 picks allocated 0 managed bytes and took 8.546 ms total in the test environment. This measures collider picking, not the whole game's frame time or other hardware.

Verification log: `Logs/tile-collider-verified-check.log`.
