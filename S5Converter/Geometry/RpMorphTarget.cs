using System.Text.Json.Serialization;

namespace S5Converter.Geometry
{
    internal struct RpMorphTarget
    {
        [JsonPropertyName("vertices")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Vec3[]? Verts;
        [JsonPropertyName("normals")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public Vec3[]? Normals;
        [JsonPropertyName("sphere")]
        public RwSphere BoundingSphere;

        internal readonly int NumVerts => Verts?.Length ?? Normals?.Length ?? 0;

        internal readonly int Size => (Verts?.Length ?? 0) * Vec3.Size + (Normals?.Length ?? 0) * Vec3.Size + RwSphere.Size + sizeof(int) * 2;

        internal static RpMorphTarget Read(BinaryReader s, int numVert)
        {
            RpMorphTarget r = new()
            {
                BoundingSphere = RwSphere.Read(s),
            };
            bool hasverts = s.ReadInt32() != 0;
            bool hasnorm = s.ReadInt32() != 0;
            if (hasverts)
            {
                r.Verts = new Vec3[numVert];
                for (int i = 0; i < numVert; i++)
                    r.Verts[i] = Vec3.Read(s);
            }
            if (hasnorm)
            {
                r.Normals = new Vec3[numVert];
                for (int i = 0; i < numVert; i++)
                    r.Normals[i] = Vec3.Read(s);
            }
            return r;
        }

        internal readonly void Write(BinaryWriter s, int nvert)
        {
            BoundingSphere.Write(s);
            s.Write(Verts == null ? 0 : 1);
            s.Write(Normals == null ? 0 : 1);
            if (Verts != null)
            {
                if (nvert != Verts.Length)
                    throw new IOException("morphtarget vertex number missmatch");
                foreach (var v in Verts)
                    v.Write(s);
            }
            if (Normals != null)
            {
                if (nvert != Normals.Length)
                    throw new IOException("morphtarget normals number missmatch");
                foreach (var n in Normals)
                    n.Write(s);
            }
        }
    }
}
