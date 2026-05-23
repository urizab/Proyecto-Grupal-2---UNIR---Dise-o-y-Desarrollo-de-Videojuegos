using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonController : MonoBehaviour
{
    // ── MOVIMIENTO
    public float movementSpeed = 2;       // Velocidad de desplazamiento del personaje
    public float gravity = -9.8f;         // Fuerza de gravedad aplicada al personaje
    public float jumpHeight = 0.7f;       // Altura máxima del salto

    // ── CÁMARA
    public Transform cameraTransform;     // Referencia a la cámara del personaje
    public float sensitivity = 0.5f;     // Sensibilidad del movimiento del ratón
    public float minLimit = -80f;         // Límite inferior de rotación vertical de la cámara
    public float maxLimit = 80;           // Límite superior de rotación vertical de la cámara

    // ── REFERENCIAS PRIVADAS
    private PlayerInputActions _inputActions;       // Sistema de input del jugador
    private CharacterController _characterController; // Componente de movimiento del personaje

    // ── VARIABLES DE ESTADO
    private Vector2 _movement;          // Dirección de movimiento (WASD)
    private Vector2 _velocity;          // Velocidad acumulada ( .y para la gravedad)
    private Vector2 _look;              // Dirección del ratón
    private float _currentRotationY;   // Rotación vertical actual de la cámara

    // ── INICIALIZACIÓN
    private void Awake()
    {
        // Obtener componentes antes de que empiece el juego
        _inputActions = new PlayerInputActions();
        _characterController = GetComponent<CharacterController>();
    }

    private void Start()
    {
        // Ocultar y bloquear el cursor en el centro de la pantalla
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Activar el mapa de acciones del jugador
        _inputActions.Player.Enable();

        // Suscribir los eventos de input a sus métodos correspondientes
        _inputActions.Player.Move.performed += SetMovement;
        _inputActions.Player.Move.canceled += obj => _movement = Vector2.zero;   // Al soltar, detener movimiento

        _inputActions.Player.Look.performed += SetLook;
        _inputActions.Player.Look.canceled += obj => _look = Vector2.zero;       // Al soltar, detener cámara

        _inputActions.Player.Jump.performed += Jump;                             // Saltar al presionar espacio
    }

    // ── LECTURA DE INPUT

    // Guarda la dirección de movimiento cuando se pulsa una tecla
    private void SetMovement(InputAction.CallbackContext obj)
    {
        _movement = obj.ReadValue<Vector2>();
    }

    // Guarda la dirección del ratón cuando se mueve
    private void SetLook(InputAction.CallbackContext obj)
    {
        _look = obj.ReadValue<Vector2>();
    }

    // ── BUCLE PRINCIPAL
    private void Update()
    {
        Movement();
        Look();
    }

    // ── MOVIMIENTO Y GRAVEDAD
    private void Movement()
    {
        // Calcular dirección de movimiento horizontal según la orientación del personaje
        Vector3 move = transform.right * _movement.x + transform.forward * _movement.y;
        _characterController.Move(move * movementSpeed * Time.deltaTime);

        // Resetear velocidad vertical al tocar el suelo para evitar acumulación infinita
        if (_characterController.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        // Acumular gravedad con el tiempo
        _velocity.y += gravity * Time.deltaTime;

        // Aplicar velocidad vertical al personaje
        _characterController.Move(_velocity * Time.deltaTime);
    }

    // ── ROTACIÓN DE CÁMARA
    private void Look()
    {
        // Escalar el input del ratón por la sensibilidad
        Vector2 mouseNormalized = _look * sensitivity;

        // Calcular y limitar la rotación vertical de la cámara (mirar arriba/abajo)
        _currentRotationY = Mathf.Clamp(_currentRotationY - mouseNormalized.y, minLimit, maxLimit);
        cameraTransform.localRotation = Quaternion.Euler(_currentRotationY, 0, 0);

        // Rotar el cuerpo del personaje horizontalmente (mirar izquierda/derecha)
        transform.Rotate(Vector3.up * _look.x);
    }

    // ── SALTO
    private void Jump(InputAction.CallbackContext obj)
    {
        // Solo saltar si el personaje está en el suelo
        if (_characterController.isGrounded)
        {
            // Fórmula física para convertir altura deseada en velocidad inicial de salto
            _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}