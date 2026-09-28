using UnityEngine;

namespace TheReckoning.ECS.Presentation
{
    [CreateAssetMenu(menuName = "The Reckoning/View Catalog", fileName = "ViewCatalog")]
    public sealed class ViewCatalog : ScriptableObject
    {
        [SerializeField] private GameObject[] prefabs = new GameObject[0];

        public int Count => prefabs != null ? prefabs.Length : 0;

        public GameObject Get(int id) =>
            id >= 0 && id < Count ? prefabs[id] : null;

        public int IndexOf(GameObject prefab)
        {
            if (prefab == null || prefabs == null)
                return -1;

            for (int i = 0; i < prefabs.Length; i++)
            {
                if (prefabs[i] == prefab)
                    return i;
            }

            return -1;
        }
    }
}
