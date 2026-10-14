from pathlib import Path
import json, hashlib, re
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parent
original = (ROOT / 'build_bento_report.py').read_text(encoding='utf-8-sig')
helpers = original.split('d.add_page_break()')[0]
start = helpers.index('# Reuse the academic cover')
end = helpers.index("normal=d.styles['Normal']")
helpers = helpers[:start] + helpers[end:]
helpers = helpers.replace("WORK=ROOT/'bento_report'", "WORK=ROOT/'bento_report_final'")
helpers = helpers.replace("REF=ROOT.parent/'Báo cáo dịch ngược SandSort rà soát ảnh và nội dung.docx'", "REF=ROOT.parent/'Báo cáo dịch ngược BentoLoop tìm license key.docx'")
helpers = helpers.replace("OUT=ROOT.parent/'Báo cáo dịch ngược BentoLoop tìm license key.docx'", "OUT=ROOT.parent/'Báo cáo thực hành dịch ngược BentoLoop hoàn chỉnh.docx'")
exec(compile(helpers, str(ROOT/'build_bento_report.py'), 'exec'))

# Retain the existing cover, identity, logo, page geometry, and footer.
for style_name in ['Title', 'Subtitle', 'Heading 1', 'Heading 2', 'Heading 3']:
    st = d.styles[style_name]
    st.font.color.rgb = RGBColor(0,0,0)
    st.font.underline = False
    pp = st.element.find(qn('w:pPr'))
    if pp is not None:
        for tag in ['w:pBdr', 'w:shd']:
            for el in list(pp.findall(qn(tag))): pp.remove(el)
for el in cover.xpath('.//w:p'):
    p = Paragraph(el,d)
    if p.text.strip() == 'DỊCH NGƯỢC': p.style=d.styles['Title']
    pp=p._p.find(qn('w:pPr'))
    if pp is not None:
        for border in list(pp.findall(qn('w:pBdr'))): pp.remove(border)
    for run in p.runs:
        run.font.color.rgb=RGBColor(0,0,0)

def oldfig(n, caption, width=16):
    source=ROOT/'bento_report'/f'figure-{n:02d}.png'
    im=Image.open(source)
    return fig(source,(0,0,im.width,im.height),caption,width)

def screenshot(uid,box,caption,width=16):
    source=TEMP/f'codex-clipboard-{uid}.png'
    evidence=WORK/'evidence'
    evidence.mkdir(exist_ok=True)
    saved=evidence/source.name
    if not saved.exists(): saved.write_bytes(source.read_bytes())
    return fig(saved,box,caption,width)

d.add_page_break()
p=d.add_paragraph('MỤC LỤC')
p.alignment=WD_ALIGN_PARAGRAPH.CENTER
p.paragraph_format.first_line_indent=Pt(0)
for r in p.runs: r.bold=True; r.font.size=Pt(18)
p=d.add_paragraph();p.paragraph_format.first_line_indent=Pt(0)
for kind in ['begin','instruction','separate','cached','end']:
    r=p.add_run()
    if kind=='instruction':
        e=OxmlElement('w:instrText');e.set(qn('xml:space'),'preserve');e.text=' TOC \\o "1-2" \\h \\z \\u ';r._r.append(e)
    elif kind=='cached': r.text='Mục lục'
    else:
        e=OxmlElement('w:fldChar');e.set(qn('w:fldCharType'),kind);r._r.append(e)
d.add_page_break()

