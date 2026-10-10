from pathlib import Path
from PIL import Image, ImageFont, ImageDraw, ImageOps
from html import escape
import xml.etree.ElementTree as ET
import base64, io, json, hashlib, zipfile

ROOT = Path(r'D:/UnityProgram/GameJam3D2/GameJam3D22')
OUT = ROOT / 'Artifacts/TechnicalEditorialFour'
ASSETS = OUT / 'assets'
SOURCE = ROOT / 'Artifacts/TechnicalShowcase'
ORIGINAL = Path(r'C:/Users/Windows11/Downloads/3.svg')
ASSETS.mkdir(parents=True, exist_ok=True)
INK, BODY, BLUE, LILAC = '#F4F0FC', '#C7C3D8', '#85B7DA', '#C4AFF3'
MUTED, RULE, BASE = '#A6A0BA', '#514566', '#171329'
FONT = Path('C:/Windows/Fonts')
PARAS = {
 'pool': 'I prewarm and reuse enemies, projectiles and effects through a shared object pool. On checkout, each instance resets its state; on return, it is deactivated and stored for reuse. The same lifecycle keeps frequent spawns organised during dense enemy waves.',
 'ai': 'Player requests become structured game operations. Unity checks the source text, known skills, targets and costs before spending points and executing the plan. The companion then reports the result, connecting a conversational request to a visible action on the battlefield.',
 'skill_play': 'Players spend kill points to expand the rings, strengthen turrets and unlock zone effects.',
 'skill_tech': 'ScriptableObject nodes define costs, prerequisites and effects. Purchases check the node state and balance before applying upgrades and refreshing the UI.',
 'team': 'As team captain and lead programmer, I coordinated Git contributions and divided the work into clear tasks. I exposed gameplay and visual settings through configuration assets and prefabs, so artists could tune the game without changing its core logic.'
}

def crop(source, name, box=None):
    im = Image.open(source).convert('RGB')
    if box: im = im.crop(box)
    im.save(ASSETS / (name+'.png'))

# Cropping only: these pictures retain real Unity / game / source content.
crop(SOURCE/'01-pooling-ready.png','pool')
crop(SOURCE/'02-pool-lifecycle-ready.png','lifecycle')
crop(SOURCE/'03-ai-input-ready.png','input',(813,592,1280,720))
crop(SOURCE/'04-ai-result-ready.png','result',(888,603,1280,720))
crop(SOURCE/'05-skill-config-ready.png','config')
crop(SOURCE/'05-skill-config-ready.png','config-effects',(0,299,401,452))
crop(ASSETS/'skill-tree-full.png','skill',(68,158,1642,1000))
crop(ASSETS/'skill-tree-full.png','skill-branches',(194,278,1133,838))
crop(ASSETS/'skill-tree-prerequisite-full.png','prerequisite',(1175,309,1516,835))
crop(ASSETS/'skill-tree-prerequisite-full.png','prerequisite-copy',(1175,310,1516,540))

original_tree = ET.parse(ORIGINAL)
bg_node = next(n for n in original_tree.getroot().iter() if n.tag.endswith('}image'))
bg_uri = bg_node.get('{http://www.w3.org/1999/xlink}href') or bg_node.get('href')
bg = Image.open(io.BytesIO(base64.b64decode(bg_uri.split(',')[1]))).convert('RGB')
bg = ImageOps.fit(bg,(1920,1080),method=Image.Resampling.LANCZOS)
bg = Image.blend(Image.new('RGB',bg.size,BASE),bg,0.55)
bg.save(ASSETS/'background.jpg',quality=91)

FONT_CACHE={}
def font(size,bold=False):
    key=(size,bold)
    if key not in FONT_CACHE: FONT_CACHE[key]=ImageFont.truetype(str(FONT/('arialbd.ttf' if bold else 'arial.ttf')),size)
    return FONT_CACHE[key]

def wrap(value,width,size=22,bold=False):
    result=[]
    for paragraph in value.split('\n'):
        line=''
        for word in paragraph.split():
            trial=(line+' '+word).strip()
            if line and font(size,bold).getlength(trial)>width:
                result.append(line);line=word
            else:line=trial
        result.append(line)
    return result

def uri(path):
    mime='image/jpeg' if path.suffix=='.jpg' else 'image/png'
    return f'data:{mime};base64,'+base64.b64encode(path.read_bytes()).decode()

