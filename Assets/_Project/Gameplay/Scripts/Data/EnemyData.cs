using System.Collections.Generic;
using UnityEngine;

public enum EnemyMoveType
{
	Attack,
	AttackMemory,
	HealSelf,
	ApplyStateToPlayer,
	BuffSelf
}

[System.Serializable]
public class EnemyMoveData
{
	public string moveID = string.Empty;
	public string displayName = "Enemy Move";
	public EnemyMoveType moveType = EnemyMoveType.AttackMemory;
	public int minValue = 1;
	public int maxValue = 3;
	public string statusId = string.Empty;
	public int durationTurns = 1;

	public int GetRoll(System.Random random)
	{
		int min = Mathf.Min(minValue, maxValue);
		int max = Mathf.Max(minValue, maxValue);

		if (random == null)
		{
			return Random.Range(min, max + 1);
		}

		return random.Next(min, max + 1);
	}
}

[CreateAssetMenu(fileName = "EnemyData", menuName = "Memories/Data/Enemy")]
public class EnemyData : ScriptableObject
{
	public string enemyID = string.Empty;
	public string displayName = "Enemy";

	[Header("Combat Visual")]
	public Sprite enemySprite;

	[Min(1)]
	public int maxHealth = 20;

	[Range(0, 100)]
	public int aggressiveness = 50;

	public List<EnemyMoveData> moves = new List<EnemyMoveData>();

	public EnemyMoveData GetRandomMove(System.Random random)
	{
		if (moves == null || moves.Count == 0)
		{
			return null;
		}

		if (random == null)
		{
			int fallbackIndex = Random.Range(0, moves.Count);
			return moves[fallbackIndex];
		}

		return moves[random.Next(0, moves.Count)];
	}
}
