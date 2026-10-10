from pathlib import Path
from PIL import Image, ImageDraw, ImageFont, ImageOps
import textwrap, zipfile

root = Path(__file__).resolve().parents[2]
out = Path(__file__).resolve().parent
font_root = Path('C:/Windows/Fonts')
def font(size, mono=False):
    return ImageFont.truetype(str(font_root / ('consola.ttf' if mono else 'msyh.ttc')), size)

# Only crop real screenshots; do not paint over or alter UI content.
crops = [
    ('01-pooling-unity-full.png', '01-pooling-ready.png', (0, 78, 1708, 780)),
    ('02-pool-lifecycle-full.png', '02-pool-lifecycle-ready.png', (12, 604, 534, 922)),
    ('03-ai-input-full.png', '03-ai-input-ready.png', (213, 203, 1493, 923)),
    ('04-ai-result-full.png', '04-ai-result-ready.png', (213, 203, 1493, 923)),
    ('05-skill-config-full.png', '05-skill-config-ready.png', (1307, 78, 1708, 530)),
]
for source, target, box in crops:
    Image.open(out/source).crop(box).save(out/target)

def code_card(target, title, sections):
    rows = []
    for relative, ranges in sections:
        lines = (root/relative).read_text(encoding='utf-8-sig').splitlines()
        rows.append(('file', Path(relative).name))
        for first, last in ranges:
            segment = lines[first-1:last]
            indent = min((len(s)-len(s.lstrip()) for s in segment if s.strip()), default=0)
            for n, line in enumerate(segment, first):
                line = line[indent:]
                for j, part in enumerate(textwrap.wrap(line, 100, replace_whitespace=False, drop_whitespace=False) or ['']):
                    rows.append(('code', f'{n:3d}  {part}' if j==0 else '     '+part))
            rows.append(('gap',''))
    w, h = 1660, 160+len(rows)*34+30
    im = Image.new('RGB',(w,h),'#171d30'); d=ImageDraw.Draw(im)
    d.text((42,28),title,font=font(32),fill='#eee9fc')
    d.text((42,80),'PROJECT SOURCE EXCERPT / rendered from actual files, not a model response',font=font(19),fill='#ae9ed7')
    for i,(kind,line) in enumerate(rows):
        y=145+i*34
        if kind=='file':
            d.rounded_rectangle((30,y-4,w-30,y+30),radius=5,fill='#2d304c')
            d.text((44,y),line,font=font(22),fill='#c1a4fc')
        elif kind=='code':
            # CJK-safe font retains the original Chinese validation messages.
            d.text((44,y),line,font=font(21),fill='#dbe2ef')
    im.save(out/target)

code_card('06-json-protocol-source.png','Structured commands / protocol fields',[
    ('Assets/AnchorDefense/Scripts/AI/AICommandContracts.cs',[(26,52)])])
code_card('07-local-validation-source.png','Validate first / charge once / execute',[
    ('Assets/AnchorDefense/Scripts/AI/AICommandValidator.cs',[(66,69),(84,91)]),
    ('Assets/AnchorDefense/Scripts/AI/AICommandService.cs',[(247,250),(256,267)])])

