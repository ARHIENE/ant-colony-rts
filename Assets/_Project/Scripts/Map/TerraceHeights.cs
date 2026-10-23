using System.Collections.Generic;
using UnityEngine;

namespace AntColony.Map
{
    // 스타크래프트식 지형(2026-10-19): 높이는 몇 단계의 평지뿐이고 단계 사이는 절벽, 경사로로만 오르내린다.
    // 가장 낮은 단계(Pit)는 액체 웅덩이(못 지나감·건설 불가). 정점 격자 (xSize+1)×(zSize+1) 위에서 계산한다.
    public static class TerraceHeights
    {
        public const float LevelHeight = 2f;   // 정점 간격 1m라 절벽 경사 63° → NavMesh 경사 한계(45°)를 넘어 걸어 오를 수 없다
        public const float WaterDepth = .6f;   // 기준 지면보다 이만큼 아래가 수면(웅덩이 바닥은 LevelHeight 아래)
        public const int Pit = -1;
        static readonly float[] LandCuts = { .55f, .75f }; // 정규화 노이즈 경계: 저지대 / 언덕1 / 언덕2
        const int MinRegion = 60;      // 이보다 작은 언덕·웅덩이는 주변 높이로 메운다(건설 못 할 자투리 방지)
        const int RampHalfLength = 4;  // 경계 양쪽 4칸 → 7m에 한 단계(약 16°)
        const int RampHalfWidth = 2;   // 폭 4m
        const int RampSpacing = 40;    // 같은 두 지대 사이 추가 경사로 간격
        const int MaxRampsPerPair = 3;
        const int RampClear = 3;       // 경사로 둘레 장식 금지 폭

        public sealed class Result
        {
            public float[] heights; // 단계 단위 높이(경사로만 소수). 월드 높이 = 기준 + heights × LevelHeight
            public bool[] clear;    // 장식을 두지 않는 칸: 웅덩이·절벽·경사로 주변
            public bool[] open;     // 둘레 4칸이 모두 같은 높이: 장식 크기와 NavMesh 여유 폭을 남긴다.
        }

        // lockOf(x, z): 2 = 기준 단계로 고정(본거지 평지), 1 = 웅덩이 금지, 0 = 자유.
        public static Result Build(float[] noise01, int xSize, int zSize, float waterCut, bool water, System.Func<int, int, int> lockOf, int seed)
        {
            var w = xSize + 1; var n = w * (zSize + 1);
            var level = new int[n]; var locks = new int[n];
            for (var z = 0; z <= zSize; z++)
                for (var x = 0; x <= xSize; x++)
                {
                    var i = z * w + x; var v = noise01[i]; locks[i] = lockOf(x, z);
                    level[i] = locks[i] == 2 ? 0
                        : water && locks[i] == 0 && v < waterCut ? Pit
                        : v < LandCuts[0] ? 0 : v < LandCuts[1] ? 1 : 2;
                }

            var label = Label(level, w, n, out var sizes);
            // 작은 지대는 가장 많이 맞닿은 이웃 높이로 메운다. 이미 메운 이웃 값을 읽을 수 있지만 한 번 훑기로 충분하다.
            var small = new Dictionary<int, List<int>>();
            for (var i = 0; i < n; i++)
                if (sizes[label[i]] < MinRegion) { if (!small.TryGetValue(label[i], out var m)) small[label[i]] = m = new List<int>(); m.Add(i); }
            var neighborCount = new Dictionary<int, int>();
            foreach (var kv in small)
            {
                neighborCount.Clear(); var locked = false;
                foreach (var i in kv.Value)
                {
                    locked |= locks[i] != 0;
                    foreach (var j in Neighbors(i, w, n))
                        if (label[j] != kv.Key) neighborCount[level[j]] = neighborCount.TryGetValue(level[j], out var c) ? c + 1 : 1;
                }
                if (locked || neighborCount.Count == 0) continue;
                var fill = 0; var best = -1;
                foreach (var nc in neighborCount) if (nc.Value > best) { best = nc.Value; fill = nc.Key; }
                foreach (var i in kv.Value) level[i] = fill;
            }
            // 경사로를 놓고, 본거지에서 걸어서 못 가는 땅(물속 섬·두 단계 절벽에 갇힌 언덕)은 이웃 높이로 메운 뒤 다시 놓는다.
            var result = new Result { heights = new float[n], clear = new bool[n] };
            bool[] ramp;
            for (var pass = 0; ; pass++)
            {
                label = Label(level, w, n, out sizes);
                for (var i = 0; i < n; i++) result.heights[i] = level[i];
                ramp = new bool[n];
                PlaceRamps(level, label, ramp, result.heights, w, zSize + 1, seed);
                var reach = Reach(level, result.heights, label, sizes, locks, w, n, out var inner);
                if (pass == 4 || !FillUnreached(level, reach, inner, w, n)) break;
            }

            for (var i = 0; i < n; i++)
            {
                if (ramp[i])
                {
                    // 경사로 둘레 3칸(발판·꼭대기 진입로 포함)은 비운다. 숲 나무가 입구를 막아 언덕이 고립된 적 있음.
                    int x = i % w, z = i / w;
                    for (var dz = -RampClear; dz <= RampClear; dz++)
                        for (var dx = -RampClear; dx <= RampClear; dx++)
                            if (x + dx >= 0 && x + dx < w && z + dz >= 0 && z + dz <= zSize) result.clear[(z + dz) * w + x + dx] = true;
                }
                if (level[i] == Pit) { result.clear[i] = true; continue; }
                foreach (var j in Neighbors(i, w, n))
                    if (Mathf.Abs(result.heights[j] - result.heights[i]) > .2f) { result.clear[i] = true; break; }
            }
            result.open = new bool[n];
            for (var i = 0; i < n; i++)
            {
                int x = i % w, z = i / w; var ok = !result.clear[i];
                for (var dz = -4; dz <= 4 && ok; dz++)
                    for (var dx = -4; dx <= 4 && ok; dx++)
                        ok = x + dx >= 0 && x + dx < w && z + dz >= 0 && z + dz <= zSize && result.heights[(z + dz) * w + x + dx] == result.heights[i];
                result.open[i] = ok;
            }
            return result;
        }

