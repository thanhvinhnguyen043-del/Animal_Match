# Scene gốc duy nhất

Mở `Assets/Scenes/SampleScene.unity` rồi bấm Play. Đây là scene duy nhất trong Build Settings.

SampleScene chứa MainMenu_Panel, LevelSelect_Panel, Gameplay_Panel, Settings_Panel và GameOver_Panel. UIManager dùng chế độ Combined: chuyển giữa các panel trong cùng scene, không tải scene khác khi Play hoặc Return Home.

Play tiếp màn đã lưu; thắng mở khóa màn kế; danh sách level hỗ trợ kéo/vuốt; âm thanh và các nút ON/OFF vẫn hoạt động. Ảnh thú lấy từ Assets/Resources/LevelDatabase.asset.

Hai scene đã tách trước đây cùng file meta được giữ ngoài Assets tại `Logs/SceneBackups/SingleScene-2026-10-10/` để có thể khôi phục khi cần. Prefab SettingsPanel vẫn được giữ làm tài nguyên tham khảo; giao diện đang chạy nằm trong SampleScene.
