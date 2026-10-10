# Giao diện khu vườn

Đã áp dụng vào `Assets/Scenes/SampleScene.unity`, bố cục tham chiếu 1920×1080 và kiểm tra thêm ở 1920×1200.

- Settings dùng hộp xanh, ba thanh âm lượng FMOD với nút kéo trắng và phần trăm, nút đóng, Restart Level và Return Home. Không có About Us, Contact Us hay Privacy Settings.
- Settings mở từ menu hoặc nút bánh răng trong màn chơi. Mở trong màn chơi sẽ tạm dừng thời gian, đóng sẽ cho chơi tiếp. Restart chỉ hoạt động khi đang ở màn chơi.
- Nền khu vườn và các icon bánh răng/đảo bàn/gợi ý dùng ảnh người dùng cung cấp.
- Bảng thắng dùng ba nút ảnh Restart/Continue/Return Home. Continue chỉ hiện khi thắng và còn màn kế tiếp.
- Banner ảnh dành cho màn 1 ghi `LEVEL 1 COMPLETED!`; các màn sau dùng tiêu đề theo số màn hiện tại. Nhạc vẫn dừng khi thắng và phát lại khi bắt đầu bàn mới.

Các ảnh xem trước do Unity render nằm trong `GardenPreviews/`. Chúng kiểm tra giao diện tĩnh; chưa thay thế việc Play thử toàn bộ trận đấu và nghe âm thanh.

## Asset

Ảnh đã được sao chép vào `Assets/Art/GardenUI/`, nhập kiểu Sprite, giữ alpha và tỉ lệ của nút. Font UI riêng là `Assets/TextMesh Pro/Resources/Fonts & Materials/AudioSettings SDF.asset`, hỗ trợ thêm chữ tiếng Việt bằng atlas động.

Hai asset dùng công cụ imagegen tích hợp:

- `Assets/Art/GardenUI/Level1Completed.png`.
- `Assets/Art/GardenUI/SettingsTitle.png`.

Prompt cuối cho banner:

> Edit target: supplied blue glossy cartoon game completion ribbon. Change ONLY top text from LEVEL 10 to LEVEL 1. Exact text: LEVEL 1 on the first line, COMPLETED! on the second line. Preserve the ribbon shape, side tails, white/black bold bubbly outlined type, glossy blue gradients and proportions. Clean fully transparent background, no black backdrop, no additional graphics. This is a sprite for a landscape Unity game's level-one victory screen. Save as a usable PNG asset.

Prompt cuối cho tiêu đề:

> Style reference: bold bubbly white cartoon lettering with very thick black outline from this blue game banner. Generate ONLY a single line typographic game settings heading, exact Vietnamese text: CÀI ĐẶT. No ribbon, no panel, no background, no objects. White chunky rounded bold playful lettering with thick black outline and subtle blue-grey bottom shadow, matching reference's COMPLETED lettering. Accurate Vietnamese diacritics especially À, Đ, Ặ. Wide horizontal typography centered and tightly framed on fully transparent canvas, suitable as a Unity UI title sprite.
