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
