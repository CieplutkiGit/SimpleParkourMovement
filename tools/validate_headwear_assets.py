#!/usr/bin/env python3
"""Static headwear OBJ / scene integration audit. Requires Python stdlib only.
This does NOT compile C# or prove the Unity scene plays correctly.
"""
from __future__ import annotations

import math
import re
import sys
import struct
import zlib
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "Assets/Art/Headwear/Source"
SCENE = ROOT / "Assets/Scenes/Main.unity"
SCRIPT = ROOT / "Assets/Scripts/Character/Animation/HeadwearController.cs.meta"
WIND = ROOT / "Assets/Settings/Wind.asset.meta"
ERR: list[str] = []


def check(condition: bool, message: str) -> None:
    if not condition:
        ERR.append(message)


def parse_obj(path: Path, min_faces: int, *, min_groups: int = 5,
              expected_materials: frozenset[str] = frozenset(
                  {"TECH_FABRIC", "TECH_TRIM", "TECH_ACCENT"})) -> int:
    vertices = []
    texcoords = []
    face_count = 0
    groups = set()
    active_material = None
    material_faces: dict[str, int] = {}
    materials = set()
    try:
        lines = path.read_text(encoding="utf-8").splitlines()
    except OSError as exc:
        ERR.append(f"{path}: {exc}")
        return 0

    for line_number, line in enumerate(lines, 1):
        fields = line.strip().split()
        if not fields or fields[0] == "#":
            continue
        command = fields[0]
        if command == "v":
            try:
                point = tuple(float(x) for x in fields[1:4])
                check(len(point) == 3 and all(map(math.isfinite, point)),
                      f"{path}:{line_number}: nonfinite vertex")
                vertices.append(point)
            except ValueError:
                ERR.append(f"{path}:{line_number}: nonnumeric vertex")
        elif command == "vt":
            try:
                uv = tuple(float(x) for x in fields[1:3])
                check(len(uv) == 2 and all(map(math.isfinite, uv)),
                      f"{path}:{line_number}: invalid uv")
                texcoords.append(uv)
            except ValueError:
                ERR.append(f"{path}:{line_number}: nonnumeric uv")
        elif command == "g" and len(fields) > 1:
            groups.add(fields[1])
        elif command == "usemtl" and len(fields) > 1:
            active_material = fields[1]
            materials.add(active_material)
        elif command == "f":
            face_count += 1
            check(len(fields) == 4, f"{path}:{line_number}: nontriangular face")
            try:
                refs = [(int(piece.split("/")[0]), int(piece.split("/")[1]))
                        for piece in fields[1:]]
                check(all(1 <= v <= len(vertices) and 1 <= t <= len(texcoords)
                          for v, t in refs),
                      f"{path}:{line_number}: invalid vertex/UV index")
                if len(refs) == 3 and all(1 <= v <= len(vertices) for v, _ in refs):
                    a, b, c = (vertices[v - 1] for v, _ in refs)
                    ab = tuple(b[i] - a[i] for i in range(3))
                    ac = tuple(c[i] - a[i] for i in range(3))
                    n = (ab[1] * ac[2] - ab[2] * ac[1],
                         ab[2] * ac[0] - ab[0] * ac[2],
                         ab[0] * ac[1] - ab[1] * ac[0])
                    check(sum(x * x for x in n) > 1e-14,
                          f"{path}:{line_number}: degenerate face")
                check(active_material is not None,
                      f"{path}:{line_number}: face has no material")
                if active_material:
                    material_faces[active_material] = material_faces.get(active_material, 0) + 1
            except (ValueError, IndexError):
                ERR.append(f"{path}:{line_number}: invalid face format")

    check(face_count >= min_faces,
          f"{path}: expected at least {min_faces} faces, found {face_count}")
    check(len(texcoords) == len(vertices),
          f"{path}: expected one texture coordinate per vertex")
    check(len(groups) >= min_groups, f"{path}: not enough mesh groups")
    check(materials == expected_materials,
          f"{path}: unexpected material groups {materials}")
    check(all(material_faces.get(m, 0) > 0 for m in materials),
          f"{path}: empty material assignment")
    print(f"{path.relative_to(ROOT)}: {len(vertices)} verts, {face_count} triangles, "
          f"{len(groups)} groups, {len(materials)} materials")
    return face_count


