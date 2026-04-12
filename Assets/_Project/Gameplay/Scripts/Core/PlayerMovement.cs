using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private PlayerInteraction _playerInteraction;
    [SerializeField] private float speed = 2f;
    private Vector2 movement;
    private bool _isMovementEnabled = true;
    private bool _wasInteractPressed;
    public CameraSmoothFollow cameraScript;
    private IInteractable currentTarget;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        _playerInteraction = GetComponent<PlayerInteraction>();
    }

    private void FixedUpdate()
    {
        if (!_isMovementEnabled)
        {
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }

            return;
        }

        rb.linearVelocity = movement * speed; 

        if (movement.x > 0f)
        {
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
        else if (movement.x < 0f)
        {
            transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    private void OnMove(InputValue inputValue)
    {
        if (!_isMovementEnabled)
        {
            movement = Vector2.zero;
            return;
        }

        if (cameraScript != null)
        {
            cameraScript.UpdateCameraDirection(inputValue.Get<Vector2>().x);
        }
        movement = inputValue.Get<Vector2>();
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        _isMovementEnabled = isEnabled;

        if (_isMovementEnabled)
        {
            return;
        }

        movement = Vector2.zero;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void OnInteract(InputValue value)
    {
        if (_playerInteraction != null && _playerInteraction.enabled)
        {
            return;
        }

        if (!_isMovementEnabled || value == null)
        {
            _wasInteractPressed = false;
            return;
        }

        bool isPressed = value.isPressed;
        if (!isPressed)
        {
            _wasInteractPressed = false;
            return;
        }

        if (_wasInteractPressed)
        {
            return;
        }

        _wasInteractPressed = true;

        if (currentTarget != null)
        {
            // Chỉ cần gọi Interact(), nó tự chạy code của Shop hoặc Fragment tùy đối tượng
            currentTarget.Interact();
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Kiểm tra xem đối tượng va chạm có thực thi IInteractable không
        IInteractable interactable = collision.GetComponent<IInteractable>();
        
        if (interactable != null)
        {
            currentTarget = interactable;
            Debug.Log("Nhấn F để tương tác");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<IInteractable>() != null)
        {
            currentTarget = null;
        }
    }
}