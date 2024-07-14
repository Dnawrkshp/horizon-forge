﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class UnityTerrainToTfrags : BaseAssetGenerator
{
    [Range(4f, 8f)] public float m_TfragSize = 4f;
    public TextureSize m_TextureSize = TextureSize._128;
    public bool m_RenderGenerated = false;

    private Terrain m_Terrain;

    private void OnValidate()
    {
        SetVisible(m_RenderGenerated);
    }

    #region Generate

    public override void Generate()
    {
        m_Terrain = GetComponent<Terrain>();

        var triOfs = new int[] { 0, 4, 2, 1 };
        var universalShader = Shader.Find("Horizon Forge/Universal");
        var chunks = GetChunkInstances();
        var chunkCount = 0;

        try
        {
            TerrainHelper.ToMesh(m_Terrain, out var terrainVertices, out var terrainNormals, out var terrainUvs, out var terrainTriangles, out var terrainTextures, faceSize: m_TfragSize, textureSize: m_TextureSize);

            var vertexPerRow = Mathf.CeilToInt(m_Terrain.terrainData.size.x / m_TfragSize) + 1;
            var vertexPerColumn = Mathf.CeilToInt(m_Terrain.terrainData.size.z / m_TfragSize) + 1;
            var facePerRow = vertexPerRow - 1;
            var facePerColumn = vertexPerColumn - 1;
            var chunkPerRow = Mathf.CeilToInt(facePerRow / 2f);
            var chunkPerColumn = Mathf.CeilToInt(facePerColumn / 2f);
            var textures = new List<Texture2D>();
            var materials = new List<Material>();
            chunkCount = chunkPerRow * chunkPerColumn;

            // generate
            for (int i = 0; i < chunkCount; ++i)
            {
                var chunk = chunks.ElementAtOrDefault(i);
                MeshFilter chunkMeshFilter = null;
                MeshRenderer chunkMeshRenderer = null;
                if (!chunk)
                {
                    var go = new GameObject(i.ToString());
                    go.transform.SetParent(this.transform, false);
                    Hide(go);
                    chunkMeshFilter = go.AddComponent<MeshFilter>();
                    chunkMeshRenderer = go.AddComponent<MeshRenderer>();
                    chunk = go.AddComponent<TfragChunk>();
                    chunk.Octants = UnityHelper.GetAllOctants().ToArray();
                }
                else
                {
                    chunkMeshFilter = chunk.GetComponent<MeshFilter>();
                    chunkMeshRenderer = chunk.GetComponent<MeshRenderer>();
                }

                byte[] headerBytes = null;
                byte[] dataBytes = null;
                var quadValid = new List<bool>();
                var quads = new List<int[]>();
                var colors = new List<Color>();
                var vertices = new List<Vector3>();
                var normals = new List<Vector3>();
                var uvs = new List<Vector2>();
                var texs = new List<int>();
                var newMesh = new Mesh();

                // select faces in groups of 2x2s
                var x = (i % chunkPerRow) * 2;
                var y = (i / chunkPerRow) * 2;
                var texIdx = 0;
                for (int f = 0; f < 4; ++f)
                {
                    var fx = x + (f % 2);
                    var fy = y + (f / 2);
                    var isValid = fx < facePerRow && fy < facePerColumn;
                    var fIdx = ((fy * facePerRow) + fx) * 2;
                    var triIdx = fIdx * 3;
                    var vIdx = 0;

                    if (!isValid) continue;

                    var quad = new int[4];
                    for (int t = 0; t < 4; ++t)
                    {
                        var tOfs = triIdx + triOfs[t];
                        vIdx = terrainTriangles[tOfs];

                        quad[t] = vertices.Count;
                        vertices.Add(terrainVertices[vIdx]);
                        normals.Add(terrainNormals[vIdx]);
                        uvs.Add(terrainUvs[vIdx]);
                        colors.Add(Color.white * 0.5f);
                    }

                    var tex = terrainTextures[fIdx];
                    texIdx = textures.IndexOf(tex);
                    if (texIdx < 0)
                    {
                        texIdx = textures.Count;
                        textures.Add(tex);

                        // configure material
                        var mat = new Material(universalShader);
                        mat.SetTexture("_MainTex", tex);
                        mat.SetColor("_Color", new Color(1, 1, 1, 0.5f));
                        mat.name = tex.name;
                        materials.Add(mat);
                    }

                    texs.Add(texIdx);
                    quads.Add(quad);
                    quadValid.Add(isValid);
                }

                if (quads.Count == 4)
                    TfragHelper.GenerateTfrag_2x2(vertices, normals, uvs, quads, texs, out headerBytes, out dataBytes);
                else if (quads.Count == 2)
                    TfragHelper.GenerateTfrag_1x2(vertices, normals, uvs, quads, texs, out headerBytes, out dataBytes);
                else if (quads.Count == 1)
                    TfragHelper.GenerateTfrag_1x1(vertices, normals, uvs, quads, texs, out headerBytes, out dataBytes);
                else
                    throw new NotImplementedException();

                newMesh.SetVertices(vertices);
                newMesh.SetUVs(0, uvs);
                newMesh.SetColors(colors);
                newMesh.subMeshCount = quads.Count;
                for (int f = 0; f < quads.Count; ++f)
                {
                    var quad = quads[f];
                    newMesh.SetIndices(new int[] { quad[0], quad[1], quad[2], quad[1], quad[3], quad[2] }, MeshTopology.Triangles, f);
                }

                chunk.gameObject.name = i.ToString();
                chunk.HeaderBytes = headerBytes;
                chunk.DataBytes = dataBytes;
                chunkMeshFilter.sharedMesh = newMesh;
                chunkMeshRenderer.sharedMaterials = texs.Select(x => materials[x]).ToArray();
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        finally
        {
            // cleanup
        }

        // remove excess
        for (int i = chunkCount; i < chunks.Length; ++i)
            GameObject.DestroyImmediate(chunks[i].gameObject);
    }

    private void TestGen()
    {
        var chunks = HierarchicalSorting.Sort(this.GetComponentsInChildren<TfragChunk>());

        for (int i = 0; i < 1; ++i)
        {
            var chunk = chunks.ElementAtOrDefault(i);
            if (!chunk)
            {
                var go = new GameObject(i.ToString());
                go.transform.SetParent(this.transform, false);
                chunk = go.AddComponent<TfragChunk>();
                chunk.Octants = UnityHelper.GetAllOctants().ToArray();
            }

            var verts = new Vector3[]
            {
                new Vector3(0, 0, 0),
                new Vector3(-1, 0, 0),
                new Vector3(0, 0, 1),
                new Vector3(-1, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(-1, 0, 1),
                new Vector3(0, 0, 2),
                new Vector3(-1, 0, 2),
                new Vector3(1, 0, 0),
                new Vector3(0, 0, 0),
                new Vector3(1, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 1),
                new Vector3(0, 0, 1),
                new Vector3(1, 0, 2),
                new Vector3(0, 0, 2),
            };

            var normals = new Vector3[]
            {
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
                Vector3.up,
            };

            var uvs = new Vector2[]
            {
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(0, 0),
                new Vector2(1, 1),
                new Vector2(0, 1),
            };

            var quads = new int[][]
            {
                new int[] { 0, 1, 2, 3 },
                new int[] { 4, 5, 6, 7 },
                new int[] { 8, 9, 10, 11 },
                new int[] { 12, 13, 14, 15 },
            };

            var texs = new int[] { 0, 1, 2, 3, };

            TfragHelper.GenerateTfrag_2x2(verts, normals, uvs, quads, texs, out var headerBytes, out var dataBytes);
            chunk.gameObject.name = i.ToString();
            chunk.HeaderBytes = headerBytes;
            chunk.DataBytes = dataBytes;

            File.WriteAllBytes("M:\\Unity\\horizon-forge\\levels\\Tower\\rc4\\assets\\terrain\\test.tfragdef", headerBytes);
            File.WriteAllBytes("M:\\Unity\\horizon-forge\\levels\\Tower\\rc4\\assets\\terrain\\test.tfrag", dataBytes);
        }
    }

    #endregion

    #region Bake

    public override void OnPreBake()
    {
        // render tfrags
        SetVisible(true);
    }

    public override void OnPostBake()
    {
        // return to normal render mode
        SetVisible(m_RenderGenerated);
    }

    #endregion

    private TfragChunk[] GetChunkInstances()
    {
        return HierarchicalSorting.Sort(this.GetComponentsInChildren<TfragChunk>(true).Where(x => x.gameObject.hideFlags.HasFlag(HideFlags.HideInHierarchy)).ToArray());
    }

    private void SetVisible(bool visible)
    {
        var chunks = GetChunkInstances();
        if (chunks != null)
        {
            foreach (var chunk in chunks)
            {
                if (chunk) chunk.gameObject.SetActive(visible);
            }
        }

        m_Terrain = GetComponent<Terrain>();
        if (m_Terrain) m_Terrain.enabled = !visible;
    }

    private void Hide(GameObject go)
    {
        go.hideFlags = HideFlags.HideInHierarchy;
        go.SetActive(m_RenderGenerated);
    }
}