        // 맞닿은 두 지대(한 단계 차이)마다 경사로를 1~3개 판다. 경사로는 축 방향 직사각형.
        // 경사로가 안 들어가 갇힌 지대는 Build의 Reach/FillUnreached가 메운다.
        static void PlaceRamps(int[] level, int[] label, bool[] ramp, float[] heights, int w, int h, int seed)
        {
            var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            var candidates = new Dictionary<long, List<(Vector2Int v, Vector2Int d)>>();
            for (var z = 0; z < h; z++)
                for (var x = 0; x < w; x++)
                {
                    var l = level[z * w + x]; if (l == Pit) continue;
                    foreach (var d in dirs)
                    {
                        int ux = x + d.x, uz = z + d.y;
                        if (ux < 0 || uz < 0 || ux >= w || uz >= h || level[uz * w + ux] != l + 1) continue;
                        var key = (long)label[z * w + x] << 32 | (uint)label[uz * w + ux];
                        if (!candidates.TryGetValue(key, out var list)) candidates[key] = list = new List<(Vector2Int, Vector2Int)>();
                        list.Add((new Vector2Int(x, z), d));
                    }
                }

            var random = new System.Random(seed);
            foreach (var list in candidates.Values)
            {
                var chosen = new List<Vector2Int>();
                for (var k = list.Count - 1; k > 0; k--) { var j = random.Next(k + 1); (list[k], list[j]) = (list[j], list[k]); }
                foreach (var (v, d) in list)
                {
                    if (chosen.Count >= MaxRampsPerPair) break;
                    if (chosen.Exists(c => (c - v).sqrMagnitude < RampSpacing * RampSpacing)) continue;
                    if (!TryRamp(level, ramp, heights, w, h, v, d)) continue;
                    chosen.Add(v);
                }
            }
        }

        // v(낮은 쪽 경계 정점)에서 d 방향으로 한 단계 오른다. 발판·꼭대기까지 같은 단계 평지일 때만 판다.
        static bool TryRamp(int[] level, bool[] ramp, float[] heights, int w, int h, Vector2Int v, Vector2Int d)
        {
            var low = level[v.y * w + v.x]; var side = new Vector2Int(Mathf.Abs(d.y), Mathf.Abs(d.x));
            for (var s = -RampHalfLength; s <= RampHalfLength + 1; s++)
                for (var q = -RampHalfWidth - 1; q <= RampHalfWidth + 1; q++)
                {
                    var p = v + d * s + side * q;
                    if (p.x < 0 || p.y < 0 || p.x >= w || p.y >= h) return false;
                    var i = p.y * w + p.x;
                    if (ramp[i] || level[i] != (s <= 0 ? low : low + 1)) return false;
                }
            for (var s = -RampHalfLength + 1; s <= RampHalfLength; s++)
                for (var q = -RampHalfWidth; q <= RampHalfWidth; q++)
                {
                    var p = v + d * s + side * q; var i = p.y * w + p.x;
                    heights[i] = low + (s + RampHalfLength - 1) / (2f * RampHalfLength - 1);
                    ramp[i] = true;
                }
            return true;
        }

