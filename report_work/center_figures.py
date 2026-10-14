from pathlib import Path
from docx import Document
from docx.enum.text import WD_ALIGN_PARAGRAPH
from docx.shared import Pt, RGBColor
from docx.text.paragraph import Paragraph
from zipfile import ZipFile
from lxml import etree
import shutil, re

root = Path(__file__).resolve().parent
target = root.parent / 'Bao cao dich nguoc SandSort bo sung day du.docx'
backup = root / 'before_center_figures.docx'
if not backup.exists():
    shutil.copy2(target, backup)
d = Document(backup)
pictures = captions = 0
for element in d.element.xpath('.//w:p'):
    p = Paragraph(element, d)
    has_picture = bool(p._p.xpath('.//w:drawing | .//w:pict'))
    is_caption = bool(re.match(r'^\s*Hình\s+\d+\.', p.text))
    if not (has_picture or is_caption):
        continue
    p.alignment = WD_ALIGN_PARAGRAPH.CENTER
    p.paragraph_format.left_indent = Pt(0)
    p.paragraph_format.right_indent = Pt(0)
    p.paragraph_format.first_line_indent = Pt(0)
    if has_picture:
        pictures += 1
        p.paragraph_format.keep_with_next = True
    if is_caption:
        captions += 1
        p.paragraph_format.keep_together = True
        for run in p.runs:
            run.bold = True
            run.italic = True
            run.font.color.rgb = RGBColor(0, 0, 0)
d.save(target)
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
with ZipFile(backup) as a, ZipFile(target) as b:
    ax = etree.fromstring(a.read('word/document.xml'))
    bx = etree.fromstring(b.read('word/document.xml'))
    assert [t.text for t in ax.findall('.//w:t', ns)] == [t.text for t in bx.findall('.//w:t', ns)]
    media = [n for n in a.namelist() if n.startswith('word/media/')]
    assert all(a.read(n) == b.read(n) for n in media)
assert pictures == 41 and captions == 40
print(f'Centered {pictures} image paragraphs; formatted {captions} captions bold italic. Text and image bytes preserved.')
