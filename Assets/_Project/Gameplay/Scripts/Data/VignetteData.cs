using UnityEngine;

public enum VignetteType
{
	Newspaper,
	Scratch,
	Terminal
}

[System.Flags]
public enum VignetteEffect
{
	None = 0,
	Typewriter = 1 << 0,
	Glitch = 1 << 1,
	Static = 1 << 2,
	Scroll = 1 << 3,
	Shake = 1 << 4,
	Invert = 1 << 5
}

[CreateAssetMenu(fileName = "VignetteData", menuName = "Memories/Data/Vignette")]
public class VignetteData : ScriptableObject
{
	public string vignetteID = string.Empty;
	public VignetteType vignetteType = VignetteType.Newspaper;
	public string[] textLines = new string[0];
	public VignetteEffect displayEffects = VignetteEffect.Typewriter;
	public AudioClip audioCue;
	public string revealMemoryFragment = string.Empty;
	public bool replayable = true;

	public bool HasEffect(VignetteEffect effect)
	{
		return (displayEffects & effect) != 0;
	}
}
