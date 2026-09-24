using UnityEngine;

namespace TheReckoning.Morale
{
    // Optional visual for the Fear and Battle Cry modifiers applied to this unit.
    [RequireComponent(typeof(Unit))]
    public sealed class UnitMoraleAuraView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private Sprite[] ringFrames;
        [SerializeField, Min(0.02f)] private float frameDuration = 0.1f;
        [SerializeField] private Color battleCryColor = new Color(1f, 0.85f, 0.3f, 0.85f);
        [SerializeField] private Color fearColor = new Color(0.9f, 0.4f, 0.85f, 0.9f);

        private Unit unit;
        private MoraleEffect? shown;
        private float frameClock;
        private int frame;
        private Vector3 unflippedRingPosition;

        private void Awake()
        {
            unit = GetComponent<Unit>();
            if (ring != null)
            {
                unflippedRingPosition = ring.transform.localPosition;
                ring.enabled = false;
            }
        }

        private void Update()
        {
            if (ring == null || ringFrames == null || ringFrames.Length == 0 ||
                unit == null || !unit.IsAlive || unit.Match == null || !unit.Match.IsRunning ||
                unit.Match.Morale == null)
            {
                Hide();
                return;
            }

            // Unit.Initialize flips the body SpriteRenderer, not its child transforms.
            // Mirror both the ring pixels and its offset around the same local origin.
            bool faceLeft = unit.Side == Team.Right;
            ring.flipX = faceLeft;
            Vector3 position = unflippedRingPosition;
            position.x = faceLeft ? -position.x : position.x;
            ring.transform.localPosition = position;

            MoraleSystem morale = unit.Match.Morale;

            // A unit may have both statuses; Fear takes visual priority.
            MoraleEffect? effect = morale.HasLocalEffect(unit, MoraleEffect.Fear)
                ? MoraleEffect.Fear
                : morale.HasLocalEffect(unit, MoraleEffect.BattleCry)
                    ? MoraleEffect.BattleCry
                    : (MoraleEffect?)null;

            if (!effect.HasValue)
            {
                Hide();
                return;
            }

            if (shown != effect)
            {
                shown = effect;
                frame = 0;
                frameClock = 0f;
                ring.color = effect == MoraleEffect.Fear ? fearColor : battleCryColor;
            }

            frameClock += Time.deltaTime;
            float step = Mathf.Max(0.02f, frameDuration);
            while (frameClock >= step)
            {
                frameClock -= step;
                frame = (frame + 1) % ringFrames.Length;
            }

            ring.sprite = ringFrames[frame];
            ring.enabled = ring.sprite != null;
        }

        private void Hide()
        {
            shown = null;
            if (ring != null) ring.enabled = false;
        }

        private void OnDisable() => Hide();
    }
}
