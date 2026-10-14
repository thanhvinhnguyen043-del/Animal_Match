from pathlib import Path

work=Path(__file__).parent
source=(work/'build_report.py').read_text(encoding='utf-8')
definitions,authoring=source.split('doc = Document()',1)
exec(compile(definitions,str(work/'build_report.py'),'exec'))

OUT=ROOT/'Bao cao thuc hanh dich nguoc SandSort cap nhat moi truong.docx'
figs.update({
 'sample_zip':crop('sample_zip','334d69c1-2868-40b9-828d-491123c0e157',(309,135,1116,251)),
 'working_folders':crop('working_folders','6aa268f1-fbe7-4d4f-8f3e-8e010dc0d0e1',(307,135,977,367)),
 'uabea_startup':crop('uabea_startup','79096991-ce7c-4ee5-ac4c-c9854f3b8bc2',(254,144,1539,937)),
 'dnspy_startup':crop('dnspy_startup','a9ccb6f9-10c7-4309-890f-01e32cba0bda',(205,91,1918,967)),
 'dumper_startup':crop('dumper_startup','76a41fb1-c59f-43c2-9707-2f48e55c7a07',(207,90,1396,579)),
 'ghidra_startup':crop('ghidra_startup','a3a4d871-d376-4420-8ed5-d61bdb9b34ed',(589,219,1357,817)),
})

