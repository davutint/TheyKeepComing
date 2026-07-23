#if UNITY_EDITOR
namespace DeadWalls
{
    /// <summary>
    /// Dead Walls Editor araclarinin tek ve tutarli menu hiyerarsisi.
    /// Yeni editor araclari dogrudan menu yolu yazmak yerine bu sabitleri kullanmalidir.
    /// </summary>
    public static class DeadWallsEditorMenuPaths
    {
        public const string Root = "Tools/Dead Walls/";
        public const string Audio = Root + "Audio/";
        public const string Balancing = Root + "Balancing/";
        public const string Content = Root + "Content/";
        public const string Maps = Root + "Maps/";
        public const string Profiling = Root + "Profiling/";
        public const string SetupAndRepair = Root + "Setup & Repair/";
    }
}
#endif
