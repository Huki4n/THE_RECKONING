using UnityEngine;

namespace TheReckoning
{
    public sealed class TurretSlot : MonoBehaviour
    {
        [SerializeField] private GameManager match;
        [SerializeField] private Team side = Team.Left;
        [SerializeField] private Turret turretPrefab;
        [SerializeField] private TurretData startingTurret;
        [SerializeField] private TurretData rapid;
        [SerializeField] private TurretData marksman;
        [SerializeField] private TurretData heavy;

        private Turret installed;
        private TurretData installedData;

        public bool Occupied => installed != null;

        private void Start()
        {
            if (match == null || turretPrefab == null || !turretPrefab.enabled)
            {
                Debug.LogError("TurretSlot: assign GameManager and Turret Prefab.", this);
                enabled = false;
                return;
            }

            TurretData starting =
                match.Era != null && match.Era.StartingTurret != null
                    ? match.Era.StartingTurret
                    : startingTurret;

            if (starting != null)
            {
                if (turretPrefab.CanUse(starting)) Install(starting);
                else Debug.LogError("TurretSlot: Starting Turret or Turret Prefab is incomplete.", this);
            }
        }

        public void BuyRapid() => TryBuy(EraTurret(0, rapid));
        public void BuyMarksman() => TryBuy(EraTurret(1, marksman));
        public void BuyHeavy() => TryBuy(EraTurret(2, heavy));

        private TurretData EraTurret(int slot, TurretData fallback) =>
            match != null && match.Era != null ? match.Era.GetTurret(slot) : fallback;

        public bool CanBuy(TurretData data) => data != null && enabled && gameObject.activeInHierarchy &&
            match != null && match.IsRunning && side == Team.Left &&
            turretPrefab != null && turretPrefab.CanUse(data) &&
            (installed == null || installedData != data) &&
            match.LeftEconomy != null && match.LeftEconomy.CanAfford(data.Cost);

        public bool TryBuy(TurretData data)
        {
            if (!CanBuy(data) || !match.LeftEconomy.TrySpend(data.Cost))
                return false;

            Install(data);
            return true;
        }

        private void Install(TurretData data)
        {
            if (installed != null)
            {
                installed.gameObject.SetActive(false);
                Destroy(installed.gameObject);
            }

            installed = Instantiate(turretPrefab, transform.position, transform.rotation, transform);
            installed.gameObject.SetActive(true);
            installed.Initialize(match, side, data);
            installedData = data;
        }
    }
}
