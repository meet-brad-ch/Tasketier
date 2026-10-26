"""Draws the Tasketier icon: a rounded teal tile with a white check mark, as a multi-size .ico.

    python tools/make_icon.py src/Tasketier.App/Tasketier.ico

Needs Pillow (pip install pillow). The icon is committed; rerun only when the look changes.
"""

from __future__ import annotations

import sys
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw


@dataclass(frozen=True)
class IconStyle:
    """The colours and proportions of the tile."""

    tile: tuple[int, int, int, int] = (14, 128, 112, 255)
    mark: tuple[int, int, int, int] = (255, 255, 255, 255)
    corner_fraction: float = 0.22
    stroke_fraction: float = 0.13


class IconMaker:
    """Renders the tile at one size and writes every size into one .ico."""

    SIZES = (256, 64, 48, 32, 24, 16)

    def __init__(self, style: IconStyle) -> None:
        self._style = style

    def render(self, size: int) -> Image.Image:
        scale = 4  # draw large, shrink for smooth edges
        big = size * scale
        image = Image.new("RGBA", (big, big), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        radius = int(big * self._style.corner_fraction)
        draw.rounded_rectangle((0, 0, big - 1, big - 1), radius=radius, fill=self._style.tile)
        stroke = max(1, int(big * self._style.stroke_fraction))
        points = [(big * 0.24, big * 0.52), (big * 0.42, big * 0.70), (big * 0.77, big * 0.32)]
        draw.line(points, fill=self._style.mark, width=stroke, joint="curve")
        for point in points:
            draw.ellipse((point[0] - stroke / 2, point[1] - stroke / 2, point[0] + stroke / 2, point[1] + stroke / 2), fill=self._style.mark)
        return image.resize((size, size), Image.LANCZOS)

    def write(self, target: Path) -> None:
        images = [self.render(size) for size in self.SIZES]
        target.parent.mkdir(parents=True, exist_ok=True)
        images[0].save(target, format="ICO", sizes=[(size, size) for size in self.SIZES], append_images=images[1:])


def main(argv: list[str]) -> int:
    if len(argv) != 2:
        print("usage: make_icon.py <target.ico>", file=sys.stderr)
        return 2
    IconMaker(IconStyle()).write(Path(argv[1]))
    print(f"wrote {argv[1]}")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
