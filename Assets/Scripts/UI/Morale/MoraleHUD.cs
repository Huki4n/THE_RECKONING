using System;
using System.Collections.Generic;
using TheReckoning.ECS;
using Unity.Entities;
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

        [SerializeField] private Team side;
        [SerializeField] private Transform container;
        [SerializeField] private MoraleIconView iconPrefab;
        [SerializeField] private IconEntry[] icons;

        private readonly List<MoraleStatus> statuses =
            new List<MoraleStatus>(8);

        private readonly MoraleIconView[] views =
            new MoraleIconView[8];

        private readonly bool[] visible =
            new bool[8];

        private EcsMoraleRuntimeSystem source;
        private float refreshIn;

        private static readonly string[] Titles =
        {
            "Panic",
            "Inspiration",
            "Demoralization",
            "Battle Cry",
            "Rage",
            "Resilience",
            "Confidence",
            "Fear"
        };

        private void Awake()
        {
            if (container == null ||
                iconPrefab == null)
            {
                Debug.LogError(
                    "MoraleHUD: assign Container and Icon Prefab.",
                    this);

                enabled = false;
                return;
            }

            for (int i = 0;
                 i < views.Length;
                 i++)
            {
                views[i] =
                    Instantiate(
                        iconPrefab,
                        container);

                Sprite sprite = null;

                if (icons != null)
                {
                    foreach (IconEntry entry in icons)
                    {
                        if (entry != null &&
                            (int)entry.effect == i)
                        {
                            sprite = entry.sprite;
                            break;
                        }
                    }
                }

                views[i].Configure(
                    sprite,
                    Titles[i],
                    (MoraleEffect)i);

                views[i]
                    .gameObject
                    .SetActive(false);
            }
        }

        private void Start()
        {
            TryResolveSource();
        }

        private void Update()
        {
            refreshIn -=
                Time.unscaledDeltaTime;

            if (refreshIn > 0f)
                return;

            refreshIn = 0.1f;

            if (source == null)
            {
                TryResolveSource();

                if (source == null)
                {
                    HideAll();
                    return;
                }
            }

            byte teamId =
                side == Team.Left
                    ? (byte)0
                    : (byte)1;

            source.CopyStatuses(
                teamId,
                statuses);

            Array.Clear(
                visible,
                0,
                visible.Length);

            foreach (MoraleStatus status in statuses)
            {
                int i =
                    (int)status.Effect;

                if (i < 0 ||
                    i >= views.Length)
                {
                    continue;
                }

                visible[i] = true;

                views[i].Show(status);
            }

            for (int i = 0;
                 i < views.Length;
                 i++)
            {
                if (views[i] == null)
                    continue;

                bool active =
                    views[i]
                        .gameObject
                        .activeSelf;

                if (active != visible[i])
                {
                    views[i]
                        .gameObject
                        .SetActive(
                            visible[i]);
                }
            }
        }

        private void TryResolveSource()
        {
            World world =
                World.DefaultGameObjectInjectionWorld;

            if (world == null ||
                !world.IsCreated)
            {
                source = null;
                return;
            }

            source =
                world.GetExistingSystemManaged<
                    EcsMoraleRuntimeSystem>();
        }

        private void HideAll()
        {
            statuses.Clear();

            Array.Clear(
                visible,
                0,
                visible.Length);

            for (int i = 0;
                 i < views.Length;
                 i++)
            {
                if (views[i] != null &&
                    views[i]
                        .gameObject
                        .activeSelf)
                {
                    views[i]
                        .gameObject
                        .SetActive(false);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (MoraleIconView view in views)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }
        }
    }
}