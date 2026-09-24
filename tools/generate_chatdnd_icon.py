from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


SIZE = 1024
ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "src" / "ChatDND.App" / "Assets"


def rounded_mask(size: int, radius: int) -> Image.Image:
    mask = Image.new("L", (size, size), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        (0, 0, size - 1, size - 1),
        radius=radius,
        fill=255,
    )
    return mask


def vertical_gradient(
    size: int,
    top: tuple[int, int, int],
    bottom: tuple[int, int, int],
) -> Image.Image:
    image = Image.new("RGB", (size, size))
    draw = ImageDraw.Draw(image)
    for y in range(size):
        ratio = y / (size - 1)
        color = tuple(
            round(top[index] + (bottom[index] - top[index]) * ratio)
            for index in range(3)
        )
        draw.line((0, y, size, y), fill=color)
    return image


def add_rounded_line(
    draw: ImageDraw.ImageDraw,
    start: tuple[int, int],
    end: tuple[int, int],
    width: int,
    fill: tuple[int, int, int, int],
) -> None:
    draw.line((start, end), fill=fill, width=width)
    radius = width // 2
    for x, y in (start, end):
        draw.ellipse((x - radius, y - radius, x + radius, y + radius), fill=fill)


def build_icon() -> Image.Image:
    background = vertical_gradient(
        SIZE,
        top=(18, 61, 94),
        bottom=(13, 148, 136),
    ).convert("RGBA")
    background.putalpha(rounded_mask(SIZE, radius=220))

    decoration = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    decoration_draw = ImageDraw.Draw(decoration)
    decoration_draw.ellipse(
        (-220, -260, 650, 570),
        fill=(255, 255, 255, 28),
    )
    decoration_draw.ellipse(
        (690, 660, 1240, 1190),
        fill=(14, 116, 144, 56),
    )
    decoration.putalpha(
        Image.composite(
            decoration.getchannel("A"),
            Image.new("L", (SIZE, SIZE), 0),
            rounded_mask(SIZE, radius=220),
        )
    )
    background = Image.alpha_composite(background, decoration)

    shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(shadow)
    shadow_draw.rounded_rectangle(
        (232, 286, 792, 684),
        radius=112,
        fill=(0, 22, 38, 110),
    )
    shadow_draw.polygon(
        [(334, 638), (292, 792), (488, 704)],
        fill=(0, 22, 38, 110),
    )
    shadow = shadow.filter(ImageFilter.GaussianBlur(28))
    background = Image.alpha_composite(background, shadow)

    bubble = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    bubble_draw = ImageDraw.Draw(bubble)
    bubble_draw.rounded_rectangle(
        (210, 258, 804, 668),
        radius=118,
        fill=(255, 255, 255, 255),
    )
    bubble_draw.polygon(
        [(330, 622), (286, 792), (504, 684)],
        fill=(255, 255, 255, 255),
    )
    background = Image.alpha_composite(background, bubble)

    slash_shadow = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    shadow_draw = ImageDraw.Draw(slash_shadow)
    add_rounded_line(
        shadow_draw,
        (646, 298),
        (854, 812),
        width=112,
        fill=(0, 30, 46, 135),
    )
    slash_shadow = slash_shadow.filter(ImageFilter.GaussianBlur(18))
    background = Image.alpha_composite(background, slash_shadow)

    slash = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    slash_draw = ImageDraw.Draw(slash)
    add_rounded_line(
        slash_draw,
        (646, 298),
        (854, 812),
        width=92,
        fill=(249, 115, 22, 255),
    )
    add_rounded_line(
        slash_draw,
        (646, 298),
        (854, 812),
        width=34,
        fill=(251, 146, 60, 255),
    )
    background = Image.alpha_composite(background, slash)

    border = Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))
    ImageDraw.Draw(border).rounded_rectangle(
        (18, 18, SIZE - 19, SIZE - 19),
        radius=204,
        outline=(255, 255, 255, 78),
        width=18,
    )
    return Image.alpha_composite(background, border)


def main() -> None:
    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    icon = build_icon()
    png_path = ASSET_DIR / "ChatDND-1024.png"
    preview_path = ASSET_DIR / "ChatDND-256.png"
    ico_path = ASSET_DIR / "ChatDND.ico"

    icon.save(png_path, format="PNG")
    icon.resize((256, 256), Image.Resampling.LANCZOS).save(preview_path, format="PNG")
    icon.save(
        ico_path,
        format="ICO",
        sizes=[
            (16, 16),
            (24, 24),
            (32, 32),
            (48, 48),
            (64, 64),
            (128, 128),
            (256, 256),
        ],
    )

    print(png_path)
    print(preview_path)
    print(ico_path)


if __name__ == "__main__":
    main()
