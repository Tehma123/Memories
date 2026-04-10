using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float speed = 2f;
    private Vector2 movement;
    public CameraSmoothFollow cameraScript;
    private IInteractable currentTarget;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
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
        if (cameraScript != null)
        {
            cameraScript.UpdateCameraDirection(inputValue.Get<Vector2>().x);
        }
        movement = inputValue.Get<Vector2>();
    }
    public void OnInteract(InputValue value)
    {
        Debug.Log("OnInteract called. isPressed=" + value.isPressed);
        if (value.isPressed && currentTarget != null)
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