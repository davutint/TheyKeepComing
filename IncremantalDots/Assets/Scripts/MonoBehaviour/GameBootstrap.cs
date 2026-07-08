namespace DeadWalls
{
    /// <summary>
    /// Sahneler arasi el-cantasi (M-E): ana menu sahnesindeki secim (Continue/NewRun),
    /// oyun sahnesi yuklendikten sonra RunBootstrap tarafindan okunur ve uygulanir.
    /// Static — sahne gecisinde yasar; editorde NewGameScene dogrudan acilirsa None kalir
    /// (oyun menusuz, temiz baslar — bot/test akislari etkilenmez).
    /// </summary>
    public static class GameBootstrap
    {
        public enum StartAction
        {
            None,
            Continue,
            NewRun
        }

        public static StartAction PendingAction = StartAction.None;

        public const string MainMenuSceneName = "MainMenuScene";
        public const string GameSceneName = "NewGameScene";
    }
}
