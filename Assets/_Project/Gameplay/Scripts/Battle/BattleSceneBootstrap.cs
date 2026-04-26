using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public class BattleSceneBootstrap : MonoBehaviour
{
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private EnemyData[] enemyCatalog = Array.Empty<EnemyData>();
    [SerializeField] private bool logResolutionDetails = true;

    private void Reset()
    {
        AutoAssignReferencesInEditor();
    }

    private void OnValidate()
    {
        AutoAssignReferencesInEditor();
    }

    private void Awake()
    {
        if (battleManager == null)
        {
            Debug.LogWarning($"{nameof(BattleSceneBootstrap)} on '{name}' is missing {nameof(battleManager)} reference.");
            return;
        }

        if (!SceneTransitionContext.TryConsumeEncounterPayloadForActiveScene(SceneManager.GetActiveScene().name, out EncounterPayload payload))
        {
            return;
        }

        EnemyData[] resolvedEnemies = ResolveEnemyData(payload);

        if (resolvedEnemies.Length == 0)
        {
            string encounterId = string.IsNullOrWhiteSpace(payload.EncounterId) ? "unknown-encounter" : payload.EncounterId;
            Debug.LogWarning($"{nameof(BattleSceneBootstrap)} payload for '{encounterId}' resolved no enemies. Falling back to BattleManager defaults.");
            battleManager.ConfigureRuntimeEncounter(payload, Array.Empty<EnemyData>());
            return;
        }

        battleManager.ConfigureRuntimeEncounter(payload, resolvedEnemies);

        if (logResolutionDetails)
        {
            Debug.Log(
                $"BattleSceneBootstrap resolved encounter '{payload.EncounterId}' " +
                $"from {payload.RequestedEnemyCount} source enemies to {resolvedEnemies.Length} active combat enemies.");
        }
    }

    private void AutoAssignReferencesInEditor()
    {
        if (Application.isPlaying)
        {
            return;
        }

        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>(FindObjectsInactive.Include);
        }
    }

    private EnemyData[] ResolveEnemyData(EncounterPayload payload)
    {
        if (payload == null || payload.EnemyIds == null || payload.EnemyIds.Count == 0)
        {
            return Array.Empty<EnemyData>();
        }

        Dictionary<string, EnemyData> lookup = BuildEnemyLookup();
        List<EnemyData> resolved = new List<EnemyData>(CombatConfig.MaxEnemySlots);

        for (int i = 0; i < payload.EnemyIds.Count && resolved.Count < CombatConfig.MaxEnemySlots; i++)
        {
            string enemyId = payload.EnemyIds[i];
            if (string.IsNullOrWhiteSpace(enemyId))
            {
                continue;
            }

            string normalizedId = enemyId.Trim();
            if (!lookup.TryGetValue(normalizedId, out EnemyData enemyData) || enemyData == null)
            {
                Debug.LogWarning($"BattleSceneBootstrap could not resolve EnemyData for enemy id '{normalizedId}'.");
                continue;
            }

            resolved.Add(enemyData);
        }

        return resolved.ToArray();
    }

    private Dictionary<string, EnemyData> BuildEnemyLookup()
    {
        Dictionary<string, EnemyData> lookup = new Dictionary<string, EnemyData>(StringComparer.OrdinalIgnoreCase);

        RegisterEnemies(enemyCatalog, lookup);
        RegisterEnemies(battleManager.GetConfiguredEncounterEnemiesSnapshot(), lookup);

        return lookup;
    }

    private static void RegisterEnemies(IReadOnlyList<EnemyData> source, IDictionary<string, EnemyData> lookup)
    {
        if (source == null || lookup == null)
        {
            return;
        }

        for (int i = 0; i < source.Count; i++)
        {
            EnemyData enemy = source[i];
            if (enemy == null)
            {
                continue;
            }

            string keyFromId = (enemy.enemyID ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(keyFromId))
            {
                lookup[keyFromId] = enemy;
            }

            string keyFromName = (enemy.name ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(keyFromName) && !lookup.ContainsKey(keyFromName))
            {
                lookup[keyFromName] = enemy;
            }
        }
    }
}