class Page:
    def __init__(self,name,label,side=False):
        self.name=name;self.parts=[];self.boxes=[];self.counter=0;self.images=[]
        self.add('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="1920" height="1080" viewBox="0 0 1920 1080">')
        self.add(f'<title>Game development — {escape(label)}</title>')
        self.add('<desc>Four project contributions: object pooling, AI integration, skill tree, and development leadership. Embedded real project screenshots.</desc>')
        self.add('<g id="Background">')
        self.add(f'<image x="0" y="0" width="1920" height="1080" xlink:href="{uri(ASSETS/"background.jpg")}"/>')
        self.rect(0,0,1920,1080,BASE,opacity=.17)
        self.add('</g><g id="Page-heading">')
        if not side:
            self.line(64,42,105,42,LILAC,3)
            self.text(64,105,'Game development',52,bold=True)
            self.text(1856,58,'FLUFFY DEFENSE',15,LILAC,bold=True,anchor='end',spacing=1.8)
            self.text(1856,92,'TECH + TEAM / 03',15,MUTED,anchor='end',spacing=1.1)
        else:
            self.line(64,42,105,42,LILAC,3)
            self.text(64,107,'Game',48,bold=True)
            self.text(64,162,'development',40,bold=True)
            self.text(64,208,'FLUFFY DEFENSE',14,LILAC,bold=True,spacing=1.2)
            self.text(64,238,'TECH + TEAM / 03',14,MUTED)
        self.add('</g>')
    def add(self,x):self.parts.append(x)
    def group(self,id):self.add(f'<g id="{id}">')
    def endgroup(self):self.add('</g>')
    def rect(self,x,y,w,h,fill,stroke=None,opacity=1):
        self.add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" opacity="{opacity}"'+(f' stroke="{stroke}" stroke-width="1"' if stroke else '')+'/>')
    def line(self,x1,y1,x2,y2,color=RULE,width=1):
        self.add(f'<path d="M {x1} {y1} L {x2} {y2}" fill="none" stroke="{color}" stroke-width="{width}"/>')
    def text(self,x,y,s,size=22,color=INK,bold=False,anchor='start',spacing=0,audit=True):
        self.add(f'<text x="{x}" y="{y}" font-family="Arial" font-size="{size}" font-weight="{700 if bold else 400}" text-anchor="{anchor}" letter-spacing="{spacing}" fill="{color}">{escape(s)}</text>')
        width=font(size,bold).getlength(s)+max(0,len(s)-1)*spacing
        xx=x if anchor=='start' else x-width if anchor=='end' else x-width/2
        if audit:self.boxes.append(dict(kind='text',text=s,x=xx,y=y-size*.82,w=width,h=size))
    def para(self,x,y,width,s,size=22,leading=28,color=BODY):
        rows=wrap(s,width,size)
        for i,row in enumerate(rows):self.text(x,y+i*leading,row,size,color)
        return y+(len(rows)-1)*leading+size*.2
    def section(self,num,title,x,y):
        self.text(x,y,f'{num:02}',17,LILAC,bold=True)
        self.text(x+42,y,title,28,INK,bold=True)
    def image(self,name,x,y,w,h=None,caption=None):
        p=ASSETS/(name+'.png'); sw,sh=Image.open(p).size
        if h is None:h=w*sh/sw
        else:
            assert abs(w/h-sw/sh)<.03,f'Aspect mismatch {name}'
        self.add(f'<g id="Image-{name}-{self.counter}"><title>{escape(name)}</title>')
        self.rect(x-1,y-1,w+2,h+2,BASE,RULE)
        self.add(f'<image x="{x}" y="{y}" width="{w}" height="{h:.3f}" preserveAspectRatio="xMidYMid meet" xlink:href="{uri(p)}"/>')
        self.add('</g>');self.counter+=1
        self.boxes.append(dict(kind='image',text=name,x=x,y=y,w=w,h=h))
        self.images.append(name)
        if caption:self.text(x,y+h+23,caption,16,MUTED)
        return y+h
    def sub(self,x,y,s):self.text(x,y,s,21,BLUE,bold=True)
    def route(self,x,y,w):
        self.text(x,y,'UPGRADE PATH / CONFIGURED VALUES',14,LILAC,bold=True,spacing=.6)
        step=w/3
        for i,(name,cost) in enumerate([('Inner ring',8),('Middle ring',12),('Outer ring',16)]):
            xx=x+i*step
            self.text(xx,y+35,name,20,INK,bold=True)
            self.text(xx,y+60,f'{cost} pts  /  +2 turrets',16,BODY)
            if i<2:self.text(xx+step-28,y+35,'→',22,LILAC)
    def finish(self):
        self.group('Page-footer')
        self.text(64,1060,'FLUFFY DEFENSE / DEVELOPMENT NOTES',12,MUTED,spacing=1.1)
        self.text(1856,1060,self.name.replace('-',' ').upper(),12,MUTED,anchor='end',spacing=.8)
        self.endgroup();self.add('</svg>')
        (OUT/(self.name+'.svg')).write_text('\n'.join(self.parts),encoding='utf-8')
        # Exact text / photo geometry is recorded for mechanical QA and visual follow-up.
        errors=[]
        for b in self.boxes:
            if b['x']<40 or b['x']+b['w']>1880 or b['y']<20 or b['y']+b['h']>1070:
                errors.append({'type':'out-of-bounds','box':b})
        photos=[b for b in self.boxes if b['kind']=='image']
        for b in self.boxes:
            if b['kind']!='text':continue
            for im in photos:
                iw=min(b['x']+b['w'],im['x']+im['w'])-max(b['x'],im['x'])
                ih=min(b['y']+b['h'],im['y']+im['h'])-max(b['y'],im['y'])
                if iw>2 and ih>2:errors.append({'type':'text-over-image','text':b['text'],'image':im['text']})
        for i,a in enumerate(photos):
            for b in photos[i+1:]:
                iw=min(a['x']+a['w'],b['x']+b['w'])-max(a['x'],b['x'])
                ih=min(a['y']+a['h'],b['y']+b['h'])-max(a['y'],b['y'])
                if iw>2 and ih>2:errors.append({'type':'image-overlap','a':a['text'],'b':b['text']})
        return {'file':self.name+'.svg','images':self.images,'issues':errors,'boxes':self.boxes}

