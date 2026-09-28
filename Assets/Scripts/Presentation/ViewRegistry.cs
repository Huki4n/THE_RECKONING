using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace TheReckoning.ECS.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ViewRegistry : MonoBehaviour
    {
        private sealed class Slot
        {
            public GameObject Instance;
            public Transform Transform;
            public IEntityView[] Views;
            public int PrefabId;
        }

        private static int nextOwner;

        [SerializeField]
        private ViewCatalog catalog;

        private readonly List<Slot> slots =
            new List<Slot>();

        private readonly Stack<int> freeSlots =
            new Stack<int>();

        private readonly Dictionary<int, Stack<Slot>> pools =
            new Dictionary<int, Stack<Slot>>();

        private readonly HashSet<int> reportedIds =
            new HashSet<int>();

        public static ViewRegistry Active { get; private set; }

        public int Owner { get; private set; }

        private void OnEnable()
        {
            if (Active != null && Active != this)
            {
                Debug.LogError(
                    "ViewRegistry: only one active registry " +
                    "is supported.",
                    this);

                enabled = false;
                return;
            }

            if (catalog == null)
            {
                Debug.LogError(
                    "ViewRegistry: assign ViewCatalog.",
                    this);
            }

            Owner = ++nextOwner;
            Active = this;
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
        }

        public bool TryAcquire(
            int prefabId,
            Entity entity,
            EntityManager entityManager,
            out ViewHandle handle)
        {
            handle = default;

            GameObject prefab =
                catalog != null
                    ? catalog.Get(prefabId)
                    : null;

            if (prefab == null)
            {
                if (reportedIds.Add(prefabId))
                {
                    Debug.LogWarning(
                        $"ViewRegistry: no prefab with id " +
                        $"{prefabId} in catalog.",
                        this);
                }

                return false;
            }

            Slot slot =
                Rent(prefabId, prefab);

            if (!IsSlotAlive(slot))
            {
                slot = CreateSlot(
                    prefabId,
                    prefab);
            }

            int index =
                freeSlots.Count > 0
                    ? freeSlots.Pop()
                    : slots.Count;

            if (index == slots.Count)
            {
                slots.Add(slot);
            }
            else
            {
                slots[index] = slot;
            }

            slot.Instance.SetActive(true);

            if (slot.Views != null)
            {
                foreach (IEntityView view in slot.Views)
                {
                    if (view != null)
                    {
                        view.Bind(
                            entity,
                            entityManager);
                    }
                }
            }

            handle =
                new ViewHandle
                {
                    Index = index,
                    Owner = Owner
                };

            return true;
        }

        public void SetPose(
            ViewHandle handle,
            float3 position,
            quaternion rotation)
        {
            Slot slot =
                Resolve(handle);

            if (!IsSlotAlive(slot))
            {
                InvalidateSlot(handle);
                return;
            }

            slot.Transform.SetPositionAndRotation(
                position,
                rotation);
        }

        public void PlayAttack(
            ViewHandle handle)
        {
            Slot slot =
                Resolve(handle);

            if (!IsSlotAlive(slot))
            {
                InvalidateSlot(handle);
                return;
            }

            if (slot.Views == null)
                return;

            foreach (IEntityView view in slot.Views)
            {
                if (view != null)
                    view.PlayAttack();
            }
        }

        public void Release(
            ViewHandle handle)
        {
            Slot slot =
                Resolve(handle);

            if (slot == null)
                return;

            if (!IsSlotAlive(slot))
            {
                InvalidateSlot(handle);
                return;
            }

            if (slot.Views != null)
            {
                foreach (IEntityView view in slot.Views)
                {
                    if (view != null)
                        view.Unbind();
                }
            }

            slot.Instance.SetActive(false);

            slots[handle.Index] = null;

            freeSlots.Push(
                handle.Index);

            if (!pools.TryGetValue(
                    slot.PrefabId,
                    out Stack<Slot> pool))
            {
                pool =
                    new Stack<Slot>();

                pools.Add(
                    slot.PrefabId,
                    pool);
            }

            pool.Push(slot);
        }

        private Slot Resolve(
            ViewHandle handle)
        {
            if (handle.Owner != Owner)
                return null;

            if (handle.Index < 0 ||
                handle.Index >= slots.Count)
            {
                return null;
            }

            return slots[handle.Index];
        }

        private static bool IsSlotAlive(
            Slot slot)
        {
            if (slot == null)
                return false;

            if (slot.Instance == null)
                return false;

            if (slot.Transform == null)
                return false;

            return true;
        }

        private void InvalidateSlot(
            ViewHandle handle)
        {
            if (handle.Owner != Owner)
                return;

            if (handle.Index < 0 ||
                handle.Index >= slots.Count)
            {
                return;
            }

            if (slots[handle.Index] == null)
                return;

            slots[handle.Index] = null;

            freeSlots.Push(
                handle.Index);
        }

        private Slot Rent(
            int prefabId,
            GameObject prefab)
        {
            if (!pools.TryGetValue(
                    prefabId,
                    out Stack<Slot> pool))
            {
                pool =
                    new Stack<Slot>();

                pools.Add(
                    prefabId,
                    pool);
            }

            while (pool.Count > 0)
            {
                Slot slot =
                    pool.Pop();

                if (IsSlotAlive(slot))
                    return slot;
            }

            return CreateSlot(
                prefabId,
                prefab);
        }

        private Slot CreateSlot(
            int prefabId,
            GameObject prefab)
        {
            GameObject instance =
                Instantiate(
                    prefab,
                    transform);

            return new Slot
            {
                Instance = instance,
                Transform = instance.transform,

                Views =
                    instance.GetComponentsInChildren
                        <IEntityView>(true),

                PrefabId = prefabId
            };
        }
    }
}