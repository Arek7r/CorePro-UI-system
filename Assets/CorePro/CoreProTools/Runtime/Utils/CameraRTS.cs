using UnityEngine;
using UnityEngine.InputSystem;

namespace CorePro.CoreProTools.Utils
{
    public class CameraRTS : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 20f;
        [SerializeField] private float zoomSpeed = 10f;
        [SerializeField] private float rotationSpeed = 100f;

        [Header("Input Actions")]
        public InputAction moveAction = new InputAction("Move", InputActionType.Value, binding: "", interactions: "", processors: "");
        public InputAction rotateAction = new InputAction("Rotate", InputActionType.Value, binding: "", interactions: "", processors: "");
        public InputAction zoomAction = new InputAction("Zoom", InputActionType.Value, "<Mouse>/scroll/y");

        private Vector2 moveInput;
        private float rotateInput;
        private float zoomInput;
        private Vector3 moveDirection;

        private void Awake()
        {
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            //  Q/E composite binding
            // rotateAction.AddCompositeBinding("1DAxis")
            //     .With("Negative", "<Keyboard>/q")
            //     .With("Positive", "<Keyboard>/e");
        }

        private void OnEnable()
        {
            moveAction.Enable();
            rotateAction.Enable();
            zoomAction.Enable();
        }

        private void OnDisable()
        {
            moveAction.Disable();
            rotateAction.Disable();
            zoomAction.Disable();
        }

        private void Update()
        {
            HandleMovement();
            HandleRotation();
            HandleZoom();
        }

        private void HandleMovement()
        {
            moveInput = moveAction.ReadValue<Vector2>();

            if (moveInput.sqrMagnitude < 0.01f)
                return;

            moveDirection.x = moveInput.x;
            moveDirection.y = 0f;
            moveDirection.z = moveInput.y;

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            transform.position += moveDirection * (moveSpeed * Time.deltaTime);
        }

        private void HandleRotation()
        {
            rotateInput = rotateAction.ReadValue<float>();

            if (Mathf.Abs(rotateInput) < 0.01f)
                return;

            // Rotation around the GLOBAL Y axis
            float rotationAmount = rotateInput * rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, rotationAmount, 0f) * transform.rotation;
        }

        private void HandleZoom()
        {
            zoomInput = zoomAction.ReadValue<float>();

            if (Mathf.Abs(zoomInput) < 0.01f)
                return;

            // LOCAL movement (forward/backward relative to the camera's viewing direction)
            float zoomAmount = zoomInput * zoomSpeed * Time.deltaTime;
            transform.position += transform.forward * zoomAmount;
        }
    }
}
