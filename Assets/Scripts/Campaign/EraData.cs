using UnityEngine;

namespace TheReckoning
{
    [CreateAssetMenu(fileName = "EraData", menuName = "The Reckoning/Era Data")]
    public sealed class EraData : ScriptableObject
    {
        [SerializeField] private string displayName = "Era";

        [Tooltip("Units available for hire, in hire button order.")]
        [SerializeField] private UnitData[] units = new UnitData[0];

        [Tooltip("Turrets available for purchase, in turret button order.")]
        [SerializeField] private TurretData[] turrets = new TurretData[0];

        [Tooltip("Optional free turret installed at match start.")]
        [SerializeField] private TurretData startingTurret;

        [Header("Bases")]
        [SerializeField] private Sprite orderBaseSprite;
        [SerializeField] private Sprite cultBaseSprite;

        public string DisplayName => displayName;
        public TurretData StartingTurret => startingTurret;
        public Sprite OrderBaseSprite => orderBaseSprite;
        public Sprite CultBaseSprite => cultBaseSprite;

        public int UnitCount => units != null ? units.Length : 0;
        public int TurretCount => turrets != null ? turrets.Length : 0;

        public UnitData GetUnit(int slot) =>
            slot >= 0 && slot < UnitCount ? units[slot] : null;

        public TurretData GetTurret(int slot) =>
            slot >= 0 && slot < TurretCount ? turrets[slot] : null;

        public bool HasUnit(UnitData unit) =>
            unit != null && units != null && System.Array.IndexOf(units, unit) >= 0;
    }
}
