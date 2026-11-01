// Static FBX audit; never loads Unity or changes imported assets.
const fs = require('fs');
const zlib = require('zlib');
const path = require('path');
const root = path.resolve(__dirname, '..');
function inspect(file) {
    const b = fs.readFileSync(file), version = b.readUInt32LE(23), wide = version >= 7500;
    let p = 27;
    const integer = () => { const n = wide ? Number(b.readBigUInt64LE(p)) : b.readUInt32LE(p); p += wide ? 8 : 4; return n; };
    function property() {
        const t = String.fromCharCode(b[p++]);
        if (t === 'S' || t === 'R') { const n = b.readUInt32LE(p); p += 4; const v = b.subarray(p, p += n); return t === 'S' ? v.toString('utf8') : null; }
        const scalar = { Y: ['readInt16LE', 2], C: ['readUInt8', 1], I: ['readInt32LE', 4], F: ['readFloatLE', 4], D: ['readDoubleLE', 8], L: ['readBigInt64LE', 8] }[t];
        if (scalar) { const v = b[scalar[0]](p); p += scalar[1]; return typeof v === 'bigint' ? v.toString() : v; }
        const n = b.readUInt32LE(p), encoding = b.readUInt32LE(p + 4), length = b.readUInt32LE(p + 8); p += 12;
        const bytes = b.subarray(p, p += length), data = encoding ? zlib.inflateSync(bytes) : bytes;
        if (t !== 'd') return null;
        return Array.from({ length: n }, (_, i) => data.readDoubleLE(i * 8));
    }
    function node() {
        const end = integer(), count = integer(); integer(); const length = b[p++];
        if (!end) return null;
        const name = b.toString('utf8', p, p += length), props = Array.from({ length: count }, property), children = [];
        while (p < end - (wide ? 25 : 13)) { const child = node(); if (!child) break; children.push(child); }
        p = end; return { name, props, children };
    }
    const all = [];
    function walk(n) { if (!n) return; all.push(n); n.children.forEach(walk); }
    while (p < b.length - 25) { const n = node(); if (!n) break; walk(n); }
    const vertices = all.filter(n => n.name === 'Vertices').flatMap(n => n.props[0]);
    const bounds = [0, 1, 2].map(axis => { const a = vertices.filter((_, i) => i % 3 === axis); return [Math.min(...a), Math.max(...a)]; });
    return { file: path.basename(file), models: all.filter(n => n.name === 'Model').map(n => ({ name: n.props[1].split('\0')[0], kind: n.props[2],
        transforms: n.children.filter(c => c.name === 'Properties70').flatMap(c => c.children)
            .filter(c => c.name === 'P' && /Lcl Rotation|Lcl Scaling|Lcl Translation|PreRotation/.test(c.props[0])).map(c => [c.props[0], c.props.slice(4)]) })),
        axes: all.filter(n => n.name === 'P' && /^(UpAxis|FrontAxis|CoordAxis)(Sign)?$/.test(n.props[0])).map(n => [n.props[0], n.props[4]]), localMeshBounds: bounds };
}
const source = fs.readFileSync(path.join(root, 'Assets/_Project/Scripts/Buildings/FacilityVisual.cs'), 'utf8');
const mappings = [...source.matchAll(/BuildingKind\.(\w+), "([^"]+)"/g)];
const missing = mappings.filter(m => !fs.existsSync(path.join(root, 'Assets/Resources/Facilities', m[2] + '.fbx'))).map(m => m[1] + ':' + m[2]);
console.log(JSON.stringify({ mappings: mappings.length, missing, samples: ['CC_Hut', 'CC_House', 'CC_Dormitory', 'CC_Apartment'].map(n => inspect(path.join(root, 'Assets/Resources/Facilities', n + '.fbx'))) }, null, 2));
if (missing.length) process.exitCode = 1;
