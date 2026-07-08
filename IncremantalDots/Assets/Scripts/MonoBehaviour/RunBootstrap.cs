using UnityEngine;

namespace DeadWalls
{
    /// <summary>
    /// Oyun sahnesi acilis uygulayicisi (M-E v2): ana menuden gelen secimi
    /// (GameBootstrap.PendingAction) GameManager init olur olmaz uygular.
    /// - Continue: safak-checkpoint'ten restore (icinde temiz RestartGame tabani var)
    /// - NewRun: RestartGame — onceki oturumdan default world'de kalmis runtime
    ///   entity'ler (zombi/okcu) de boylece temizlenir (sahne gecisi ECS world'u YOK ETMEZ)
    /// - None (editorde dogrudan NewGameScene acilisi): dokunma — bot/test akislari bozulmaz.
    /// </summary>
    public class RunBootstrap : MonoBehaviour
    {
        private bool _applied;

        private void Update()
        {
            if (_applied)
                return;

            var gm = GameManager.Instance;
            if (gm == null || !gm.ContinuousSiegeCycle.Enabled)
                return; // init bekleniyor (lazy)

            _applied = true;
            switch (GameBootstrap.PendingAction)
            {
                case GameBootstrap.StartAction.Continue:
                    if (!gm.TryRestoreRunFromCheckpoint())
                        gm.RestartGame(); // kayit bozuksa temiz baslangica dus
                    break;
                case GameBootstrap.StartAction.NewRun:
                    gm.RestartGame();
                    break;
            }
            GameBootstrap.PendingAction = GameBootstrap.StartAction.None;
            Time.timeScale = 1f;
            enabled = false;
        }
    }
}
