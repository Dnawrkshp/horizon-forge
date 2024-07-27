using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TfragHelper
{
    #region Transformations

    public static void SetChunkTextureIndices(byte[] def, byte[] data, IEnumerable<int> texIndices)
    {
        var texOff = BitConverter.ToInt16(def, 0x1c);
        var texCnt = def[0x28];
        var msphereOff = BitConverter.ToInt16(def, 0x2e);
        var msphereCnt = def[0x2c];

        // invalid
        if (texIndices.Count() != texCnt) throw new InvalidOperationException($"Attempting to set tfrag chunk texture indices of size {texIndices.Count()} for chunk with {texCnt} textures");

        using (var ms = new MemoryStream(data, true))
        {
            using (var writer = new BinaryWriter(ms))
            {
                // update texture defs
                var remap = new Dictionary<int, int>();
                for (int i = 0; i < texCnt; i++)
                {
                    ms.Position = texOff + (i * 0x50);
                    var ogValue = BitConverter.ToInt32(data, (int)ms.Position);
                    var newValue = texIndices.ElementAt(i);
                    remap.Add(ogValue, newValue);
                    writer.Write(newValue);
                }

                // update mspheres
                for (int i = 0; i < msphereCnt; ++i)
                {
                    var off = msphereOff + (i * 0x10) + 0xF;
                    if (remap.TryGetValue(data[off], out var newTexIdx))
                        data[off] = (byte)newTexIdx;
                }
            }
        }
    }

    public static void TransformChunk(byte[] def, byte[] data, Matrix4x4 transformationMatrix)
    {
        var defReadStream = new MemoryStream(def.ToArray());
        var defWriteStream = new MemoryStream(def, true);
        var dataReadStream = new MemoryStream(data.ToArray());
        var dataWriteStream = new MemoryStream(data, true);

        var defReader = new BinaryReader(defReadStream);
        var defWriter = new BinaryWriter(defWriteStream);
        var dataReader = new BinaryReader(dataReadStream);
        var dataWriter = new BinaryWriter(dataWriteStream);

        var bSpherePosition = ReadVector3_1024(defReader);
        var bSphereRadius = defReader.ReadSingle() / 1024f;
        var bSpherePost = bSpherePosition;

        var preCenterPositions = new List<Vector3>();
        var postCenterPositions = new List<Vector3>();

        defReader.BaseStream.Position = 0x2C;
        var vCount = (int)defReader.ReadByte();
        defReader.ReadByte();
        var vOffset = (int)defReader.ReadInt16();

        defReader.BaseStream.Position = 0x1E;
        var colorOffset = (int)defReader.ReadInt16();

        defReader.BaseStream.Position = 0x30;
        var pOffset = (int)defReader.ReadInt16();

        defReader.BaseStream.Position = 0x38;
        var cubeOffset = (int)defReader.ReadInt16();

        // read og position
        dataReader.BaseStream.Position = pOffset;
        var ogPosition = ReadVector3_32(dataReader);

        // write bsphere
        defWriter.BaseStream.Position = 0;
        WriteVector3_1024(defWriter, bSpherePost = transformationMatrix.MultiplyPoint(bSpherePosition));

        // write vertices
        for (int v = 0; v < vCount; v++)
        {
            dataReader.BaseStream.Position = dataWriter.BaseStream.Position = vOffset + (0x10 * v);
            WriteVector3_1024(dataWriter, transformationMatrix.MultiplyPoint(ReadVector3_1024(dataReader)));
        }

        // write position
        dataReader.BaseStream.Position = dataWriter.BaseStream.Position = pOffset;
        WriteVector3_1024(dataWriter, transformationMatrix.MultiplyPoint(ReadVector3_1024(dataReader)));

        // write cube
        for (int c = 0; c < 8; ++c)
        {
            dataReader.BaseStream.Position = dataWriter.BaseStream.Position = cubeOffset + (c * 8);
            WriteVector3_16(dataWriter, transformationMatrix.MultiplyPoint(ReadVector3_16(dataReader)));
        }

        // find positions in packet
        for (int w = 0; w < colorOffset; w += 4)
        {
            dataReader.BaseStream.Position = w;
            var word = dataReader.ReadInt32();

            // STROW
            if (word == 0x30000000)
            {
                // there are multiple STROWs with different data
                // hack: check if read vector3_32 is within bsphere
                // will probably fail when bsphere includes 0,0,0
                // as integer values will likely be read as a vector near 0,0,0
                var strowPosition = ReadVector3_32(dataReader);
                if (ogPosition == strowPosition) 
                //var dist = Vector3.Distance(strowPosition, bSpherePosition);
                //if (dist < bSphereRadius)
                {
                    var transformed = transformationMatrix.MultiplyPoint(strowPosition);

                    preCenterPositions.Add(strowPosition);
                    postCenterPositions.Add(transformed);

                    dataWriter.BaseStream.Position = w + 4;
                    WriteVector3_32(dataWriter, transformed);
                }
            }
        }

        // find and transform displacements
        int lod = 0;
        bool match = false;
        for (int w = 0; w < colorOffset; w += 4)
        {
            dataReader.BaseStream.Position = w;
            var word = dataReader.ReadInt32();

            // UNPACK
            if (((word >> 24) & 0b01100000) == 0b01100000)
            {
                var vn = (word >> 26) & 0b11;
                var vl = (word >> 24) & 0b11;
                var num = (word >> 16) & 0b11111111;
                if (num == 0)
                    num = 256;
                var gsize = ((32 >> vl) * (vn + 1)) / 8;
                var size = num * gsize;
                if (size % 4 != 0)
                    size += 4 - (size % 4);

                size = (1 + (size / 4)) * 4;

                if (gsize == 6)
                {
                    for (int di = 0; di < num; ++di)
                    {
                        dataReader.BaseStream.Position = dataWriter.BaseStream.Position = w + 4 + (di * 6);

                        var displacement = ReadVector3_16_1024(dataReader);
                        var realPos = preCenterPositions[lod] + displacement;
                        WriteVector3_16_1024(dataWriter, transformationMatrix.MultiplyPoint(realPos) - postCenterPositions[lod]);

                        if (!match)
                        {
                            match = true;
                            ++lod;
                        }
                    }
                }

                // skip
                w += size - 4;
            }
        }
    }

    #endregion

    #region Build

    // 2x2
    static readonly int[] GENERATE_TFRAG_DATA_2X2_LODOFS = new[] { 0x260, 0x314 };
    static readonly int[] GENERATE_TFRAG_DATA_2X2_STRIPOFS = new[] { 0x1C, 0x2F4 };
    static readonly string GENERATE_TFRAG_DATA_2X2 = "AAAAMDUAAAA1AAAANQAAADUAAAABAAAFRcAEbgABAgMJBAoFBgcLDA0IDg8AAAAFSYAFboQAAACEAAUAhAAKAIQADwAA////AAAAAAAAAAAAwAVtCQAHAAAAAAAAAAAAHQA1AD4ARQBFAEUARQBFAEUARQBFAEUASQAJAAmAFGwCAAAAAAAAAAYAAAAAAAAAd/8AAAQAAAAUAAAAAAAARQAAAAAAAAAACAAAAAAAAAAAAAAAAAAAADQAAAAAAAAAAAAAAAAAAAA2AAAAAAAAAAMAAAAAAAAABgAAAAAAAAB3/wAABAAAABQAAAAAAABFAAAAAAAAAAAIAAAAAAAAAAAAAAAAAAAANAAAAAAAAAAAAAAAAAAAADYAAAAAAAAAEQAAAAAAAAAGAAAAAAAAAHf/AAAEAAAAFAAAAAAAAEUBAAAAAQAAAAgAAAAAAAAAAAAAAAAAAAA0AAAAAAAAAAAAAAAAAAAANgAAAAAAAAAMAAAAAAAAAAYAAAAAAAAAd/8AAAQAAAAUAAAAAAAARQAAAAABAAAACAAAAAAAAAAAAAAAAAAAADQAAAAAAAAAAAAAAAAAAAA2AAAAAAAAAAAAADAAAABFAAAARQAAAAAdAAAAAQAABTWAEG0AEAAAABAAAAAgAAAAEAIAABAAEAAQBAAAIAAQABAGAAAQAAAAEAgAABAAEAAQCgAAAAAAABAMAAAQAAAAEA4AABAAAAAQEAAAAAAAABACAAAAABAAEAYAAAAAEAAQAAAAEAAQABACAAAAAAAAEA4AAAAAEAAQAgAAEAAQABAIAAAAADDdCAYAAR0IAOmaAQAAAAAAAgEAAR2ACWn/H+z/1f/0/+7/1f8gIBYg7P8WAAcg1f/z3+3/1f8Y4PQf1f/pH+zfpADe/+vfXP/g3+vf1f8AAAQEAAEAAAAFAAAAAAAAAABJgAVuhAAAAIQABQCEAAoAhAAPAAD///8AAAAwNQAAADUAAAA1AAAANQAAAAEAAAVFwARuAAECAwkECgUGBwsMDQgODwAAAAAAAAAAAAAAAAAAADDdCAYAAR0IAOmaAQAAAAAAAgEAAQAAAAAAAAAAAAAABQQEAAFJgAVuhAAAAIQABQCEAAoAhAAPAAD///8AAAAwNQAAADUAAAA1AAAANQAAAAEAAAVFwARuAAECAwkECgUGBwsMDQgODwAAAAVDRTuANzkxgDs+NIBXWk2ARUg9gE9RRYAvMSqAODsygFFUR4AAAAAAAAAAAAAAAADdCAYAAR0IAOmaAQAAAAAAAHqBPgidAAADeYpA5pgAAAN9ej/nmAAAA3qPQGqlAAADeilAKJ0AAAN6oEBJoQAAA31aPcWUAAADeD0+55gAAAN5OT1KoQAAAAAAAAAAAADrHMNINdACSflkzUe+Fv8CQxy/SLPPAklQX81Hthb/A4cZw0jUzgBJCkHNR6wW/xHtGL9I3M4ASVBfzUeoFv8Mohf1IGsGAACkGPUgbgYAAKMX8x9vBgAApBjzH28GAACjF/QgawYAAKQY9SBpBgAAoxfzH2kGAACkGPMfaQYAAA==";

    // 1x2
    static readonly int[] GENERATE_TFRAG_DATA_1X2_LODOFS = new[] { 0x170, 0x204 };
    static readonly int[] GENERATE_TFRAG_DATA_1X2_STRIPOFS = new[] { 0x1C, 0x254 };
    static readonly string GENERATE_TFRAG_DATA_1X2 = "AAAAMCMAAAAjAAAAIwAAACMAAAABAAAFK8ACbgABAgMEBQYHAAAABS2AA26EAAAAhAAFAAD///8AAAAAAAAAAADABW0GAAIAAAAAAAAAAAATACMAKQArACsAKwArACsAKwArACsAKwAtAAkACYAKbBEAAAAAAAAABgAAAAAAAAB3/wAABAAAABQAAAAAAABFAQAAAAEAAAAIAAAAAAAAAAAAAAAAAAAANAAAAAAAAAAAAAAAAAAAADYAAAAAAAAAEwAAAAAAAAAGAAAAAAAAAHf/AAAEAAAAFAAAAAAAAEUBAAAAAQAAAAgAAAAAAAAAAAAAAAAAAAA0AAAAAAAAAAAAAAAAAAAANgAAAAAAAAAAAAAwAAAARQAAAEUAAAAAEwAAAAEAAAUjgAhtAAAAAAAQAAAAEAAAABACAAAAABAAEAQAABAAEAAQBgAAAAAAABAIAAAQAAAAEAoAAAAAEAAQAAAAEAAQABACAAAAADBSGQcA69wHAJymAQAAAAAAAgEAAROABmkfEAkAhf4K8AUAiAEeEAsgaP0J8AUgSv77DwTglwLh7/XfnwEEBAABAAAABQAAAAAAAAAAAAAAAC2AA26EAAAAhAAFAAD///8AAAAwIwAAACMAAAAjAAAAIwAAAAEAAAUrwAJuAAECAwQFBgcAAAAAAAAAAAAAAAAAAAAwUhkHAOvcBwCcpgEAAAAAAAIBAAEAAAAAAAAAAAAAAAUEBAABLYADboQAAACEAAUAAP///wAAADAjAAAAIwAAACMAAAAjAAAAAQAABSvAAm4AAQIDBAUGBwAAAAU4OjGAS05DgC4wKYA0Ni6AOjwzgExPRIAAAAAAAAAAAFIZBwDr3AcAnKYBAAAAAAAAfy0855gAAAN/WTspoQAAA4AyOcWUAAADe1c3xpQAAAOAOTrnmAAAA31IOSmhAADGLONIYp79SDUK00fBFv8RSCrjSFid+UhYV9NHyhb/E4AczR+ZBgAAphzzHqUGAAAkHPQfpQYAACQc8x6lBgAAphz1H5AGAACmHJQfkAYAACQc9B+QBgAAJRzzHqEGAAA=";

    static readonly Vector3[] CUBE_AXES = new Vector3[]
    {
        new Vector3(1, 1, 1),
        new Vector3(1, 1, -1),
        new Vector3(1, -1, 1),
        new Vector3(1, -1, -1),
        new Vector3(-1, 1, 1),
        new Vector3(-1, 1, -1),
        new Vector3(-1, -1, 1),
        new Vector3(-1, -1, -1),
    };

    /// <summary>
    /// Generate 2x2 tfrag from 4 quads.
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="uvs"></param>
    /// <param name="quads"></param>
    /// <param name="quadTextures"></param>
    /// <param name="def"></param>
    /// <param name="data"></param>
    public static void GenerateTfrag_2x2(IEnumerable<Vector3> vertices, IEnumerable<Vector3> normals, IEnumerable<Color> colors, IEnumerable<Vector2> uvs, IEnumerable<int[]> quads, IEnumerable<int> quadTextures, out byte[] def, out byte[] data)
    {
        data = Convert.FromBase64String(GENERATE_TFRAG_DATA_2X2);
        var header = new TfragHeader()
        {
            lod_2_ofs = 0,
            shared_ofs = 0x50,
            lod_1_ofs = 0x2C0,
            lod_0_ofs = 0x310,
            tex_ofs = 0x80,
            rgba_ofs = 0x380,
            common_size = 0x27,
            lod_2_size = 0x2C,
            lod_1_size = 0x2E,
            lod_0_size = 0x07,
            lod_2_rgba_cnt = 0x0C,
            lod_1_rgba_cnt = 0x0C,
            lod_0_rgba_cnt = 0x0C,
            base_only = true,
            tex_cnt = 4,
            rgba_size = 3,
            rgba_verts_loc = 0x1E,
            occl_index_stash = 0,
            msphere_cnt = 4,
            flags = 1,
            msphere_ofs = 0x410,
            light_ofs = 0x3B0,
            light_vert_start_off = 0x278,
            dir_lights_one = -1,
            dir_lights_upd = 0,
            point_lights = 0xFFFF,
            cube_ofs = 0x450,
            occl_index = 0,
            vert_cnt = 9,
            tri_cnt = 8,
            mip_dist = short.MinValue
        };

        GenerateTfrag(4, 9, GENERATE_TFRAG_DATA_2X2_STRIPOFS, GENERATE_TFRAG_DATA_2X2_LODOFS, vertices, normals, colors, uvs, quads, quadTextures, header, data, out def);
    }

    /// <summary>
    /// Generate 1x2 tfrag from 2 quads.
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="uvs"></param>
    /// <param name="quads"></param>
    /// <param name="quadTextures"></param>
    /// <param name="def"></param>
    /// <param name="data"></param>
    public static void GenerateTfrag_1x2(IEnumerable<Vector3> vertices, IEnumerable<Vector3> normals, IEnumerable<Color> colors, IEnumerable<Vector2> uvs, IEnumerable<int[]> quads, IEnumerable<int> quadTextures, out byte[] def, out byte[] data)
    {
        data = Convert.FromBase64String(GENERATE_TFRAG_DATA_1X2);
        var header = new TfragHeader()
        {
            lod_2_ofs = 0,
            shared_ofs = 0x40,
            lod_1_ofs = 0x1C0,
            lod_0_ofs = 0x200,
            tex_ofs = 0x70,
            rgba_ofs = 0x260,
            common_size = 0x18,
            lod_2_size = 0x1C,
            lod_1_size = 0x1E,
            lod_0_size = 0x06,
            lod_2_rgba_cnt = 0x08,
            lod_1_rgba_cnt = 0x08,
            lod_0_rgba_cnt = 0x08,
            base_only = true,
            tex_cnt = 2,
            rgba_size = 2,
            rgba_verts_loc = 0x14,
            occl_index_stash = 0,
            msphere_cnt = 2,
            flags = 1,
            msphere_ofs = 0x2C0,
            light_ofs = 0x280,
            light_vert_start_off = 0x188,
            dir_lights_one = -1,
            dir_lights_upd = 0,
            point_lights = 0xFFFF,
            cube_ofs = 0x2E0,
            occl_index = 0,
            vert_cnt = 6,
            tri_cnt = 4,
            mip_dist = short.MinValue
        };

        GenerateTfrag(2, 6, GENERATE_TFRAG_DATA_1X2_STRIPOFS, GENERATE_TFRAG_DATA_1X2_LODOFS, vertices, normals, colors, uvs, quads, quadTextures, header, data, out def);
    }

    /// <summary>
    /// Generate 1x1 tfrag from 1 quads.
    /// </summary>
    /// <param name="vertices"></param>
    /// <param name="uvs"></param>
    /// <param name="quads"></param>
    /// <param name="quadTextures"></param>
    /// <param name="def"></param>
    /// <param name="data"></param>
    public static void GenerateTfrag_1x1(IEnumerable<Vector3> vertices, IEnumerable<Vector3> normals, IEnumerable<Color> colors, IEnumerable<Vector2> uvs, IEnumerable<int[]> quads, IEnumerable<int> quadTextures, out byte[] def, out byte[] data)
    {
        data = Convert.FromBase64String(GENERATE_TFRAG_DATA_1X2);
        var header = new TfragHeader()
        {
            lod_2_ofs = 0,
            shared_ofs = 0x40,
            lod_1_ofs = 0x1C0,
            lod_0_ofs = 0x200,
            tex_ofs = 0x70,
            rgba_ofs = 0x260,
            common_size = 0x18,
            lod_2_size = 0x1C,
            lod_1_size = 0x1E,
            lod_0_size = 0x06,
            lod_2_rgba_cnt = 0x08,
            lod_1_rgba_cnt = 0x08,
            lod_0_rgba_cnt = 0x08,
            base_only = true,
            tex_cnt = 1,
            rgba_size = 2,
            rgba_verts_loc = 0x14,
            occl_index_stash = 0,
            msphere_cnt = 1,
            flags = 1,
            msphere_ofs = 0x2C0,
            light_ofs = 0x280,
            light_vert_start_off = 0x188,
            dir_lights_one = -1,
            dir_lights_upd = 0,
            point_lights = 0xFFFF,
            cube_ofs = 0x2E0,
            occl_index = 0,
            vert_cnt = 4,
            tri_cnt = 2,
            mip_dist = short.MinValue
        };

        GenerateTfrag(1, 4, GENERATE_TFRAG_DATA_1X2_STRIPOFS, GENERATE_TFRAG_DATA_1X2_LODOFS, vertices, normals, colors, uvs, quads, quadTextures, header, data, out def);
    }

    private static void GenerateTfrag(int quadCount,
                                      int expectedVertices,
                                      int[] stripOffsets,
                                      int[] lodPosOffsets,
                                      IEnumerable<Vector3> vertices,
                                      IEnumerable<Vector3> normals,
                                      IEnumerable<Color> colors,
                                      IEnumerable<Vector2> uvs,
                                      IEnumerable<int[]> quads,
                                      IEnumerable<int> quadTextures,
                                      TfragHeader header,
                                      byte[] data,
                                      out byte[] def)
    {
        def = new byte[0x40];

        // generate ordered vertices from quads
        List<Vector3> baseVertices = new List<Vector3>();
        List<Vector3> baseNormals = new List<Vector3>();
        List<Color> baseColors = new List<Color>();
        List<int> baseCounts = new List<int>();
        List<TfragVertexEx> orderedVertices = new List<TfragVertexEx>();
        List<int[]> orderedQuads = new List<int[]>();

        // add base vertices first
        foreach (var quad in quads.Take(quadCount))
        {
            for (int i = 0; i < 4; ++i)
            {
                var vertex = vertices.ElementAtOrDefault(quad[i]).SwizzleXZY();
                var normal = normals.ElementAtOrDefault(quad[i]).SwizzleXZY();
                var color = colors.ElementAtOrDefault(quad[i]);
                var baseVertexIdx = baseVertices.IndexOf(vertex);
                if (baseVertexIdx < 0)
                {
                    baseVertexIdx = baseVertices.Count;
                    baseVertices.Add(vertex);
                    baseNormals.Add(normal);
                    baseColors.Add(color);
                    baseCounts.Add(1);

                    orderedVertices.Add(new TfragVertexEx()
                    {
                        position = vertex,
                        normal = normal,
                        color = color,
                        baseVertexIdx = baseVertexIdx,
                        uv = uvs.ElementAtOrDefault(quad[i])
                    });
                }
            }
        }

        // add rest of vertices and build quads
        foreach (var quad in quads.Take(quadCount))
        {
            var orderedQuad = new int[4];
            for (int i = 0; i < 4; ++i)
            {
                var vertex = vertices.ElementAtOrDefault(quad[i]).SwizzleXZY();
                var normal = normals.ElementAtOrDefault(quad[i]).SwizzleXZY();
                var color = colors.ElementAtOrDefault(quad[i]);
                var uv = uvs.ElementAtOrDefault(quad[i]);
                var vertexIdx = orderedVertices.FindIndex(x => x.position == vertex && x.uv == uv && x.normal == normal);
                if (vertexIdx < 0)
                {
                    vertexIdx = orderedVertices.Count;
                    var baseVertexIdx = baseVertices.IndexOf(vertex);
                    orderedVertices.Add(new TfragVertexEx()
                    {
                        position = vertex,
                        normal = normal,
                        color = color,
                        baseVertexIdx = baseVertexIdx,
                        uv = uv
                    });

                    baseCounts[baseVertexIdx] += 1;
                    baseNormals[baseVertexIdx] += normal;
                    baseColors[baseVertexIdx] += color;
                }

                // add
                orderedQuad[i] = vertexIdx;
            }

            orderedQuads.Add(orderedQuad);
        }

        // average merged colors
        for (int i = 0; i < baseColors.Count; ++i)
            baseColors[i] /= baseCounts[i];

        if (baseVertices.Count != expectedVertices) throw new Exception($"Base vertices does not matched expected {expectedVertices}.");

        using (var defMs = new MemoryStream(def, true))
        using (var dataMs = new MemoryStream(data, true))
        using (var defWriter = new BinaryWriter(defMs))
        using (var dataWriter = new BinaryWriter(dataMs))
        {
            // compute bsphere
            var bCenter = orderedVertices.Select(x => x.position).Average();
            var bRadius = orderedVertices.Max(x => Vector3.Distance(x.position, bCenter));
            header.bSphere = bCenter.SwizzleXZY();
            header.bSphere.w = bRadius * 2f;

            // compute bounds
            var bounds = new Bounds(bCenter, Vector3.one / 8f);
            foreach (var vertex in orderedVertices) bounds.Encapsulate(vertex.position);
            var center = bounds.center.Quantize(1024);

            // update cube
            for (int i = 0; i < 8; ++i)
            {
                dataMs.Position = header.cube_ofs + (i * 8);
                WriteVector3_16(dataWriter, center + Vector3.Scale(bounds.size, CUBE_AXES[i]));
            }

            // write position
            dataMs.Position = header.light_ofs;
            WriteVector3_32(dataWriter, center);

            // set colors to default
            dataMs.Position = header.rgba_ofs;
            for (int i = 0; i < header.vert_cnt; ++i)
                dataWriter.Write(0x80808080);

            // update msphere
            for (int i = 0; i < header.msphere_cnt; ++i)
            {
                var quadCenter = orderedQuads[i].Select(x => orderedVertices[x].position).Average();
                var quadRadius = orderedQuads[i].Max(x => Vector3.Distance(orderedVertices[x].position, quadCenter)) * 0f;

                dataMs.Position = header.msphere_ofs + (0x10 * i);
                WriteVector3_1024(dataWriter, quadCenter);
                //dataWriter.Write((ushort)(quadRadius * 1024f));
            }

            // update tristrips
            foreach (var stripOfs in stripOffsets)
            {
                dataMs.Position = stripOfs;
                foreach (var quad in orderedQuads)
                    for (int i = 0; i < 4; ++i)
                        dataWriter.Write((byte)quad[i]);
            }

            // update positions of both lods
            foreach (var lodOfs in lodPosOffsets)
            {
                dataMs.Position = lodOfs;
                WriteVector3_32(dataWriter, center);
            }

            // update displacements
            dataMs.Position = header.light_vert_start_off;
            for (int i = 0; i < baseVertices.Count; ++i)
                WriteVector3_16_1024(dataWriter, baseVertices[i] - center);

            // update normals
            for (int i = 0; i < baseNormals.Count; ++i)
            {
                dataMs.Position = header.light_ofs + 0x10 + (8 * i);
                dataWriter.Write(PackNormal(baseNormals[i], baseColors[i]));
            }

            // update vertices
            dataMs.Position = header.tex_ofs + (0x50 * header.tex_cnt) + 0x1C;
            for (int i = 0; i < orderedVertices.Count; ++i)
            {
                var v = orderedVertices[i];
                WriteUV_16(dataWriter, new Vector2(v.uv.x, 1 - v.uv.y));
                dataWriter.Write((ushort)0x1000);
                dataWriter.Write((short)(v.baseVertexIdx * 2));
            }

            // clamp textures
            for (int i = 0; i < header.tex_cnt; ++i)
            {
                dataMs.Position = header.tex_ofs + (0x50 * i) + 0x20;
                dataWriter.Write(1); // clamp U
                dataWriter.Write(1); // clamp V
            }

            // write header
            header.Write(defWriter);
        }
    }

    #endregion

    #region Read/Write Helpers

    private static Vector3 ReadVector3(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }

    private static Vector3 ReadVector3_1024(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle() / 1024f, reader.ReadSingle() / 1024f, reader.ReadSingle() / 1024f);
    }

    private static Vector3 ReadVector3_32(BinaryReader reader)
    {
        return new Vector3(reader.ReadInt32() / 1024f, reader.ReadInt32() / 1024f, reader.ReadInt32() / 1024f);
    }

    private static Vector3 ReadVector3_16_1024(BinaryReader reader)
    {
        return new Vector3(reader.ReadInt16() / 1024f, reader.ReadInt16() / 1024f, reader.ReadInt16() / 1024f);
    }

    private static Vector3 ReadVector3_16(BinaryReader reader)
    {
        return new Vector3(reader.ReadInt16() / 16f, reader.ReadInt16() / 16f, reader.ReadInt16() / 16f);
    }

    private static void WriteVector3(BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }
    private static void WriteVector3_1024(BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x * 1024f);
        writer.Write(vector.y * 1024f);
        writer.Write(vector.z * 1024f);
    }

    private static void WriteVector3_32(BinaryWriter writer, Vector3 vector)
    {
        writer.Write((int)Mathf.Round(vector.x * 1024f));
        writer.Write((int)Mathf.Round(vector.y * 1024f));
        writer.Write((int)Mathf.Round(vector.z * 1024f));
    }

    private static void WriteVector3_16_1024(BinaryWriter writer, Vector3 vector)
    {
        writer.Write((short)Mathf.Round(vector.x * 1024f));
        writer.Write((short)Mathf.Round(vector.y * 1024f));
        writer.Write((short)Mathf.Round(vector.z * 1024f));
    }

    private static void WriteVector3_16(BinaryWriter writer, Vector3 vector)
    {
        writer.Write((short)Mathf.Round(vector.x * 16f));
        writer.Write((short)Mathf.Round(vector.y * 16f));
        writer.Write((short)Mathf.Round(vector.z * 16f));
    }

    private static void WriteUV_16(BinaryWriter writer, Vector2 uv)
    {
        // encode extra scaling in negative
        if (uv.x < 0) uv.x *= 2;
        if (uv.y < 0) uv.y *= 2;

        writer.Write((short)Mathf.Round(uv.x * 4096f));
        writer.Write((short)Mathf.Round(uv.y * 4096f));
    }

    #endregion

    #region Normals

    public static Vector3 UnpackNormal(ushort intensity, ushort normal, ushort color)
    {
        float factor = intensity / (float)0x7FFF;
        int azimuthIdx = normal & 0xFF;
        int elevationIdx = normal >> 8;
        Color baseColor = new Color32((byte)((color & 0x1F) << 3), (byte)(((color >> 5) & 0x1F) << 3), (byte)(((color >> 10) & 0x1F) << 3), (byte)(((color >> 15) & 0x1) << 7));

        var azimuth = LookupTrigValues(azimuthIdx);
        var elevation = LookupTrigValues(elevationIdx);
        var normalVec = ((Vector3)azimuth * elevation.x + Vector3.forward * elevation.y);

        return normalVec * factor;
    }

    public static ulong PackNormal(Vector3 normal, Color color)
    {
        float factor = Mathf.Clamp01(normal.magnitude);
        var normalNormalized = normal.normalized;

        // find closest base normal
        var azimuthIdx = (byte)(Mathf.Atan2(normalNormalized.y, normalNormalized.x) * (128f / Mathf.PI));
        var elevationIdx = (byte)(Mathf.Asin(normalNormalized.z) * (128f / Mathf.PI));

        // convert color to 16 bit
        ushort col16 = (ushort)((Mathf.RoundToInt(color.r * 255) >> 3) | ((Mathf.RoundToInt(color.g * 255) >> 3) << 5) | ((Mathf.RoundToInt(color.b * 255) >> 3) << 10) | ((Mathf.RoundToInt(color.a * 255) >> 7) << 15));

        return ((ulong)(factor * 0x7FFF) | ((ulong)azimuthIdx << 16) | ((ulong)elevationIdx << 24) | ((ulong)col16 << 32) | ((ulong)0x0000 << 48));
    }

    private static Vector2 LookupTrigValues(int idx)
    {
        return new Vector2(Mathf.Cos(idx * Mathf.PI / 128f), Mathf.Sin(idx * Mathf.PI / 128f));
    }

    #endregion

}

