from pathlib import Path
import os
from PIL import Image

source = Path(os.environ['TEMP']) / 'AnchorWaveValidation20261006/ValidationArtifacts/DisintegrationVfx'
out = Path(__file__).parent
# Encode actual Unity render frames using one palette to avoid flickering between frames.
sample_indices = [2, 8, 12, 18, 35, 85]
board = Image.new('RGB', (640, 360 * len(sample_indices)))
for slot, index in enumerate(sample_indices):
    with Image.open(source / f'frame-{index:03d}.png') as im:
        board.paste(im.resize((640, 360)), (0, slot * 360))
palette = board.quantize(colors=256, method=Image.Quantize.MEDIANCUT)
frames = []
for path in sorted(source.glob('frame-*.png')):
    with Image.open(path) as im:
        frames.append(im.convert('RGB').resize((960, 540), Image.Resampling.LANCZOS)
            .quantize(palette=palette, dither=Image.Dither.NONE))
frames[0].save(out / 'disintegration-preview.gif', save_all=True, append_images=frames[1:],
    duration=[60 if i % 3 == 0 else 70 for i in range(len(frames))], loop=0, optimize=True, disposal=1)
with Image.open(source / 'frame-018.png') as im:
    im.save(out / 'disintegration-preview.png')
print(f'Encoded {len(frames)} Unity frames; GIF size {(out / "disintegration-preview.gif").stat().st_size:,} bytes')
