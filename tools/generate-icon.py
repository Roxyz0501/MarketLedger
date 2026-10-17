"""Original geometric Market Ledger icon; no third-party or game artwork."""
from pathlib import Path
from PIL import Image, ImageDraw

scale = 3
im = Image.new('RGB', (512 * scale, 512 * scale), '#101d2b')
d = ImageDraw.Draw(im)
def box(coords, fill, radius=0, outline=None, width=1):
    d.rounded_rectangle(tuple(int(x*scale) for x in coords), radius*scale, fill=fill, outline=outline, width=width*scale)
def line(points, fill, width):
    d.line([(x*scale,y*scale) for x,y in points], fill=fill, width=width*scale, joint='curve')
box((28,28,484,484), '#162d3d', 75)
box((104,90,364,418), '#f0e7ce', 22)
box((104,90,146,418), '#37b5b3', 14)
for y in (152,195,238): line([(175,y),(316,y)], '#73939b', 13)
line([(177,355),(220,319),(254,331),(310,275)], '#198f95', 15)
box((292,277,450,435), '#e9ad39', 79, '#ffdb7b', 9)
box((318,303,424,409), '#c7871e', 53, '#ffdb7b', 5)
line([(371,325),(371,389)], '#fff1b2', 10)
line([(348,343),(391,343)], '#fff1b2', 8)
line([(348,370),(391,370)], '#fff1b2', 8)
target = Path(__file__).resolve().parents[1] / 'MarketLedger/images/icon.png'
target.parent.mkdir(parents=True, exist_ok=True)
im.resize((512,512), Image.Resampling.LANCZOS).save(target)
print(target)
