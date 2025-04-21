using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using KinematicCharacterController.Examples;


namespace KinematicCharacterController.Examples
{


    public class ExamplePlayer : MonoBehaviour
    {
        public ExampleCharacterController Character;
        public ExampleCharacterCamera CharacterCamera;

        private const string MouseXInput = "Mouse X";
        private const string MouseYInput = "Mouse Y";
        private const string MouseScrollInput = "Mouse ScrollWheel";
        private const string HorizontalInput = "Horizontal";
        private const string VerticalInput = "Vertical";

        [Header("Arrow Key Camera Control")]
        public float ArrowKeyRotationSpeed = 90f; // degrees per second
        public float ArrowKeySmoothTime = 0.1f;

        private Vector2 _arrowKeyLookVelocity;
        private Vector2 _arrowKeyLookInput;
        private Vector2 _arrowKeyLookCurrent;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;

            // Tell camera to follow transform
            CharacterCamera.SetFollowTransform(Character.CameraFollowPoint);

            // Ignore the character's collider(s) for camera obstruction checks
            CharacterCamera.IgnoredColliders.Clear();
            CharacterCamera.IgnoredColliders.AddRange(Character.GetComponentsInChildren<Collider>());
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
            }

            HandleCharacterInput();
        }

        private void LateUpdate()
        {
            // Handle rotating the camera along with physics movers
            if (CharacterCamera.RotateWithPhysicsMover && Character.Motor.AttachedRigidbody != null)
            {
                CharacterCamera.PlanarDirection = Character.Motor.AttachedRigidbody.GetComponent<PhysicsMover>().RotationDeltaFromInterpolation * CharacterCamera.PlanarDirection;
                CharacterCamera.PlanarDirection = Vector3.ProjectOnPlane(CharacterCamera.PlanarDirection, Character.Motor.CharacterUp).normalized;
            }

            HandleCameraInput();
        }

        private void HandleCameraInput()
        {
            // Mouse look input
            float mouseLookAxisUp = Input.GetAxisRaw(MouseYInput);
            float mouseLookAxisRight = Input.GetAxisRaw(MouseXInput);
            Vector3 lookInputVector = new Vector3(mouseLookAxisRight, mouseLookAxisUp, 0f);

            // Prevent mouse movement when cursor unlocked
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                lookInputVector = Vector3.zero;
            }

            // Arrow key input (adds to look vector)
            Vector2 targetArrowLook = Vector2.zero;
            if (Input.GetKey(KeyCode.UpArrow)) targetArrowLook.y += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) targetArrowLook.y -= 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) targetArrowLook.x -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) targetArrowLook.x += 1f;

            // Smooth the arrow key input
            _arrowKeyLookInput = Vector2.SmoothDamp(
                _arrowKeyLookInput,
                targetArrowLook,
                ref _arrowKeyLookVelocity,
                ArrowKeySmoothTime
            );

            // Convert to a scaled look vector (degrees per second)
            Vector3 arrowLookVector = new Vector3(
                _arrowKeyLookInput.x * ArrowKeyRotationSpeed * Time.deltaTime,
                _arrowKeyLookInput.y * ArrowKeyRotationSpeed * Time.deltaTime,
                0f
            );

            // Combine with mouse input
            Vector3 finalLookInput = lookInputVector + arrowLookVector;

            // Zoom
            float scrollInput = -Input.GetAxis(MouseScrollInput);
#if UNITY_WEBGL
    scrollInput = 0f;
#endif

            // Apply to camera
            CharacterCamera.UpdateWithInput(Time.deltaTime, scrollInput, finalLookInput);

            // Toggle first/third person
            if (Input.GetMouseButtonDown(1))
            {
                CharacterCamera.TargetDistance = (CharacterCamera.TargetDistance == 0f) ? CharacterCamera.DefaultDistance : 0f;
            }
        }





        private void HandleCharacterInput()
        {
            PlayerCharacterInputs characterInputs = new PlayerCharacterInputs();

            // Build the CharacterInputs struct
            characterInputs.MoveAxisForward = Input.GetAxisRaw(VerticalInput);
            characterInputs.MoveAxisRight = Input.GetAxisRaw(HorizontalInput);
            characterInputs.CameraRotation = CharacterCamera.Transform.rotation;
            characterInputs.JumpDown = Input.GetKeyDown(KeyCode.Space);
            characterInputs.CrouchDown = Input.GetKeyDown(KeyCode.C);
            characterInputs.CrouchUp = Input.GetKeyUp(KeyCode.C);

            // Apply inputs to character
            Character.SetInputs(ref characterInputs);
        }
    }
}