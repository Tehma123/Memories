using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;

    private Rigidbody2D _rigidbody2D;
    private Vector2 _moveInput;
    private bool _isMovementEnabled = true;

    public Vector2 MoveInput => _moveInput;
    public Vector2 LookDirection { get; private set; } = Vector2.down;

    public void Initialize()
    {
        if (_rigidbody2D == null)
        {
            _rigidbody2D = GetComponent<Rigidbody2D>();
        }

        _moveInput = Vector2.zero;
        _isMovementEnabled = true;
    }

    private void Awake()
    {
        Initialize();
    }

    public void OnMove(InputValue inputValue)
    {
        if (!_isMovementEnabled || inputValue == null)
        {
            _moveInput = Vector2.zero;
            return;
        }

        _moveInput = inputValue.Get<Vector2>();
        if (_moveInput.sqrMagnitude > 0.0001f)
        {
            LookDirection = _moveInput.normalized;
        }
    }

    public void SetMovementEnabled(bool isEnabled)
    {
        _isMovementEnabled = isEnabled;

        if (_isMovementEnabled)
        {
            return;
        }

        _moveInput = Vector2.zero;

        if (_rigidbody2D != null)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
        }
    }

    private void FixedUpdate()
    {
        if (_rigidbody2D == null)
        {
            return;
        }

        if (!_isMovementEnabled)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            return;
        }

        _rigidbody2D.linearVelocity = _moveInput * moveSpeed;
    }
}
