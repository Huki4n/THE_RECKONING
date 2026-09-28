using System;
using TheReckoning.ECS;
using TheReckoning.Morale;
using Unity.Entities;
using UnityEngine;

namespace TheReckoning.Authoring
{
    [DisallowMultipleComponent]
    public sealed class MoraleConfigAuthoring : MonoBehaviour
    {
        [SerializeField] private MoraleConfig config;

        private sealed class Baker : Baker<MoraleConfigAuthoring>
        {
            public override void Bake(MoraleConfigAuthoring authoring)
            {
                DependsOn(authoring.config);

                if (authoring.config == null)
                {
                    Debug.LogError($"{authoring.name}: assign MoraleConfig.", authoring);
                    return;
                }

                MoraleSettings settings;

                try
                {
                    settings = authoring.config.CreateSettings();
                }
                catch (ArgumentException e)
                {
                    Debug.LogError($"{authoring.name}: {e.Message}", authoring);
                    return;
                }

                Entity entity = GetEntity(TransformUsageFlags.None);
                AddComponentObject(entity, new MoraleSettingsData { Value = settings });
            }
        }
    }
}
