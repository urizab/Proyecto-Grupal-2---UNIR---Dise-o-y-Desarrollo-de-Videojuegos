using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    public float movementSpeed = 2;
    public float gravity = -9.8f;
    public Transform cameraTransform;

    // Settings de la camara
    public float sensitivity = 0.5f;
    public float minLimit = -80f;
    public float maxLimit = 80;

    // Altura de salto
    public float jumpHeight = 0.7f;

    private PlayerInputActions _inputActions;
    private CharacterController _characterController;

    private Vector2 _movement;
    private Vector2 _velocity;
    private Vector2 _look;

    private float _currentRotationY;

    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        //Quitar visibilidad cursor
        Cursor.visible = false;
        //Quitar visibilidad cursor
        Cursor.lockState = CursorLockMode.Locked;

        //Mapeo principal del control de Player
        _inputActions.Player.Enable();

        // MOVIMIENTOS DIRECCIONALES
        _inputActions.Player.Move.performed += SetMovement;
        _inputActions.Player.Move.canceled += obj => _movement = Vector2.zero;

        // CAMARA
        _inputActions.Player.Look.performed += SetLook;
        _inputActions.Player.Look.canceled += obj => _look = Vector2.zero;

        //SALTO
        _inputActions.Player.Jump.performed += Jump;
    }

    private void SetMovement(InputAction.CallbackContext obj)
    {
        _movement = obj.ReadValue<Vector2>();
    }
    private void SetLook(InputAction.CallbackContext obj)
    {
        _look = obj.ReadValue<Vector2>();
    }

    private void Update()
    {
        Movement();
        Look();
    }

    private void Movement()
    {
        Vector3 move = transform.right * _movement.x + transform.forward * _movement.y;
        _characterController.Move(move * movementSpeed * Time.deltaTime);

        // Reset de gravedad
        if (_characterController.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void Look()
    {
        Vector2 mouseNormalized = _look * sensitivity;

        _currentRotationY = Mathf.Clamp(_currentRotationY - mouseNormalized.y, minLimit, maxLimit);

        cameraTransform.localRotation = Quaternion.Euler(_currentRotationY, 0, 0);
        transform.Rotate(Vector3.up * _look.x);
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        if (_characterController.isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}
