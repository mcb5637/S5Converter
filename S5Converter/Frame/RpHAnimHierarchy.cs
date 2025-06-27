using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace S5Converter.Frame
{
    internal class RpHAnimHierarchy
    {
        internal struct RpHAnimHierarchyFlagS
        {
            [Flags]
            internal enum RpHAnimHierarchyFlag : int
            {
                None = 0,
                SubHierarchy = 0x01,
                NoMatrices = 0x02,
                UpdateModellingMatrices = 0x1000,
                UpdateLTMs = 0x2000,
                LocalSpaceMatrices = 0x4000,
            };

            internal RpHAnimHierarchyFlag Flag;

            [JsonRequired]
            public bool SubHierarchy
            {
                readonly get => Flag.HasFlag(RpHAnimHierarchyFlag.SubHierarchy);
                set => Flag.SetFlag(value, RpHAnimHierarchyFlag.SubHierarchy);
            }
            [JsonRequired]
            public bool NoMatrices
            {
                readonly get => Flag.HasFlag(RpHAnimHierarchyFlag.NoMatrices);
                set => Flag.SetFlag(value, RpHAnimHierarchyFlag.NoMatrices);
            }
            [JsonRequired]
            public bool UpdateModellingMatrices
            {
                readonly get => Flag.HasFlag(RpHAnimHierarchyFlag.UpdateModellingMatrices);
                set => Flag.SetFlag(value, RpHAnimHierarchyFlag.UpdateModellingMatrices);
            }
            [JsonRequired]
            public bool UpdateLTMs
            {
                readonly get => Flag.HasFlag(RpHAnimHierarchyFlag.UpdateLTMs);
                set => Flag.SetFlag(value, RpHAnimHierarchyFlag.UpdateLTMs);
            }
            [JsonRequired]
            public bool LocalSpaceMatrices
            {
                readonly get => Flag.HasFlag(RpHAnimHierarchyFlag.LocalSpaceMatrices);
                set => Flag.SetFlag(value, RpHAnimHierarchyFlag.LocalSpaceMatrices);
            }
        }

        [JsonPropertyName("nodeID")]
        public required int NodeID;
        [JsonPropertyName("flags")]
        public RpHAnimHierarchyFlagS Flags;
        [JsonPropertyName("keyFrameSize")]
        public int KeyFrameSize = 0;
        [JsonPropertyName("nodes")]
        public Node[] Nodes = [];
        [JsonPropertyName("parents")]
        public int[]? Parents;
        public bool ReBuildNodesArray = false;

        internal class Node
        {
            internal struct HAnimNodeFlagsS
            {
                public bool HasChildren;
                public bool LastSibling;

                [JsonIgnore]
                internal HAnimNodeFlags Flags
                {
                    readonly get
                    {
                        HAnimNodeFlags f = HAnimNodeFlags.None;
                        if (!LastSibling)
                            f |= HAnimNodeFlags.Push;
                        if (!HasChildren)
                            f |= HAnimNodeFlags.Pop;
                        return f;
                    }
                    set
                    {
                        LastSibling = !value.HasFlag(HAnimNodeFlags.Push);
                        HasChildren = !value.HasFlag(HAnimNodeFlags.Pop);
                    }
                }
            }
            [Flags]
            internal enum HAnimNodeFlags : int
            {
                None = 0,
                Pop = 0x01,
                Push = 0x02,
            };
            [JsonPropertyName("flags")]
            public HAnimNodeFlagsS Flags;
            [JsonPropertyName("nodeID")]
            public required int NodeID;
            [JsonPropertyName("nodeIndex")]
            public int NodeIndex;

            internal const int Size = sizeof(int) * 3;
        }

        internal int Size => sizeof(int) * (3 + (Nodes.Length > 0 ? 2 : 0)) + Node.Size * Nodes.Length;
        internal int SizeH => Size + ChunkHeader.Size;

        internal static RpHAnimHierarchy Read(BinaryReader s, bool header)
        {
            if (header)
                ChunkHeader.FindChunk(s, RwCorePluginID.HANIMPLUGIN);
            if (s.ReadInt32() != 256)
                throw new IOException("RpHAnimHierarchy read missing 256 constant");
            RpHAnimHierarchy r = new()
            {
                NodeID = s.ReadInt32()
            };
            int boneCount = s.ReadInt32();
            if (boneCount > 0)
            {
                r.Flags = new()
                {
                    Flag = (RpHAnimHierarchyFlagS.RpHAnimHierarchyFlag)s.ReadInt32(),
                };
                r.KeyFrameSize = s.ReadInt32();
                r.Nodes = new Node[boneCount];
                r.Parents = new int[boneCount];
                for (int i = 0; i < boneCount; ++i)
                {
                    r.Nodes[i] = new Node()
                    {
                        NodeID = s.ReadInt32(),
                        NodeIndex = s.ReadInt32(),
                        Flags = new()
                        {
                            Flags = (Node.HAnimNodeFlags)s.ReadInt32(),
                        },
                    };
                }
                BuildParents(r.Nodes, r.Parents);
            }
            return r;
        }

        private static void BuildParents(Node[] n, int[] p)
        {
            int last = Build(n, p, 0, -1);
            if (last != p.Length - 1)
                Console.Error.WriteLine("hanim hierarchy messed up, check before using");

            static int Build(Node[] n, int[] p, int i, int c)
            {
                for (; i < n.Length; ++i)
                {
                    p[i] = c;
                    bool lsib = n[i].Flags.LastSibling;
                    if (n[i].Flags.HasChildren)
                        i = Build(n, p, i + 1, i);
                    if (lsib)
                        return i;
                }
                return i;
            }
        }

        internal void Write(BinaryWriter s, bool header, uint versionNum, uint buildNum)
        {
            if (header)
            {
                new ChunkHeader()
                {
                    Length = Size,
                    Type = RwCorePluginID.HANIMPLUGIN,
                    BuildNum = buildNum,
                    Version = versionNum,
                }.Write(s);
            }
            s.Write(256);
            s.Write(NodeID);
            s.Write(Nodes.Length);
            if (Nodes.Length > 0)
            {
                s.Write((int)Flags.Flag);
                s.Write(KeyFrameSize);
                foreach (Node n in Nodes)
                {
                    s.Write(n.NodeID);
                    s.Write(n.NodeIndex);
                    s.Write((int)n.Flags.Flags);
                }
            }
        }

        internal static void RebuildNodeHierarchy(FrameWithExt[] frames)
        {
            (FrameWithExt? hierlist, RpHAnimHierarchy? hlist) = HInfo.GetHierarchy(frames);
            if (hierlist == null || hlist == null)
                return;

            if (hlist.ReBuildNodesArray)
                BuildNodeArray(frames, hlist);

            if (hlist.Parents != null && hlist.Parents.Length != hlist.Nodes.Length)
                throw new IOException("hanim parents length missmatch");

            ClearFlags(hlist);
            List<HInfo> hier = HInfo.BuildHAnimHierarchyWithFallback(frames, hlist, false);
            // sort children to preserve old hanim node order, whenever possible
            foreach (HInfo i in hier)
                i.Children.Sort((c1, c2) => c1.NodeIndexSave.CompareTo(c2.NodeIndexSave));
            RebuildNodes(hierlist, hlist, hier);
            if (hlist.Parents != null)
                BuildParents(hlist.Nodes, hlist.Parents);


            static void BuildNodeArray(FrameWithExt[] frames, RpHAnimHierarchy hlist)
            {
                hlist.Nodes = [.. frames.Where(x => x.Extension.HanimPLG != null).Select(x => new Node() { NodeID = x.Extension.HanimPLG!.NodeID })];
                hlist.Parents = null; // clear, because order has changed now
                hlist.ReBuildNodesArray = false;
            }
            static void RebuildNodes(FrameWithExt hierlist, RpHAnimHierarchy hlist, List<HInfo> hier)
            {
                if (RebuildNodeOrder(hier.First(x => x.F == hierlist), 0, hlist.Nodes) != hlist.Nodes.Length)
                    throw new IOException("hierarchy rebuild messed up");
                foreach (HInfo i in hier)
                {
                    if (i.Parent == null && i.N != null)
                        i.N.Flags.LastSibling = true;
                }

                static int RebuildNodeOrder(HInfo h, int i, Node[] nodes)
                {
                    if (h.N == null)
                        throw new IOException($"hanim rebuild node {h.FrameIndex} missing node???");
                    nodes[i] = h.N;
                    ++i;
                    if (h.Children.Count > 0)
                    {
                        h.N.Flags.HasChildren = true;
                        foreach (HInfo c in h.Children)
                            i = RebuildNodeOrder(c, i, nodes);
                        h.Children[^1].N!.Flags.LastSibling = true;
                    }
                    return i;
                }
            }
            static void ClearFlags(RpHAnimHierarchy hlist)
            {
                for (int i = 0; i < hlist.Nodes.Length; i++)
                {
                    hlist.Nodes[i].Flags.HasChildren = false;
                    hlist.Nodes[i].Flags.LastSibling = false;
                    hlist.Nodes[i].NodeIndex = i;
                }
            }
        }
    }
}
