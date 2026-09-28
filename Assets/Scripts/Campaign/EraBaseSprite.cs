using UnityEngine;

namespace TheReckoning
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class EraBaseSprite : MonoBehaviour
    {
        [SerializeField] private GameManager match;
        [SerializeField] private Team side = Team.Left;

        private void Start()
        {
            if (match == null)
            {
                Debug.LogError("EraBaseSprite: assign GameManager.", this);
                return;
            }

            if (match.Era == null)
                return;

            Sprite sprite = side == Team.Left
                ? match.Era.OrderBaseSprite
                : match.Era.CultBaseSprite;

            if (sprite != null)
                GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }
}
