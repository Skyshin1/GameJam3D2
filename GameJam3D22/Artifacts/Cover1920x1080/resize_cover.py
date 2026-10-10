from pathlib import Path
import base64
import xml.etree.ElementTree as ET

OUT = Path(__file__).resolve().parent
SOURCE = Path('C:/Users/Windows11/Downloads/Frame1.svg')
PHOTO = Path('C:/Users/Windows11/Downloads/1.png')
SVG = 'http://www.w3.org/2000/svg'
XLINK = 'http://www.w3.org/1999/xlink'
ET.register_namespace('', SVG)
ET.register_namespace('xlink', XLINK)
tree = ET.parse(SOURCE)
root = tree.getroot()
root.set('width', '1920')
root.set('height', '1080')
root.set('viewBox', '0 0 1920 1080')
root.set('version', '1.1')
root.set('id', 'Fluffy-Defense-Cover-1920x1080')
layer, defs = list(root)
nodes = list(layer)
nodes[0].set('d', 'M1920 0H0V1080H1920V0Z')
nodes[2].set('d', 'M1920 0H0V1080H1920V0Z')
# The original image rectangle is already proportional to the supplied PNG.
# Crop 180 px from each vertical edge instead of squashing the artwork.
nodes[1].set('y', '-180')
for e in defs.iter():
    if e.tag == '{'+SVG+'}clipPath':
        e[0].set('height', '1080')
    if e.tag == '{'+SVG+'}image':
        e.set('{'+XLINK+'}href', 'data:image/png;base64,'+base64.b64encode(PHOTO.read_bytes()).decode())

# Independently re-space the original outlined text; widths and letterforms stay exact.
offsets = {3:-17, 4:-51, 5:-51, 6:-112, 7:-110, 8:-120,
           9:-143, 10:-152, 11:-164, 12:-164, 13:-196,
           14:-197, 15:-200, 16:-203, 17:-206, 18:-310, 19:-332}
names = ['Background-color','Gameplay-image','Readability-gradient','Game-project-label',
         '72h-badge-background','72h-badge-text','Tagline','Title-FLUFFY','Title-DEFENSE',
         'Genre-subtitle','Divider','My-role-label','Role-description','Project-overview-label',
         'Overview-line-1','Overview-line-2','Overview-line-3','Overview-line-4',
         'Unity-footer','Portfolio-footer']
for i,n in enumerate(nodes):
    n.set('id', names[i])
    if i in offsets: n.set('transform', f'translate(0 {offsets[i]})')
layer.set('id','Cover-layout')
tree.write(OUT/'Frame1-1920x1080.svg',encoding='utf-8',xml_declaration=True)
print(OUT/'Frame1-1920x1080.svg')
