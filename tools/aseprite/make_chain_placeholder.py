#!/usr/bin/env python3
"""Generate and verify the tiny Aseprite chain-link placeholder."""

import argparse
import struct
import zlib
from pathlib import Path


WIDTH = 8
HEIGHT = 6
FILE_MAGIC = 0xA5E0
FRAME_MAGIC = 0xF1FA
LAYER_CHUNK = 0x2004
CEL_CHUNK = 0x2005
COLOR_PROFILE_CHUNK = 0x2007
OUT_PATH = Path(__file__).resolve().parents[2] / "Assets/_Project/Art/Pixel/prop_chain.aseprite"


def chunk(chunk_type, payload):
    return struct.pack("<IH", len(payload) + 6, chunk_type) + payload


def pixels_rgba():
    transparent = (0, 0, 0, 0)
    dark = (0x3A, 0x3A, 0x44, 0xFF)
    highlight = (0x8A, 0x8A, 0x99, 0xFF)
    # The tile's left and right columns match exactly, so repeated tiles join.
    rows = [
        [transparent] * 8,
        [transparent, transparent, dark, dark, dark, dark, transparent, transparent],
        [dark, highlight, highlight, transparent, transparent, highlight, highlight, dark],
        [dark, highlight, highlight, transparent, transparent, highlight, highlight, dark],
        [transparent, transparent, dark, dark, dark, dark, transparent, transparent],
        [transparent] * 8,
    ]
    return b"".join(bytes(pixel) for row in rows for pixel in row)


def build_file():
    layer_name = b"link"
    layer = struct.pack(
        "<HHHHHHB3sH",
        1,  # visible
        0,  # normal layer
        0,  # child level
        0,  # default width
        0,  # default height
        0,  # normal blend mode
        255,
        b"\0" * 3,
        len(layer_name),
    ) + layer_name
    cel_pixels = zlib.compress(pixels_rgba())
    cel = struct.pack(
        "<HhhBHh5sHH",
        0,  # layer index
        0,
        0,
        255,
        2,  # compressed image cel
        0,
        b"\0" * 5,
        WIDTH,
        HEIGHT,
    ) + cel_pixels
    # Profile type 1 means sRGB. The remaining fields are flags, gamma, and reserved bytes.
    color_profile = struct.pack("<HHH8s", 1, 0, 0, b"\0" * 8)
    chunks = chunk(LAYER_CHUNK, layer) + chunk(CEL_CHUNK, cel) + chunk(COLOR_PROFILE_CHUNK, color_profile)

    frame_size = 16 + len(chunks)
    frame = struct.pack(
        "<IHHH2sI",
        frame_size,
        FRAME_MAGIC,
        3,  # old chunk count
        100,  # frame duration in milliseconds
        b"\0" * 2,
        0,  # new chunk count (unused for this file)
    ) + chunks

    header = struct.pack(
        "<IHHHHHIH8sB3sHBBhhHH84s",
        128 + len(frame),
        FILE_MAGIC,
        1,
        WIDTH,
        HEIGHT,
        32,
        1,  # flags
        100,
        b"\0" * 8,
        0,  # transparent palette index
        b"\0" * 3,
        0,  # palette size (0 means 256)
        1,
        1,
        0,
        0,
        0,
        0,
        b"\0" * 84,
    )
    assert len(header) == 128
    return header + frame


def verify_file(path):
    data = path.read_bytes()
    assert len(data) >= 144, "file is shorter than header plus frame header"
    file_size, magic, frames, width, height, depth = struct.unpack_from("<IHHHHH", data)
    assert magic == FILE_MAGIC, f"bad file magic: 0x{magic:04x}"
    assert file_size == len(data), f"file size field {file_size} != actual {len(data)}"
    assert frames == 1, f"expected one frame, got {frames}"
    assert (width, height, depth) == (WIDTH, HEIGHT, 32), f"unexpected canvas {width}x{height} at {depth} bpp"

    frame_size, frame_magic, old_chunks = struct.unpack_from("<IHH", data, 128)
    assert frame_magic == FRAME_MAGIC, f"bad frame magic: 0x{frame_magic:04x}"
    assert frame_size == len(data) - 128, "frame size does not match file"
    assert old_chunks == 3, f"expected three chunks, got {old_chunks}"

    offset = 144
    layer_names = []
    decompressed_size = None
    profile_type = None
    for _ in range(old_chunks):
        chunk_size, chunk_type = struct.unpack_from("<IH", data, offset)
        payload_start = offset + 6
        payload_end = offset + chunk_size
        assert chunk_size >= 6 and payload_end <= len(data), "invalid chunk size"
        if chunk_type == LAYER_CHUNK:
            name_length = struct.unpack_from("<H", data, payload_start + 16)[0]
            name_start = payload_start + 18
            layer_names.append(data[name_start:name_start + name_length].decode("utf-8"))
        elif chunk_type == CEL_CHUNK:
            cel_width, cel_height = struct.unpack_from("<HH", data, payload_start + 16)
            raw = zlib.decompress(data[payload_start + 20:payload_end])
            assert (cel_width, cel_height) == (WIDTH, HEIGHT), "cel dimensions do not match canvas"
            decompressed_size = len(raw)
        elif chunk_type == COLOR_PROFILE_CHUNK:
            profile_type = struct.unpack_from("<H", data, payload_start)[0]
        offset = payload_end

    assert offset == len(data), "unparsed data remains after chunks"
    assert layer_names == ["link"], f"expected one layer named link, got {layer_names}"
    assert decompressed_size == WIDTH * HEIGHT * 4, f"expected 192 RGBA bytes, got {decompressed_size}"
    assert profile_type == 1, f"expected sRGB profile type 1, got {profile_type}"
    return f"verified: magic A5E0/F1FA, 8x6 RGBA32, 1 frame, layer link, 48 decompressed pixels (sRGB)"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--verify", action="store_true", help="write the asset and parse it back")
    args = parser.parse_args()
    OUT_PATH.parent.mkdir(parents=True, exist_ok=True)
    OUT_PATH.write_bytes(build_file())
    print(f"wrote: {OUT_PATH.relative_to(Path(__file__).resolve().parents[2])}")
    if args.verify:
        print(verify_file(OUT_PATH))


if __name__ == "__main__":
    main()