heading('1 Mục tiêu và kết quả thực hành')
para('Bài thực hành phân tích cơ chế kiểm tra license của BentoLoop, tìm key được chấp nhận và sửa thư viện native để build lại APK. Kết quả tìm được là BENTO-EASY-2025. APK gốc báo kích hoạt thành công khi nhập key này và chuyển vào Level 1. Sau khi sửa VerifyLicense để trả về thành công ngay đầu hàm, APK được build và ký lại bằng APK Easy Tool. Bản APK mới báo kích hoạt thành công với chuỗi vinh123.')
para('Quá trình thực hiện gồm giải mã cấu trúc APK, tra cứu dữ liệu IL2CPP, phân tích mã native trong Ghidra, sửa hai lệnh ARM64 và kiểm thử ứng dụng. Các ảnh minh chứng được đặt cùng phần giải thích tương ứng để theo dõi dữ liệu đầu vào, thao tác và kết quả của từng bước.')
table(['Thành phần','Thông tin sử dụng'],[
('Môi trường','Windows trong máy ảo VMware Workstation; kiểm thử trên thiết bị Android kết nối với máy thật'),
('Ứng dụng','BentoLoop.apk; package com.ig.bento.loop; version name 1.0.0; version code 5'),
('Thư viện phân tích','lib/arm64-v8a/libil2cpp.so; kiến trúc AARCH64, little-endian, 64 bit'),
('APK Easy Tool','Phiên bản 1.60; Decompile bằng Apktool 2.6.1; Compile bằng Apktool 2.12.1'),
('Ghidra','Phiên bản 12.1.2; project Thuchanh2; Image Base 0x100000'),
('Dữ liệu dump','Gói libil2cpp_il2cppdump_20261007_0259 được cung cấp trong bài thực hành'),
],[4.2,12.7])

heading('2 Decompile APK và lấy thư viện native')
para('File đầu vào là C:\\Thuchanh2\\BentoLoop.apk trong máy ảo. Tại APK Easy Tool, Browse được dùng để chọn file, sau đó bấm Decompile. Công cụ hiển thị Decompile successful và nhận dạng package com.ig.bento.loop. APK khai báo min SDK 25 và target SDK 36.')
oldfig(1,'Decompile BentoLoop.apk bằng APK Easy Tool',15)
para('Kết quả nằm trong thư mục BentoLoop dưới 1-Decompiled APKs. Thư mục có assets, lib, res, các thư mục smali, AndroidManifest.xml và apktool.yml. File apktool.yml giúp công cụ nhận diện thư mục này khi Compile lại.')
oldfig(2,'Cấu trúc thư mục BentoLoop sau Decompile',14)
para('Thư viện được chọn để phân tích là libil2cpp.so trong lib\\arm64-v8a. Đây là mã native của phần IL2CPP trong ứng dụng. File có dung lượng hiển thị 71.122 KB, tương ứng 72.828.928 byte. Ở bước sửa hàm, chương trình được lưu thành bản sao trong Ghidra để giữ phần phân tích của bản gốc.')
oldfig(3,'Thư viện libil2cpp.so của bản ARM64',16)
code('Thư mục làm việc\nC:\\Users\\DucNM\\Documents\\APK Easy Tool\\1-Decompiled APKs\\BentoLoop\n\nThư viện cần thay khi build\nlib\\arm64-v8a\\libil2cpp.so')

heading('3 Xác định hàm kiểm tra license từ dữ liệu dump')
para('Bộ dump được cung cấp gồm dump.cs, script.json, stringliteral.json, các script hỗ trợ Ghidra và thư mục DummyDll. Trong bước này, dữ liệu dump được dùng để tra cứu tên lớp và địa chỉ; thư viện native được lấy từ APK đã Decompile.')
para('Tìm LicenseVerifier trong dump.cs cho kết quả lớp static thuộc namespace GeckoArrow.Runtime.LoadingSystem. VerifyLicense nhận string inputKey và trả về bool. Lớp còn có CheckTransformedLicense, IsLicenseActivated, SaveLicenseActivated và field EncryptedBytes. Thân hàm trong dump.cs không chứa logic thực thi nên cần mở thư viện trong Ghidra để đọc mã native.')
oldfig(4,'Lớp LicenseVerifier và địa chỉ các phương thức trong dump.cs',16)
table(['Phương thức','RVA','Địa chỉ trong Ghidra'],[
('VerifyLicense','0x1E7689C','0x1F7689C'),
('CheckTransformedLicense','0x1E76FA0','0x1F76FA0'),
('IsLicenseActivated','0x1E76EF4','0x1F76EF4'),
('SaveLicenseActivated','0x1E769FC','0x1F769FC'),
('.cctor','0x1E77110','0x1F77110'),
],[7.3,4.8,4.8])
para('RVA là địa chỉ tương đối so với Image Base. Cột địa chỉ Ghidra trong bảng đã cộng Image Base 0x100000. Với VerifyLicense, dump còn ghi File Offset 0x1E7289C. Offset này chỉ vị trí trong file và khác địa chỉ hiển thị trong Ghidra.')