reports=[]

# 01: reference-led, three unequal columns with deliberately staggered figure starts.
p=Page('01-editorial-collage','Editorial collage')
p.group('01-Object-pooling')
p.section(1,'Object pooling',64,165)
p.para(64,210,580,PARAS['pool'])
p.image('pool',64,383,584,caption='Unity Play Mode / active and pooled instances')
p.image('lifecycle',96,666,354,caption='ComponentPool<T> / checkout and return')
p.sub(478,727,'Reuse the')
p.sub(478,754,'same lifecycle.')
p.para(478,796,178,'Enemies. Projectiles. Effects.',19,25)
p.endgroup()
p.group('02-AI-integration')
p.section(2,'AI integration',718,193)
p.para(718,238,478,PARAS['ai'])
p.image('input',696,438,506,caption='01 / A request in the player’s own words')
p.image('result',754,646,448,caption='02 / Actual execution feedback')
p.sub(718,865,'Interpret. Validate. Execute.')
p.endgroup()
p.group('03-Skill-tree')
p.section(3,'Skill tree',1284,159)
p.para(1284,203,570,PARAS['skill_play'])
p.image('skill',1260,303,596,caption='Live upgrade tree / ring and turret branches')
p.para(1284,680,568,PARAS['skill_tech'])
p.route(1284,810,570)
p.endgroup()
p.group('04-Leading-the-build')
p.line(64,921,1856,921)
p.section(4,'Leading the build',64,967)
p.text(106,1000,'TEAM CAPTAIN + LEAD PROGRAMMER',13,LILAC)
p.para(450,958,1040,PARAS['team'],21,27)
p.image('config-effects',1620,930,236,caption='Artist-facing configuration')
p.endgroup()
reports.append(p.finish())

# 02: a large skill-tree study to the left, supporting systems on the right.
p=Page('02-skill-tree-spotlight','Skill-tree spotlight')
p.group('03-Skill-tree')
p.section(3,'Skill tree',64,172)
p.sub(64,214,'Make every point a choice.')
p.para(64,253,1010,PARAS['skill_play'])
p.image('skill-branches',64,293,695,caption='Live tree / expansion, firepower and range')
p.image('prerequisite',807,331,235,caption='Requires Inner Ring Expansion')
p.para(64,772,1028,PARAS['skill_tech'])
p.route(64,836,1036)
p.endgroup()
p.group('02-AI-integration')
p.section(2,'AI integration',1196,157)
p.para(1196,200,660,PARAS['ai'])
p.image('input',1196,340,460,caption='Request / natural-language input')
p.image('result',1370,518,486,caption='Result / confirmed execution')
p.endgroup()
p.group('01-Object-pooling')
p.section(1,'Object pooling',1196,738)
p.para(1196,782,660,PARAS['pool'],21,26)
p.image('pool',1196,884,380)
p.text(1626,891,'Source / pool lifecycle',16,MUTED)
p.image('lifecycle',1626,907,230)
p.endgroup()
# Leadership closes the left column, with a short resource-structure note.
p.group('04-Leading-the-build')
p.line(64,918,1100,918)
p.section(4,'Leading the build',64,958)
p.text(106,994,'ART / PREFABS / CONFIGS',14,LILAC)
p.para(429,946,671,PARAS['team'],20,26)
p.endgroup()
reports.append(p.finish())

