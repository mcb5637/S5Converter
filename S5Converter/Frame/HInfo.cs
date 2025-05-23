namespace S5Converter.Frame
{
    internal class HInfo
    {
        internal required FrameWithExt F;
        internal required RpHAnimHierarchy.Node? N;
        internal required int FrameIndex;
        internal required int? NodeId;
        internal List<HInfo> Children = [];
        internal HInfo? Parent = null;

        internal int NodeIndexSave => N?.NodeIndex ?? -1;

        public override string ToString()
        {
            return $"{FrameIndex} ({(NodeId != null ? NodeId.Value.ToString() : "-")})";
        }

        internal static (FrameWithExt?, RpHAnimHierarchy?) GetHierarchy(FrameWithExt[] frames)
        {
            FrameWithExt? hierlist = frames.SingleOrDefault(x => (x.Extension.HanimPLG?.Nodes?.Length ?? 0) > 0);
            if (hierlist == null || hierlist.Extension.HanimPLG == null)
                return (null, null);
            return (hierlist, hierlist.Extension.HanimPLG);
        }

        private static List<HInfo> BuildBasicHierarchy(FrameWithExt[] frames, RpHAnimHierarchy? hlist)
        {
            List<HInfo> hier = [];
            for (int i = 0; i < frames.Length; ++i)
            {
                FrameWithExt f = frames[i];
                RpHAnimHierarchy.Node? n = null;
                int? nid = null;
                if (f.Extension.HanimPLG != null && hlist != null)
                {
                    nid = f.Extension.HanimPLG.NodeID;
                    n = hlist.Nodes.FirstOrDefault(x => x.NodeID == f.Extension.HanimPLG.NodeID);
                    if (n == null)
                        throw new IOException($"hanim frame with node id {nid.Value} has no hierarchy info");
                }
                hier.Add(new()
                {
                    F = f,
                    N = n,
                    FrameIndex = i,
                    NodeId = nid,
                });
            }
            return hier;
        }

        static void CheckEverythingHasFrame(RpHAnimHierarchy hlist, List<HInfo> hier)
        {
            foreach (RpHAnimHierarchy.Node n in hlist.Nodes)
            {
                if (!hier.Any(x => x.NodeId == n.NodeID))
                    throw new IOException($"hanim hierarchy info {n.NodeID} has no frame");
            }
        }

        private static List<HInfo> BuildHierarchy(FrameWithExt[] frames, RpHAnimHierarchy? hlist, Func<List<HInfo>, HInfo, HInfo?> getParent)
        {
            List<HInfo> hier = BuildBasicHierarchy(frames, hlist);
            if (hlist != null)
                CheckEverythingHasFrame(hlist, hier);
            foreach (HInfo i in hier)
            {
                if (i.NodeId == null)
                    continue;
                HInfo? p = getParent(hier, i);
                if (p == null)
                    continue;
                i.Parent = p;
                p.Children.Add(i);
            }
            return hier;
        }

        internal static List<HInfo> BuildFrameHierarchy(FrameWithExt[] frames)
        {
            (FrameWithExt? hierlist, RpHAnimHierarchy? hlist) = GetHierarchy(frames);
            return BuildHierarchy(frames, hlist, (hier, i) => hier.FirstOrDefault(x => x.FrameIndex == i.F.Frame.ParentFrameIndex));
        }

        internal static List<HInfo> BuildHAnimHierarchy(FrameWithExt[] frames)
        {
            (FrameWithExt? hierlist, RpHAnimHierarchy? hlist) = GetHierarchy(frames);
            if (hlist == null || hlist.Parents == null)
                return [];
            List<HInfo> hier = BuildHierarchy(frames, hlist, (hier, i) =>
            {
                if (i.N == null)
                    return null;
                return hier.FirstOrDefault(x => x.N?.NodeIndex == hlist.Parents[i.N.NodeIndex]);
            });
            hier.RemoveAll(x => x.N == null || x.NodeId == null);
            return hier;
        }

        internal static List<HInfo> BuildHAnimHierarchyWithFallback(FrameWithExt[] frames, RpHAnimHierarchy hlist)
        {
            List<HInfo> hier = BuildHierarchy(frames, hlist, (hier, i) =>
            {
                if (i.N == null)
                    return null;
                if (hlist.Parents != null && i.N != null)
                    return hier.FirstOrDefault(x => x.N?.NodeIndex == hlist.Parents[i.N.NodeIndex]);
                else
                    return hier.FirstOrDefault(x => x.FrameIndex == i.F.Frame.ParentFrameIndex);
            });
            hier.RemoveAll(x => x.N == null || x.NodeId == null);
            return hier;
        }
    }
}
