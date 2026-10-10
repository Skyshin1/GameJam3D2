from pathlib import Path
import xml.etree.ElementTree as ET

OUT=Path(__file__).resolve().parent
NS='http://www.w3.org/2000/svg'
ET.register_namespace('',NS)
ET.register_namespace('xlink','http://www.w3.org/1999/xlink')
source=Path('C:/Users/Windows11/Downloads/2.svg')
tree=ET.parse(source); root=tree.getroot()
root.set('width','1920');root.set('height','1080');root.set('viewBox','0 0 1920 1080')
root.set('id','AI-Companionship-1920x1080')
layer,defs=list(root);nodes=list(layer)
for e in list(layer):layer.remove(e)
nodes[0].set('d','M1920 0H0V1080H1920V0Z')
# Maintain the texture's original ratio and crop the upper/lower margins.
nodes[1].set('y','-180')
nodes[2].set('d','M1920 0H0V1080H1920V0Z')
nodes[18].set('d','M1920 860H0V1080H1920V860Z')
clip_id=layer.get('clip-path')[5:-1]
for e in defs:
    if e.get('id')==clip_id:e[0].set('height','1080')

transforms={}
def translate(indices,dy):
    for i in indices:transforms[i]=f'translate(0 {dy})'
def scaled(indices,x,y,nx,ny,s):
    for i in indices:transforms[i]=f'translate({nx} {ny}) scale({s}) translate({-x} {-y})'
def picture(i,y,s):
    n=nodes[i];x=float(n.get('x','0'));old_y=float(n.get('y','0'))
    scaled([i],x,old_y,x,y,s)

translate([3],-12)
scaled([4,5],89,156,89,113,.92)
scaled([65,66],1133,143,1133,105,.92)
picture(67,106,.85)
translate([6],-76)

# Left: command examples above, enemy teaching below.
for i,y,s in [(82,233,.83),(79,355,.83),(83,476,.83),
              (85,206,.83),(84,335,.83),(86,476,.83),
              (75,674,.70),(76,758,.70)]:picture(i,y,s)
translate([7],-211);translate([8],-218)
translate([17],-243);translate([77],-246);translate([78],-250)

# Right: recommendations and rescue, followed by free chat/lore.
for i,y,s in [(68,208,.80),(70,338,.80),(69,404,.80),
              (80,208,.80),(81,347,.80),
              (71,603,.65),(72,620,.65),(73,700,.65),(74,713,.65)]:picture(i,y,s)
translate([9,12],-112);translate([10,13],-119);translate([11,14],-125)
translate([15],-213);translate([16],-223)

# Keep the entire flowchart connected, scaling it uniformly around the page centre.
scaled(range(21,65),960,1217,960,900,.8)
translate([19],-306);translate([20],-305)
for i,e in enumerate(nodes):
    e.set('id',f'original-element-{i:02d}')
    if i in transforms:
        g=ET.SubElement(layer,'{'+NS+'}g',{'id':f'layout-item-{i:02d}','transform':transforms[i]})
        g.append(e)
    else:layer.append(e)
layer.set('id','Companionship-layout')
tree.write(OUT/'2-1920x1080.svg',encoding='utf-8',xml_declaration=True)
assert len(root.findall('.//{'+NS+'}image'))==len(ET.parse(source).getroot().findall('.//{'+NS+'}image'))
print(OUT/'2-1920x1080.svg')
