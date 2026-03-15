using System.Text.Json.Serialization;

namespace S5Converter.Anim;

internal abstract class RtAnimAnimation
{
    [JsonConverter(typeof(EnumJsonConverter<AnimType>))]
    internal enum AnimType
    {
        HierarchicalAnim = 1,
        CompressedAnim = 2,
        UVAnimLinear = 0x1C0,
        UVAnimParam = 0x1C1,
    }

    [JsonRequired]
    public AnimType InterpolatorTypeId;

    public int Flags = 0;

    [JsonRequired]
    public float Duration;

    internal const int Size = sizeof(int) * 5;

    internal int ReadA(BinaryReader s, bool header)
    {
        if (header)
            ChunkHeader.FindChunk(s, RwCorePluginID.ANIMANIMATION);
        if (s.ReadInt32() != 256)
            throw new IOException("anim missing 256 constant");
        InterpolatorTypeId = (AnimType)s.ReadInt32();
        int nframes = s.ReadInt32();
        Flags = s.ReadInt32();
        Duration = s.ReadSingle();
        return nframes;
    }

    internal void WriteA(BinaryWriter s, int nKeyFrames, int headerSize, uint versionNum, uint buildNum)
    {
        if (headerSize > 0)
        {
            new ChunkHeader()
            {
                Type = RwCorePluginID.ANIMANIMATION,
                Length = headerSize,
                BuildNum = buildNum,
                Version = versionNum,
            }.Write(s);
        }

        s.Write(256);
        s.Write((int)InterpolatorTypeId);
        s.Write(nKeyFrames);
        s.Write(Flags);
        s.Write(Duration);
    }

    internal static RtAnimAnimation ReadAnyAnim(BinaryReader s, bool header)
    {
        if (header)
            ChunkHeader.FindChunk(s, RwCorePluginID.ANIMANIMATION);
        if (s.ReadInt32() != 256)
            throw new IOException("anim missing 256 constant");
        AnimType interpolatorTypeId = (AnimType)s.ReadInt32();
        int nframes = s.ReadInt32();
        int flags = s.ReadInt32();
        float duration = s.ReadSingle();

        switch (interpolatorTypeId)
        {
            case AnimType.CompressedAnim:
                RtCompressedAnim ca = new()
                {
                    Duration = duration,
                    Flags = flags,
                    InterpolatorTypeId = interpolatorTypeId,
                };
                ca.ReadAfterPeek(s, nframes);
                return ca;
            case AnimType.HierarchicalAnim:
                RpHierarchicalAnim ha = new()
                {
                    Duration = duration,
                    Flags = flags,
                    InterpolatorTypeId = interpolatorTypeId,
                };
                ha.ReadAfterPeek(s, nframes);
                return ha;
            default:
                throw new IOException($"anim type {interpolatorTypeId} is not supported.");
        }
    }
    
    internal abstract class AnimKeyframe
    {
        public required float Time;
        public required int PrevKeyFrame;
        public int NodeId = 0;
    }

    protected void RecreateNodeIds<T>(T[] keyframes, int[]? nodeIds) where T : AnimKeyframe
    {
        for (int i = 0; i < keyframes.Length; i++)
        {
            var f = keyframes[i];
            if (f.PrevKeyFrame < 0 && f.Time <= 0.0)
            {
                f.NodeId = i;
            }
            else if (f.PrevKeyFrame >= 0 && f.PrevKeyFrame < keyframes.Length)
            {
                f.NodeId = keyframes[f.PrevKeyFrame].NodeId;
            }
            else
            {
                throw new IOException("anim nodeid rebuild failed");
            }
        }

        if (nodeIds != null)
        {
            foreach (var f in keyframes)
                f.NodeId = nodeIds[f.NodeId];
        }
    }

    protected void RebuildKeyframeOrders<T>(T[] keyframes, int[]? nodeIds) where T : AnimKeyframe
    {
        keyframes.Sort((a, b) => GetSortKey(a).CompareTo(GetSortKey(b)));

        (float, int) GetSortKey(T f)
        {
            return (TimeOfPrevKeyframe(f), nodeIds?[f.NodeId] ?? f.NodeId);
        }
        float TimeOfPrevKeyframe(AnimKeyframe f)
        {
            if (f.PrevKeyFrame < 0)
                return -1.0f;
            return keyframes[f.PrevKeyFrame].Time;
        }
    }
}