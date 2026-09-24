using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheReckoning.Morale
{
    public sealed class MoraleHUD : MonoBehaviour
    {
        [Serializable]
        private sealed class IconEntry
        {
            public MoraleEffect effect;
            public Sprite sprite;
        }

        [SerializeField] private MoraleSystem source;
        [SerializeField] private Team side;
        [SerializeField] private Transform container;
        [SerializeField] private MoraleIconView iconPrefab;
        [SerializeField] private IconEntry[] icons;

        private readonly List<MoraleStatus> statuses = new List<MoraleStatus>(8);
        private readonly MoraleIconView[] views = new MoraleIconView[8];
        private readonly bool[] visible = new bool[8];
        private bool dirty = true;
        private float refreshIn;
        private static readonly string[] Titles =
        {
            "Panic", "Inspiration", "Demoralization", "Battle Cry",
            "Rage", "Resilience", "Confidence", "Fear"
        };

        private void Awake()
        {
            if (source == null || container == null || iconPrefab == null)
            {
                Debug.LogError("MoraleHUD: assign Source, Container and Icon Prefab.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < views.Length; i++)
            {
                views[i] = Instantiate(iconPrefab, container);
                Sprite sprite = null;
                if (icons != null)
                    foreach (IconEntry entry in icons)
                        if (entry != null && (int)entry.effect == i)
                        {
                            sprite = entry.sprite;
                            break;
                        }

                views[i].Configure(sprite, Titles[i], (MoraleEffect)i);
                views[i].gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            if (source != null) source.StatusChanged += MarkDirty;
            dirty = true;
        }

        private void OnDisable()
        {
            if (source != null) source.StatusChanged -= MarkDirty;
        }

        private void MarkDirty(Team changedSide)
        {
            if (changedSide == side) dirty = true;
        }

        private void Update()
        {
            refreshIn -= Time.unscaledDeltaTime;
            if (!dirty && refreshIn > 0f) return;
            dirty = false;
            refreshIn = .1f;
            source.CopyStatuses(side, statuses);
            Array.Clear(visible, 0, visible.Length);
            foreach (MoraleStatus status in statuses)
            {
                int i = (int)status.Effect;
                visible[i] = true;
                views[i].Show(status);
            }
            for (int i = 0; i < views.Length; i++)
                if (views[i].gameObject.activeSelf != visible[i])
                    views[i].gameObject.SetActive(visible[i]);
        }

        private void OnDestroy()
        {
            foreach (MoraleIconView view in views)
                if (view != null) Destroy(view.gameObject);
        }
    }
}