def scene_audit() -> None:
    text = SCENE.read_text(encoding="utf-8")
    guid = re.search(r"^guid: ([a-f0-9]{32})$", SCRIPT.read_text(), re.M)
    wind_guid = re.search(r"^guid: ([a-f0-9]{32})$", WIND.read_text(), re.M)
    check(bool(guid and f"guid: {guid.group(1)}" in text),
          "headwear component GUID is not referenced in Main.unity")
    check(bool(wind_guid and f"_wind: {{fileID: 11400000, guid: {wind_guid.group(1)}" in text),
          "wind asset is not connected to the headwear controller")
    anchors = re.findall(r"^--- !u!\d+ &(\d+)", text, re.M)
    check(len(anchors) == len(set(anchors)), "duplicate Unity YAML scene anchor")
    check("Assembly-CSharp::HeadwearController" in text,
          "Main.unity does not contain HeadwearController")
    player = re.search(r"--- !u!1 &810815790\nGameObject:(.*?)m_Name: Player", text, re.S)
    check(bool(player and "- component: {fileID: 4451223355}" in player.group(1)),
          "HeadwearController is not registered on Player")
    check(text.rfind("--- !u!1660057539 &") > text.rfind("Assembly-CSharp::HeadwearController"),
          "Unity SceneRoots must remain the last scene section")
    print(f"Main.unity: {len(anchors)} unique anchors, Player script and wind refs checked")


def metadata_audit() -> None:
    paths = list((ROOT / "Assets/Art/Headwear").rglob("*.meta"))
    paths += list((ROOT / "Assets/Scripts/Character/Animation").glob("Headwear*.cs.meta"))
    guids: dict[str, Path] = {}
    for path in paths:
        match = re.search(r"^guid: ([a-f0-9]{32})$", path.read_text(), re.M)
        check(match is not None, f"Missing GUID: {path}")
        if match:
            key = match.group(1)
            check(key not in guids, f"Duplicate GUID in {path} and {guids.get(key)}")
            guids[key] = path
    print(f"Unity metadata: {len(guids)} unique GUIDs")


def png_texture_audit() -> None:
    folder = ART / "Textures"
    expected = (
        "Fabric_Weave_Albedo.png",
        "Fabric_Weave_Normal.png",
        "Fabric_Weave_Occlusion.png",
    )
    for name in expected:
        path = folder / name
        try:
            data = path.read_bytes()
            check(data[:8] == b"\x89PNG\r\n\x1a\n",
                  f"{path}: invalid PNG signature")
            offset = 8
            image_data = bytearray()
            width = height = 0
            while offset + 12 <= len(data):
                length = struct.unpack(">I", data[offset:offset + 4])[0]
                tag = data[offset + 4:offset + 8]
                payload = data[offset + 8:offset + 8 + length]
                checksum = struct.unpack(">I", data[offset + 8 + length:offset + 12 + length])[0]
                check((zlib.crc32(tag + payload) & 0xffffffff) == checksum,
                      f"{path}: CRC mismatch in {tag!r}")
                if tag == b"IHDR":
                    width, height, bitdepth, colortype = struct.unpack(">IIBB", payload[:10])
                    check((width, height, bitdepth, colortype) == (256, 256, 8, 6),
                          f"{path}: unexpected dimensions / encoding")
                elif tag == b"IDAT":
                    image_data.extend(payload)
                elif tag == b"IEND":
                    break
                offset += length + 12

            pixels = zlib.decompress(image_data)
            check(len(pixels) == height * (1 + width * 4),
                  f"{path}: invalid decompressed size")
            for y in range(height):
                check(pixels[y * (1 + width * 4)] == 0,
                      f"{path}: unsupported scanline PNG filter")
            check(len(set(pixels[1:300:4])) > 8,
                  f"{path}: map appears blank or constant")
            print(f"{path.relative_to(ROOT)}: {width}x{height}, CRC and pixel data OK")
        except (OSError, ValueError, IndexError, struct.error, zlib.error) as exc:
            ERR.append(f"{path}: {exc}")