heading('4 Import thư viện và gắn nhãn trong Ghidra')
heading('4.1 Import libil2cpp và kiểm tra Image Base',2)
para('libil2cpp.so được import vào project Thuchanh2. Ghidra nhận dạng file ELF với language AARCH64:LE:64:v8A:default. Sau khi mở CodeBrowser, phân tích tự động được chạy để nhận diện các hàm và tham chiếu.')
oldfig(5,'Ghidra nhận dạng thư viện ELF ARM64',14)
para('Memory Map hiển thị Image Base 00100000. Vì vậy, địa chỉ cần nhập trong Go To được tính bằng RVA cộng 0x100000. Phép quy đổi này được dùng nhất quán khi tìm hàm và đối chiếu chuỗi.')
code('Địa chỉ Ghidra = Image Base + RVA\n0x00100000 + 0x01E7689C = 0x01F7689C')
oldfig(7,'Memory Map xác nhận Image Base 0x100000',16.8)
heading('4.2 Gắn tên từ script json',2)
para('Thư mục chứa ghidra.py được thêm vào Script Manager. Script được chạy với file script.json trong gói dump BentoLoop. Kết quả gắn tên giúp Decompiler hiển thị các phương thức như LicenseVerifier$$VerifyLicense, String.Trim và String.Equals thay cho phần lớn tên tự động.')
oldfig(8,'Chọn script.json của BentoLoop để gắn tên trong Ghidra',14.5)
para('FUN_01f7689c là tên tự động của Ghidra tại địa chỉ 0x1F7689C. Sau khi áp dụng dữ liệu dump, cùng vị trí đó có tên GeckoArrow.Runtime.LoadingSystem.LicenseVerifier$$VerifyLicense. Nhãn được đổi để dễ đọc mã giả; mã máy chỉ thay đổi ở bước patch sau đó.')

heading('5 Phân tích điều kiện chấp nhận license')
heading('5.1 Theo dõi đầu vào trong VerifyLicense',2)
para('Go To được dùng để mở VerifyLicense tại 0x1F7689C. Phần đầu hàm gọi String.IsNullOrWhiteSpace để kiểm tra đầu vào. Với chuỗi có nội dung, String.Trim bỏ khoảng trắng ở hai đầu, sau đó String.Equals so sánh kết quả với StringLiteral_2770. Tham số kiểu so sánh hiển thị trong mã giả là 4.')
oldfig(9,'VerifyLicense kiểm tra đầu vào và so sánh với StringLiteral_2770',16.8)
para('Khi phép so sánh trực tiếp không khớp, luồng gọi CheckTransformedLicense. Nếu nhánh này cũng không chấp nhận chuỗi, SecurityAuthService.DispatchVerification được gọi tiếp. Do đó, StringLiteral_2770 là đầu mối để tìm một key có thể được chấp nhận ngay ở nhánh đầu.')
heading('5.2 Xác định nhánh trả về thành công',2)
para('Trong phần cuối VerifyLicense, nhánh chuỗi khớp chọn thông báo log rồi đi đến phép gán giá trị trả về bằng 1. Nhánh thất bại chuyển đến LAB_01f76914 và trả về 0. Kết hợp phép so sánh và giá trị trả về cho thấy chuỗi tại StringLiteral_2770 có vai trò là key được chấp nhận.')
oldfig(10,'Nhánh thành công trả về 1 và nhánh thất bại trả về 0',16.8)
para('Có thể rút gọn điều kiện quan sát được thành mã giả dưới đây. Phần khởi tạo kiểu dữ liệu và các lời gọi ghi log được lược đi để tập trung vào quyết định chấp nhận license.')
code('if (IsNullOrWhiteSpace(inputKey)) return false;\nkey = Trim(inputKey);\nif (Equals(key, StringLiteral_2770, 4)) return true;\nif (CheckTransformedLicense(key)) return true;\nreturn SecurityAuthService.DispatchVerification(key);')
para('Nhánh CheckTransformedLicense còn đối chiếu từng ký tự với một mảng byte bằng XOR 0x53 và ((i × 13) AND 0xFF). Nhánh so sánh trực tiếp đã cho phép tìm được key dùng trong thử nghiệm, nên không cần giải toàn bộ mảng để hoàn thành kết quả này.')

