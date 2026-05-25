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

    // ── DOBLE SALTO
    [Header("Doble Salto")]
    [Tooltip("Agua mínima en el tanque para poder hacer el doble salto.")]
    public float waterCostForDoubleJump = 100f;
    [Tooltip("Agua que se consume al hacer el doble salto.")]
    public float waterDrainOnDoubleJump = 100f;

    // ── PROPIEDADES DE PROPULSIÓN POR AGUA (JETPACK)
    [Header("Propulsión por Agua (Jetpack)")]
    [Tooltip("Velocidad constante en 3D aplicada al jugador en dirección contraria a la cámara mientras propulsa con agua en el aire.")]
    public float waterPropulsionSpeed = 3f;
    [Tooltip("Umbral de inclinación requerido para flotar. 1.0 es mirar 100% vertical abajo, 0.95 es mirar casi totalmente abajo (aprox. 72 grados).")]
    [Range(0.5f, 1.0f)]
    public float hoverAngleThreshold = 0.95f;

    // ── REFERENCIAS PRIVADAS
    private PlayerInputActions _inputActions;       // Sistema de input del jugador
    private CharacterController _characterController; // Componente de movimiento del personaje
    private WaterTank _waterTank;                   // Almacenaje de agua y tal
    private WaterHose _waterHose;                   // Referencia al disparador de agua

    // ── VARIABLES DE ESTADO
    private Vector2 _movement;          // Dirección de movimiento (WASD)
    private Vector3 _velocity;          // Velocidad acumulada (3D para soportar retroceso de agua y gravedad)
    private Vector2 _look;              // Dirección del ratón
    private float _currentRotationY;   // Rotación vertical actual de la cámara

    // ── ESTADO DEL SALTO
    private bool _isGrounded;
    private bool _hasDoubleJumped = false;   // Controla que solo se haga UN doble salto por vuelo

    // ── INICIALIZACIÓN
    private void Awake()
    {
        // Obtener componentes antes de que empiece el juego
        _inputActions = new PlayerInputActions();
        _characterController = GetComponent<CharacterController>();
        _waterTank = GetComponent<WaterTank>();
        _waterHose = GetComponent<WaterHose>();

        if (_waterTank == null)
            Debug.LogWarning("[FirstPersonController] No se encontró WaterTank en el Player.");
        if (_waterHose == null)
            Debug.LogWarning("[FirstPersonController] No se encontró WaterHose en el Player.");
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
    private void SetMovement(InputAction.CallbackContext obj) => _movement = obj.ReadValue<Vector2>();
    // Guarda la dirección del ratón cuando se mueve
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
        // Calcular dirección de movimiento horizontal según la orientación del personaje
        _isGrounded = _characterController.isGrounded;

        Vector3 move = transform.right * _movement.x + transform.forward * _movement.y;
        _characterController.Move(move * movementSpeed * Time.deltaTime);

        if (_isGrounded && _velocity.y < 0)
        {
            _velocity.y = -2f;
            _velocity.x = 0f;
            _velocity.z = 0f;
            _hasDoubleJumped = false;   // Resetear el doble salto al tocar el suelo
        }

        // Comprobar si estamos en el aire y disparando para aplicar el recoil del jetpack
        bool estaPropulsando = !_isGrounded && _waterHose != null && _waterHose.IsFiring;

        if (estaPropulsando)
        {
            // Calcular la velocidad de retroceso directa (dirección opuesta a la cámara)
            Vector3 recoilDirection = -cameraTransform.forward;
            Vector3 recoil = recoilDirection * waterPropulsionSpeed;

            // Activar la flotación vertical únicamente si apuntamos casi verticalmente hacia abajo
            if (recoilDirection.y >= hoverAngleThreshold)
            {
                // Aplicar la velocidad constante vertical de propulsión (anulando la gravedad para poder flotar/ascender)
                _velocity.y = recoil.y;
            }
            else
            {
                // Si apunta de forma horizontal o hacia arriba, la gravedad sigue actuando normalmente
                _velocity.y += gravity * Time.deltaTime;
            }

            // Aplicar velocidad constante horizontal según el retroceso direccional
            _velocity.x = recoil.x;
            _velocity.z = recoil.z;
        }
        else
        {
            // Acumular gravedad normal con el tiempo si no se está propulsando
            _velocity.y += gravity * Time.deltaTime;

            // Limitar velocidad máxima de caída libre por seguridad física
            if (_velocity.y < -20f) _velocity.y = -20f;

            // Desacelerar suavemente cualquier inercia horizontal residual del retroceso de agua al no propulsarse
            float desaceleracionInercia = _isGrounded ? 25f : 2f;
            _velocity.x = Mathf.MoveTowards(_velocity.x, 0f, desaceleracionInercia * Time.deltaTime);
            _velocity.z = Mathf.MoveTowards(_velocity.z, 0f, desaceleracionInercia * Time.deltaTime);
        }

        // Aplicar velocidad acumulada (3D) al personaje
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
        if (!_hasDoubleJumped && CanDoubleJump()) // si quiero doble salto ilimitado dejar solo CanDoubleJump()
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