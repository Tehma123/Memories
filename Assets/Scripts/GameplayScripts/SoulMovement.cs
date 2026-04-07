using UnityEngine;
using UnityEngine.InputSystem;

public class SoulMovement : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    [SerializeField] private float speed = 2f;
    private Vector2 movement;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movement * speed; 

        if (movement.x > 0) sr.flipX = false;
        else if (movement.x < 0) sr.flipX = true;
    }

    private void OnMove(InputValue inputValue)
    {
        movement = inputValue.Get<Vector2>();
    }
}