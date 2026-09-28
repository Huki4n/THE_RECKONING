using UnityEngine;

namespace TheReckoning
{
    [CreateAssetMenu(fileName = "EraCatalog", menuName = "The Reckoning/Era Catalog")]
    public sealed class EraCatalog : ScriptableObject
    {
        [Tooltip("Campaign eras in play order.")]
        [SerializeField] private EraData[] eras = new EraData[0];

        public int Count => eras != null ? eras.Length : 0;

        public EraData Get(int index) =>
            index >= 0 && index < Count ? eras[index] : null;
    }
}
