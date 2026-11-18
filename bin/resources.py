"""Prepare designer images as data-only resources for the macOS WinForms runtime."""
import base64
import io
import json
import struct
import sys
import xml.etree.ElementTree as ET
import zlib
from pathlib import Path

TYPES_KEY = "__MacWinForms_DesignerTypes_v1__"
READER = "MacWinForms.Resources.DesignerReader, MacWinForms"
RESOURCE_SET = "MacWinForms.Resources.DesignerResourceSet, MacWinForms"
BINARY = "application/x-microsoft.net.object.binary.base64"
BYTE_ARRAY = "application/x-microsoft.net.object.bytearray.base64"
SCALARS = {"System."+s for s in ("Boolean", "Byte", "SByte", "Int16", "UInt16", "Int32", "UInt32", "Int64",
                                  "UInt64", "Char", "Decimal", "Double", "Single", "DateTime", "TimeSpan", "Byte[]")}
SCALARS |= {"System.Drawing."+s for s in ("Point", "PointF", "Size", "SizeF", "Rectangle", "Color")}


def varint(stream):
    value = 0
    for shift in range(0, 35, 7):
        byte = stream.read(1)
        if not byte:
            raise ValueError("Truncated string length")
        value |= (byte[0] & 127) << shift
        if byte[0] < 128:
            return value
    raise ValueError("Invalid string length")


def image_record(data):
    """Read the one-byte-array serialization used by Bitmap and ImageListStreamer.
    Every tag, field, object reference and length must match this specific shape.
    No serializer, type resolution, callbacks, or object graph construction occurs.
    """
    s = io.BytesIO(data)
    def take(n):
        b = s.read(n)
        if len(b) != n:
            raise ValueError("Incomplete image record")
        return b
    def expect(value):
        if take(len(value)) != value:
            raise ValueError("Unsupported serialized resource layout")
    def text():
        return take(varint(s)).decode("utf-8")
    def number():
        return struct.unpack("<i", take(4))[0]
    expect(b"\0")
    root = number()
    expect(struct.pack("<iii", -1, 1, 0))
    expect(b"\x0c")
    library = number()
    text()
    expect(b"\x05")
    if number() != root:
        raise ValueError("Invalid resource object ID")
    name = text()
    if name not in ("System.Windows.Forms.ImageListStreamer", "System.Drawing.Bitmap"):
        raise ValueError("Unsupported serialized resource type " + name)
    if number() != 1 or text().lower() != "data":
        raise ValueError("Expected a single image Data field")
    expect(b"\x07\x02")
    if number() != library:
        raise ValueError("Invalid resource library")
    expect(b"\x09")
    array = number()
    expect(b"\x0f")
    if number() != array:
        raise ValueError("Invalid image data reference")
    size = number()
    if size < 0 or size > len(data):
        raise ValueError("Invalid resource data size")
    expect(b"\x02")
    payload = take(size)
    expect(b"\x0b")
    if s.read(1):
        raise ValueError("Unsupported trailing serialized data")
    return name, payload