public struct TfragHeader
{
    public Vector4 bSphere;
    public uint pData;
    public ushort lod_2_ofs;
    public ushort shared_ofs;
    public ushort lod_1_ofs;
    public ushort lod_0_ofs;
    public ushort tex_ofs;
    public ushort rgba_ofs;
    public sbyte common_size;
    public sbyte lod_2_size;
    public sbyte lod_1_size;
    public sbyte lod_0_size;
    public sbyte lod_2_rgba_cnt;
    public sbyte lod_1_rgba_cnt;
    public sbyte lod_0_rgba_cnt;
    public bool base_only;
    public sbyte tex_cnt;
    public sbyte rgba_size;
    public sbyte rgba_verts_loc;
    public sbyte occl_index_stash;
    public byte msphere_cnt;
    public byte flags;
    public short msphere_ofs;
    public short light_ofs;
    public short light_vert_start_off;
    public sbyte dir_lights_one;
    public sbyte dir_lights_upd;
    public ushort point_lights;
    public short cube_ofs;
    public short occl_index;
    public byte vert_cnt;
    public sbyte tri_cnt;
    public short mip_dist;

    public void Write(BinaryWriter writer)
    {
        writer.Write(bSphere.x * 1024f);
        writer.Write(bSphere.z * 1024f);
        writer.Write(bSphere.y * 1024f);
        writer.Write(bSphere.w * 1024f);
        writer.Write(pData);
        writer.Write(lod_2_ofs);
        writer.Write(shared_ofs);
        writer.Write(lod_1_ofs);
        writer.Write(lod_0_ofs);
        writer.Write(tex_ofs);
        writer.Write(rgba_ofs);
        writer.Write(common_size);
        writer.Write(lod_2_size);
        writer.Write(lod_1_size);
        writer.Write(lod_0_size);
        writer.Write(lod_2_rgba_cnt);
        writer.Write(lod_1_rgba_cnt);
        writer.Write(lod_0_rgba_cnt);
        writer.Write(base_only);
        writer.Write(tex_cnt);
        writer.Write(rgba_size);
        writer.Write(rgba_verts_loc);
        writer.Write(occl_index_stash);
        writer.Write(msphere_cnt);
        writer.Write(flags);
        writer.Write(msphere_ofs);
        writer.Write(light_ofs);
        writer.Write(light_vert_start_off);
        writer.Write(dir_lights_one);
        writer.Write(dir_lights_upd);
        writer.Write(point_lights);
        writer.Write(cube_ofs);
        writer.Write(occl_index);
        writer.Write(vert_cnt);
        writer.Write(tri_cnt);
        writer.Write(mip_dist);
    }

