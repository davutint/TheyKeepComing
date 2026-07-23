using Unity.Mathematics;
using UnityEngine;

namespace DeadWalls
{
    public partial class GameManager
    {
        private int _arrowRefillDeliveryTotal;
        private float _arrowRefillDeliveryElapsed;

        public bool IsArrowRefillDeliveryActive => _arrowRefillDeliveryTotal > 0;

        public float ArrowRefillDeliveryProgress01 => IsArrowRefillDeliveryActive
            ? ArrowEconomyUtility.GetDeliveryProgress01(_arrowRefillDeliveryElapsed)
            : 0f;

        public float ArrowRefillDeliveryRemainingSeconds => IsArrowRefillDeliveryActive
            ? Mathf.Max(
                0f,
                ArrowEconomyUtility.RefillDeliveryDurationSeconds
                - _arrowRefillDeliveryElapsed)
            : 0f;

        public int PendingArrowRefillDeliveryAmount =>
            math.max(0, _arrowRefillDeliveryTotal);

        private void BeginArrowRefillDelivery(int arrowAmount)
        {
            _arrowRefillDeliveryTotal = math.max(0, arrowAmount);
            _arrowRefillDeliveryElapsed = 0f;
        }

        private void TickArrowRefillDelivery()
        {
            if (!IsArrowRefillDeliveryActive
                || GameState.IsGameOver
                || Time.deltaTime <= 0f)
                return;

            _arrowRefillDeliveryElapsed = Mathf.Min(
                ArrowEconomyUtility.RefillDeliveryDurationSeconds,
                _arrowRefillDeliveryElapsed + Time.deltaTime);

            if (_arrowRefillDeliveryElapsed
                >= ArrowEconomyUtility.RefillDeliveryDurationSeconds
                && TryApplyArrowRefillDelivery())
            {
                ResetArrowRefillDelivery();
            }
        }

        private void CompleteArrowRefillDeliveryImmediately()
        {
            if (!IsArrowRefillDeliveryActive)
                return;

            if (TryApplyArrowRefillDelivery())
                ResetArrowRefillDelivery();
        }

        private bool TryApplyArrowRefillDelivery()
        {
            if (_arrowRefillDeliveryTotal <= 0
                || !TryGetArrowSupply(out Unity.Entities.Entity entity, out ArrowSupply supply))
                return false;

            int capacity = ArrowEconomyUtility.GetCapacity(
                supply,
                GetEconomyPriceTuning());
            int current = math.clamp(supply.Current, 0, capacity);
            long next = (long)current + _arrowRefillDeliveryTotal;
            supply.Current = (int)math.min((long)capacity, next);
            supply.Accumulator = 0f;
            _entityManager.SetComponentData(entity, supply);
            ArrowSupply = supply;
            return true;
        }

        private void ResetArrowRefillDelivery()
        {
            _arrowRefillDeliveryTotal = 0;
            _arrowRefillDeliveryElapsed = 0f;
        }
    }
}