heading('6 Tìm key và kiểm chứng trên APK gốc')
heading('6.1 Đọc chuỗi trong Ghidra và đối chiếu JSON',2)
para('Bấm đúp StringLiteral_2770 trong lời gọi String.Equals chuyển Listing đến 044fce70. Chú thích tại nhãn hiển thị BENTO-EASY-2025. Địa chỉ này tương ứng 0x044FCE70; Ghidra hiển thị chữ số hex thường và không thêm tiền tố 0x.')
oldfig(13,'StringLiteral_2770 tại 0x044FCE70 chứa BENTO-EASY-2025',16.8)
para('Trong stringliteral.json, tìm 43FCE70 cho mục có value là BENTO-EASY-2025 và address là 0x43FCE70. Cộng Image Base 0x100000 cho địa chỉ JSON được 0x44FCE70, khớp Listing. Hai địa chỉ thể hiện cùng vị trí sau khi quy đổi.')
code('0x043FCE70 + 0x00100000 = 0x044FCE70')
oldfig(14,'Giá trị và địa chỉ tương ứng trong stringliteral.json',13)
para('Đối chiếu JSON xác nhận nội dung của nhãn. Vai trò của chuỗi được xác định từ phép String.Equals và nhánh trả về thành công trong VerifyLicense. BENTO_LICENSE_ACTIVATED ở vị trí khác là khóa lưu trạng thái kích hoạt, không phải key cần nhập.')
heading('6.2 Thử key trên bản APK gốc',2)
para('APK gốc được mở trên thiết bị Android. Nhập BENTO-EASY-2025 vào màn hình BENTO SECURITY SYSTEM rồi bấm XÁC NHẬN (VERIFY). Ứng dụng hiển thị thông báo màu xanh KÍCH HOẠT THÀNH CÔNG! ĐANG VÀO GAME...')
fig(ROOT/'bento_report'/'evidence'/'original_apk_valid_key.png',(82,333,563,717),'APK gốc báo kích hoạt thành công với BENTO-EASY-2025',11.8)
para('Sau khi xác nhận, trò chơi chuyển vào Level 1. Kết quả này kiểm chứng key tìm được bằng phân tích tĩnh có hiệu lực trong APK gốc.')
oldfig(16,'Màn chơi Level 1 sau khi vượt kiểm tra license trên APK gốc',7.4)

