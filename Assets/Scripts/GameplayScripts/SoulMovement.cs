using UnityEngine;
using UnityEngine.InputSystem;

public class SoulMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    [SerializeField] private float speed = 2f;
    private Vector2 movement;
    public CameraSmoothFollow cameraScript;
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
}