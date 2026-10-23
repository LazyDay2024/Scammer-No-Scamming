"""Convert a .glb scene into OBJ+MTL files Unity can import, one OBJ per cluster of nearby nodes.

Used for the CC0 Old West pack (one big glb with ~136 loose parts). Nodes whose footprints touch are
grouped into one building. Usage:  python glb_to_obj.py <in.glb> <out_dir> [gap_metres]
"""
import json, os, struct, sys
import numpy as np

COMPONENTS = {"SCALAR": 1, "VEC2": 2, "VEC3": 3, "VEC4": 4, "MAT4": 16}
DTYPES = {5120: np.int8, 5121: np.uint8, 5122: np.int16, 5123: np.uint16, 5125: np.uint32, 5126: np.float32}


def load(path):
    data = open(path, "rb").read()
    json_len = struct.unpack("<I", data[12:16])[0]
    doc = json.loads(data[20:20 + json_len])
    rest = data[20 + json_len:]
    bin_len = struct.unpack("<I", rest[0:4])[0]
    return doc, rest[8:8 + bin_len]


def accessor(doc, blob, index):
    acc = doc["accessors"][index]
    view = doc["bufferViews"][acc["bufferView"]]
    n = COMPONENTS[acc["type"]]
    dtype = DTYPES[acc["componentType"]]
    start = view.get("byteOffset", 0) + acc.get("byteOffset", 0)
    stride = view.get("byteStride", 0)
    item = np.dtype(dtype).itemsize * n
    if stride and stride != item:
        raw = np.frombuffer(blob, np.uint8, count=stride * acc["count"], offset=start).reshape(acc["count"], stride)[:, :item]
        return np.frombuffer(raw.tobytes(), dtype).reshape(acc["count"], n)
    return np.frombuffer(blob, dtype, count=acc["count"] * n, offset=start).reshape(acc["count"], n)


