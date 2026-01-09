# Các thay đổi mới

## 1. Thêm cơ chế CheckRow
- `Board.cs`: Bổ sung hệ thống **CheckRow**
  - `MoveItemToCheckRow()` – Di chuyển item vào CheckRow
  - `CheckRowMatchAndCollapse()` – Xử lý match và collapse trong CheckRow
  - `OrganizeCheckRowByType()` – Sắp xếp item trong CheckRow theo loại
  - `CheckRowForMatches()` – Phát hiện match từ 3 item trở lên trong CheckRow
  - `ShiftCheckRowItemsLeft()` – Dồn item sang trái sau khi clear

- Cơ chế **thắng / thua**
  - **THẮNG (WIN)**: Clear toàn bộ item trên board → `GAME_WIN`
  - **THUA (LOSE)**:
    - CheckRow đầy **và không phải chế độ TIMER** → `GAME_OVER`
    - **Chế độ TIMER**: Không tự động thua (có thể trả item về board)

- Các hàm kiểm tra trạng thái:
  - `Board.IsBoardEmpty()` – Kiểm tra board đã được clear hoàn toàn
  - `Board.IsCheckRowFull()` – Kiểm tra CheckRow đã đầy hay chưa

---

## 2. Các chế độ chơi (Game Modes)
*(Mới trong `GameManager`)*

Được thêm vào enum `GameManager.eLevelMode`:

### AUTOPLAY Mode
- Tự động chơi sau lần click đầu tiên
- **Chiến thuật**: Ưu tiên chọn item có cùng loại với các item đang có trong CheckRow
- **Mục tiêu**: Thắng bằng cách clear toàn bộ board
- Hàm hỗ trợ:
  - `BoardController.GetBestCellForAutoPlay()`

### AUTOLOSE Mode
- Tự động chơi sau lần click đầu tiên
- **Chiến thuật**: Liên tục chọn cell khả dụng đầu tiên
- **Mục tiêu**: Thua bằng cách làm đầy CheckRow

### TIMER Mode (Đã chỉnh sửa)
- **MỚI**: Không tự động thua khi CheckRow bị đầy
- **MỚI**: Có thể click vào item trong CheckRow để trả lại vị trí ban đầu trên board
- `Board.ReturnItemFromCheckRow()`:
  - Trả item về đúng cell ban đầu trên board
  - Dồn các item còn lại trong CheckRow sang trái
  - Cho phép tiếp tục gameplay



## 3. Các file đã chỉnh sửa
- `Board.cs` – Cơ chế CheckRow, phát hiện match
- `Cell.cs` – Gán item kèm theo thông tin cell ban đầu
- `Item.cs` – Thuộc tính lưu cell ban đầu
- `BoardController.cs` – Xử lý input, logic autoplay
- `GameManager.cs` – Chọn và khởi tạo chế độ chơi