front=[
 {'title':PAGES[0]['title'],'blocks':[
  PAGES[0]['blocks'][0],PAGES[0]['blocks'][1],
  h('1 Môi trường và dữ liệu thực hành'),
  p('Thực hành được thực hiện trong Windows chạy trên VMware Workstation, với máy ảo có tên thuchanhdichnguoc1. Thư mục làm việc là C:\\LabGame. Gói mẫu đang lưu trong APK_goc có tên SandSort_14-9.zip; dữ liệu game giải nén được đặt trong APK_giai_nen. Kiến trúc dùng xuyên suốt phần dịch ngược là ARM64.'),
  table(['Thành phần','Thông tin và trạng thái'],[
   ['Gói mẫu game','SandSort_14-9.zip, lưu tại C:\\LabGame\\APK_goc. Tên và đuôi file được xác nhận bằng ảnh File Explorer.'],
   ['Thư viện được chọn','C:\\LabGame\\APK_giai_nen\\lib\\arm64-v8a\\libil2cpp.so; ELF AARCH64, little-endian, địa chỉ 64 bit.'],
   ['UABEA','Gói uabea-windows; chạy UABEANext4.Desktop.exe thành công. Đã xem Preview và xuất Texture2D ở phần thực hành.'],
   ['Il2CppDumper','Gói v6.7.46; ứng dụng khởi chạy đến hộp chọn file đầu vào. Lần phân tích trước đã tạo dump.cs, DummyDll và script.json.'],
   ['dnSpy','v6.1.8, 64 bit .NET; cửa sổ Assembly Explorer mở được. Đã tra cứu DummyDll của game.'],
   ['Ghidra','Gói v12.1.2; ghidraRun.bat mở được cửa sổ quản lý project. Đã phân tích thư viện và chạy script gắn tên thành công.']], [3.4,13.4]),
  p('Các ảnh ở mục 1.1 đến 1.3 chứng minh vị trí dữ liệu và trạng thái khởi chạy công cụ. Kiến trúc cùng mã nhận diện libil2cpp.so được đối chiếu ở mục 1.4 và phần import Ghidra. Các công cụ được giữ trong thư mục giải nén cùng thư viện và file cấu hình đi kèm.'),
 ]},
 {'title':'1.1 Gói mẫu và thư mục làm việc','blocks':[
  p('File Explorer hiển thị đầy đủ tên SandSort_14-9.zip tại C:\\LabGame\\APK_goc, loại Compressed (zipped) Folder, dung lượng hiển thị 110 605 KB. Đây là tên gói đầu vào thực tế được ghi nhận. Chưa xác minh gói ZIP là APK được đổi đuôi hay gói chứa APK bên trong; versionName và versionCode của ứng dụng cũng chưa được đọc. Vì vậy, không dùng phần 14-9 trong tên file để suy ra phiên bản Android của game.'),
  fig('sample_zip','Tên gói mẫu SandSort_14-9.zip trong thư mục APK_goc'),
  p('Thư mục C:\\LabGame tập hợp APK_goc, APK_giai_nen, Asset_xuat và các thư mục công cụ. APK_goc giữ gói mẫu; APK_giai_nen chứa dữ liệu phân tích; Asset_xuat chứa tài nguyên đã xuất. Các đầu ra dump hiện nằm trong thư mục Il2CppDumper-win-v6.7.46 (1), đúng vị trí đã được dùng để chạy script Ghidra.'),
  fig('working_folders','Thư mục làm việc LabGame với dữ liệu và các công cụ đã giải nén',16.4),
  p('Project Ghidra có tên dichnguoc và được lưu riêng tại vị trí đã chọn khi tạo project. Dữ liệu phân tích được giữ trong file project cùng thư mục dữ liệu đi kèm. Thư mục chương trình ghidra_12.1.2_PUBLIC_20260605 chứa bộ công cụ; các file dump.cs, DummyDll và script.json nằm trong thư mục Il2CppDumper được nêu trên. Các vị trí này giúp đối chiếu ảnh với kết quả thực hành.'),
 ]},
 {'title':'1.2 Khởi chạy UABEA và dnSpy','blocks':[
  h('UABEA'),
  p('Trong C:\\LabGame\\uabea-windows, chạy UABEANext4.Desktop.exe. Cửa sổ UABEANext xuất hiện với Workspace Explorer, Inspector và vùng mở asset. Ảnh cho thấy ứng dụng đã khởi chạy thành công; nội dung Preview và kết quả Export được trình bày ở mục 2.'),
  fig('uabea_startup','UABEANext mở thành công và file chạy hiện trong thư mục công cụ',14.0),
  h('dnSpy'),
  p('Trong C:\\LabGame\\dnSpy-net-win64, chạy dnSpy.exe. Thanh tiêu đề hiển thị dnSpy v6.1.8, 64 bit .NET và bảng Assembly Explorer xuất hiện. Đây là trạng thái công cụ đã mở được; việc tra cứu lớp và phương thức trong DummyDll được đối chiếu ở mục 4.'),
  fig('dnspy_startup','dnSpy v6.1.8 khởi chạy với bảng Assembly Explorer',14.4),
 ]},
 {'title':'1.3 Khởi chạy Il2CppDumper và Ghidra','blocks':[
  h('Il2CppDumper'),
  p('Chạy Il2CppDumper.exe trong C:\\LabGame\\Il2CppDumper-win-v6.7.46 (1). Console và hộp Open xuất hiện, yêu cầu Il2Cpp binary file. Ảnh chứng minh ứng dụng đã khởi chạy, chưa chọn đầu vào. Khi dump, chọn libil2cpp.so trước, rồi global-metadata.dat ở hộp tiếp theo; không dùng file metadata cho hộp chọn binary.'),
  fig('dumper_startup','Il2CppDumper đã khởi chạy và chờ chọn file binary đầu vào',14.1),
  h('Ghidra'),
  p('Chạy ghidraRun.bat trong C:\\LabGame\\ghidra_12.1.2_PUBLIC_20260605\\ghidra_12.1.2_PUBLIC. Cửa sổ quản lý project Ghidra hiện tên dichnguoc và dòng Ghidra startup complete. Trạng thái này chứng minh bộ công cụ mở được; Listing, Decompiler và kết quả gắn tên được trình bày ở phần phân tích native.'),
  fig('ghidra_startup','Ghidra hoàn tất khởi động và mở cửa sổ quản lý project',12.4),
 ]},
]
fingerprint=PAGES[1]
fingerprint['title']='1.4 Mã nhận diện thư viện được phân tích'
fingerprint['blocks'][2]=p('Gói đầu vào đang lưu có tên SandSort_14-9.zip. Chưa xác minh versionName, versionCode hoặc tên file APK trước khi đóng gói. Mã nhận diện dưới đây thuộc chính thư viện libil2cpp.so đã import, giúp đối chiếu bản native đang phân tích; không thay thế mã băm của gói ZIP hoặc APK.')
PAGES=front+[fingerprint]+PAGES[2:]

for pageobj in PAGES:
 for i,block in enumerate(pageobj['blocks']):
  if block[0]=='p':
   text=block[1]
   if 'Kết quả kèm báo cáo gồm file asset' in text:
    pageobj['blocks'][i]=p('Kết quả kèm báo cáo gồm file asset đã xuất trong Asset_xuat; dump.cs, DummyDll, script.json và ghidra.py đã thêm runtime Jython; project Ghidra đã lưu và ảnh minh chứng. Gói SandSort_14-9.zip cùng thư mục APK_giai_nen được giữ để đối chiếu theo hướng dẫn GVTH. Khi nộp, ghi tên gói đầu vào đã xác nhận ở mục 1.1 và mã nhận diện thư viện ở mục 1.4; bổ sung thông tin phiên bản ứng dụng nếu xác minh được từ APK.')

exec(compile('doc = Document()'+authoring,str(work/'build_report.py'),'exec'))

print('ORIGINAL_REPORT_PRESERVED')
