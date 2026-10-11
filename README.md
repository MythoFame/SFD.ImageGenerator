<div align="center">

[![Superfighters Deluxe Logo](https://raw.githubusercontent.com/MythoFame/.github/refs/heads/master/assets/SFD_titleLoop.gif)](https://store.steampowered.com/app/855860)

# Image Generator

Desktop GUI that converts images into frame data for pixel rendering in Superfighters Deluxe

[![GitHub License](https://img.shields.io/github/license/MythoFame/SFD.ImageGenerator)](LICENSE.txt)

</div>

Drop an image (or pick one with Browse), press OK, and the tool writes `imgdump.txt` in the game's Shared script storage, ready to be rendered on a BgText/Text canvas by a script.

## 🚀 Installation

Download a binary from [releases](https://github.com/MythoFame/SFD.ImageGenerator/releases) and execute it.

## 📄 Frame data format v1

Text-based image interchange for pixel rendering (BgText/Text canvas). One string carries a full (possibly animated) image.

Layers, outside in:

1. **Frames:** one or more frames joined by `_`. Single-frame images have no `_`. Frame order = animation order.
2. **Frame:** standard Base64 (with `=` padding) of the binary blob below.
3. **Blob:** little-endian, row-major, top-left origin:

| Offset | Size      | Field                                                        |
|--------|-----------|--------------------------------------------------------------|
| 0      | u32       | width (≥ 1)                                                  |
| 4      | u32       | height (≥ 1)                                                 |
| 8      | w × h × 4 | pixels, top row first, left to right, RGBA order, alpha 0-255 preserved |

Decoder validation (reject with chat error, decode nothing): Base64 must parse; decoded length must equal exactly `8 + w*h*4`; w,h ≥ 1. Payload beyond that is an error, not ignored.

Worked example; 2×2, top row red/green, bottom row blue/white, all opaque (24 bytes → 32 chars):

```
AgAAAAIAAAD/AAD/AP8A/wAA////////
```
