from pathlib import Path
import xml.etree.ElementTree as ET
from html import escape

OUT = Path(r'D:/UnityProgram/GameJam3D2/GameJam3D22/Artifacts/TechnicalLayoutPreview')
root = ET.parse(r'C:/Users/Windows11/Downloads/3.svg').getroot()
image = next(e for e in root.iter() if e.tag.endswith('}image'))
bg = image.get('{http://www.w3.org/1999/xlink}href') or image.get('href')
INK='#F4F1FC'; MUTED='#B8B6CD'; DIM='#8D8DAA'
ACC=['#BAA2F0','#9CCBDD','#ABD3C6','#D9ACC6']
FILL=['#30334E','#293B4A','#2D423F','#423444']
parts=[]
def add(s): parts.append(s)
def text(x,y,content,size=22,color=INK,weight=400,spacing=0,anchor='start',family='Arial'):
    add(f'<text x="{x}" y="{y}" font-family="{family}" font-size="{size}" font-weight="{weight}" fill="{color}" letter-spacing="{spacing}" text-anchor="{anchor}">{escape(content)}</text>')
def lines(x,y,items,size=22,color=MUTED,leading=30,weight=400):
    for i,s in enumerate(items):text(x,y+i*leading,s,size,color,weight)
def rect(x,y,w,h,fill,rx=0,stroke=None):
    add(f'<rect x="{x}" y="{y}" width="{w}" height="{h}" fill="{fill}" rx="{rx}"'+(f' stroke="{stroke}"' if stroke else '')+'/>')
def path(d,stroke,width=1,opacity=1):
    add(f'<path d="{d}" fill="none" stroke="{stroke}" stroke-width="{width}" stroke-linecap="round" opacity="{opacity}"/>')
def circle(x,y,r,fill,stroke=None,width=1):
    add(f'<circle cx="{x}" cy="{y}" r="{r}" fill="{fill}"'+(f' stroke="{stroke}" stroke-width="{width}"' if stroke else '')+'/>')
def begin():
    parts.clear()
    add('<svg xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" width="1920" height="1080" viewBox="0 0 1920 1080">')
    add('<defs><linearGradient id="veil" x2="0" y2="1"><stop stop-color="#121426" stop-opacity=".28"/><stop offset="1" stop-color="#101121" stop-opacity=".6"/></linearGradient></defs>')
    rect(0,0,1920,1080,'#111326')
    add(f'<image x="0" y="-380" width="1920" height="1920" xlink:href="{bg}" opacity=".21"/>')
    rect(0,0,1920,1080,'url(#veil)')
def end(name):
    add('</svg>')
    (OUT/name).write_text('\n'.join(parts),encoding='utf-8')
def placeholder(x,y,w,h,fill,label,sub,accent):
    rect(x,y,w,h,fill,8)
    path(f'M {x+18} {y+30} V {y+18} H {x+30}',accent,1,.65)
    path(f'M {x+w-30} {y+h-18} H {x+w-18} V {y+h-30}',accent,1,.65)
    text(x+w/2,y+h/2-3,label,16,accent,500,.4,'middle')
    text(x+w/2,y+h/2+24,sub,13,'#B2B5C8',400,.2,'middle')
def steps(x,y,w,labels,accent):
    path(f'M {x} {y-15} H {x+w}',accent,1,.22)
    gap=w/3
    for i,s in enumerate(labels):
        text(x+i*gap,y+15,s,15,accent,600,.3)
        if i<2:text(x+(i+1)*gap-30,y+15,'→',19,DIM)

content=[
 dict(category='PERFORMANCE',title='Object pooling',
      image='LIVE BATTLE + POOL HIERARCHY',sub='Gameplay evidence',detail='LIFECYCLE DETAIL',detail_sub='Code / inspector crop',
      desc=['Prewarm and reuse enemies, projectiles and VFX.',
            'Reset state on return, ready for the next spawn.'],
      steps=['TAKE','RESET','RETURN'],source='ComponentPool<T>'),
 dict(category='INTELLIGENCE',title='AI integration',
      image='PLAYER COMMAND → VISIBLE RESULT',sub='Gameplay evidence',detail='STRUCTURED RESPONSE',detail_sub='JSON / validation crop',
      desc=['Convert player intent into structured operations.',
            'Check skill IDs, targets and cost before execution.'],
      steps=['INTERPRET','VALIDATE','EXECUTE'],source='AICommandService'),
 dict(category='INTERACTION',title='Orbit interception',
      image='TURRET + COLLISION-ONLY ENEMY',sub='Gameplay evidence',detail='MOTION BETWEEN FRAMES',detail_sub='Path diagram / code crop',
      desc=['Check enemy and turret motion across consecutive frames.',
            'Register contact during fast rotation; filter invalid actors.'],
      steps=['TRACK','SWEEP','CONTACT'],source='EndlessEnemySpawner.LateUpdate'),
 dict(category='COLLABORATION',title='Leading the build',
      image='GIT + CONFIGURATION WORKFLOW',sub='Development evidence',detail='ARTIST-FACING SETTINGS',detail_sub='Inspector / task breakdown',
      desc=['I coordinated Git contributions and divided work into tasks.',
            'Exposed gameplay settings so artists could tune assets.'],
      steps=['COORDINATE','IMPLEMENT','REFINE'],source='Team Captain & Lead Programmer')
]