    public void Read(BinaryReader reader)
    {
        bSphere.x = reader.ReadSingle() / 1024f;
        bSphere.z = reader.ReadSingle() / 1024f;
        bSphere.y = reader.ReadSingle() / 1024f;
        bSphere.w = reader.ReadSingle() / 1024f;
        pData = reader.ReadUInt32();
        lod_2_ofs = reader.ReadUInt16();
        shared_ofs = reader.ReadUInt16();
        lod_1_ofs = reader.ReadUInt16();
        lod_0_ofs = reader.ReadUInt16();
        tex_ofs = reader.ReadUInt16();
        rgba_ofs = reader.ReadUInt16();
        common_size = reader.ReadSByte();
        lod_2_size = reader.ReadSByte();
        lod_1_size = reader.ReadSByte();
        lod_0_size = reader.ReadSByte();
        lod_2_rgba_cnt = reader.ReadSByte();
        lod_1_rgba_cnt = reader.ReadSByte();
        lod_0_rgba_cnt = reader.ReadSByte();
        base_only = reader.ReadBoolean();
        tex_cnt = reader.ReadSByte();
        rgba_size = reader.ReadSByte();
        rgba_verts_loc = reader.ReadSByte();
        occl_index_stash = reader.ReadSByte();
        msphere_cnt = reader.ReadByte();
        flags = reader.ReadByte();
        msphere_ofs = reader.ReadInt16();
        light_ofs = reader.ReadInt16();
        light_vert_start_off = reader.ReadInt16();
        dir_lights_one = reader.ReadSByte();
        dir_lights_upd = reader.ReadSByte();
        point_lights = reader.ReadUInt16();
        cube_ofs = reader.ReadInt16();
        occl_index = reader.ReadInt16();
        vert_cnt = reader.ReadByte();
        tri_cnt = reader.ReadSByte();
        mip_dist = reader.ReadInt16();
    }
}

struct TfragVertexEx
{
    public Vector3 position;
    public Vector3 normal;
    public Color color;
    public Vector2 uv;
    public int parent;
    public int baseVertexIdx;
}
