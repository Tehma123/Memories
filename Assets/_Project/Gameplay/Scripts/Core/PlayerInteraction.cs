using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactionRange = 1.5f;

    public void Initialize()
    {
    }

    public void OnInteract(InputValue inputValue)
    {
    }

    public void SetInteractionEnabled(bool isEnabled)
    {
    }

    public void TryInteract()
    {
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
    }

    private void OnTriggerExit2D(Collider2D other)
    {
    }
}
