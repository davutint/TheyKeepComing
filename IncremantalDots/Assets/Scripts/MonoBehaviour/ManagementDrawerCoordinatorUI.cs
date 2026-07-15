using UnityEngine;

namespace DeadWalls
{
    public enum ManagementDrawerId : byte
    {
        None = 0,
        WorkersHousing = 1,
        ArcherRecruitment = 2,
        ArrowSupply = 3
    }

    /// <summary>
    /// Player-facing management yuzeylerinin exclusive acilis sahibidir.
    /// Her drawer kendi presentation ve transaction sorumlulugunu korur; coordinator
    /// yalnizca yeni bir yuzey acilirken digerlerini aninda kapatir.
    /// </summary>
    public sealed class ManagementDrawerCoordinatorUI : MonoBehaviour
    {
        public ManagementDrawerId ActiveDrawer { get; private set; }

        private MarketUI _archerDrawer;
        private WorkerEconomyDrawerUI _workerDrawer;
        private ArrowSupplyUI _arrowSupply;
        private bool _isApplyingExclusiveState;

        private void Awake()
        {
            ResolveOwners();
        }

        private void OnEnable()
        {
            ResolveOwners();
            CloseAll();
        }

        public void Claim(ManagementDrawerId drawer)
        {
            if (drawer == ManagementDrawerId.None)
            {
                CloseAll();
                return;
            }

            ResolveOwners();
            _isApplyingExclusiveState = true;
            try
            {
                if (drawer != ManagementDrawerId.WorkersHousing)
                    _workerDrawer?.SetOpen(false);
                if (drawer != ManagementDrawerId.ArcherRecruitment)
                    _archerDrawer?.SetDrawerOpen(false, true);
                if (drawer != ManagementDrawerId.ArrowSupply)
                    _arrowSupply?.SetOpen(false);

                ActiveDrawer = drawer;
            }
            finally
            {
                _isApplyingExclusiveState = false;
            }
        }

        public void Release(ManagementDrawerId drawer)
        {
            if (_isApplyingExclusiveState)
                return;

            if (ActiveDrawer == drawer)
                ActiveDrawer = ManagementDrawerId.None;
        }

        public void CloseAll()
        {
            ResolveOwners();
            _isApplyingExclusiveState = true;
            try
            {
                _workerDrawer?.SetOpen(false);
                _archerDrawer?.SetDrawerOpen(false, true);
                _arrowSupply?.SetOpen(false);
                ActiveDrawer = ManagementDrawerId.None;
            }
            finally
            {
                _isApplyingExclusiveState = false;
            }
        }

        private void ResolveOwners()
        {
            _archerDrawer ??= GetComponent<MarketUI>();
            _workerDrawer ??= GetComponent<WorkerEconomyDrawerUI>();
            _arrowSupply ??= GetComponent<ArrowSupplyUI>();
        }
    }
}
