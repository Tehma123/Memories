using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 1.5f;
    [SerializeField] private LayerMask interactionLayers = ~0;

    private readonly List<IInteractable> _targetsInRange = new List<IInteractable>();
    private bool _isInteractionEnabled = true;

    public void Initialize()
    {
        _targetsInRange.Clear();
        _isInteractionEnabled = true;
    }

    private void Awake()
    {
        Initialize();
    }

    public void OnInteract(InputValue inputValue)
    {
        if (!_isInteractionEnabled || inputValue == null || !inputValue.isPressed)
        {
            return;
        }

        TryInteract();
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
        _isInteractionEnabled = isEnabled;

        if (!_isInteractionEnabled)
        {
            _targetsInRange.Clear();
        }
    }

    public void TryInteract()
    {
        if (!_isInteractionEnabled)
        {
            return;
        }

        IInteractable target = GetNearestKnownTarget();
        if (target == null)
        {
            target = FindNearestByOverlap();
        }

        target?.Interact();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null || _targetsInRange.Contains(interactable))
        {
            return;
        }

        _targetsInRange.Add(interactable);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null)
        {
            return;
        }

        _targetsInRange.Remove(interactable);
    }

    private IInteractable GetNearestKnownTarget()
    {
        float bestDistance = float.MaxValue;
        IInteractable bestTarget = null;
        Vector3 origin = transform.position;

        for (int i = _targetsInRange.Count - 1; i >= 0; i--)
        {
            IInteractable candidate = _targetsInRange[i];
            Component component = candidate as Component;

            if (candidate == null || component == null)
            {
                _targetsInRange.RemoveAt(i);
                continue;
            }

            float sqrDistance = (component.transform.position - origin).sqrMagnitude;
            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestTarget = candidate;
            }
        }

        return bestTarget;
    }

    private IInteractable FindNearestByOverlap()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Mathf.Max(0.1f, interactionRange), interactionLayers);
        if (hits == null || hits.Length == 0)
        {
            return null;
        }

        float bestDistance = float.MaxValue;
        IInteractable bestTarget = null;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hit == null)
            {
                continue;
            }

            IInteractable interactable = hit.GetComponent<IInteractable>();
            if (interactable == null)
            {
                continue;
            }

            float sqrDistance = (hit.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance < bestDistance)
            {
                bestDistance = sqrDistance;
                bestTarget = interactable;
            }
        }

        return bestTarget;
    }
}