def matrix(node):
    if "matrix" in node:
        return np.array(node["matrix"], dtype=np.float64).reshape(4, 4).T
    t = np.array(node.get("translation", [0, 0, 0]), dtype=np.float64)
    x, y, z, w = node.get("rotation", [0, 0, 0, 1])
    r = np.array([[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
                  [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
                  [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]])
    s = np.array(node.get("scale", [1, 1, 1]), dtype=np.float64)
    m = np.eye(4)
    m[:3, :3] = r * s
    m[:3, 3] = t
    return m


def walk(doc, index, parent, out):
    node = doc["nodes"][index]
    world = parent @ matrix(node)
    if "mesh" in node:
        out.append((index, node.get("name", f"node{index}"), world, node["mesh"]))
    for child in node.get("children", []):
        walk(doc, child, world, out)


def main():
    src, out_dir = sys.argv[1], sys.argv[2]
    gap = float(sys.argv[3]) if len(sys.argv) > 3 else 0.6
    os.makedirs(out_dir, exist_ok=True)
    doc, blob = load(src)

    # Textures: write each image once; materials reference the base colour map.
    tex_dir = os.path.join(out_dir, "Textures")
    os.makedirs(tex_dir, exist_ok=True)
    image_files = []
    for i, image in enumerate(doc.get("images", [])):
        view = doc["bufferViews"][image["bufferView"]]
        data = blob[view.get("byteOffset", 0):view.get("byteOffset", 0) + view["byteLength"]]
        ext = ".png" if image.get("mimeType", "image/png").endswith("png") else ".jpg"
        name = (image.get("name") or f"image{i}").replace("/", "_").replace("\\", "_") + ext
        open(os.path.join(tex_dir, name), "wb").write(data)
        image_files.append(name)
    materials = []
    with open(os.path.join(out_dir, "materials.mtl"), "w") as mtl:
        for i, mat in enumerate(doc.get("materials", [])):
            name = mat.get("name", f"mat{i}").replace(" ", "_")
            materials.append(name)
            pbr = mat.get("pbrMetallicRoughness", {})
            col = pbr.get("baseColorFactor", [1, 1, 1, 1])
            mtl.write(f"newmtl {name}\nKd {col[0]:.4f} {col[1]:.4f} {col[2]:.4f}\n")
            if "baseColorTexture" in pbr:
                img = doc["textures"][pbr["baseColorTexture"]["index"]]["source"]
                mtl.write(f"map_Kd Textures/{image_files[img]}\n")
            if "normalTexture" in mat:
                img = doc["textures"][mat["normalTexture"]["index"]]["source"]
                mtl.write(f"map_Bump Textures/{image_files[img]}\n")
            mtl.write("\n")

    parts = []
    for root in doc["scenes"][doc.get("scene", 0)]["nodes"]:
        walk(doc, root, np.eye(4), parts)

    # Bake every part into world space and record its XZ footprint.
    baked = []
    for index, name, world, mesh_index in parts:
        prims = []
        lo, hi = np.full(3, np.inf), np.full(3, -np.inf)
        for prim in doc["meshes"][mesh_index]["primitives"]:
            pos = accessor(doc, blob, prim["attributes"]["POSITION"]).astype(np.float64)
            pos = (np.c_[pos, np.ones(len(pos))] @ world.T)[:, :3]
            nrm = None
            if "NORMAL" in prim["attributes"]:
                nrm = accessor(doc, blob, prim["attributes"]["NORMAL"]).astype(np.float64) @ np.linalg.inv(world[:3, :3])
                nrm /= np.maximum(np.linalg.norm(nrm, axis=1, keepdims=True), 1e-9)
            uv = accessor(doc, blob, prim["attributes"]["TEXCOORD_0"]).astype(np.float64) if "TEXCOORD_0" in prim["attributes"] else None
            idx = accessor(doc, blob, prim["indices"]).reshape(-1, 3) if "indices" in prim else np.arange(len(pos)).reshape(-1, 3)
            prims.append((pos, nrm, uv, idx, materials[prim["material"]] if "material" in prim else None))
            lo, hi = np.minimum(lo, pos.min(0)), np.maximum(hi, pos.max(0))
        baked.append((name, prims, lo, hi))

    # Union parts whose XZ boxes come within `gap` metres of each other.
    parent = list(range(len(baked)))
    def find(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    for a in range(len(baked)):
        for b in range(a + 1, len(baked)):
            la, ha, lb, hb = baked[a][2], baked[a][3], baked[b][2], baked[b][3]
            if la[0] - gap <= hb[0] and lb[0] - gap <= ha[0] and la[2] - gap <= hb[2] and lb[2] - gap <= ha[2]:
                parent[find(a)] = find(b)
    groups = {}
    for i in range(len(baked)):
        groups.setdefault(find(i), []).append(i)

    summary = []
    for n, members in enumerate(sorted(groups.values(), key=lambda m: -len(m))):
        lo = np.min([baked[i][2] for i in members], axis=0)
        hi = np.max([baked[i][3] for i in members], axis=0)
        centre = np.array([(lo[0] + hi[0]) / 2, lo[1], (lo[2] + hi[2]) / 2])
        fname = f"oldwest_{n:02d}.obj"
        with open(os.path.join(out_dir, fname), "w") as obj:
            obj.write("mtllib materials.mtl\n")
            v_off = vt_off = vn_off = 1
            for i in members:
                name, prims, _, _ = baked[i]
                obj.write(f"o {name.replace(' ', '_')}\n")
                for pos, nrm, uv, idx, mat in prims:
                    p = pos - centre
                    # glTF is right-handed (+Z toward viewer); OBJ importers in Unity flip X, matching glTF->Unity.
                    for v in p:
                        obj.write(f"v {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n")
                    if uv is not None:
                        for t in uv:
                            obj.write(f"vt {t[0]:.5f} {1 - t[1]:.5f}\n")
                    if nrm is not None:
                        for v in nrm:
                            obj.write(f"vn {v[0]:.5f} {v[1]:.5f} {v[2]:.5f}\n")
                    if mat:
                        obj.write(f"usemtl {mat}\n")
                    for tri in idx:
                        refs = []
                        for k in tri:
                            k = int(k)
                            vt = f"{vt_off + k}" if uv is not None else ""
                            vn = f"{vn_off + k}" if nrm is not None else ""
                            refs.append(f"{v_off + k}/{vt}/{vn}")
                        obj.write("f " + " ".join(refs) + "\n")
                    v_off += len(pos)
                    if uv is not None: vt_off += len(uv)
                    if nrm is not None: vn_off += len(nrm)
        size = hi - lo
        summary.append({"file": fname, "parts": len(members), "size": [round(float(x), 1) for x in size],
                        "centre": [round(float(x), 1) for x in centre],
                        "names": [baked[i][0] for i in members][:6]})
    json.dump(summary, open(os.path.join(out_dir, "clusters.json"), "w"), indent=1, ensure_ascii=False)
    for s in summary:
        print(s["file"], s["parts"], "parts size", s["size"], "at", s["centre"], s["names"][:3])


if __name__ == "__main__":
    main()
