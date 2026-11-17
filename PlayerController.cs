using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    private CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    
    [Header("Movement")]
        public float walkSpeed = 5.0f;
        public float sprintSpeed = 9.0f;
        public float slideSpeed = 12.0f;
        public float crouchSpeed = 2.5f;
        private float currentSpeed;
        
        public float jumpHeight = 1.2f;
        public float gravityValue = -29.43f;
        public float mouseSensitivity = 2.0f;
    
        private float verticalRotation = 0f; 
        private Transform cameraTransform;
        
        // crouch and slide variables 
        private bool isSliding = false;
        private bool isCrouching = false;
        private float slideTimer = 0f;
        public float slideDuration = 0.75f; 
        private Vector3 slideDirection;
        
        private float normalHeight;
        public float slideHeight = 1.0f; // Height of capsule when sliding or crouching
        
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        cameraTransform = Camera.main.transform;
        Cursor.lockState = CursorLockMode.Locked; // Lock the cursor in the middle of the screen
        normalHeight = controller.height;
    }

    // Update is called once per frame
    void Update()
    {
        // Camera (mouse)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        
        transform.Rotate(Vector3.up * mouseX);
        
        // Movement
        groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0) { 
                playerVelocity.y = 0f; 
        }
        // Slide
        if (currentSpeed.Equals(sprintSpeed) && Input.GetKeyDown(KeyCode.C) && groundedPlayer && !isSliding)
        {
            StartCoroutine(StartSlide());
        }

        Vector3 move = Vector3.zero;
        
        if (isSliding)
        {
            // While sliding the direction is locked and the player slowly decelerates
            slideTimer -= Time.deltaTime;
            currentSpeed = Mathf.Lerp(slideSpeed, walkSpeed, slideTimer / slideDuration);
            move = slideDirection;

            if (slideTimer <= 0)
            {
                EndSlide();
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.C)) {
                isCrouching = true;
                controller.height = slideHeight;
                currentSpeed = crouchSpeed;
            }
            else if (isCrouching) { // if he does not hold c and was already crouching, get up
                isCrouching = false;
                controller.height = normalHeight;
            }

            if (!isCrouching)
            {
                if (Input.GetKey(KeyCode.LeftShift))
                {
                    currentSpeed = sprintSpeed;
                }
                else
                {
                    currentSpeed = walkSpeed;
                }
            }
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");
            move = transform.right * moveX + transform.forward * moveZ;
        }
        
        controller.Move(move * (currentSpeed * Time.deltaTime));
        
        // Jump 
        if (Input.GetButtonDown("Jump") && groundedPlayer && !isSliding) {
            playerVelocity.y += Mathf.Sqrt(jumpHeight * -3.0f * gravityValue);
        }
        
        // Gravity
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);
    }
    
    private IEnumerator StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        controller.height = slideHeight;

        // Prende la direzione in cui il giocatore si sta muovendo (o guarda in avanti se è fermo)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        slideDirection = transform.right * moveX + transform.forward * moveZ;
        if (slideDirection == Vector3.zero)
        {
            slideDirection = transform.forward;
        }
        slideDirection.Normalize();

        yield return new WaitForSeconds(slideDuration);
        if (Input.GetKey(KeyCode.C))
        {
            isSliding = false;
            isCrouching = true;
        }
        else
        {
            EndSlide();
        }
    }

    private void EndSlide()
    {
        isSliding = false;
        isCrouching = false;
        controller.height = normalHeight; // Ripristina l'altezza normale della capsula
    }
    
    
}
