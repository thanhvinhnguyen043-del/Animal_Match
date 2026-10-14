from pathlib import Path
from zipfile import ZipFile
from lxml import etree
from PIL import Image, ImageOps, ImageDraw
import json

ROOT=Path(__file__).resolve().parent
NS={'w':'http://schemas.openxmlformats.org/wordprocessingml/2006/main','a':'http://schemas.openxmlformats.org/drawingml/2006/main','r':'http://schemas.openxmlformats.org/officeDocument/2006/relationships'}
files={'uploaded':Path(r'C:\Users\ADMIN\Downloads\Báo cáo dịch ngược.docx'),'updated':ROOT.parent/'Bao cao thuc hanh dich nguoc SandSort cap nhat moi truong.docx'}
for tag,path in files.items():
    out=ROOT/('audit_'+tag);out.mkdir(exist_ok=True)
    with ZipFile(path) as z:
        xml=etree.fromstring(z.read('word/document.xml'))
        relxml=etree.fromstring(z.read('word/_rels/document.xml.rels'))
        rels={x.get('Id'):x.get('Target') for x in relxml}
        lines=[];pics=[];num=0
        for i,node in enumerate(xml.find('w:body',NS)):
            texts=node.xpath('.//w:t/text()',namespaces=NS)
            text=' | '.join(texts) if node.tag.endswith('tbl') else ''.join(texts)
            if text:lines.append(f'[{i}] {text}')
            for b in node.xpath('.//a:blip',namespaces=NS):
                num+=1;rid=b.get('{'+NS['r']+'}embed');target=rels.get(rid)
                if not target:continue
                imfile=out/f'fig_{num:02d}{Path(target).suffix}'
                imfile.write_bytes(z.read('word/'+target))
                extent=b.getparent().getparent().getparent().find('wp:extent',{'wp':'http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing'})
                info={'figure':num,'body_index':i,'file':str(imfile),'px':Image.open(imfile).size,'previous_text':lines[-1] if lines else ''}
                pics.append(info);lines.append(f'[{i}] IMAGE {num}: {imfile.name} {info["px"]}')
        (out/'text.txt').write_text('\n'.join(lines),encoding='utf-8')
        (out/'images.json').write_text(json.dumps(pics,ensure_ascii=False,indent=2),encoding='utf-8')
        for start in range(0,len(pics),6):
            batch=pics[start:start+6];sheet=Image.new('RGB',(1600,1120),'#eeeeee');draw=ImageDraw.Draw(sheet)
            for j,p in enumerate(batch):
                im=Image.open(p['file']).convert('RGB');im.thumbnail((785,330))
                x=(j%2)*800;y=(j//2)*373
                sheet.paste(im,(x+(800-im.width)//2,y+28));draw.text((x+8,y+5),f"Figure {p['figure']} | block {p['body_index']} | {p['px']}",fill='black')
            sheet.save(out/f'contact_{start//6+1}.png')
    print(tag, 'figures',len(pics),'blocks',len(xml.find('w:body',NS)), 'text',out/'text.txt')
