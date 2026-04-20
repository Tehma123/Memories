using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EncounterPayload
{
    [SerializeField] private string encounterId = string.Empty;
    [SerializeField] private List<string> enemyIds = new List<string>();
    [SerializeField] private int enemyLevel = 1;
    [SerializeField] private string spawnPattern = string.Empty;
    [SerializeField] private bool hasRngSeed;
    [SerializeField] private int rngSeed;

    public string EncounterId => encounterId;
    public IReadOnlyList<string> EnemyIds => enemyIds;
    public int EnemyLevel => enemyLevel;
    public string SpawnPattern => spawnPattern;
    public int RequestedEnemyCount => enemyIds != null ? enemyIds.Count : 0;
    public bool HasRngSeed => hasRngSeed;
    public int RngSeed => rngSeed;

    public static EncounterPayload Create(
        string encounterId,
        IEnumerable<string> enemyIds,
        int enemyLevel = 1,
        string spawnPattern = "",
        int? rngSeed = null)
    {
        EncounterPayload payload = new EncounterPayload
        {
            encounterId = (encounterId ?? string.Empty).Trim(),
            enemyLevel = Mathf.Max(1, enemyLevel),
            spawnPattern = (spawnPattern ?? string.Empty).Trim(),
            hasRngSeed = rngSeed.HasValue,
            rngSeed = rngSeed.GetValueOrDefault()
        };

        if (enemyIds != null)
        {
            foreach (string enemyId in enemyIds)
            {
                string normalizedId = (enemyId ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(normalizedId))
                {
                    payload.enemyIds.Add(normalizedId);
                }
            }
        }

        return payload;
    }

    public static EncounterPayload FromEnemyData(
        string encounterId,
        IEnumerable<EnemyData> enemies,
        int enemyLevel = 1,
        string spawnPattern = "",
        int? rngSeed = null)
    {
        List<string> ids = new List<string>();

        if (enemies != null)
        {
            foreach (EnemyData enemy in enemies)
            {
                string resolvedId = ResolveEnemyId(enemy);
                if (!string.IsNullOrWhiteSpace(resolvedId))
                {
                    ids.Add(resolvedId);
                }
            }
        }

        return Create(encounterId, ids, enemyLevel, spawnPattern, rngSeed);
    }

    public bool TryGetRngSeed(out int seed)
    {
        seed = rngSeed;
        return hasRngSeed;
    }

    public EncounterPayload Clone()
    {
        return Create(encounterId, enemyIds, enemyLevel, spawnPattern, hasRngSeed ? rngSeed : (int?)null);
    }

    private static string ResolveEnemyId(EnemyData enemyData)
    {
        if (enemyData == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(enemyData.enemyID))
        {
            return enemyData.enemyID.Trim();
        }

        return (enemyData.name ?? string.Empty).Trim();
    }
}