heading('7 Sửa VerifyLicense và xuất thư viện')
heading('7.1 Sửa hai lệnh đầu hàm',2)
para('Chương trình được lưu thành bản sao libil2cpp_patched trong project Ghidra. Tại địa chỉ 0x1F7689C, hai lệnh đầu của VerifyLicense được thay bằng mov w0, #0x1 và ret qua Patch Instruction. Lệnh đầu ghi giá trị 1 vào thanh ghi trả về, lệnh sau kết thúc hàm ngay.')
table(['Địa chỉ Ghidra','Lệnh trước khi sửa','Lệnh sau khi sửa'],[
('0x1F7689C','str x30, [sp, #-0x20]!','mov w0, #0x1'),
('0x1F768A0','stp x20, x19, [sp, #0x10]','ret'),
],[4.6,7.0,5.3])
code('0x1F7689C   20 00 80 52   mov w0, #0x1\n0x1F768A0   C0 03 5F D6   ret')
screenshot('66cfd80d-de62-4ff1-af14-ce955325e492',(1246,170,1915,322),'VerifyLicense sau patch hiển thị return 1 trong Decompiler',16.8)
para('Hai lệnh mới thực hiện trước khi hàm cấp phát stack hoặc gọi hàm con, nên kết quả trả về không còn phụ thuộc các nhánh kiểm tra phía sau. Decompiler rút gọn thân hàm thành return 1. Phần lệnh cũ còn nằm trong file nhưng không được đi tới từ điểm vào hàm này.')
heading('7.2 Xuất file có chứa phần mã đã sửa',2)
para('Sau khi lưu chương trình Ghidra, File → Export Program được dùng để xuất thư viện. Format được chọn là Original File, đồng thời tích Export User Byte Modifications trong Options. Tùy chọn này đưa phần byte vừa patch vào file ELF kết quả.')
screenshot('048ee7fd-98e5-43f2-ba4c-034ebc53bb5b',(898,423,1225,624),'Bật Export User Byte Modifications khi xuất Original File',10.2)
para('Export Results Summary hiển thị file đích C:\\Thuchanh2\\libil2cpp_patched, Format Original File và kích thước 72.828.928 byte. File xuất được đổi tên thành libil2cpp.so để thay đúng tên thư viện mà ứng dụng tải.')
screenshot('8da9a840-6263-4f6e-978a-dc8c373ccf51',(700,750,1447,838),'File ELF xuất ra giữ kích thước 72.828.928 byte',15.3)
heading('7.3 Thay thư viện trong thư mục đã Decompile',2)
para('File libil2cpp.so đã xuất được copy vào lib\\arm64-v8a của thư mục BentoLoop đã Decompile. Khi Windows báo trùng tên, chọn Replace the file in the destination. Đầu ra của bước này là thư mục APK chứa thư viện ARM64 đã patch, sẵn sàng dùng làm đầu vào Compile.')
screenshot('b9767d67-a577-45d4-a515-bbb923631f8e',(532,254,1316,847),'Thay libil2cpp.so trong thư mục arm64-v8a',15.4)

heading('8 Build APK và kiểm thử bản đã sửa')
heading('8.1 Cập nhật công cụ build',2)
para('Trong APK Easy Tool, Select decompiled APK được dùng để chọn thư mục BentoLoop chứa apktool.yml, sau đó bấm Compile. Lần build đầu với Apktool 2.6.1 dừng ở manifest do không nhận thẻ property. Thư viện đã patch không phải thành phần gây ra lỗi này.')
para('Apktool 2.12.1 được thêm vào thư mục Apktool của APK Easy Tool và chọn trong Options → General. Giao diện cũ vẫn có thể thêm tham số --use-aapt2, khiến bản mới báo Unrecognized option. Ô Use the aapt2 binary instead of aapt trong Options → Apktool được bỏ tích. Apktool 2.12.1 sử dụng AAPT2 mặc định khi build.')
screenshot('1c77ab7f-9dac-4205-8b81-6da57f4fb61c',(480,282,1169,878),'Cấu hình Apktool 2.12.1 và bỏ tùy chọn thêm use aapt2',15.2)
heading('8.2 Làm mới framework và Compile',2)
para('Sau khi bỏ tham số cũ, quá trình build tiếp tục nhưng báo attribute android:enableOnBackInvokedCallback not found. Log cho thấy công cụ đang dùng framework được lưu tại C:\\Users\\DucNM\\AppData\\Local\\apktool\\framework\\1.apk. Bộ framework cũ chưa có thuộc tính này.')
screenshot('3764b83f-1742-4d98-a5c6-2240115dfc53',(555,477,1227,831),'Lỗi thiếu enableOnBackInvokedCallback khi dùng framework cũ',16)
para('File 1.apk được đổi tên thành 1.apk.bak để giữ bản sao. Khi Compile lại, Apktool tạo file 1.apk mới từ framework đi kèm phiên bản 2.12.1. Ảnh thư mục cho thấy file mới có dung lượng 4.643 KB, còn bản sao cũ là 3.845 KB. Sau bước này, quá trình build tiến tới tạo và ký APK thành công.')
screenshot('ab74fd96-a6f9-43df-8d64-438e6fb36e29',(551,270,1334,446),'Framework mới được tạo bên cạnh bản sao 1.apk.bak',15.4)
heading('8.3 Lấy APK đã build và ký',2)
para('APK Easy Tool hiển thị Sign successful sau quá trình build và ký. Bấm Compiled APK directory để mở thư mục 2-Recompiled APKs. File đầu ra là BentoLoop.apk, có dung lượng hiển thị 135.005 KB. Đây là APK dùng để kiểm thử bản đã sửa.')
screenshot('9d8363bc-1a72-44fd-9509-67c6f90bb106',(551,299,1238,899),'APK Easy Tool hoàn tất và báo Sign successful',14.6)
screenshot('4e4a438e-5f2b-4b0e-984e-022becd8c31b',(551,270,1334,442),'BentoLoop.apk trong thư mục 2 Recompiled APKs',15.4)
para('APK được chuyển từ máy ảo sang máy thật để đưa lên thiết bị Android. Khi kiểm thử, cần dùng bản APK vừa build và bắt đầu từ trạng thái chưa kích hoạt, tránh nhầm với trạng thái được lưu từ lần nhập key đúng trước đó.')
heading('8.4 Kiểm thử với chuỗi khác key đã tìm được',2)
para('Trên bản APK mới, nhập vinh123 rồi bấm XÁC NHẬN (VERIFY). Ứng dụng hiển thị KÍCH HOẠT THÀNH CÔNG! ĐANG VÀO GAME... dù chuỗi này khác BENTO-EASY-2025. Ảnh dưới được ghi nhận từ APK đã sửa, còn ảnh ở mục 6.2 thuộc APK gốc.')
fig(ROOT/'bento_report'/'evidence'/'patched_apk_invalid_key.png',(68,341,575,725),'APK đã sửa báo kích hoạt thành công với chuỗi vinh123',12.4)
para('Kết quả phù hợp với việc VerifyLicense trả về 1 ngay khi được gọi. Thử nghiệm xác nhận phần kiểm tra license tại hàm này đã được vòng tránh trên bản APK build lại. Phạm vi sửa là thư viện arm64-v8a và luồng kiểm tra license đã quan sát; thử nghiệm không đánh giá những chức năng khóa khác trong trò chơi.')
table(['Bản ứng dụng','Chuỗi thử nghiệm','Kết quả ghi nhận'],[
('APK gốc','BENTO-EASY-2025','Báo kích hoạt thành công và vào Level 1'),
('APK đã sửa','vinh123','Báo kích hoạt thành công và đang vào game'),
],[4.3,5.0,7.6])

