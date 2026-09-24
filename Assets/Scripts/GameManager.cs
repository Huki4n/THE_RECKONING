using System.Collections.Generic;
using UnityEngine;
using TheReckoning.Morale;
using UnityEngine.SceneManagement;

namespace TheReckoning
{
    public enum MatchState
    {
        Preparing,
        Running,
        Finished
    }

    [DefaultExecutionOrder(-200)]
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private MoraleSystem moraleSystem;
        [SerializeField] private AbilitySystem abilitySystem;
        public AbilitySystem Abilities => abilitySystem;
        public MoraleSystem Morale => moraleSystem;

        [SerializeField] private Base leftBase;
        [SerializeField] private Base rightBase;

        [SerializeField] private Economy leftEconomy;

        [SerializeField] private UnitSpawner leftSpawner;
        [SerializeField] private UnitSpawner rightSpawner;

        private readonly List<Combatant> combatants =
            new List<Combatant>();

        public MatchState State
        {
            get; private set;
        }
            = MatchState.Preparing;

        public bool IsRunning => State == MatchState.Running;
        public string Result { get; private set; } = "";

        public Base LeftBase => leftBase;
        public Base RightBase => rightBase;

        public Economy LeftEconomy => leftEconomy;

        public UnitSpawner LeftSpawner => leftSpawner;
        public UnitSpawner RightSpawner => rightSpawner;

        private void Start()
        {
            if (leftBase == null || rightBase == null ||
                leftEconomy == null ||
                leftSpawner == null || rightSpawner == null)
            {
                Debug.LogError(
                    "GameManager: assign both bases, both spawners " +
                    "and the player economy.",
                    this);

                enabled = false;
                return;
            }

            if (moraleSystem == null)
                moraleSystem = GetComponent<MoraleSystem>();
            if (moraleSystem == null)
            {
                Debug.LogError("GameManager: add MoraleSystem and assign its reference.", this);
                enabled = false;
                return;
            }
            leftEconomy.Initialize(this);

            leftBase.Initialize(this, Team.Left);
            rightBase.Initialize(this, Team.Right);

            leftSpawner.Initialize(this, Team.Left, leftEconomy);

            // Противнику кошелёк не передаём.
            rightSpawner.Initialize(this, Team.Right);

            try
            {
                moraleSystem.Initialize(this);
            }
            catch (System.ArgumentException error)
            {
                Debug.LogError("GameManager: invalid morale settings. " + error.Message, this);
                enabled = false;
                return;
            }
            if (abilitySystem == null) abilitySystem = GetComponent<AbilitySystem>();
            if (abilitySystem == null)
            {
                Debug.LogError("GameManager: add AbilitySystem.", this);
                enabled = false;
                return;
            }
            abilitySystem.Initialize(this);
            // Preparing until the player chooses one ability.
        }

        public void BeginMatch()
        {
            if (enabled && State == MatchState.Preparing &&
                abilitySystem != null && abilitySystem.Selected != OrderAbility.None)
                State = MatchState.Running;
        }

        private void Update()
        {
            if (IsRunning)
                moraleSystem.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            if (State == MatchState.Running)
            {
                State = MatchState.Finished;
                Result = "Match stopped";
            }
            moraleSystem?.Stop();
        }

        private void OnDestroy()
        {
            moraleSystem?.Shutdown();
        }

        private void LateUpdate()
        {
            if (!IsRunning)
                return;

            bool leftDead = !leftBase.IsAlive;
            bool rightDead = !rightBase.IsAlive;

            if (!leftDead && !rightDead)
                return;

            State = MatchState.Finished;
            moraleSystem.Stop();

            Result = leftDead && rightDead
                ? "Draw"
                : rightDead ? "Victory" : "Defeat";
        }

        public void GrantKillReward(Team attacker, int amount)
        {
            if (!IsRunning || attacker != Team.Left)
                return;

            leftEconomy.Add(amount);
        }

        public void Register(Combatant combatant)
        {
            if (combatant != null && !combatants.Contains(combatant))
            {
                combatants.Add(combatant);
                if (combatant is Unit unit)
                    moraleSystem?.Register(unit);
            }
        }

        public void Unregister(Combatant combatant)
        {
            if (combatants.Remove(combatant) && combatant is Unit unit)
                moraleSystem?.Unregister(unit);
        }

        public Combatant FindEnemyAhead(Combatant seeker)
        {
            Combatant nearest = null;
            float nearestDistance = float.PositiveInfinity;

            float direction = seeker.Side == Team.Left ? 1f : -1f;

            foreach (Combatant candidate in combatants)
            {
                if (candidate == null || !candidate.IsAlive)
                    continue;

                if (candidate.Side == seeker.Side)
                    continue;

                float distance = (
                    candidate.transform.position.x
                    - seeker.transform.position.x) * direction;

                if (distance < 0f || distance >= nearestDistance)
                    continue;

                nearest = candidate;
                nearestDistance = distance;
            }

            return nearest;
        }

        public float GetFreeDistanceAhead(Combatant mover)
        {
            float freeDistance = float.PositiveInfinity;
            float direction = mover.Side == Team.Left ? 1f : -1f;

            foreach (Combatant other in combatants)
            {
                if (other == null || other == mover || !other.IsAlive)
                    continue;

                // Своя база не препятствует движению.
                if (other is Base && other.Side == mover.Side)
                    continue;

                float distance = (
                    other.transform.position.x
                    - mover.transform.position.x) * direction;

                if (distance < 0f)
                    continue;

                float gap = distance
                    - mover.BodyRadius
                    - other.BodyRadius;

                freeDistance = Mathf.Min(
                    freeDistance,
                    Mathf.Max(0f, gap));
            }

            return freeDistance;
        }

        public bool IsSpawnSpaceFree(float x, float radius, Team side)
        {
            foreach (Combatant other in combatants)
            {
                if (other == null || !other.IsAlive)
                    continue;

                // Разрешаем появление внутри своей базы.
                if (other is Base && other.Side == side)
                    continue;

                float distance = Mathf.Abs(
                    other.transform.position.x - x);

                if (distance < radius + other.BodyRadius + 0.05f)
                    return false;
            }

            return true;
        }

        public void CopyUnits(Team side, List<Unit> destination)
        {
            destination.Clear();
            foreach (Combatant actor in combatants)
                if (actor is Unit unit && unit.IsAlive && unit.Side == side)
                    destination.Add(unit);
        }

        public int CountUnits(Team side)
        {
            int count = 0;

            foreach (Combatant combatant in combatants)
            {
                if (combatant is Unit &&
                    combatant.IsAlive &&
                    combatant.Side == side)
                {
                    count++;
                }
            }

            return count;
        }

        public void Restart()
        {
            int index = SceneManager.GetActiveScene().buildIndex;

            if (index < 0)
            {
                Debug.LogError(
                    "Add the current scene to the build scene list.",
                    this);

                return;
            }

            SceneManager.LoadScene(index);
        }
    }
}
