using System.Collections;
using TheReckoning.ECS;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
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
        [SerializeField] private AbilitySystem abilitySystem;
        public AbilitySystem Abilities => abilitySystem;

        [SerializeField] private Economy leftEconomy;

        [SerializeField] private UnitSpawner leftSpawner;

        [Header("Campaign")]
        [SerializeField] private EraCatalog eraCatalog;
        [SerializeField] private string mainMenuSceneName;

        private bool restartRequested;
        private Entity matchRunningEntity;

        private World ecsWorld;
        private EntityQuery killRewardQuery;
        private EntityQuery deadBaseQuery;

        public MatchState State
        {
            get; private set;
        }
            = MatchState.Preparing;

        public bool IsRunning => State == MatchState.Running;
        public string Result { get; private set; } = "";

        public Economy LeftEconomy => leftEconomy;

        public UnitSpawner LeftSpawner => leftSpawner;

        public int EraIndex { get; private set; }

        public EraData Era { get; private set; }

        public bool NewEraUnlocked { get; private set; }
        public bool CampaignComplete { get; private set; }

        private int EraCount => eraCatalog != null ? eraCatalog.Count : 0;

        private void Awake()
        {
            EraIndex = EraCount > 0
                ? Mathf.Clamp(CampaignProgress.SelectedEra, 0, EraCount - 1)
                : 0;

            Era = eraCatalog != null ? eraCatalog.Get(EraIndex) : null;
        }

        private void Start()
        {
            if (leftEconomy == null || leftSpawner == null)
            {
                Debug.LogError(
                    "GameManager: assign the player spawner " +
                    "and the player economy.",
                    this);

                enabled = false;
                return;
            }

            leftEconomy.Initialize(this);

            leftSpawner.Initialize(this, leftEconomy);

            if (abilitySystem == null) abilitySystem = GetComponent<AbilitySystem>();
            if (abilitySystem == null)
            {
                Debug.LogError("GameManager: add AbilitySystem.", this);
                enabled = false;
                return;
            }
            abilitySystem.Initialize(this);
        }

        public void BeginMatch()
        {
            if (enabled && State == MatchState.Preparing &&
                abilitySystem != null && abilitySystem.Selected != OrderAbility.None)
            {
                State = MatchState.Running;
                SetEcsMatchRunning(true);
            }
        }

        private void OnDisable()
        {
            if (State == MatchState.Running)
            {
                State = MatchState.Finished;
                Result = "Match stopped";
            }

            SetEcsMatchRunning(false);
        }

        private void Update()
        {
            if (!IsRunning || !TryPrepareEcsQueries())
                return;

            CollectKillRewards();
            CheckBaseDestroyed();
        }

        private bool TryPrepareEcsQueries()
        {
            World world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
                return false;

            if (ecsWorld != world)
            {
                EntityManager entityManager = world.EntityManager;

                killRewardQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadWrite<PendingKillReward>());

                deadBaseQuery = entityManager.CreateEntityQuery(
                    ComponentType.ReadOnly<BaseTag>(),
                    ComponentType.ReadOnly<TeamId>(),
                    ComponentType.ReadOnly<Dead>());

                ecsWorld = world;
            }

            return true;
        }

        private void CollectKillRewards()
        {
            if (!killRewardQuery.TryGetSingleton(out PendingKillReward reward) ||
                reward.Gold <= 0)
            {
                return;
            }

            killRewardQuery.SetSingleton(new PendingKillReward());
            leftEconomy.Add(reward.Gold);
        }

        private void CheckBaseDestroyed()
        {
            if (deadBaseQuery.IsEmpty)
                return;

            using NativeArray<TeamId> teams =
                deadBaseQuery.ToComponentDataArray<TeamId>(Allocator.Temp);

            FinishMatch(teams[0].Value == 0 ? Team.Left : Team.Right);
        }

        private void FinishMatch(Team destroyedSide)
        {
            if (!IsRunning)
                return;

            State = MatchState.Finished;
            SetEcsMatchRunning(false);

            Result =
                destroyedSide == Team.Right
                    ? "Victory"
                    : "Defeat";

            if (destroyedSide == Team.Right && EraCount > 0)
            {
                NewEraUnlocked = CampaignProgress.UnlockNext(EraIndex, EraCount);
                CampaignComplete = EraIndex == EraCount - 1;

                if (CampaignComplete)
                    Result += "\nCampaign complete";
                else if (NewEraUnlocked)
                    Result += $"\n{eraCatalog.Get(EraIndex + 1)?.DisplayName} unlocked";
            }

            Debug.Log(
                $"ECS base destroyed: {destroyedSide}. " +
                $"Result: {Result}",
                this);
        }

        private void SetEcsMatchRunning(bool running)
        {
            World world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
            {
                matchRunningEntity = Entity.Null;
                return;
            }

            EntityManager entityManager = world.EntityManager;
            bool exists = entityManager.Exists(matchRunningEntity);

            if (running)
            {
                if (exists)
                    return;

                matchRunningEntity = entityManager.CreateEntity(typeof(MatchRunning));

#if UNITY_EDITOR
                entityManager.SetName(matchRunningEntity, "Match Running");
#endif
                return;
            }

            if (exists)
                entityManager.DestroyEntity(matchRunningEntity);

            matchRunningEntity = Entity.Null;
        }

        public void ReturnToMainMenu()
        {
            if (restartRequested)
                return;

            if (string.IsNullOrWhiteSpace(mainMenuSceneName) ||
                !Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
            {
                Debug.LogError(
                    "GameManager: assign Main Menu Scene Name and add that scene to the build list.",
                    this);

                return;
            }

            restartRequested = true;
            StartCoroutine(LoadAfterCurrentFrame(mainMenuSceneName));
        }

        private IEnumerator LoadAfterCurrentFrame(string sceneName)
        {
            yield return null;
            SceneManager.LoadScene(sceneName);
        }

        public void Restart()
        {
            if (restartRequested)
                return;

            int index = SceneManager.GetActiveScene().buildIndex;

            if (index < 0)
            {
                Debug.LogError(
                    "Add the current scene to the build scene list.",
                    this);

                return;
            }

            restartRequested = true;
            StartCoroutine(RestartAfterCurrentFrame(index));
        }

        private IEnumerator RestartAfterCurrentFrame(int sceneIndex)
        {
            yield return null;

            AsyncOperation load = SceneManager.LoadSceneAsync(
                sceneIndex, LoadSceneMode.Single);

            if (load == null)
            {
                restartRequested = false;
                Debug.LogError("GameManager: scene reload could not start.", this);
            }
        }
    }
}
