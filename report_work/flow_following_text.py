from pathlib import Path
from docx import Document
root = Path(__file__).resolve().parent
path = root.parent / 'Bao cao dich nguoc SandSort bo sung day du.docx'
d = Document(path)
changes = 0
for p in d.paragraphs:
    if p.style.name.startswith('Heading') or p._p.xpath('.//w:drawing'):
        continue
    if p.text.strip() in {'UABEA', 'dnSpy', 'Il2CppDumper', 'Ghidra'}:
        p.paragraph_format.keep_with_next = True
        continue
    if p.text.strip() and p.paragraph_format.keep_with_next is True:
        p.paragraph_format.keep_with_next = False
        changes += 1
d.save(path)
print('Released text from following image blocks:', changes)
