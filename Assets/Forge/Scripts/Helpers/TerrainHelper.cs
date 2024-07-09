using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class TerrainHelper
{
    static readonly RectInt? DEBUG_RENDER_SUBMESH = null; // new RectInt(6, 10, 2, 2);
    const bool DEBUG_RENDER_SPLAT_POINT_FILTER = false;
    const int QUANTIZATION_RESOLUTION = 1;
    const int QUANTIZATION_BUFFER = 1;
    const int QUANTIZATION_RESOLUTION_WITH_BUFFER = QUANTIZATION_RESOLUTION + (QUANTIZATION_BUFFER * 2);
    const float TERRAIN_MESH_RESOLUTION = 4f;
    const int TERRAIN_BAKED_TEX_RESOLUTION = 64;

    private static Dictionary<Hash128, Mesh> _terrainColliderMeshCache = new Dictionary<Hash128, Mesh>();

    public static Hash128 ComputeHash(this TerrainData terrainData)
    {
        const int HEIGHTMAP_SAMPLE_RESOLUTION = 10;
        Hash128 hash = new Hash128();

        float sum = 0f;
        for (int i = 0; i < (HEIGHTMAP_SAMPLE_RESOLUTION * HEIGHTMAP_SAMPLE_RESOLUTION); ++i)
        {
            var x = (i % HEIGHTMAP_SAMPLE_RESOLUTION) / (float)(HEIGHTMAP_SAMPLE_RESOLUTION - 1);
            var y = (i / HEIGHTMAP_SAMPLE_RESOLUTION) / (float)(HEIGHTMAP_SAMPLE_RESOLUTION - 1);
            sum += terrainData.GetInterpolatedHeight(x, y);

            foreach (var tex in terrainData.alphamapTextures)
                if (tex)
                    hash.Append(tex.GetPixelBilinear(x, y));
        }
        hash.Append(sum);

        foreach (var layer in terrainData.terrainLayers)
            if (layer.diffuseTexture)
                hash.Append(layer.diffuseTexture.imageContentsHash.ToString());

        return hash;
    }


    public static Mesh GetCollider(this TerrainCollider terrainCollider)
    {
        const float terrainMeshResolution = 4f;

        var hash = terrainCollider.terrainData.ComputeHash();
        if (_terrainColliderMeshCache.TryGetValue(hash, out var mesh))
            return mesh;

        var terrainData = terrainCollider.terrainData;
        var vertexPerRow = Mathf.CeilToInt(terrainData.size.x / terrainMeshResolution);
        var vertexPerColumn = Mathf.CeilToInt(terrainData.size.z / terrainMeshResolution);
        var facePerRow = vertexPerRow - 1;
        var facePerColumn = vertexPerColumn - 1;

        mesh = new Mesh();
        var vertices = new List<Vector3>();
        var triangles = new int[3 * 2 * facePerColumn * facePerRow];
        var normals = new Vector3[vertexPerRow * vertexPerColumn];

        // construct mesh
        for (int y = 0; y < vertexPerColumn; ++y)
        {
            for (int x = 0; x < vertexPerRow; ++x)
            {
                var tx = x / (float)facePerRow;
                var ty = y / (float)facePerColumn;
                var height = terrainData.GetInterpolatedHeight(tx, ty);
                var normal = terrainData.GetInterpolatedNormal(tx, ty);
                var vertex = new Vector3(tx * terrainData.size.x, height, ty * terrainData.size.z);
                vertices.Add(vertex);

                if (y > 0 && x > 0)
                {
                    var idx = ((y - 1) * facePerRow + (x - 1)) * 6;
                    var rowS1 = ((y - 1) * vertexPerRow) + (x - 1);
                    var rowE1 = ((y - 0) * vertexPerRow) + (x - 1);

                    triangles[idx + 2] = rowS1 + 0;
                    triangles[idx + 1] = rowS1 + 1;
                    triangles[idx + 0] = rowE1 + 0;
                    triangles[idx + 5] = rowS1 + 1;
                    triangles[idx + 4] = rowE1 + 1;
                    triangles[idx + 3] = rowE1 + 0;
                }

                var vIdx = (y * vertexPerRow) + x;
                normals[vIdx] = normal;
            }
        }

        // update mesh
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTriangles(triangles, 0);

        _terrainColliderMeshCache[hash] = mesh;
        return mesh;
    }

    public static void ToMesh(this Terrain terrain, GameObject targetGameObject)
    {

        var universalShader = Shader.Find("Horizon Forge/Universal");

        var meshFilter = targetGameObject.GetComponent<MeshFilter>();
        var meshRenderer = targetGameObject.GetComponent<MeshRenderer>();
        if (!meshFilter) meshFilter = targetGameObject.AddComponent<MeshFilter>();
        if (!meshRenderer) meshRenderer = targetGameObject.AddComponent<MeshRenderer>();

        var uvCenter = Vector2.one * 0.5f;

        var texDb = new TerrainTextureDatabase(terrain);
        var vertexPerRow = Mathf.CeilToInt(terrain.terrainData.size.x / TERRAIN_MESH_RESOLUTION) + 1;
        var vertexPerColumn = Mathf.CeilToInt(terrain.terrainData.size.z / TERRAIN_MESH_RESOLUTION) + 1;
        var facePerRow = vertexPerRow - 1;
        var facePerColumn = vertexPerColumn - 1;
        var iFacePerRow = 1f / facePerRow;
        var iFacePerColumn = 1f / facePerColumn;

        var mesh = meshFilter.sharedMesh = new Mesh();
        var vertices = new Vector3[4 * facePerColumn * facePerRow];
        var triangles = new int[3 * 2 * facePerColumn * facePerRow];
        var normals = new Vector3[4 * facePerColumn * facePerRow];
        var uvs = new Vector2[4 * facePerColumn * facePerRow];
        var textures = new Texture2D[2 * facePerColumn * facePerRow];
        var splatClassifications = new int[QUANTIZATION_RESOLUTION * vertexPerRow * QUANTIZATION_RESOLUTION * vertexPerColumn];

        // construct mesh
        for (int y = 0; y < facePerColumn; ++y)
        {
            for (int x = 0; x < facePerRow; ++x)
            {
                if (!DEBUG_RENDER_SUBMESH.HasValue || (DEBUG_RENDER_SUBMESH.HasValue && DEBUG_RENDER_SUBMESH.Value.Contains(new Vector2Int(x, y))))
                {
                    var idx = (y * facePerRow + x) * 6;
                    var vIdx = (y * facePerRow + x) * 4;

                    triangles[idx + 2] = vIdx + 0;
                    triangles[idx + 1] = vIdx + 1;
                    triangles[idx + 0] = vIdx + 2;
                    triangles[idx + 5] = vIdx + 1;
                    triangles[idx + 4] = vIdx + 3;
                    triangles[idx + 3] = vIdx + 2;

                    // add vertices
                    for (int vy = 0; vy < 2; ++vy)
                    {
                        for (int vx = 0; vx < 2; ++vx)
                        {
                            var tx = (x + vx) / (float)facePerRow;
                            var ty = (y + vy) / (float)facePerColumn;
                            var height = terrain.terrainData.GetInterpolatedHeight(tx, ty);
                            var vertex = new Vector3((x + vx) * TERRAIN_MESH_RESOLUTION, height, (y + vy) * TERRAIN_MESH_RESOLUTION);

                            vertices[vIdx] = vertex;
                            uvs[vIdx] = (new Vector2(vx, vy) - uvCenter)*0.99f + uvCenter;
                            normals[vIdx] = terrain.terrainData.GetInterpolatedNormal(tx, ty);
                            ++vIdx;
                        }
                    }

                    // classify splat
                    {
                        var tx = x / (float)facePerRow;
                        var ty = y / (float)facePerColumn;
                        var face = new Rect(tx, ty, iFacePerRow, iFacePerColumn);
                        var classification = texDb.Classify(terrain.terrainData, face, 0);

                        var ci = 0;
                        for (int cy = 0; cy < QUANTIZATION_RESOLUTION_WITH_BUFFER; cy++)
                        {
                            for (int cx = 0; cx < QUANTIZATION_RESOLUTION_WITH_BUFFER; cx++)
                            {
                                var sx = (cx - QUANTIZATION_BUFFER) + (x * QUANTIZATION_RESOLUTION);
                                var sy = (cy - QUANTIZATION_BUFFER) + (y * QUANTIZATION_RESOLUTION);
                                if (sx < 0) continue;
                                if (sx >= (vertexPerRow * QUANTIZATION_RESOLUTION)) continue;
                                if (sy < 0) continue;
                                if (sy >= (vertexPerColumn * QUANTIZATION_RESOLUTION)) continue;

                                var cIdx = (sy * vertexPerRow * QUANTIZATION_RESOLUTION) + sx;
                                if (cIdx < splatClassifications.Length)
                                    splatClassifications[cIdx] = classification[ci++];
                            }
                        }
                    }
                }
            }
        }

        // assign textures
        for (int y = 0; y < vertexPerColumn; ++y)
        {
            for (int x = 0; x < vertexPerRow; ++x)
            {
                if (x > 0 && y > 0)
                {
                    var tIdx = ((y - 1) * facePerRow + (x - 1)) * 2;
                    textures[tIdx + 0] = textures[tIdx + 1] = texDb.GetTexture(splatClassifications, QUANTIZATION_RESOLUTION * vertexPerRow, x, y);
                }
            }
        }

        // update mesh
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);

        // construct submeshes by textures
        var materials = new List<Material>();
        var texVisited = new HashSet<Texture2D>();
        for (int i = 0; i < textures.Length; ++i)
        {
            var tex = textures[i];
            if (texVisited.Contains(tex))
                continue;

            // configure texture
            tex.wrapMode = TextureWrapMode.Clamp;

            // configure material
            var mat = new Material(universalShader);
            mat.SetTexture("_MainTex", tex);
            mat.name = tex.name;
            materials.Add(mat);

            // find all with same texture
            var submeshTriangles = new List<int>();
            for (int j = i; j < textures.Length; ++j)
            {
                if (textures[j] == tex)
                {
                    var idx = j * 3;
                    submeshTriangles.Add(triangles[idx + 0]);
                    submeshTriangles.Add(triangles[idx + 1]);
                    submeshTriangles.Add(triangles[idx + 2]);
                }
            }

            mesh.subMeshCount = texVisited.Count + 1;
            mesh.SetTriangles(submeshTriangles, texVisited.Count);
            texVisited.Add(tex);
        }

        // set materials
        meshRenderer.sharedMaterials = materials.ToArray();
    }

    class TerrainTextureDatabase
    {
        private Terrain _terrain;
        private Texture2D _noiseTexture;
        private Dictionary<string, Texture2D> _classificationTexCache = new Dictionary<string, Texture2D>();

        public TerrainTextureDatabase(Terrain terrain)
        {
            _terrain = terrain;
            _noiseTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.Combine(FolderNames.ForgeFolder, "Textures", "terrain_noise.asset"));
        }

        public Texture2D GetTexture(Rect face)
        {
            // sample layers
            var layers = _terrain.terrainData.terrainLayers;
            if (layers == null || !layers.Any())
                return UnityHelper.DefaultTexture;

            Texture2D tex = null;
            for (int i = 0; i < _terrain.terrainData.alphamapTextureCount; ++i)
            {
                tex = SampleSplatmap(_terrain.terrainData, i, face, 64, 64);
            }

            // default to default tex
            if (!tex)
                tex = UnityHelper.DefaultTexture;

            return tex;
        }

        public Texture2D GetTexture(int[] classifications, int stride, int x, int y)
        {
            // sample layers
            var layers = _terrain.terrainData.terrainLayers;
            if (layers == null || !layers.Any())
                return UnityHelper.DefaultTexture;

            if (DEBUG_RENDER_SUBMESH.HasValue && !DEBUG_RENDER_SUBMESH.Value.Contains(new Vector2Int(x, y)))
                return UnityHelper.DefaultTexture;

            Texture2D tex = null;
            for (int i = 0; i < _terrain.terrainData.alphamapTextureCount; ++i)
            {
                var classification = GetClassificationBlock(classifications, stride, x, y);
                tex = SampleSplatmap(_terrain.terrainData, i, classification, TERRAIN_BAKED_TEX_RESOLUTION, TERRAIN_BAKED_TEX_RESOLUTION);
            }

            // default to default tex
            if (!tex)
                tex = UnityHelper.DefaultTexture;

            return tex;
        }

        private Texture2D SampleSplatmap(TerrainData terrainData, int splatmapIdx, Rect face, int width, int height)
        {
            var classification = Classify(terrainData, face, splatmapIdx);
            return SampleSplatmap(terrainData, splatmapIdx, classification, width, height);
        }

        private Texture2D SampleSplatmap(TerrainData terrainData, int splatmapIdx, int[] classification, int width, int height)
        {
            var shader = Shader.Find("Horizon Forge/SplatBlit");
            if (DEBUG_RENDER_SPLAT_POINT_FILTER)
            {
                width = QUANTIZATION_RESOLUTION;
                height = QUANTIZATION_RESOLUTION;
            }

            var classificationStr = GetClassificationString(classification);
            if (_classificationTexCache.TryGetValue(classificationStr, out var tex))
                return tex;

            var splatmap = GenerateFromClassification(classification, out var curvatureCenter);
            var rt = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32);
            rt.Create();
            try
            {
                var mat = new Material(shader);
                mat.SetTexture("_NoiseMap", _noiseTexture);
                mat.SetTexture("_SplatMap", splatmap);
                mat.SetVector("_SplatMap_ST", new Vector4(QUANTIZATION_RESOLUTION, QUANTIZATION_RESOLUTION, QUANTIZATION_BUFFER, QUANTIZATION_BUFFER) / QUANTIZATION_RESOLUTION_WITH_BUFFER);
                mat.SetTexture("_Splat0", terrainData.terrainLayers.ElementAtOrDefault(splatmapIdx * 4 + 0)?.diffuseTexture);
                mat.SetTexture("_Splat1", terrainData.terrainLayers.ElementAtOrDefault(splatmapIdx * 4 + 1)?.diffuseTexture);
                mat.SetTexture("_Splat2", terrainData.terrainLayers.ElementAtOrDefault(splatmapIdx * 4 + 2)?.diffuseTexture);
                mat.SetTexture("_Splat3", terrainData.terrainLayers.ElementAtOrDefault(splatmapIdx * 4 + 3)?.diffuseTexture);
                mat.SetVector("_CurvatureCenter", curvatureCenter);
                Graphics.Blit(splatmap, rt, mat);

                var oldRt = RenderTexture.active;
                RenderTexture.active = rt;
                var tex2 = new Texture2D(width, height, TextureFormat.ARGB32, false);
                tex2.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex2.Apply();
                RenderTexture.active = oldRt;

                _classificationTexCache[classificationStr] = tex2;
                tex2.name = classificationStr;
                if (DEBUG_RENDER_SPLAT_POINT_FILTER) tex2.filterMode = FilterMode.Point;
                return tex2;
            }
            finally
            {
                if (RenderTexture.active == rt)
                    RenderTexture.active = null;

                rt.Release();
            }
        }

        private Texture2D GenerateFromClassification(int[] classification, out Vector2 curvatureCenter)
        {
            var splatmap = new Texture2D(QUANTIZATION_RESOLUTION_WITH_BUFFER, QUANTIZATION_RESOLUTION_WITH_BUFFER, TextureFormat.ARGB32, false);

            // build splat
            for (int y = 0; y < QUANTIZATION_RESOLUTION_WITH_BUFFER; ++y)
            {
                for (int x = 0; x < QUANTIZATION_RESOLUTION_WITH_BUFFER; ++x)
                {
                    var color = Color.clear;
                    switch (classification[(y * QUANTIZATION_RESOLUTION_WITH_BUFFER) + x])
                    {
                        case 0: color = new Color(1, 0, 0, 0); break;
                        case 1: color = new Color(0, 1, 0, 0); break;
                        case 2: color = new Color(0, 0, 1, 0); break;
                        case 3: color = new Color(0, 0, 0, 1); break;
                    }

                    splatmap.SetPixel(x, y, color);
                }
            }

            // build curvature
            Vector2 center = Vector2.zero;
            var count = 0;
            var c0 = classification[classification.Length / 2];
            for (int i = 0; i < classification.Length; ++i)
            {
                if (classification[i] == c0)
                {
                    var x = i % QUANTIZATION_RESOLUTION_WITH_BUFFER;
                    var y = i / QUANTIZATION_RESOLUTION_WITH_BUFFER;
                    center += new Vector2((x - (QUANTIZATION_RESOLUTION_WITH_BUFFER / 2)), (y - (QUANTIZATION_RESOLUTION_WITH_BUFFER / 2))) / (QUANTIZATION_RESOLUTION_WITH_BUFFER - 1);
                    count++;
                }
            }
            curvatureCenter = new Vector2(center.x < 0 ? -1f : (center.x == 0 ? 0f : 1f), center.y < 0 ? -1f : (center.y == 0 ? 0f : 1f)) * 0.5f; // center / count;

            if (DEBUG_RENDER_SPLAT_POINT_FILTER) splatmap.filterMode = FilterMode.Point;
            splatmap.wrapMode = TextureWrapMode.Clamp;
            splatmap.Apply();
            return splatmap;
        }

        public int[] Classify(TerrainData terrainData, Rect face, int splatmapIdx)
        {
            var alphamapRect = new RectInt((int)(face.x * terrainData.alphamapResolution), (int)(face.y * terrainData.alphamapResolution), (int)(face.width * terrainData.alphamapResolution), (int)(face.height * terrainData.alphamapResolution));
            var alphamap = terrainData.GetAlphamapTexture(splatmapIdx);
            var classification = new int[QUANTIZATION_RESOLUTION_WITH_BUFFER * QUANTIZATION_RESOLUTION_WITH_BUFFER];

            // divide face into 3x3 grid
            // buffer with 1 row on each side (so 5x5 grid)
            // assign each section a texture
            for (int y = 0; y < QUANTIZATION_RESOLUTION_WITH_BUFFER; ++ y)
            {
                for (int x = 0; x < QUANTIZATION_RESOLUTION_WITH_BUFFER; ++x)
                {
                    Vector4 weights = Vector4.zero;
                    for (int j = 0; j < 9; ++j)
                    {
                        for (int i = 0; i < 9; ++i)
                        {
                            const float scale = 1f;
                            var xDisp = scale * (i - 4f) / 5f;
                            var yDisp = scale * (j - 4f) / 5f;
                            var xOff = (xDisp + (x - QUANTIZATION_BUFFER)) / QUANTIZATION_RESOLUTION;
                            var yOff = (yDisp + (y - QUANTIZATION_BUFFER)) / QUANTIZATION_RESOLUTION;
                            var weight = Mathf.Pow(Mathf.Exp(-Mathf.Sqrt(Mathf.Pow(xDisp, 2) + Mathf.Pow(yDisp, 2))), 1f);

                            var sample = (Vector4)alphamap.GetPixelBilinear(face.x + xOff * face.width, face.y + yOff * face.height);
                            weights += sample * weight;
                        }
                    }

                    var max = 0f;
                    for (int i = 0; i < 4; ++i)
                    {
                        if (weights[i] > max)
                        {
                            classification[(y * QUANTIZATION_RESOLUTION_WITH_BUFFER) + x] = i;
                            max = weights[i];
                        }
                    }
                }
            }

            return classification;
        }

        private int[] GetClassificationBlock(int[] classifications, int stride, int x, int y)
        {
            int[] classification = new int[QUANTIZATION_RESOLUTION_WITH_BUFFER * QUANTIZATION_RESOLUTION_WITH_BUFFER];
            var resMax = QUANTIZATION_RESOLUTION_WITH_BUFFER - 1;

            var rows = classifications.Length / stride;
            for (int cy = 0; cy < QUANTIZATION_RESOLUTION_WITH_BUFFER; ++cy)
            {
                for (int cx = 0; cx <  QUANTIZATION_RESOLUTION_WITH_BUFFER; ++cx)
                {
                    var sx = (cx - QUANTIZATION_BUFFER) + (x * QUANTIZATION_RESOLUTION);
                    var sy = (cy - QUANTIZATION_BUFFER) + (y * QUANTIZATION_RESOLUTION);
                    if (sx < 0) sx = 0;
                    if (sx >= stride) sx = stride - 1;
                    if (sy < 0) sy = 0;
                    if (sy >= rows) sy = rows - 1;

                    var idx = (sy * stride) + sx;
                    classification[(cy * QUANTIZATION_RESOLUTION_WITH_BUFFER) + cx] = classifications[idx];
                }
            }

            // ordered filter reduction pass
            if (!classification.All(x => x == classification[0]))
            {
                for (int i = 0; i < classification.Length; ++i)
                {
                    var sx = i % QUANTIZATION_RESOLUTION_WITH_BUFFER;
                    var sy = i / QUANTIZATION_RESOLUTION_WITH_BUFFER;
                    var dx = sx >= QUANTIZATION_BUFFER ? (sx == QUANTIZATION_BUFFER ? 0 : -1) : 1;
                    var dy = sy >= QUANTIZATION_BUFFER ? (sy == QUANTIZATION_BUFFER ? 0 : -1) : 1;

                    // edge
                    if ((sx == 0 || sx == resMax) || (sy == 0 || sy == resMax))
                    {
                        //var counts = new int[4];
                        //for (int m = 0; m < 3; ++m)
                        //{
                        //    for (int n = 0; n < 3; ++n)
                        //    {
                        //        var msx = sx + (m - 1);
                        //        var msy = sy + (m - 1);

                        //        if (msx < 0) continue;
                        //        if (msx > resMax) continue;
                        //        if (msy < 0) continue;
                        //        if (msy > resMax) continue;

                        //        counts[classification[(msy * QUANTIZATION_RESOLUTION_WITH_BUFFER) + msx]]++;
                        //    }
                        //}

                        //var c = classification[i];
                        //var count = counts[c];
                        //for (int m = 0; m < counts.Length; ++m)
                        //{
                        //    if (counts[m] > 0)
                        //    {
                        //        count = counts[m];
                        //        c = m;
                        //        break;
                        //    }
                        //}

                        //classification[i] = c;

                        var c0 = classification[i];
                        var c1 = classification[((sy + dy) * QUANTIZATION_RESOLUTION_WITH_BUFFER) + (sx + dx)];
                        if (c0 > c1)
                            classification[i] = c1;
                    }

                    // corner
                    if ((sx == 0 || sx == resMax) && (sy == 0 || sy == resMax))
                    {
                        var c0 = classification[i];
                        var c1 = classification[((sy + dy) * QUANTIZATION_RESOLUTION_WITH_BUFFER) + (sx + 0)];
                        var c2 = classification[((sy + 0) * QUANTIZATION_RESOLUTION_WITH_BUFFER) + (sx + dx)];

                        if (c1 != c2 && c0 > c1)
                            classification[i] = c1;
                        else if (c1 != c2 && c0 > c2)
                            classification[i] = c2;
                    }
                }
            }

            return classification;
        }

        private string GetClassificationString(int[] classification)
        {
            return String.Join("-", classification.Select(x => x.ToString()));
        }
    }
}
