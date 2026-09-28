using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace TheReckoning
{
    public sealed class BattleUI : MonoBehaviour
    {
        [Serializable]
        private sealed class HireButton
        {
            public Button button;
            public UnitData unit;
            [NonSerialized] public UnityAction callback;
        }

        [Serializable]
        private sealed class TurretButton
        {
            public Button button;
            public TurretData turret;
            public TurretSlot slot;
            [NonSerialized] public UnityAction callback;
        }

        [SerializeField] private GameManager match;
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private HireButton[] hireButtons;
        [SerializeField] private TurretButton[] turretButtons;
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Tooltip("Optional; shows the current era name.")]
        [SerializeField] private TMP_Text eraText;

        private void Start()
        {
            if (match == null)
            {
                Debug.LogError("BattleUI: assign GameManager.", this);
                enabled = false;
                return;
            }

            ApplyEra(match.Era);

            if (hireButtons != null)
            {
                foreach (HireButton entry in hireButtons)
                {
                    if (entry == null || entry.button == null || entry.unit == null) continue;
                    UnitData selectedUnit = entry.unit;
                    entry.callback = () => match.LeftSpawner.TrySpawn(selectedUnit);
                    entry.button.onClick.AddListener(entry.callback);
                }
            }

            if (turretButtons != null)
            {
                foreach (TurretButton entry in turretButtons)
                {
                    if (entry == null || entry.button == null) continue;
                    entry.button.interactable = false;
                    if (entry.turret == null || entry.slot == null) continue;

                    TurretData selectedTurret = entry.turret;
                    TurretSlot selectedSlot = entry.slot;
                    entry.callback = () => selectedSlot.TryBuy(selectedTurret);
                    entry.button.onClick.AddListener(entry.callback);
                }
            }

            if (restartButton != null) restartButton.onClick.AddListener(match.Restart);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(match.ReturnToMainMenu);
            if (eraText != null) eraText.text = match.Era != null ? match.Era.DisplayName : "";
        }

        private void ApplyEra(EraData era)
        {
            if (era == null)
                return;

            if (hireButtons != null)
            {
                for (int i = 0; i < hireButtons.Length; i++)
                {
                    HireButton entry = hireButtons[i];
                    if (entry == null || entry.button == null) continue;

                    entry.unit = era.GetUnit(i);
                    entry.button.gameObject.SetActive(entry.unit != null);

                    UnitCardView card = entry.button.GetComponentInChildren<UnitCardView>(true);
                    if (card != null && entry.unit != null) card.SetUnit(entry.unit, card.Portrait);
                }
            }

            if (turretButtons != null)
            {
                for (int i = 0; i < turretButtons.Length; i++)
                {
                    TurretButton entry = turretButtons[i];
                    if (entry == null || entry.button == null) continue;

                    entry.turret = era.GetTurret(i);
                    entry.button.gameObject.SetActive(entry.turret != null);

                    UnitCardView card = entry.button.GetComponentInChildren<UnitCardView>(true);
                    if (card != null && entry.turret != null) card.SetTurret(entry.turret);
                }
            }
        }

        private void Update()
        {
            if (match == null || match.State == MatchState.Preparing) return;

            if (goldText != null) goldText.text = $"{match.LeftEconomy.Gold}";

            if (hireButtons != null)
                foreach (HireButton entry in hireButtons)
                    if (entry != null && entry.button != null)
                        entry.button.interactable = match.LeftSpawner.CanSpawn(entry.unit);

            if (turretButtons != null)
                foreach (TurretButton entry in turretButtons)
                    if (entry != null && entry.button != null)
                        entry.button.interactable = entry.slot != null && entry.slot.CanBuy(entry.turret);

            bool finished = match.State == MatchState.Finished;
            if (resultPanel != null && resultPanel.activeSelf != finished)
                resultPanel.SetActive(finished);
            if (resultText != null && finished) resultText.text = match.Result;
        }

        private void OnDestroy()
        {
            if (hireButtons != null)
                foreach (HireButton entry in hireButtons)
                    if (entry?.button != null && entry.callback != null)
                        entry.button.onClick.RemoveListener(entry.callback);

            if (turretButtons != null)
                foreach (TurretButton entry in turretButtons)
                    if (entry?.button != null && entry.callback != null)
                        entry.button.onClick.RemoveListener(entry.callback);

            if (restartButton != null && match != null)
                restartButton.onClick.RemoveListener(match.Restart);

            if (mainMenuButton != null && match != null)
                mainMenuButton.onClick.RemoveListener(match.ReturnToMainMenu);
        }
    }
}
