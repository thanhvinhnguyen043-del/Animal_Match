# Database và bảng xếp level

Mở **Tools → Level Database** trong Unity. Database mẫu nằm tại `Assets/Resources/LevelDatabase.asset`; game tự đọc asset này khi chưa gán database khác vào GridManager.

## Xếp tự động

1. Chọn level hoặc bấm **Them level**.
2. Chọn chế độ **Automatic**; chỉnh số cột/hàng, kích thước ô, thời gian, thưởng combo, trọng lực và số loại thú.
3. Chỉnh seed và **Ngau nhien moi lan choi**. Tắt tùy chọn này để cùng seed luôn tạo cùng bố cục.
4. Tỉ lệ băng/chặn có giá trị 0–1 và tổng không vượt quá 1. Các thú được sinh theo cặp.
5. Bấm **Luu database**.

## Xếp tay

1. Bấm **Sinh tu dong va chuyen sang xep tay** để lấy bố cục mẫu, hoặc chọn **Manual** rồi **Tao/doi kich thuoc ban**.
2. Chọn **But: ID thu**, trạng thái Normal/Ice/Barrier, rồi bấm ô để đặt thú. ID bắt đầu từ 0 theo danh sách hình thú ở trên.
3. Bật **But xoa** để tạo ô trống. Khi đổi kích thước, các ô được giữ theo thứ tự danh sách; cần kiểm tra lại vị trí.
4. Kiểm tra thông báo phía dưới, rồi bấm **Luu database**. Mỗi loại thú phải có số lượng chẵn.
5. Bấm **Gan database va level nay vao GridManager trong scene**, lưu scene, rồi Play và vào game bằng nút chơi hiện có.

Database chứa sẵn Level 1 tự động, Level 2 xếp tay với các cặp liền nhau và Level 3 tự động có băng. Thêm/nhân bản/xóa level trong cửa sổ; Undo được hỗ trợ.

## Nút chọn level

Trong Button → On Click, chọn `UIManager.StartLevel(int)` với tham số `0` cho level đầu, `1` cho level thứ hai, v.v. `StartGameplay()` hiện có sẽ chơi level đang chọn trong GridManager. Restart giữ level hiện tại; chế độ Manual giữ nguyên bố cục, Automatic sinh lại theo seed/tùy chọn ngẫu nhiên.

Trong SampleScene, bảng kết quả có nút **Màn tiếp theo**. Nút chỉ hiện khi thắng và còn level kế tiếp trong database; thua hoặc thắng màn cuối sẽ ẩn nút. Bấm nút sẽ sang level kế tiếp, đặt lại điểm/combo/thời gian và đóng bảng kết quả. Nếu level kế tiếp có dữ liệu không hợp lệ, game giữ bảng thắng và báo lỗi trong Console.

## Luật và kiểm tra

- Khi nối thú thường với thú băng, thú thường bị xóa còn thú băng mất lớp băng và trở thành thú thường. Hai thú băng nối nhau đều mất lớp băng; hai thú thường nối nhau đều bị xóa. Bộ đếm thắng chỉ giảm khi thú thực sự bị loại.
- Barrier mở khóa khi có cặp được nối cạnh nó. Hidden không dùng trong trình xếp level vì game hiện chưa có cơ chế mở thú ẩn.
- Kiểm tra dữ liệu phát hiện kích thước sai, ID sai, số thú lẻ và thiếu ô có thể chọn. Điều này **không đảm bảo bàn luôn giải được**. Bố cục tự động hoặc xếp tay có thể bị bí đường; hãy chơi thử, dùng gợi ý/đảo bàn và điều chỉnh chướng ngại.
- Cửa sổ này là công cụ thiết kế trong Unity Editor; dữ liệu level là asset đi cùng bản build. Chưa có bảng chỉnh level dành cho người chơi trong bản build.