heading('9 Nhận xét và phương án phòng tránh')
heading('9.1 Nguyên nhân cơ chế có thể bị vòng tránh',2)
para('Key BENTO-EASY-2025 được lưu dưới dạng chuỗi và so sánh trong thư viện phía client. Dữ liệu dump giúp xác định tên phương thức, từ đó lần đến phép so sánh và nội dung key. Nhánh kiểm tra mảng XOR không bảo vệ được nhánh chấp nhận chuỗi trực tiếp đang tồn tại song song.')
para('Việc sửa hai lệnh đầu VerifyLicense cho thấy quyết định chấp nhận license phụ thuộc vào một giá trị trả về có thể thay đổi trong mã client. APK sau khi ký lại vẫn chạy được trong thử nghiệm và chấp nhận chuỗi khác key. Kết quả này cho thấy cơ chế bảo vệ đã quan sát chưa ngăn được cách can thiệp đã thực hiện.')
heading('9.2 Các biện pháp giảm rủi ro',2)
para('Với ứng dụng có máy chủ, quyền sử dụng nên được xác minh theo tài khoản hoặc license trên máy chủ. Các thao tác cần quyền phải tiếp tục được kiểm tra tại nơi cung cấp dữ liệu hoặc dịch vụ. Cách này hạn chế việc sửa một màn hình hoặc một hàm client để nhận toàn bộ quyền sử dụng.')
para('Không đưa key chung và nhánh chấp nhận thử nghiệm vào bản phát hành. Nếu cần hoạt động offline, license có thể được ký số: client giữ khóa công khai để kiểm tra, còn khóa riêng phát hành license được giữ ngoài ứng dụng. Cơ chế này giúp tránh nhúng một bí mật chung có thể đọc trực tiếp từ APK.')
para('Kiểm tra chữ ký ứng dụng, tính toàn vẹn thư viện và phát hiện môi trường can thiệp có thể bổ sung khả năng phát hiện APK bị sửa. Làm rối mã giúp tăng chi phí phân tích nhưng không thay thế việc xác minh quyền ở phía tin cậy. Với môi trường offline, vẫn cần tính đến khả năng mã kiểm tra bị patch và lựa chọn mức bảo vệ phù hợp.')
heading('9.3 Kết luận thực hành',2)
para('Bài thực hành hoàn thành việc tìm key BENTO-EASY-2025 và kiểm chứng trên APK gốc. VerifyLicense được sửa bằng hai lệnh ARM64 để trả về thành công, sau đó thư viện được xuất, thay vào thư mục đã Decompile và build lại bằng APK Easy Tool. Lỗi công cụ được xử lý bằng Apktool 2.12.1, bỏ tham số use aapt2 và làm mới framework. APK đã ký báo kích hoạt thành công với chuỗi vinh123.')

