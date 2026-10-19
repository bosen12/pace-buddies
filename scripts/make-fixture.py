"""Generate a tiny transparent GIF with intentionally unequal frame delays for integration checks."""
import sys
from pathlib import Path
from PIL import Image, ImageDraw

directory = Path(sys.argv[1] if len(sys.argv) > 1 else "artifacts/smoke")
directory.mkdir(parents=True, exist_ok=True)
frames = []
for i in range(3):
    frame = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    ImageDraw.Draw(frame).rectangle((8 + i * 2, 8, 22, 24), fill=(245, 182, 119, 255))
    frames.append(frame)
frames[0].save(directory / "fixture.gif", save_all=True, append_images=frames[1:],
               duration=[100, 200, 300], loop=0, disposal=2, transparency=0)

# Wide frames exercise height-first display without relying on a user's files.
wide_frames = []
for i in range(3):
    frame = Image.new("P", (128, 32), 0)
    frame.putpalette([0, 0, 0, 245, 182, 119, 32, 49, 74] + [0] * (768 - 9))
    draw = ImageDraw.Draw(frame)
    draw.rounded_rectangle((i * 2, 0, 123 + i * 2, 31), radius=6, fill=1)
    draw.rectangle((12 + i * 2, 8, 15 + i * 2, 15), fill=2)
    draw.rectangle((106 + i * 2, 8, 109 + i * 2, 15), fill=2)
    wide_frames.append(frame)
wide_frames[0].save(directory / "wide-fixture.gif", save_all=True, append_images=wide_frames[1:],
                    duration=[100, 200, 300], loop=0, disposal=2, transparency=0, optimize=False)
panorama = wide_frames[0].resize((2048, 32))
panorama.save(directory / "panorama-fixture.gif", transparency=0, optimize=False)

# 24M source pixels: accepted input that exercises scaling and both cache budgets.
large_frames = []
for i in range(48):
    frame = Image.new("P", (1024, 512), 0)
    frame.putpalette([0, 0, 0, 245, 182, 119, 32, 49, 74] + [0] * (768 - 9))
    draw = ImageDraw.Draw(frame)
    draw.rectangle((i * 10, 40, i * 10 + 400, 480), fill=1)
    large_frames.append(frame)
large_frames[0].save(directory / "large-fixture.gif", save_all=True, append_images=large_frames[1:],
                     duration=60, loop=0, disposal=2, transparency=0, optimize=False)
