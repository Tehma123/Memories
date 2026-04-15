using System.Collections.Generic;
using UnityEngine;

public enum CardType
{
	Attack,
	Support,
	Skill,
	Control,
	Utility
}

public enum EffectType
{
	Damage,
	HealMemory,
	Buff,
	Debuff,
	ApplyStatus,
	Draw,
	Discard,
	Exile,
	ReturnFromDiscard
}

public enum TargetScope
{
	Single,
	AllEnemies,
	Self,
	Ally
}

[System.Serializable]
public class EffectData
{
	public EffectType effectType = EffectType.Damage;
	public int flatValue = 0;
	public float multiplier = 1f;
	public string statusId = string.Empty;
	public int durationTurns = 0;
	public TargetScope targetScope = TargetScope.Single;
	public string conditionKey = string.Empty;

	public int GetResolvedValue()
	{
		return Mathf.RoundToInt(flatValue * Mathf.Max(0f, multiplier));
	}
}

[CreateAssetMenu(fileName = "CardData", menuName = "Memories/Data/Card")]
public class CardData : ScriptableObject
{
	public string cardID = string.Empty;
	public string displayName = "New Card";
	public CardType type = CardType.Attack;

	[Header("Presentation")]
	public GameObject cardPrefabOverride;

	[Range(0, 100)]
	public int costPercentage = 0;

	public List<EffectData> effects = new List<EffectData>();

	[TextArea]
	public string flavorText = string.Empty;

	public Sprite sprite1Bit;

	public int GetMemoryCost(int maxMemory)
	{
		if (maxMemory <= 0 || costPercentage <= 0)
		{
			return 0;
		}

		return Mathf.CeilToInt(maxMemory * Mathf.Clamp(costPercentage, 0, 100) / 100f);
	}
}