heading('Tài liệu tham khảo')
para('Yêu cầu báo cáo và hướng dẫn APK Easy Tool được cung cấp trong bài thực hành.')
for reference in [
    'Apktool 2.12.1\nhttps://github.com/iBotPeaches/Apktool/releases/tag/v2.12.1',
    'Tham số build của Apktool\nhttps://apktool.org/docs/2.x/cli-parameters/',
    'Quản lý framework trong Apktool\nhttps://apktool.org/wiki/in-depth/frameworks/',
    'Android Developers, phần tử property trong manifest\nhttps://developer.android.com/guide/topics/manifest/property-element',
    'OWASP MASVS RESILIENCE 2, bảo vệ ứng dụng trước can thiệp mã\nhttps://mas.owasp.org/MASVS/controls/MASVS-RESILIENCE-2/',
    'Android Developers, tổng quan Play Integrity API\nhttps://developer.android.com/google/play/integrity/overview',
]:
    p=para(reference);p.alignment=WD_ALIGN_PARAGRAPH.LEFT
    p.paragraph_format.first_line_indent=Pt(0)
    p.paragraph_format.keep_together=True

# Keep only relationships of retained and new images.
used={el.get(qn('r:embed')) for el in d.element.xpath('.//a:blip')}
for rid,rel in list(d.part.rels.items()):
    if rel.reltype.endswith('/image') and rid not in used:d.part.drop_rel(rid)
d.core_properties.title='Báo cáo thực hành dịch ngược BentoLoop tìm key và sửa kiểm tra license'
d.core_properties.subject='Tìm license key và kiểm thử APK có VerifyLicense đã sửa'
d.core_properties.author='Nguyễn Thành Vinh'
settings=d.settings.element
update=settings.find(qn('w:updateFields'))
if update is None:update=OxmlElement('w:updateFields');settings.append(update)
update.set(qn('w:val'),'true')
d.save(OUT)
assert hashlib.sha256(REF.read_bytes()).hexdigest()==ref_hash
check=Document(OUT)
captions=[p for p in check.paragraphs if re.match(r'^Hình \d+\.',p.text)]
assert len(captions)==len(figures)
assert all(p.alignment==WD_ALIGN_PARAGRAPH.CENTER and all(r.bold and r.italic for r in p.runs if r.text) for p in captions)
body='\n'.join(p.text for p in check.paragraphs)
assert not re.search(r'\b(tôi|chúng tôi|chúng ta|mình|bạn)\b',body,re.I)
assert 'chưa được thực hiện' not in body and 'SandSort' not in body
hashes=[hashlib.sha256(Path(f['file']).read_bytes()).hexdigest() for f in figures]
assert len(set(hashes))==len(hashes), 'Duplicate figure image'
manifest={'reference':str(REF),'reference_sha256':ref_hash,'output':str(OUT),'figures':figures,'key':'BENTO-EASY-2025','patched_test':'vinh123; successful activation shown on rebuilt APK','patch_bytes':'20 00 80 52 C0 03 5F D6','patch_address':'0x1F7689C','all_figures_unique':True,'render_pending':True}
(WORK/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
print('OUTPUT',OUT)
print('FIGURES',len(figures),'PARAGRAPHS',len(check.paragraphs))
