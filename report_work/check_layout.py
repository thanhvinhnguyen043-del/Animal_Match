from pathlib import Path
import json, html
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import Paragraph, Table, TableStyle
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib import colors
from PIL import Image

root=Path(__file__).parent
manifest=json.loads((root/'report_manifest.json').read_text(encoding='utf-8'))
for name,filename in [('TimesVN','times.ttf'),('TimesVNB','timesbd.ttf'),('TimesVNI','timesi.ttf'),('Mono','consola.ttf')]:
    pdfmetrics.registerFont(TTFont(name,str(Path('C:/Windows/Fonts')/filename)))
pdfmetrics.registerFontFamily('TimesVN',normal='TimesVN',bold='TimesVNB',italic='TimesVNI',boldItalic='TimesVNB')
cm=72/2.54
width,height=21*cm,29.7*cm
left,right,top,bottom=2.1*cm,2.1*cm,1.8*cm,1.8*cm
usable=width-left-right
pdf=canvas.Canvas(str(root/'layout_proxy.pdf'),pagesize=(width,height))
styles={
 'p':ParagraphStyle('p',fontName='TimesVN',fontSize=11.5,leading=13.8,spaceAfter=6,alignment=4),
 'title':ParagraphStyle('title',fontName='TimesVNB',fontSize=20,leading=24,spaceAfter=7),
 'h1':ParagraphStyle('h1',fontName='TimesVNB',fontSize=15,leading=18,spaceAfter=7),
 'h':ParagraphStyle('h',fontName='TimesVNB',fontSize=12.5,leading=15,spaceAfter=7),
 'code':ParagraphStyle('code',fontName='Mono',fontSize=9,leading=11,spaceAfter=8),
 'caption':ParagraphStyle('caption',fontName='TimesVNI',fontSize=10,leading=12,spaceAfter=8,alignment=1),
 'cell':ParagraphStyle('cell',fontName='TimesVN',fontSize=10.5,leading=12.4),
}
figure=0
report=[]
for i,page in enumerate(manifest['pages']):
    y=height-top
    def para(text,sty,w=usable):
        global y
        obj=Paragraph(html.escape(text).replace('\n','<br/>'),styles[sty])
        _,ph=obj.wrap(w,10000)
        obj.drawOn(pdf,left,y-ph)
        y-=ph+styles[sty].spaceAfter
    para(page['title'],'title' if i==0 else 'h1')
    for block in page['blocks']:
        k=block[0]
        if k in ['p','h','code']:
            para(block[1],k)
        elif k=='fig':
            figure+=1
            path=manifest['figures'][block[1]]
            iw,ih=Image.open(path).size
            fw=block[3]*cm;fh=fw*ih/iw
            pdf.drawImage(path,left+(usable-fw)/2,y-fh,width=fw,height=fh)
            y-=fh+8
            para(f'Hình {figure}. {block[2]}','caption')
        elif k=='table':
            data=[[Paragraph(html.escape(v),styles['cell']) for v in row] for row in [block[1]]+block[2]]
            t=Table(data,colWidths=[v*cm for v in block[3]])
            t.setStyle(TableStyle([('GRID',(0,0),(-1,-1),.4,colors.HexColor('#D9D9D9')),('BACKGROUND',(0,0),(-1,0),colors.HexColor('#E7EDF4')),('VALIGN',(0,0),(-1,-1),'MIDDLE'),('LEFTPADDING',(0,0),(-1,-1),4.5),('RIGHTPADDING',(0,0),(-1,-1),4.5),('TOPPADDING',(0,0),(-1,-1),4.5),('BOTTOMPADDING',(0,0),(-1,-1),7.5)]))
            _,th=t.wrap(usable,10000)
            t.drawOn(pdf,left,y-th)
            y-=th+14
    margin=y-bottom
    report.append({'page':i+1,'title':page['title'],'remaining_pt':round(margin,1),'overflow':margin<0})
    pdf.setFont('TimesVN',9);pdf.drawCentredString(width/2,25,f'{i+1} — Kiểm tra bố cục dự kiến')
    pdf.showPage()
pdf.save()
(root/'layout_check.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
for r in report:
    print(f"{r['page']:2d} {r['remaining_pt']:6.1f}pt {'OVERFLOW' if r['overflow'] else 'OK'} {r['title']}")