# 03: a staggered spread. Pictures cross invisible columns, without masking content.
p=Page('03-staggered-spread','Staggered spread')
p.group('01-Object-pooling')
p.section(1,'Object pooling',64,172)
p.para(64,218,425,PARAS['pool'])
p.image('pool',533,194,494,caption='Battle + pool hierarchy')
p.image('lifecycle',750,456,277,caption='Source / ComponentPool<T>')
p.sub(64,458,'Many spawns.')
p.sub(64,489,'One reusable lifecycle.')
p.endgroup()
p.group('02-AI-integration')
p.section(2,'AI integration',1100,217)
p.para(1100,262,754,PARAS['ai'])
p.image('input',1100,421,565,caption='Player intent')
p.image('result',1360,625,496,caption='Execution feedback')
p.endgroup()
p.group('03-Skill-tree')
p.section(3,'Skill tree',64,637)
p.para(64,681,980,PARAS['skill_play'])
p.image('skill',64,725,558,caption='Gameplay / choosing the next upgrade')
p.para(694,787,350,PARAS['skill_tech'])
p.sub(694,962,'Inner → Middle → Outer')
p.text(694,995,'8 / 12 / 16 points · +2 each',18,BODY)
p.endgroup()
p.group('04-Leading-the-build')
p.section(4,'Leading the build',1100,844)
p.para(1100,890,511,PARAS['team'],21,27)
p.image('config',1672,811,184,caption='Tunable skill asset')
p.endgroup()
reports.append(p.finish())

# 04: a narrow personal note rail; asymmetrical technical evidence in the main field.
p=Page('04-development-notebook','Development notebook',side=True)
p.group('04-Leading-the-build')
p.line(355,42,355,1030)
p.text(64,321,'04 / TEAM + PROCESS',14,LILAC,bold=True,spacing=.6)
p.text(64,370,'Leading',31,bold=True)
p.text(64,407,'the build.',31,bold=True)
p.para(64,456,264,PARAS['team'],22,29)
p.sub(64,847,'Built to be tuned.')
p.para(64,884,270,'Art / Prefabs / Configs',18,26)
p.image('config-effects',64,925,270)
p.endgroup()
p.group('02-AI-integration')
p.text(413,62,'INTENT / SYSTEMS / PLAYER CHOICE',15,LILAC,bold=True,spacing=1.2)
p.section(2,'AI integration',413,120)
p.para(413,164,1060,PARAS['ai'])
p.image('input',413,299,550,caption='01 / Ask for support')
p.image('result',1000,373,485,caption='02 / Report what happened')
p.image('config',1576,168,280,caption='Configured skills / Unity Inspector')
p.endgroup()
p.group('03-Skill-tree')
p.section(3,'Skill tree',413,548)
p.para(413,590,772,PARAS['skill_play'])
p.para(413,648,772,PARAS['skill_tech'])
p.text(413,729,'Inner → Middle → Outer: 8 / 12 / 16 pts · +2 turrets each',17,LILAC)
p.image('skill',413,755,500,caption='Upgrade tree / live gameplay')
p.image('prerequisite',963,762,175,caption='Prerequisite check')
p.endgroup()
p.group('01-Object-pooling')
p.section(1,'Object pooling',1250,580)
p.image('pool',1250,620,606,caption='Live battle beside the object pool')
p.para(1250,918,390,'I prewarm enemies, projectiles and effects in a shared pool. Checkout resets each instance; returning deactivates and stores it for reuse. This reusable lifecycle supports frequent spawning while keeping object state consistent across successive uses.',20,24)
p.image('lifecycle',1658,916,198)
p.endgroup()
reports.append(p.finish())

(OUT/'layout-audit.json').write_text(json.dumps(reports,indent=2),encoding='utf-8')
(OUT/'asset-provenance.txt').write_text('''ASSET PROVENANCE
All screenshot images are real captures, cropped without altering game or editor content.
pool, lifecycle, input, result, config: existing TechnicalShowcase captures.
skill, skill-branches: new Unity Play Mode capture of the actual upgrade UI.
prerequisite: new Unity capture of Middle Ring Expansion with its prerequisite locked.
Background: embedded background from the supplied 3.svg; darkened for text readability.
Skill-tree values: actual Ring01/02/03Capacity assets; costs 8/12/16, each adds two turrets.
Upgrade runtime: UpgradeSystem.GetState / TryPurchase; UpgradeNodeDefinition; UpgradeEffect.
Leadership wording: supplied portfolio copy. No team timeline or Git history is fabricated.
The input/result images demonstrate player input and execution feedback, not a raw model response.
The original SVG and gameplay source files are not edited by this artifact generator.
''',encoding='utf-8')
for r in reports:print(r['file'], 'images', len(r['images']), 'issues', len(r['issues']))
print('Original SHA256:',hashlib.sha256(ORIGINAL.read_bytes()).hexdigest())