        // 본거지(고정 칸, 없으면 가장 큰 땅)에서 걸어서 퍼진다. 둘레 8칸이 모두 높이 차 0.2단계 이하(경사로 한 칸 = 1/7)인
        // '안쪽' 칸만 지나간다 — 절벽 사이 폭 1~2m 띠는 NavMesh가 개미 반경만큼 깎아 실제로 못 지나가기 때문.
        // 안쪽 칸에 붙은 같은 높이의 가장자리 칸도 닿은 것으로 친다(절벽 끝을 메우지 않게).
        static bool[] Reach(int[] level, float[] heights, int[] label, List<int> sizes, int[] locks, int w, int n, out bool[] inner)
        {
            inner = new bool[n];
            for (var i = 0; i < n; i++)
            {
                if (level[i] == Pit) continue;
                int x = i % w, z = i / w; var ok = x > 0 && x < w - 1 && z > 0 && z < n / w - 1;
                for (var dz = -1; dz <= 1 && ok; dz++)
                    for (var dx = -1; dx <= 1 && ok; dx++)
                    {
                        var j = i + dz * w + dx;
                        ok = level[j] != Pit && Mathf.Abs(heights[j] - heights[i]) <= .2f;
                    }
                inner[i] = ok;
            }
            // 본거지 평지의 안쪽 칸에서 출발한다. 가장자리 칸은 옆 언덕 때문에 안쪽이 아니라 1칸만 닿고 끝나 반복 한 번을 버렸다(2026-10-22).
            var start = -1;
            for (var i = 0; i < n && start < 0; i++) if (locks[i] == 2 && inner[i]) start = i;
            if (start < 0) start = System.Array.IndexOf(locks, 2);
            if (start < 0)
                for (var i = 0; i < n; i++)
                    if (inner[i] && (start < 0 || sizes[label[i]] > sizes[label[start]])) start = i;
            var reach = new bool[n];
            if (start < 0) return reach;
            var stack = new Stack<int>(); reach[start] = true; stack.Push(start);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                foreach (var j in Neighbors(c, w, n))
                    if (!reach[j] && inner[j] && Mathf.Abs(heights[j] - heights[c]) <= .2f) { reach[j] = true; stack.Push(j); }
            }
            for (var i = 0; i < n; i++)
            {
                if (!inner[i] || !reach[i]) continue;
                foreach (var j in Neighbors(i, w, n))
                    if (!inner[j] && level[j] != Pit && Mathf.Abs(heights[j] - heights[i]) <= .2f) reach[j] = true;
            }
            return reach;
        }

        // 못 가는 땅 지대를 닿는 이웃(갈 수 있는 땅·웅덩이) 중 가장 많이 맞닿은 높이로 바꾼다. 바꾼 게 있으면 true.
        // 못 가는 땅을 덩어리(같은 높이로 이어진 못 가는 칸)별로, 맞닿은 다른 높이 중 가장 많이 맞닿은 높이로 바꾼다.
        // 갈 수 있는 같은 높이 땅에 폭 1~2칸 띠로만 붙은 넓은 땅도 메운다(옛 지대 단위 메우기는 같은 높이를 골라 그대로 남았다, 2026-10-22).
        // 같은 높이 땅에 붙은 절벽 끝 자투리(안쪽 칸 없음)는 원래 못 걷는 칸이라 둔다 — 메우면 반복마다 언덕이 깎여 사라진다.
        static bool FillUnreached(int[] level, bool[] reach, bool[] inner, int w, int n)
        {
            var seen = new bool[n]; var stack = new Stack<int>(); var cells = new List<int>(); var counts = new Dictionary<int, int>();
            var changed = false;
            for (var s = 0; s < n; s++)
            {
                if (seen[s] || reach[s] || level[s] == Pit) continue;
                cells.Clear(); counts.Clear(); var hasInner = false; var attached = false;
                seen[s] = true; stack.Push(s);
                while (stack.Count > 0)
                {
                    var c = stack.Pop(); cells.Add(c); hasInner |= inner[c];
                    foreach (var j in Neighbors(c, w, n))
                    {
                        if (level[j] == level[c])
                        {
                            if (reach[j]) attached = true;
                            else if (!seen[j]) { seen[j] = true; stack.Push(j); }
                        }
                        else if (reach[j] || level[j] == Pit) counts[level[j]] = counts.TryGetValue(level[j], out var k) ? k + 1 : 1;
                    }
                }
                if (attached && !hasInner || counts.Count == 0) continue;
                // 갈 수 있는 땅 높이를 웅덩이보다 먼저 고른다(좁은 띠 뒤의 넓은 땅이 통째로 호수가 되지 않게).
                int best = -1, fill = 0;
                foreach (var c in counts)
                {
                    var score = c.Value + (c.Key == Pit ? 0 : 1 << 20);
                    if (score > best) { best = score; fill = c.Key; }
                }
                foreach (var c in cells) level[c] = fill;
                changed = true;
            }
            return changed;
        }