# A — Four equally weighted branches, with the original root brought to the center.
begin()
text(72,85,'Game development',54,INK,700,-1.1)
text(74,126,'Four connected parts. One playable system.',23,MUTED)
text(1848,67,'FLUFFY DEFENSE',16,ACC[0],600,2,'end')
text(1848,98,'TECH + TEAM / 03',14,DIM,400,1.6,'end')
path('M 72 155 H 1848','#6E678B',1,.35)
# The connector is deliberately narrow, leaving the page to evidence rather than decoration.
path('M 960 250 C 936 340 987 457 960 563 C 939 671 983 829 960 973','#817294',3,.6)
branches=[('M 941 557 C 897 511 914 350 840 350',0,840,350),
          ('M 979 557 C 1023 511 1006 350 1080 350',1,1080,350),
          ('M 941 607 C 897 653 914 798 840 798',2,840,798),
          ('M 979 607 C 1023 653 1006 798 1080 798',3,1080,798)]
for d,i,x,y in branches:
    path(d,ACC[i],3,.75); circle(x,y,6,ACC[i]); circle(x,y,12,'none',ACC[i],1)
add('<ellipse cx="960" cy="582" rx="112" ry="54" fill="none" stroke="#77718E" opacity=".6" transform="rotate(-24 960 582)"/>')
circle(960,582,77,'#EEEAF8')
text(960,570,'MAKE',25,'#27203B',700,.5,'middle')
text(960,601,'IT WORK',25,'#27203B',700,.5,'middle')
for i,(x,y) in enumerate([(72,194),(1104,194),(72,642),(1104,642)]):
    c=content[i]; a=ACC[i]
    text(x,y+17,f'0{i+1} / {c["category"]}',15,a,600,1.4)
    text(x,y+59,c['title'],34,INK,700,-.5)
    placeholder(x,y+83,448,178,FILL[i],c['image'],c['sub'],a)
    placeholder(x+466,y+83,278,178,['#25273E','#23323F','#253932','#372C3C'][i],c['detail'],c['detail_sub'],a)
    lines(x,y+302,c['desc'],22,MUTED,29)
    steps(x,y+366,430,c['steps'],a)
    text(x+744,y+381,c['source'],13,DIM,400,0,'end')
text(72,1050,'FLUFFY DEFENSE / DEVELOPMENT NOTES',12,DIM,400,1.4)
text(1848,1050,'LAYOUT STUDY A · IMAGE PLACEHOLDERS',12,DIM,400,1.4,'end')
end('A-balanced-tree-draft.svg')

# B — An editorial four-column constellation. Same four topics, less branching machinery.
begin()
text(72,65,'GAME DEVELOPMENT',16,ACC[0],600,2)
text(72,143,'Behind the play.',76,INK,700,-2)
lines(1198,94,['From reusable objects to a responsive battlefield.',
                'Four contributions to FLUFFY DEFENSE.'],23,MUTED,33)
text(1848,184,'TECH + TEAM / 03',13,DIM,400,1.5,'end')
# The faint orbital return joins the stations as peers rather than a dependency chain.
path('M 172 283 C 324 220 476 220 628 283 S 932 346 1084 283 S 1388 220 1540 283 C 1609 312 1679 312 1748 283','#8878AC',3,.7)
path('M 172 283 C 400 347 1500 344 1748 283','#74698E',1,.23)
xs=[72,528,984,1440]
column_descriptions=[
 ['Enemies and projectiles share', 'a reusable lifecycle. Prewarm', 'instances, reset their state,', 'then return them to the pool.'],
 ['Natural language becomes', 'structured operations. Local', 'checks resolve known skills,', 'targets and costs before play.'],
 ['Compare relative movement', 'between frames to detect', 'contact during ring rotation.', 'Ignore inactive or stale actors.'],
 ['Coordinate contributions with', 'Git and clear task ownership.', 'Expose settings so artists can', 'tune the game without code.']
]
for i,x in enumerate(xs):
    c=content[i]; a=ACC[i]
    node=x+100
    circle(node,283,24,'#18192D',a,1.5)
    text(node,290,f'0{i+1}',17,a,600,0,'middle')
    text(x,362,c['category'],14,a,600,1.8)
    titles=[['Object','pooling'],['AI','integration'],['Orbit','interception'],['Leading','the build']][i]
    lines(x,413,titles,40,INK,45,700)
    placeholder(x,489,408,213,FILL[i],['POOL / LIVE BATTLE','COMMAND / RESULT','ROTATION / CONTACT','GIT / CONFIGURATION'][i],c['sub'],a)
    text(x,734,['REUSABLE LIFECYCLE','VALIDATED INTENT','CONTINUOUS CONTACT','SHARED OWNERSHIP'][i],13,a,600,1.1)
    lines(x,779,column_descriptions[i],23,MUTED,31)
    steps(x,916,408,c['steps'],a)
    text(x,977,c['source'],13,DIM)
    if i<3:path(f'M {x+437} 358 V 985','#6E678B',1,.25)
path('M 72 1020 H 1848','#6E678B',1,.35)
text(72,1054,'FLUFFY DEFENSE / DEVELOPMENT NOTES',12,DIM,400,1.4)
text(1848,1054,'LAYOUT STUDY B · IMAGE PLACEHOLDERS',12,DIM,400,1.4,'end')
end('B-orbital-editorial-draft.svg')
print('Two 1920 x 1080 preview layouts created; original SVG unchanged.')
