# Menu và tiến độ

Menu trong SampleScene dùng hai ảnh Play và Select Level mới. Play nằm trên Select Level.

- Người mới bắt đầu ở level 1.
- Play mở màn vừa chơi gần nhất. Sau khi thắng, Play chuyển sang màn kế tiếp; nếu thắng màn cuối, Play chơi lại màn cuối.
- Thắng màn N mở khóa màn N+1. Select Level tạo danh sách từ LevelDatabase; các màn chưa mở khóa bị khóa.
- Chơi lại màn cũ không mất các màn đã mở khóa.
- Thua hoặc về menu giữa trận giữ màn đó để Play thử lại. Bàn chơi được tạo lại khi vào level, không lưu từng ô của trận đang dở.
- Tiến độ lưu trên máy qua PlayerPrefs: Progress.LatestLevel và Progress.HighestUnlockedLevel, chỉ số bắt đầu từ 0.

LevelSelectionUI dùng nút mẫu ẩn trong SampleScene để tạo danh sách lúc chạy; thêm level trong database sẽ tự thêm nút. Danh sách có thể cuộn khi nhiều màn.