def bitmap(data, offset):
    if data[offset:offset+2] != b"BM":
        raise ValueError("Missing image list bitmap")
    pixels = offset + struct.unpack_from("<I", data, offset+10)[0]
    header = offset + 14
    size, width, height, planes, bits, compression = struct.unpack_from("<IiiHHI", data, header)
    if size < 40 or width <= 0 or not height or planes != 1 or compression or bits not in (1, 4, 8, 24, 32):
        raise ValueError("Unsupported image list bitmap encoding")
    stride = ((width*bits+31)//32)*4
    end = pixels + abs(height)*stride
    if pixels < header + size or end > len(data):
        raise ValueError("Truncated image list bitmap")
    palette = data[header+size:pixels]
    def pixel(x, y):
        row = pixels + (height-1-y if height > 0 else y)*stride
        if bits < 24:
            byte = data[row+x*bits//8]
            index = (byte >> (8-bits-x*bits%8)) & ((1 << bits)-1)
            b, g, r, _ = palette[index*4:index*4+4]
            return r, g, b, 255
        start = row+x*(bits//8)
        b, g, r = data[start:start+3]
        return r, g, b, data[start+3] if bits == 32 else 255
    return width, abs(height), end, pixel


def image_list(data):
    if data.startswith(b"MSFt"):
        runs = data[4:]
        if len(runs)%2 or sum(runs[::2]) > 64*1024*1024:
            raise ValueError("Invalid compressed image list")
        decoded = bytearray()
        for n, v in zip(runs[::2], runs[1::2]):
            decoded.extend(bytes([v])*n)
        data = decoded
    if data[:4] != b"IL\x01\x01" or len(data) < 28:
        raise ValueError("Unsupported image list header")
    count, _, _, w, h = struct.unpack_from("<5H", data, 4)
    if not (1 <= w <= 256 and 1 <= h <= 256) or count*w*h > 16*1024*1024:
        raise ValueError("Unsupported image list dimensions")
    width, height, end, color = bitmap(data, 28)
    mask = None
    if len(data) > end:
        mw, mh, _, mask = bitmap(data, end)
        if (mw, mh) != (width, height):
            raise ValueError("Mismatched image list mask")
    across = width // w
    if not across or (height//h)*across < count:
        raise ValueError("Invalid image list frame count")
    frames = bytearray(struct.pack("<III", w, h, count))
    def chunk(tag, body):
        return struct.pack(">I", len(body)) + tag + body + struct.pack(">I", zlib.crc32(tag+body))
    for i in range(count):
        pixels = []
        for y in range(h):
            for x in range(w):
                xx, yy = i%across*w+x, i//across*h+y
                r, g, b, a = color(xx, yy)
                pixels.append((r,g,b,a,mask(xx, yy)[0] if mask else 0))
        has_alpha = any(p[3] for p in pixels)
        raw = bytearray()
        for j, (r,g,b,a,m) in enumerate(pixels):
            if j%w == 0:
                raw.append(0)
            raw.extend((r,g,b,0 if m else (a if has_alpha else 255)))
        png = (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
               + chunk(b"IDAT", zlib.compress(raw)) + chunk(b"IEND", b""))
        frames.extend(struct.pack("<I", len(png)))
        frames.extend(png)
    return frames


def save(path, data):
    if not path.exists() or path.read_bytes() != data:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)


def prepare(source, target):
    et = ET.parse(source)
    types = {}
    for entry in et.findall("data"):
        name = entry.attrib["name"]
        if name == TYPES_KEY:
            raise ValueError("Reserved designer resource key " + TYPES_KEY)
        value = entry.find("value")
        if value is None:
            continue
        typename = entry.get("type", "").split(",")[0]
        mime = entry.get("mimetype", "")
        if typename in SCALARS and not mime:
            entry.attrib.pop("type")
            types[name] = typename
            continue
        if typename == "System.Resources.ResXFileRef":
            filename, _, rest = (value.text or "").partition(";")
            file = (source.parent / filename.replace("\\", "/")).resolve()
            typename = rest.split(",")[0].strip()
            if typename in ("System.Drawing.Bitmap", "System.Drawing.Icon", "System.Drawing.Image"):
                data = file.read_bytes()
            else:
                value.text = str(file) + ";" + rest
                continue
        elif mime == BINARY:
            try:
                typename, data = image_record(base64.b64decode("".join((value.text or "").split()), validate=True))
            except ValueError as e:
                raise ValueError(f"{source}, resource '{name}': {e}") from e
        elif typename in ("System.Drawing.Bitmap", "System.Drawing.Image", "System.Drawing.Icon", "System.Windows.Forms.ImageListStreamer") and mime == BYTE_ARRAY:
            data = base64.b64decode("".join((value.text or "").split()), validate=True)
        else:
            continue
        if typename == "System.Windows.Forms.ImageListStreamer":
            data = image_list(data)
        types[name] = typename
        entry.attrib.pop("mimetype", None)
        entry.attrib.pop("type", None)
        value.text = base64.b64encode(data).decode()
    if types:
        node = ET.SubElement(et.getroot(), "data", name=TYPES_KEY)
        ET.SubElement(node, "value").text = json.dumps(types, ensure_ascii=False)
    save(target, ET.tostring(et.getroot(), encoding="utf-8", xml_declaration=True))
    save(target.with_suffix(".media"), b"1" if types else b"0")


def wrap(source, compiled):
    if source.with_suffix(".media").read_bytes() != b"1":
        return
    fields = [s.encode() for s in (READER, RESOURCE_SET)]
    header = b"".join(bytes([len(s)])+s for s in fields)
    prefix = struct.pack("<III", 0xbeefcace, 1, len(header)) + header
    content = compiled.read_bytes()
    if not content.startswith(prefix):
        save(compiled, prefix+content)


if __name__ == "__main__":
    try:
        root = Path.cwd()
        for line in Path("MacResources.inputs").read_text(encoding="utf-8-sig").splitlines():
            source, name = line.rsplit("\t", 1)
            staged = root / "MacResources" / (name + ".resx")
            if sys.argv[1] == "prepare":
                prepare(Path(source), staged)
            elif sys.argv[1] == "wrap":
                wrap(staged, root / (name + ".resources"))
            else:
                raise ValueError("Unknown resource conversion mode")
    except (ValueError, OSError, ET.ParseError, struct.error) as e:
        sys.exit("Mac WinForms resource conversion: "+str(e))
