from pathlib import Path
from zipfile import ZipFile, ZIP_DEFLATED
from lxml import etree
import shutil, json, hashlib

root = Path(__file__).resolve().parent
target = root.parent / 'Bao cao dich nguoc SandSort bo sung day du.docx'
backup = root / 'before_continuous_headings.docx'
shutil.copy2(target, backup)
ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
w = '{' + ns['w'] + '}'
with ZipFile(target) as z:
    items = {n: z.read(n) for n in z.namelist()}
doc = etree.fromstring(items['word/document.xml'])
styles = etree.fromstring(items['word/styles.xml'])
heading_ids = set()
for s in styles.findall('w:style', ns):
    name = s.find('w:name', ns)
    if name is not None and name.get(w+'val', '').lower().startswith('heading '):
        heading_ids.add(s.get(w+'styleId'))
        pp = s.find('w:pPr', ns)
        if pp is None: pp = etree.SubElement(s, w+'pPr')
        br = pp.find('w:pageBreakBefore', ns)
        if br is None: br = etree.SubElement(pp, w+'pageBreakBefore')
        br.set(w+'val', '0')
changes = []
for p in doc.findall('.//w:body/w:p', ns):
    pp = p.find('w:pPr', ns)
    sid = pp.find('w:pStyle', ns) if pp is not None else None
    if sid is None or sid.get(w+'val') not in heading_ids: continue
    text = ''.join(p.itertext())
    br = pp.find('w:pageBreakBefore', ns)
    if br is None: br = etree.SubElement(pp, w+'pageBreakBefore')
    was = br.get(w+'val', '1')
    br.set(w+'val', '0')
    for b in p.findall('.//w:br', ns):
        if b.get(w+'type') == 'page': b.getparent().remove(b)
    sp = pp.find('w:spacing', ns)
    if sp is None: sp = etree.SubElement(pp, w+'spacing')
    sp.set(w+'before', '240')
    sp.set(w+'after', '120')
    changes.append({'heading': text, 'prior_page_break': was})
for name, xml in [('word/document.xml', doc), ('word/styles.xml', styles)]:
    items[name] = etree.tostring(xml, xml_declaration=True, encoding='UTF-8', standalone=True)
temp = target.with_suffix('.tmp.docx')
with ZipFile(temp, 'w', ZIP_DEFLATED) as z:
    for name, data in items.items(): z.writestr(name, data)
temp.replace(target)
with ZipFile(backup) as z:
    old = etree.fromstring(z.read('word/document.xml'))
    assert old.findall('.//w:t', ns) and [t.text for t in old.findall('.//w:t', ns)] == [t.text for t in doc.findall('.//w:t', ns)]
    for n in z.namelist():
        if n.startswith('word/media/'):
            assert hashlib.sha256(z.read(n)).digest() == hashlib.sha256(items[n]).digest()
(root/'continuous_headings_changes.json').write_text(json.dumps(changes, ensure_ascii=False, indent=2), encoding='utf-8')
print(f'Updated {len(changes)} headings; body text and image bytes unchanged.')
