"""Compare two 96-DPI Record Library client captures. Requires Pillow for this tool only."""
import argparse
from PIL import Image, ImageChops

# Client coordinates in the 750 x 450 reference at 100% scale.
REGIONS = {
    "Client": (0, 0, 750, 450),
    "TreeView": (10, 34, 186, 149),
    "CheckedListBox": (10, 187, 186, 263),
    "TrackBar": (10, 318, 186, 361),
    "ListView": (212, 96, 727, 201),
    "DataGridView": (212, 237, 727, 351),
}


def read(path):
    with Image.open(path) as image:
        if image.size != (750, 450):
            raise ValueError(f"{path} is {image.size}, expected a 750 x 450 client capture")
        background = Image.new("RGBA", image.size, "#f0f0f0")
        return Image.alpha_composite(background, image.convert("RGBA")).convert("RGB")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("reference")
    parser.add_argument("candidate")
    args = parser.parse_args()
    delta = ImageChops.difference(read(args.reference), read(args.candidate))
    for name, box in REGIONS.items():
        region = delta.crop(box)
        pixels = region.width * region.height
        colors = region.getcolors(maxcolors=pixels)
        exact = sum(count for count, color in colors if color == (0, 0, 0))
        print(f"{name}: {exact:,}/{pixels:,} pixels match exactly ({100*exact/pixels:.2f}%)")


if __name__ == "__main__":
    main()