items = [
    ('01-pooling-ready.png','01 / Pool hierarchy + live battle'),
    ('02-pool-lifecycle-ready.png','02 / Take, reset, return'),
    ('03-ai-input-ready.png','03 / Natural-language input'),
    ('04-ai-result-ready.png','04 / Actual execution feedback'),
    ('05-skill-config-ready.png','05 / Skill definition in Inspector'),
    ('06-json-protocol-source.png','06 / JSON data structure [source excerpt]'),
    ('07-local-validation-source.png','07 / Local validation [source excerpt]'),
]
preview = Image.new('RGB',(1800,1880),'#111629'); draw=ImageDraw.Draw(preview)
draw.text((42,26),'TECHNICAL SHOWCASE / screenshot selection',font=font(38),fill='#f0eafa')
for i,(name,title) in enumerate(items):
    x=30+(i%2)*890; y=100+(i//2)*440
    draw.rounded_rectangle((x,y,x+865,y+422),radius=14,fill='#252b42')
    draw.text((x+18,y+14),title,font=font(22),fill='#cbb6f5')
    im=Image.open(out/name).convert('RGB')
    im=ImageOps.contain(im,(827,350))
    preview.paste(im,(x+19+(827-im.width)//2,y+58+(350-im.height)//2))
preview.save(out/'00-preview.jpg',quality=94)

readme = '''技术展示素材说明

01-pooling-ready.png：真实 Unity 运行截图。Hierarchy 展开 Systems / Object Pools，灰色的 Projectile(Clone) 是停用、等待复用的实例；右侧 Inspector 的启用框未勾选。适合放在 Object pooling 大图区域。
02-pool-lifecycle-ready.png：真实 Unity 源码预览截图，ComponentPool.cs 第 32–52 行；Get 从栈里取出并触发重置回调，Release 停用对象、放回父节点并压栈。适合 Lifecycle detail 小图。
03-ai-input-ready.png：真实输入截图，Attack the area with the most enemies.
04-ai-result-ready.png：同次演示的真实成功提示 Done. The command is taking effect. 单张静态图不证明伤害数量或性能提升。
05-skill-config-ready.png：真实 AnchorStrike.asset Inspector，展示 anchor_strike、目标策略、效果、VFX 和解锁条件。适合参考图中“配置面板”的位置。
06-json-protocol-source.png：从 AICommandContracts.cs 第 26–52 行原文排版导出，展示 type / operations / source / operate / selector。它是源码摘录，不是原始模型响应截图。
07-local-validation-source.png：从 AICommandValidator.cs 和 AICommandService.cs 原文排版导出，展示原文验证、合法技能与类型校验，以及校验完成后的扣费和执行。它是源码摘录，不是 Unity 界面截图。

建议版面：
Object pooling：01 做大图，02 做右下角细节。配文：Reuse enemies, projectiles and effects through a shared pool. Reset state on checkout and return inactive instances for the next spawn.
AI integration：03 与 04 作为输入/结果对比，05 作为技能配置旁证；06 和 07 可作为技术细节的小图，空间不足优先用 07。配文：Natural-language requests become structured commands. Unity validates the action, source and target before spending points and executing the result.

脚本定位（均在 Assets/AnchorDefense/ 下）：
Scripts/Core/ComponentPool.cs：Get / Release。
Scripts/Combat/ProjectileController.cs：OnTakenFromPool / OnReturnedToPool 清空目标、回调和特效。
Scripts/Spawning/EndlessEnemySpawner.cs：CreateEnemyTypePool / SpawnEnemy / ReleaseEnemy。
Scripts/AI/DeepSeekCompanionProvider.cs：请求 API、读取响应与 JSON 解析。
Scripts/AI/AICompanionService.cs：区分聊天与操作，并调用 ExecuteParsed。
Scripts/AI/AICommandContracts.cs：协议数据类型。
Scripts/AI/AICommandValidator.cs：原文、技能和类型校验。
Scripts/AI/AICommandService.cs：ExecutePlan / TryFindBestContext。
Configs/AI/Skills/AnchorStrike.asset：技能 ID、目标策略与效果配置。

取材说明：01–05 均为本次真实 Unity 截图裁切；对应 -full.png 完整窗口原图保留。未合成游戏运行结果。本次未取得该请求原始 JSON，未将示例 JSON 冒充响应。截图时 Unity 状态栏存在 NullReferenceException 提示，本次仅取材，未排查该异常。未修改游戏脚本或保存场景修改。
'''
(out/'使用说明.txt').write_text(readme,encoding='utf-8-sig')
with zipfile.ZipFile(out.parent/'TechnicalShowcase.zip','w',zipfile.ZIP_DEFLATED) as z:
    for p in sorted(out.iterdir()):
        if p.suffix.lower() in ('.png','.jpg','.txt'):
            z.write(p,p.name)
print(out/'00-preview.jpg')
print(out.parent/'TechnicalShowcase.zip')
