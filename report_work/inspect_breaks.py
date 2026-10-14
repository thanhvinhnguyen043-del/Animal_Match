from docx import Document
from docx.oxml.ns import qn
from pathlib import Path
p=Path(r'C:\Users\ADMIN\Downloads\My project (1)\Bao cao dich nguoc SandSort bo sung day du.docx')
d=Document(p)
for s in d.styles:
 if s.type==1 and ('heading' in s.name.lower()):
  print('STYLE',s.name,'pagebreak',s.paragraph_format.page_break_before,'keepnext',s.paragraph_format.keep_with_next,'before',s.paragraph_format.space_before)
for i,p in enumerate(d.paragraphs):
 br=p._p.xpath('.//w:br[@w:type="page"]')
 if p.style.name.startswith('Heading') or br or p.paragraph_format.page_break_before:
  print(i,p.style.name,repr(p.text[:100]),'pagebreak',p.paragraph_format.page_break_before,'breaks',len(br))
print('SECTIONS',len(d.sections))
for s in d.sections: print('start',s.start_type)
