// FirefighterController.cs
using UnityEngine;
using UnityEngine.InputSystem;

public class FirefighterController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float mouseSensitivity = 2f;
    public float jumpForce = 8f;
    public float gravity = -20f;
    
    [Header("Hover")]
    public float hoverForce = 15f;
    public float hoverDrainRate = 20f;
    
    [Header("Water Cannon - Primary")]
    public float primaryForce = 20f;
    public float primaryRange = 10f;
    public float primaryCooldown = 0.2f;
    public float waterDrainPrimary = 10f;
    
    [Header("Water Bomb - Secondary")]
    public float bombRadius = 5f;
    public float bombForce = 15f;
    public float bombSelfForce = 25f;
    public float bombCooldown = 1f;
    public float waterDrainBomb = 40f;
    
    [Header("Water Resource")]
    public float maxWater = 100f;
    public float currentWater;
    public float waterRegenRate = 15f;
    public float regenDelay = 1.5f;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float lastUseTime;
    private float nextPrimaryTime;
    private float nextBombTime;
    
    // Camera and look
    private Camera playerCamera;
    private float xRotation = 0f;
    
    // Hover
    private bool isHovering;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction primaryAction;
    private InputAction secondaryAction;
    
    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
            playerInput = gameObject.AddComponent<PlayerInput>();
        
        // Usar el default map
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        primaryAction = playerInput.actions["Fire"];
        secondaryAction = playerInput.actions["Fire2"];
    }
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        currentWater = maxWater;
        
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleWaterRegen();
        HandleShooting();
        HandleHover();
        ApplyGravity();
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * walkSpeed * Time.deltaTime);
        
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = jumpForce;
        }
    }
    
    void HandleWaterRegen()
    {
        if (Time.time - lastUseTime > regenDelay && currentWater < maxWater)
        {
            currentWater += waterRegenRate * Time.deltaTime;
            currentWater = Mathf.Min(currentWater, maxWater);
        }
    }
    
    void HandleShooting()
    {
        // Primary fire (hold for hover)
        if (Input.GetButton("Fire1") && Time.time >= nextPrimaryTime && currentWater >= waterDrainPrimary)
        {
            ShootPrimary();
            nextPrimaryTime = Time.time + primaryCooldown;
            lastUseTime = Time.time;
        }
        
        // Secondary fire
        if (Input.GetButtonDown("Fire2") && Time.time >= nextBombTime && currentWater >= waterDrainBomb)
        {
            ShootSecondary();
            nextBombTime = Time.time + bombCooldown;
            lastUseTime = Time.time;
        }
    }
    
    void ShootPrimary()
    {
        currentWater -= waterDrainPrimary;
        
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, primaryRange))
        {
            // Push objects
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 pushDir = (hit.point - playerCamera.transform.position).normalized;
                rb.AddForce(pushDir * primaryForce, ForceMode.Impulse);
            }
        }
        
        // Visual effect (simple line)
        Debug.DrawRay(ray.origin, ray.direction * primaryRange, Color.blue, 0.1f);
    }
    
    void ShootSecondary()
    {
        currentWater -= waterDrainBomb;
        
        // Create explosion effect
        Vector3 explosionPoint = playerCamera.transform.position + playerCamera.transform.forward * 2f;
        
        // Affect enemies and objects in radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionPoint, bombRadius);
        foreach (var hitCollider in hitColliders)
        {
            Rigidbody rb = hitCollider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 direction = (hitCollider.transform.position - explosionPoint).normalized;
                rb.AddForce(direction * bombForce, ForceMode.Impulse);
            }
            
            // Check if it's an enemy (simple tag system)
            if (hitCollider.CompareTag("Enemy"))
            {
                Destroy(hitCollider.gameObject);
            }
        }
        
        // Self launch (if aimed at feet)
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2f))
        {
            if (hit.distance < 1.5f && hit.point.y < transform.position.y + 0.5f)
            {
                Vector3 launchDir = -ray.direction;
                launchDir.y = 1f;
                velocity = launchDir.normalized * bombSelfForce;
            }
        }
        
        // Visual feedback
        Debug.DrawRay(explosionPoint, Vector3.up * bombRadius, Color.red, 0.5f);
    }
    
    void HandleHover()
    {
        // Hover when in air and holding primary fire
        if (!isGrounded && Input.GetButton("Fire1") && currentWater > 0)
        {
            isHovering = true;
            currentWater -= hoverDrainRate * Time.deltaTime;
            currentWater = Mathf.Max(currentWater, 0);
            
            if (velocity.y < 0)
            {
                velocity.y = 0;
            }
            velocity.y += hoverForce * Time.deltaTime;
        }
        else
        {
            isHovering = false;
        }
    }
    
    void ApplyGravity()
    {
        isGrounded = controller.isGrounded;
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 200, 30), $"Water: {currentWater:F0}/{maxWater}");
        
        if (isHovering)
        {
            GUI.Label(new Rect(10, 50, 200, 20), "HOVERING!");
        }
        
        if (!isGrounded && Input.GetButton("Fire1") && currentWater <= 0)
        {
            GUI.Label(new Rect(10, 70, 200, 20), "NO WATER!");
        }
    }
}