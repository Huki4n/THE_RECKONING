using UnityEngine;

namespace TheReckoning
{
    // Place on a mount near a base. Player purchase uses the existing Economy.
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

            if (startingTurret != null)
            {
                if (turretPrefab.CanUse(startingTurret)) Install(startingTurret);
                else Debug.LogError("TurretSlot: Starting Turret or Turret Prefab is incomplete.", this);
            }
        }

        // Assign these three methods directly to UI button OnClick events.
        public void BuyRapid() => TryBuy(rapid);
        public void BuyMarksman() => TryBuy(marksman);
        public void BuyHeavy() => TryBuy(heavy);

        public bool CanBuy(TurretData data) => enabled && gameObject.activeInHierarchy &&
            match != null && match.IsRunning && side == Team.Left &&
            turretPrefab != null && turretPrefab.CanUse(data) &&
            (installed == null || installedData != data) &&
            match.LeftEconomy != null && match.LeftEconomy.CanAfford(data.Cost);

        // A different turret can replace the installed one at full price; no refund or era unlock is implied.
        public bool TryBuy(TurretData data)
        {
            if (!CanBuy(data) || !match.LeftEconomy.TrySpend(data.Cost))
                return false;

            Install(data);
            return true;
        }

        // Use for scripted Cult towers; they do not spend player gold.
        public bool InstallEnemy(TurretData data)
        {
            if (!enabled || match == null || !match.IsRunning || side != Team.Right ||
                turretPrefab == null || !turretPrefab.CanUse(data))
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
