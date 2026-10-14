# Gameplay architecture

- `BoardLogic`: board data, matching with at most two turns, available-pair search and gravity in four directions. It does not access Images, GameObjects, DOTween or FMOD.
- `GameManager`: authoritative Menu, LevelSelect, Playing, Paused, Win, Lose and Shop states; score, combo and timer. Publishes gameplay events. Shop can open only from Menu and returns to Menu when closed.
- `GridManager`: creates tile views, coordinates user actions and applies model results to the board. Keeps DOTween movement and hint animations.
- `GameHudView`: subscribes to score/time/match/result/availability events and updates TextMeshPro, progress bar, result panels, floating text and blocked-board notice.
- `GameAudioFeedback`: subscribes to gameplay events and plays FMOD effects, starts gameplay music and stops it at the result screen.
- `UIManager`: opens panels immediately and requests state transitions/pause/resume. Settings do not replace Win/Lose with Paused.

When no selectable pair has a valid path, the game displays “Không còn cặp nối được. Hãy đảo ô hoặc chơi lại.” Checks occur after falling and automatic matches settle. Shuffle tries up to 30 arrangements to find a valid move, retaining animal IDs and water/stone states. If the remaining animals cannot form a selectable pair, the notice remains and the player can restart from settings. An available move is not a guarantee that every generated board can be cleared.

DOTween remains installed for animal movement, hint scaling and floating score text. Input System, Rigidbody, Collision and Trigger mechanics were not added in this change.

Validation: randomized matching checked against an independent breadth-first search with a two-turn limit; all four gravity directions compared with segment packing. Unity play checks cover menu/level selection, pause/resume, score/combo/time, deadlock notices, shuffle, water/stone behavior, single result events, restart and level unlocking.
