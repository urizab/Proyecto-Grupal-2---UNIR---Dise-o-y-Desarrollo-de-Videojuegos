using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    // ── MOVIMIENTO
    public float movementSpeed = 2;
    public float gravity = -9.8f;
    public float jumpHeight = 0.7f;

    // ── CÁMARA
    public Transform cameraTransform;
    public float sensitivity = 0.5f;
    public float minLimit = -80f;
    public float maxLimit = 80;

    // ── DOBLE SALTO
    [Header("Doble Salto")]
    [Tooltip("Agua mínima en el tanque para poder hacer el doble salto.")]
    public float waterCostForDoubleJump = 100f;
    [Tooltip("Agua que se consume al hacer el doble salto.")]
    public float waterDrainOnDoubleJump = 100f;

    // ── REFERENCIAS PRIVADAS
    private PlayerInputActions _inputActions;
    private CharacterController _characterController;
    private WaterTank _waterTank;

    // ── VARIABLES DE ESTADO
    private Vector2 _movement;
    private Vector2 _velocity;
    private Vector2 _look;
    private float _currentRotationY;

    // ── ESTADO DEL SALTO
    private bool _isGrounded;
    private bool _hasDoubleJumped = false;   // Controla que solo se haga UN doble salto por vuelo

    // ── INICIALIZACIÓN
    private void Awake()
    {
        _inputActions = new PlayerInputActions();
        _characterController = GetComponent<CharacterController>();
        _waterTank = GetComponent<WaterTank>();

        if (_waterTank == null)
            Debug.LogWarning("[FirstPersonController] No se encontró WaterTank en el Player.");
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        _inputActions.Player.Enable();

        _inputActions.Player.Move.performed += SetMovement;
        _inputActions.Player.Move.canceled += obj => _movement = Vector2.zero;

        _inputActions.Player.Look.performed += SetLook;
        _inputActions.Player.Look.canceled += obj => _look = Vector2.zero;

        _inputActions.Player.Jump.performed += Jump;
    }

    // ── LECTURA DE INPUT
    private void SetMovement(InputAction.CallbackContext obj) => _movement = obj.ReadValue<Vector2>();
    private void SetLook(InputAction.CallbackContext obj) => _look = obj.ReadValue<Vector2>();

    // ── BUCLE PRINCIPAL
    private void Update()
    {
        Movement();
        Look();
    }

    // ── MOVIMIENTO Y GRAVEDAD
    private void Movement()
    {
        _isGrounded = _characterController.isGrounded;

        Vector3 move = transform.right * _movement.x + transform.forward * _movement.y;
        _characterController.Move(move * movementSpeed * Time.deltaTime);

        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
            _hasDoubleJumped = false;   // Resetear el doble salto al tocar el suelo
        }

        _velocity.y += gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    // ── ROTACIÓN DE CÁMARA
    private void Look()
    {
        Vector2 mouseNormalized = _look * sensitivity;
        _currentRotationY = Mathf.Clamp(_currentRotationY - mouseNormalized.y, minLimit, maxLimit);
        cameraTransform.localRotation = Quaternion.Euler(_currentRotationY, 0, 0);
        transform.Rotate(Vector3.up * _look.x);
    }

    // ── SALTO + DOBLE SALTO
    private void Jump(InputAction.CallbackContext obj)
    {
        // ── Salto normal: en el suelo
        if (_isGrounded)
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            return;
        }

        // ── Doble salto: en el aire, sin haber usado ya el doble salto,
        //    y con suficiente agua en el tanque
        if (!_hasDoubleJumped && CanDoubleJump())
        {
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _hasDoubleJumped = true;

            // Consumir agua del tanque
            _waterTank.Drain(waterDrainOnDoubleJump);

            Debug.Log($"[FirstPersonController] Doble salto. Agua restante: {_waterTank.CurrentWater:F0}");
        }
    }

    // ── COMPRUEBA SI SE PUEDE HACER EL DOBLE SALTO
    private bool CanDoubleJump()
    {
        if (_waterTank == null) return false;
        if (_waterTank.CurrentWater < waterCostForDoubleJump) return false;
        return true;
    }
}