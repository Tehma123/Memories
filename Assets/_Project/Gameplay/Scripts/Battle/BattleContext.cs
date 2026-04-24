using System.Collections.Generic;
using UnityEngine;

public class BattleContext
{
	private readonly List<EnemyController> _enemies = new List<EnemyController>();

	public BattleManager BattleManager { get; }
	public DeckManager DeckManager { get; }
	public StateManager StateManager { get; }
	public MemoryManager MemoryManager { get; }
	public GameObject PlayerObject { get; }
	public System.Random Random { get; private set; }

	public int TurnNumber { get; set; }
	public bool IsPlayerTurn { get; set; }

	public IReadOnlyList<EnemyController> Enemies => _enemies;

	public BattleContext(
		BattleManager battleManager,
		DeckManager deckManager,
		StateManager stateManager,
		MemoryManager memoryManager,
		GameObject playerObject,
		int? randomSeed = null)
	{
		BattleManager = battleManager;
		DeckManager = deckManager;
		StateManager = stateManager;
		MemoryManager = memoryManager;
		PlayerObject = playerObject;
		Random = randomSeed.HasValue ? new System.Random(randomSeed.Value) : new System.Random();
	}

	public void SetEnemies(IEnumerable<EnemyController> enemies)
	{
		_enemies.Clear();
		if (enemies == null)
		{
			return;
		}

		foreach (EnemyController enemy in enemies)
		{
			if (enemy != null)
			{
				_enemies.Add(enemy);
			}
		}
	}

	public IReadOnlyList<EnemyController> GetAliveEnemies()
	{
		List<EnemyController> aliveEnemies = new List<EnemyController>();
		for (int i = 0; i < _enemies.Count; i++)
		{
			EnemyController enemy = _enemies[i];
			if (enemy != null && enemy.IsAlive)
			{
				aliveEnemies.Add(enemy);
			}
		}

		return aliveEnemies;
	}

	public EnemyController GetPrimaryAliveEnemy()
	{
		for (int i = 0; i < _enemies.Count; i++)
		{
			EnemyController enemy = _enemies[i];
			if (enemy != null && enemy.IsAlive)
			{
				return enemy;
			}
		}

		return null;
	}
}
