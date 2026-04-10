using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class PortraitEntry
{
    public string portraitId = string.Empty;
    public Sprite portrait;
}

public class PortraitManager : MonoBehaviour
{
    [SerializeField] private Sprite defaultPortrait;
    [SerializeField] private List<PortraitEntry> portraits = new List<PortraitEntry>();

    private readonly Dictionary<string, Sprite> _portraitLookup = new Dictionary<string, Sprite>();
    private bool _lookupDirty = true;

    private void Awake()
    {
        RebuildLookup();
    }

    private void OnValidate()
    {
        _lookupDirty = true;
    }

    public Sprite GetPortrait(string portraitId)
    {
        if (_lookupDirty)
        {
            RebuildLookup();
        }

        if (!string.IsNullOrWhiteSpace(portraitId) && _portraitLookup.TryGetValue(portraitId, out Sprite portrait))
        {
            return portrait;
        }

        return defaultPortrait;
    }

    public bool TryGetPortrait(string portraitId, out Sprite portrait)
    {
        portrait = GetPortrait(portraitId);
        return portrait != null;
    }

    public void ApplyPortrait(Image targetImage, string portraitId)
    {
        if (targetImage == null)
        {
            return;
        }

        Sprite portrait = GetPortrait(portraitId);
        targetImage.sprite = portrait;
        targetImage.enabled = portrait != null;
    }

    private void RebuildLookup()
    {
        _lookupDirty = false;
        _portraitLookup.Clear();

        if (portraits == null)
        {
            return;
        }

        for (int i = 0; i < portraits.Count; i++)
        {
            PortraitEntry entry = portraits[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.portraitId) || entry.portrait == null)
            {
                continue;
            }

            string normalizedId = entry.portraitId.Trim();
            _portraitLookup[normalizedId] = entry.portrait;
        }
    }
}