        static int[] Label(int[] level, int w, int n, out List<int> sizes)
        {
            var label = new int[n]; sizes = new List<int>();
            for (var i = 0; i < n; i++) label[i] = -1;
            var stack = new Stack<int>();
            for (var i = 0; i < n; i++)
            {
                if (label[i] >= 0) continue;
                var id = sizes.Count; var size = 0;
                label[i] = id; stack.Push(i);
                while (stack.Count > 0)
                {
                    var c = stack.Pop(); size++;
                    foreach (var j in Neighbors(c, w, n))
                        if (label[j] < 0 && level[j] == level[i]) { label[j] = id; stack.Push(j); }
                }
                sizes.Add(size);
            }
            return label;
        }

        static IEnumerable<int> Neighbors(int i, int w, int n)
        {
            var x = i % w;
            if (x > 0) yield return i - 1;
            if (x < w - 1) yield return i + 1;
            if (i >= w) yield return i - w;
            if (i + w < n) yield return i + w;
        }

        // 먼 길 대략 경로(2026-10-22, NavRoute): stride칸 간격 격자에서 BFS. 두 격자점 사이 정점이 모두 '안쪽' 칸이고
        // 이웃끼리 높이 차 0.2단계 이하면 이어진 것으로 본다(Reach와 같은 걷기 규칙). 경로 정점 인덱스를 from→to 순으로 채운다.
        public static bool Route(float[] heights, int w, int from, int to, int stride, List<int> cells)
        {
            cells.Clear();
            var n = heights.Length; var rows = n / w;
            int cw = (w - 1) / stride + 1, ch = (rows - 1) / stride + 1;
            bool Inner(int i)
            {
                int x = i % w, z = i / w;
                if (heights[i] < 0 || x < 1 || z < 1 || x > w - 2 || z > rows - 2) return false;
                for (var dz = -1; dz <= 1; dz++)
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        var j = i + dz * w + dx;
                        if (heights[j] < 0 || Mathf.Abs(heights[j] - heights[i]) > .2f) return false;
                    }
                return true;
            }
            int Vertex(int c) => c / cw * stride * w + c % cw * stride;
            bool Edge(int a, int b)
            {
                int va = Vertex(a), vb = Vertex(b), step = (vb - va) / stride;
                for (int k = 1, v = va + step; k <= stride; k++, v += step)
                    if (!Inner(v) || Mathf.Abs(heights[v] - heights[v - step]) > .2f) return false;
                return true;
            }
            // 정점에서 가장 가까운 안쪽 격자점(3칸 반경). 경사로 옆·절벽 끝에서 출발해도 붙는다.
            int Snap(int i)
            {
                int x = i % w, z = i / w, best = -1; var bestD = float.MaxValue;
                for (var cz = Mathf.Max(0, z / stride - 3); cz <= Mathf.Min(ch - 1, z / stride + 3); cz++)
                    for (var cx = Mathf.Max(0, x / stride - 3); cx <= Mathf.Min(cw - 1, x / stride + 3); cx++)
                    {
                        float d = (cx * stride - x) * (cx * stride - x) + (cz * stride - z) * (cz * stride - z);
                        if (d < bestD && Inner(cz * stride * w + cx * stride) && Mathf.Abs(heights[cz * stride * w + cx * stride] - heights[i]) <= .2f)
                        { bestD = d; best = cz * cw + cx; }
                    }
                return best;
            }
            int start = Snap(from), goal = Snap(to);
            if (start < 0 || goal < 0) return false;
            var parent = new int[cw * ch];
            for (var i = 0; i < parent.Length; i++) parent[i] = -2;
            var queue = new Queue<int>(); parent[start] = -1; queue.Enqueue(start);
            while (queue.Count > 0 && parent[goal] == -2)
            {
                var c = queue.Dequeue(); int cx = c % cw, cz = c / cw;
                foreach (var nb in new[] { cx > 0 ? c - 1 : -1, cx < cw - 1 ? c + 1 : -1, cz > 0 ? c - cw : -1, cz < ch - 1 ? c + cw : -1 })
                    if (nb >= 0 && parent[nb] == -2 && Edge(c, nb)) { parent[nb] = c; queue.Enqueue(nb); }
            }
            if (parent[goal] == -2) return false;
            for (var c = goal; c >= 0; c = parent[c]) cells.Add(Vertex(c));
            cells.Reverse();
            return true;
        }
    }
}