def csharp_directive_audit() -> None:
    source_dirs = [
        ROOT / "Assets/Scripts/Character/Animation",
        ROOT / "Assets/Editor",
    ]
    count = 0
    for folder in source_dirs:
        for path in folder.glob("*.cs"):
            depth = 0
            for line_no, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
                token = line.strip()
                if token.startswith("#if "):
                    depth += 1
                elif token == "#endif":
                    depth -= 1
                    check(depth >= 0, f"{path}:{line_no}: unmatched #endif")
                elif token.startswith("#else") or token.startswith("#elif "):
                    check(depth > 0, f"{path}:{line_no}: {token} without #if")
            check(depth == 0, f"{path}: unterminated #if")
            count += 1
    print(f"C# source guards: {count} files have balanced preprocessor directives")


def performance_source_audit() -> None:
    animation = ROOT / "Assets/Scripts/Character/Animation"
    strap = (animation / "HeadwearStrapDynamics.cs").read_text(encoding="utf-8")
    fallback = (animation / "ProceduralHeadwearMeshes.cs").read_text(encoding="utf-8")
    scarf = (animation / "SpringChain.cs").read_text(encoding="utf-8")
    baker = (ROOT / "Assets/Editor/HeadwearAssetBaker.cs").read_text(encoding="utf-8")
    binder = (animation / "HeadwearSkinnedGarmentBinding.cs").read_text(encoding="utf-8")

    check("_mesh.RecalculateNormals()" not in strap,
          "Dynamic strap is recalculating all mesh normals at runtime")
    check("_mesh.RecalculateBounds()" not in strap,
          "Dynamic strap is rebuilding mesh bounds per frame")
    check("SetVertices(_vertices" in strap and "SetNormals(_normals" in strap,
          "Dynamic strap isn't streaming reusable mesh arrays")
    check("if (cull)" in strap and "_simulationDistance" in strap,
          "Dynamic strap no longer skips distant simulation")
    check("BatchStaticParts" in fallback and "ownedMeshes.Remove(original)" in fallback,
          "Fallback headwear is retaining many draw calls/source meshes")
    check("BakeBatches(" in baker and "mesh.CombineMeshes" in baker and '"_LOD1.obj"' in baker,
          "Baker is not combining by material or using dedicated LOD1")
    check("Mathf.Min(Time.deltaTime, 0.05f)" in scarf,
          "Scarf simulation catch-up is unbounded")
    check("_updateWhenOffscreen = false" in binder,
          "Skinned garments have unconditional offscreen updates")
    print("Headwear performance source guards OK (not an FPS benchmark)")


def main() -> int:
    performance_source_audit()
    csharp_directive_audit()
    png_texture_audit()
    cap = parse_obj(ART / "TechnicalCap.obj", 5000)
    hood = parse_obj(ART / "RaisedHood.obj", 2000)
    low_materials = frozenset({"TECH_FABRIC", "TECH_TRIM"})
    cap_lod = parse_obj(ART / "TechnicalCap_LOD1.obj", 150,
                        min_groups=2, expected_materials=low_materials)
    hood_lod = parse_obj(ART / "RaisedHood_LOD1.obj", 100,
                         min_groups=2, expected_materials=low_materials)
    check(cap_lod <= cap * 0.5, "Cap LOD1 is not at least 50% cheaper")
    check(hood_lod <= hood * 0.5, "Hood LOD1 is not at least 50% cheaper")
    scene_audit()
    metadata_audit()
    if ERR:
        for error in ERR[:60]:
            print("ERROR:", error, file=sys.stderr)
        print(f"FAILED with {len(ERR)} problems", file=sys.stderr)
        return 1
    print("Headwear static validation passed. Unity runtime validation still required.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
