using System;
using System.Collections.Generic;

namespace DeadWalls
{
    public enum HeartNodeVisibility
    {
        Hidden = 0,
        Revealed = 1
    }

    public enum HeartNodeLockState
    {
        Available = 0,
        KeystoneConflict = 1
    }

    /// <summary>
    /// Run basinda uretilen Castle Heart graph'inin save-safe durumudur.
    /// ScriptableObject referansi tasimaz; exact Continue icin primitive alanlardan olusur.
    /// </summary>
    [Serializable]
    public sealed class GeneratedRunGraph
    {
        public const int CurrentGraphVersion = 1;

        public int GraphVersion = CurrentGraphVersion;
        public int CatalogVersion = 1;
        public uint Seed;
        public string RootNodeId = "castle_heart";
        public List<GeneratedHeartNodeState> Nodes = new List<GeneratedHeartNodeState>();
        public List<GeneratedHeartEdge> Edges = new List<GeneratedHeartEdge>();
    }

    [Serializable]
    public sealed class GeneratedHeartNodeState
    {
        public string NodeId;
        public HeartNodeBranch Branch;
        public int Depth;
        public HeartNodeVisibility Visibility;
        public int Level;
        public HeartNodeLockState LockState;
        public string LockedByNodeId;
    }

    [Serializable]
    public sealed class GeneratedHeartEdge
    {
        public string FromNodeId;
        public string ToNodeId;
    }
}
