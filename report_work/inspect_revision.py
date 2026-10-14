from pathlib import Path
import json, sys
import pdfplumber, subprocess
from PIL import Image, ImageDraw
root=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parent/'revision_render'
subprocess.run([r'C:\Users\ADMIN\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\poppler\Library\bin\pdftoppm.exe','-r','110','-png',str(root/'word_actual.pdf'),str(root/'page')],check=True)
d=pdfplumber.open(root/'word_actual.pdf')
stats=[]
for n,p in enumerate(d.pages):
    text=p.extract_text() or ''
    stats.append({'page':n+1,'chars':len(text),'text':text,'bottom':max([b['bottom'] for b in p.chars],default=0)})
for start in range(0,len(d.pages),6):
    sheet=Image.new('RGB',(1530,1470),'#dddddd');draw=ImageDraw.Draw(sheet)
    for j in range(min(6,len(d.pages)-start)):
        im=Image.open(root/f'page-{start+j+1:02}.png');im.thumbnail((490,690))
        x=(j%3)*510;y=(j//3)*735
        sheet.paste(im,(x+10,y+30));draw.text((x+10,y+8),str(start+j+1),fill='black')
    sheet.save(root/f'contact-{start//6+1}.png')
(root/'pdf_text.json').write_text(json.dumps(stats,ensure_ascii=False,indent=2),encoding='utf-8')
for s in stats:
    print(s['page'],s['chars'],s['text'][:90].replace('\n',' | '))
