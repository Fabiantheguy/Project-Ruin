using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KinematicCharacterController;
using System;

namespace KinematicCharacterController.Examples
{
    public enum CharacterState
    {
        Default,
    }

    public enum OrientationMethod
    {
        TowardsCamera,
        TowardsMovement,
    }

    public struct PlayerCharacterInputs
    {
        public float MoveAxisForward;
        public float MoveAxisRight;
        public Quaternion CameraRotation;
        public bool JumpDown;
        public bool CrouchDown;
        public bool CrouchUp;
    }

    public struct AICharacterInputs
    {
        public Vector3 MoveVector;
        public Vector3 LookVector;
    }

    public enum BonusOrientationMethod
    {
        None,
        TowardsGravity,
        TowardsGroundSlopeAndGravity,
    }

    public class ExampleCharacterController : MonoBehaviour, ICharacterController
    {
        public KinematicCharacterMotor Motor;

        [Header("Stable Movement")]
        public float MaxStableMoveSpeed = 10f;
        public float StableMovementSharpness = 15f;
        public float OrientationSharpness = 10f;
        public OrientationMethod OrientationMethod = OrientationMethod.TowardsCamera;
        private bool _justBoostedThisFrame = false;


        [Header("Air Movement")]
        public float MaxAirMoveSpeed = 15f;
        public float AirAccelerationSpeed = 15f;
        public float Drag = 0.1f;

        [Header("Jumping")]
        public bool AllowJumpingWhenSliding = false;
        public float JumpUpSpeed = 10f;
        public float JumpScalableForwardSpeed = 10f;
        public float JumpPreGroundingGraceTime = 0f;
        public float JumpPostGroundingGraceTime = 0f;
        public float BounceStrength = 5f;  // Adjust this value to control how much the character bounces


        [Header("Misc")]
        public List<Collider> IgnoredColliders = new List<Collider>();
        public BonusOrientationMethod BonusOrientationMethod = BonusOrientationMethod.None;
        public float BonusOrientationSharpness = 10f;
        public Vector3 Gravity = new Vector3(0, -30f, 0);
        public Transform MeshRoot;
        public Transform CameraFollowPoint;
        public float CrouchedCapsuleHeight = 1f;

        [Header("BHop Time")]
        public float MaxGroundSpeed = 7f;
        public float MaxAirSpeed = 12f;
        public float GroundAcceleration = 50f;
        public float AirAcceleration = 20f;
        public float JumpSpeed = 7.5f;
        public float GroundFriction = 8f;
        public float BunnyHopGraceTime = 0.1f;

        private Vector3 lastVelocity;
        private float lastGroundedTime;
        private float lastJumpTime;
        private float jumpCooldown = 0.05f;


        // Transform move input to be relative to camera view
        public Transform CameraTransform; // Assign in inspector or from your camera follow system


        public CharacterState CurrentCharacterState { get; private set; }

        private Collider[] _probedColliders = new Collider[8];
        private RaycastHit[] _probedHits = new RaycastHit[8];
        private Vector3 _moveInputVector;
        private Vector3 _lookInputVector;
        private bool _jumpRequested = false;
        private bool _jumpConsumed = false;
        private bool _jumpedThisFrame = false;
        private float _timeSinceJumpRequested = Mathf.Infinity;
        private float _timeSinceLastAbleToJump = 0f;
        private Vector3 _internalVelocityAdd = Vector3.zero;
        private bool _shouldBeCrouching = false;
        private bool _isCrouching = false;
        private float _momentumTimer = 0f;
        private bool _isMomentumActive = false;
        private float _momentumDecayDuration = 4f;  // Time in seconds before momentum starts decaying
        private float _momentumDecayRate = 0f;     // Rate at which momentum decays
        [Header("Sliding")]
        public float SlideInputControl = 2f; // How much the player can steer while sliding
        public float SlideControlResponsiveness = 5f; // How quickly player input affects sliding
        public float SlideAcceleration = 20f; // Extra push on slopes
        public float SlideDecayRate = 0.01f; // How much momentum is lost per frame after timer
        public float SlideSlopeThreshold = 25f; // Minimum slope angle (in degrees) to get slope-based boost
        public float SlideFriction = 0.2f; // Sliding resistance over time
        public float SlideInputSpeedThreshold = 3f; // Minimum speed to allow input adjustment
        public float SlideInputControlStrength = 2f; // How much control input affects the slide
        public Vector3 _velocityBeforeGrounding;
        private bool wasGroundedLastFrame = true;
        public float SlideLandingBoostMultiplier = 1.2f; // Adjust as needed

        private Vector3 lastInnerNormal = Vector3.zero;
        private Vector3 lastOuterNormal = Vector3.zero;

        private void Awake()
        {
            // Handle initial state
            TransitionToState(CharacterState.Default);

            // Assign the characterController to the motor
            Motor.CharacterController = this;
        }

        /// <summary>
        /// Handles movement state transitions and enter/exit callbacks
        /// </summary>
        public void TransitionToState(CharacterState newState)
        {
            CharacterState tmpInitialState = CurrentCharacterState;
            OnStateExit(tmpInitialState, newState);
            CurrentCharacterState = newState;
            OnStateEnter(newState, tmpInitialState);
        }

        /// <summary>
        /// Event when entering a state
        /// </summary>
        public void OnStateEnter(CharacterState state, CharacterState fromState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// Event when exiting a state
        /// </summary>
        public void OnStateExit(CharacterState state, CharacterState toState)
        {
            switch (state)
            {
                case CharacterState.Default:
                    {
                        break;
                    }
            }
        }

        /// <summary>
        /// This is called every frame by ExamplePlayer in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref PlayerCharacterInputs inputs)
        {
            // Get raw input
            Vector3 rawInput = new Vector3(inputs.MoveAxisRight, 0f, inputs.MoveAxisForward);

            // Get camera-relative movement direction
            Vector3 viewForward = Vector3.ProjectOnPlane(CameraTransform.forward, Motor.CharacterUp).normalized;
            Vector3 viewRight = Vector3.ProjectOnPlane(CameraTransform.right, Motor.CharacterUp).normalized;
            _moveInputVector = (viewForward * rawInput.z + viewRight * rawInput.x).normalized;

            // Look direction based on orientation setting
            Vector3 cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.forward, Motor.CharacterUp).normalized;
            if (cameraPlanarDirection.sqrMagnitude == 0f)
            {
                cameraPlanarDirection = Vector3.ProjectOnPlane(inputs.CameraRotation * Vector3.up, Motor.CharacterUp).normalized;
            }

            switch (OrientationMethod)
            {
                case OrientationMethod.TowardsCamera:
                    _lookInputVector = cameraPlanarDirection;
                    break;
                case OrientationMethod.TowardsMovement:
                    _lookInputVector = _moveInputVector.normalized;
                    break;
            }

            // Handle jump and crouch
            if (inputs.JumpDown)
            {
                _jumpRequested = true;
                _timeSinceJumpRequested = 0f;
            }

            if (inputs.CrouchDown)
            {
                _shouldBeCrouching = true;
                if (!_isCrouching)
                {
                    _isCrouching = true;
                    Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                    MeshRoot.localScale = new Vector3(1f, 0.5f, 1f);
                }
            }
            else if (inputs.CrouchUp)
            {
                _shouldBeCrouching = false;
            }
        }

        /// <summary>
        /// This is called every frame by the AI script in order to tell the character what its inputs are
        /// </summary>
        public void SetInputs(ref AICharacterInputs inputs)
        {
            _moveInputVector = inputs.MoveVector;
            _lookInputVector = inputs.LookVector;
        }

        private Quaternion _tmpTransientRot;

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called before the character begins its movement update
        /// </summary>
        public void BeforeCharacterUpdate(float deltaTime)
        {
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its rotation should be right now. 
        /// This is the ONLY place where you should set the character's rotation
        /// </summary>
        public void UpdateRotation(ref Quaternion currentRotation, float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        if (_lookInputVector.sqrMagnitude > 0f && OrientationSharpness > 0f)
                        {
                            // Smoothly interpolate from current to target look direction
                            Vector3 smoothedLookInputDirection = Vector3.Slerp(Motor.CharacterForward, _lookInputVector, 1 - Mathf.Exp(-OrientationSharpness * deltaTime)).normalized;

                            // Set the current rotation (which will be used by the KinematicCharacterMotor)
                            currentRotation = Quaternion.LookRotation(smoothedLookInputDirection, Motor.CharacterUp);
                        }

                        Vector3 currentUp = (currentRotation * Vector3.up);
                        if (BonusOrientationMethod == BonusOrientationMethod.TowardsGravity)
                        {
                            // Rotate from current up to invert gravity
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        else if (BonusOrientationMethod == BonusOrientationMethod.TowardsGroundSlopeAndGravity)
                        {
                            if (Motor.GroundingStatus.IsStableOnGround)
                            {
                                Vector3 initialCharacterBottomHemiCenter = Motor.TransientPosition + (currentUp * Motor.Capsule.radius);

                                Vector3 smoothedGroundNormal = Vector3.Slerp(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGroundNormal) * currentRotation;

                                // Move the position to create a rotation around the bottom hemi center instead of around the pivot
                                Motor.SetTransientPosition(initialCharacterBottomHemiCenter + (currentRotation * Vector3.down * Motor.Capsule.radius));
                            }
                            else
                            {
                                Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, -Gravity.normalized, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                                currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                            }
                        }
                        else
                        {
                            Vector3 smoothedGravityDir = Vector3.Slerp(currentUp, Vector3.up, 1 - Mathf.Exp(-BonusOrientationSharpness * deltaTime));
                            currentRotation = Quaternion.FromToRotation(currentUp, smoothedGravityDir) * currentRotation;
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is where you tell your character what its velocity should be right now. 
        /// This is the ONLY place where you can set the character's velocity
        /// </summary>
        public void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
        {
            bool isGrounded = Motor.GroundingStatus.IsStableOnGround;

            // Track vertical velocity before grounding
            _velocityBeforeGrounding = currentVelocity;

            if (isGrounded)
            {
                if (!_isMomentumActive)
                {
                    _momentumTimer = _momentumDecayDuration;
                    _isMomentumActive = true;
                }

                Vector3 effectiveGroundNormal = Motor.GroundingStatus.GroundNormal;
                bool isSliding = _isCrouching;

                // Check if the player has just landed into a slide
                bool landedWhileSliding = false;
                Vector3 landingBoost = Vector3.zero;

                if (!wasGroundedLastFrame && isSliding && _velocityBeforeGrounding.y < -1f)
                {
                    landedWhileSliding = true;

                    Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, effectiveGroundNormal).normalized;
                    float fallSpeed = Mathf.Abs(_velocityBeforeGrounding.y);

                    landingBoost = slideDirection * fallSpeed * SlideLandingBoostMultiplier;
                    _justBoostedThisFrame = true;
                }

                wasGroundedLastFrame = true;

                if (isSliding)
                {
                    if (landedWhileSliding)
                    {
                        currentVelocity += landingBoost;
                    }

                    float currentSpeed = currentVelocity.magnitude;

                    // Unground if sliding off an edge
                    if (!Motor.GroundingStatus.IsStableOnGround && !Motor.GroundingStatus.SnappingPrevented)
                    {
                        Motor.ForceUnground();
                        return;
                    }

                    // Only project onto slope after landing boost
                    if (!_justBoostedThisFrame)
                    {
                        currentVelocity = Vector3.ProjectOnPlane(currentVelocity, effectiveGroundNormal);
                    }

                    if (currentSpeed > SlideInputSpeedThreshold)
                    {
                        Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                        Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;

                        Vector3 inputInfluence = reorientedInput * SlideInputControl;
                        currentVelocity = Vector3.Lerp(currentVelocity, currentVelocity + inputInfluence, deltaTime * SlideControlResponsiveness);
                    }

                    float slopeAngle = Vector3.Angle(Motor.CharacterUp, effectiveGroundNormal);
                    if (slopeAngle > SlideSlopeThreshold)
                    {
                        Vector3 slopeDir = Vector3.ProjectOnPlane(Vector3.down, effectiveGroundNormal).normalized;
                        currentVelocity += slopeDir * SlideAcceleration * deltaTime;
                    }

                    // Apply decay unless we're in the first frame of boosting
                    if (!_justBoostedThisFrame)
                    {
                        if (_momentumTimer <= 0f)
                        {
                            currentVelocity *= (1f - SlideDecayRate);
                        }
                        else
                        {
                            _momentumTimer -= deltaTime;
                        }
                    }

                    _justBoostedThisFrame = false;
                }
                else
                {
                    // Standard grounded movement
                    float currentVelocityMagnitude = currentVelocity.magnitude;

                    currentVelocity = Motor.GetDirectionTangentToSurface(currentVelocity, effectiveGroundNormal) * currentVelocityMagnitude;

                    Vector3 inputRight = Vector3.Cross(_moveInputVector, Motor.CharacterUp);
                    Vector3 reorientedInput = Vector3.Cross(effectiveGroundNormal, inputRight).normalized * _moveInputVector.magnitude;
                    Vector3 targetVelocity = reorientedInput * MaxStableMoveSpeed;

                    Vector3 horizontalVel = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp);
                    currentVelocity -= horizontalVel * GroundFriction * deltaTime;

                    currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity, 1f - Mathf.Exp(-StableMovementSharpness * deltaTime));

                    if (_momentumTimer <= 0f)
                    {
                        currentVelocity *= (1f - _momentumDecayRate);
                    }
                    else
                    {
                        _momentumTimer -= deltaTime;
                    }
                }
            }
            else
            {
                // Air movement
                wasGroundedLastFrame = false;

                if (_isCrouching)
                {
                    currentVelocity = Vector3.Lerp(currentVelocity, currentVelocity + _moveInputVector * SlideInputControl, deltaTime * SlideControlResponsiveness);
                }

                if (_moveInputVector.sqrMagnitude > 0f)
                {
                    Vector3 wishDir = _moveInputVector;
                    AccelerateInAir(ref currentVelocity, wishDir, AirAccelerationSpeed, MaxAirMoveSpeed, deltaTime);
                }

                if (Motor.GroundingStatus.FoundAnyGround)
                {
                    Vector3 obstructionNormal = Vector3.Cross(Vector3.Cross(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal), Motor.CharacterUp).normalized;
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, obstructionNormal);
                }

                currentVelocity += Gravity * deltaTime;
                currentVelocity *= (1f / (1f + Drag * deltaTime));
            }

            // Handle jumping
            _timeSinceJumpRequested += deltaTime;
            if (_jumpRequested)
            {
                bool canJump = !_jumpConsumed &&
                    (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : isGrounded) ||
                    _timeSinceLastAbleToJump <= JumpPostGroundingGraceTime;

                if (canJump)
                {
                    currentVelocity = Vector3.ProjectOnPlane(currentVelocity, Motor.CharacterUp) +
                                      Vector3.Project(currentVelocity, Motor.CharacterUp) * 0.5f;

                    Vector3 jumpDir = Motor.CharacterUp;

                    if (Motor.GroundingStatus.FoundAnyGround && !isGrounded)
                    {
                        jumpDir = Motor.GroundingStatus.GroundNormal;
                    }

                    Motor.ForceUnground();
                    currentVelocity += jumpDir * JumpUpSpeed;

                    if (_moveInputVector.sqrMagnitude > 0f)
                    {
                        Vector3 wishDir = _moveInputVector;
                        AccelerateInAir(ref currentVelocity, wishDir, AirAccelerationSpeed, MaxAirMoveSpeed, deltaTime);
                    }

                    _jumpRequested = false;
                    _jumpConsumed = true;
                    _jumpedThisFrame = true;
                }
            }

            // Apply internal forces
            if (_internalVelocityAdd.sqrMagnitude > 0f)
            {
                currentVelocity += _internalVelocityAdd;
                _internalVelocityAdd = Vector3.zero;
            }
        }



        private void AccelerateInAir(ref Vector3 velocity, Vector3 wishDir, float accel, float maxSpeed, float deltaTime)
        {
            // Calculate air velocity acceleration in a direction relative to the camera
            Vector3 velocityOnPlane = Vector3.ProjectOnPlane(velocity, Motor.CharacterUp);
            float currentSpeed = Vector3.Dot(velocityOnPlane, wishDir);
            float addSpeed = maxSpeed - currentSpeed;

            if (addSpeed <= 0f)
                return;

            float accelSpeed = accel * deltaTime;
            if (accelSpeed > addSpeed)
                accelSpeed = addSpeed;

            velocity += wishDir * accelSpeed;
        }





        /// <summary>
        /// (Called by KinematicCharacterMotor during its update cycle)
        /// This is called after the character has finished its movement update
        /// </summary>
        public void AfterCharacterUpdate(float deltaTime)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        // Handle jump-related values
                        {
                            // Handle jumping pre-ground grace period
                            if (_jumpRequested && _timeSinceJumpRequested > JumpPreGroundingGraceTime)
                            {
                                _jumpRequested = false;
                            }

                            if (AllowJumpingWhenSliding ? Motor.GroundingStatus.FoundAnyGround : Motor.GroundingStatus.IsStableOnGround)
                            {
                                // If we're on a ground surface, reset jumping values
                                if (!_jumpedThisFrame)
                                {
                                    _jumpConsumed = false;
                                }
                                _timeSinceLastAbleToJump = 0f;
                            }
                            else
                            {
                                // Keep track of time since we were last able to jump (for grace period)
                                _timeSinceLastAbleToJump += deltaTime;
                            }
                        }

                        // Handle uncrouching
                        if (_isCrouching && !_shouldBeCrouching)
                        {
                            // Do an overlap test with the character's standing height to see if there are any obstructions
                            Motor.SetCapsuleDimensions(0.5f, 2f, 1f);
                            if (Motor.CharacterOverlap(
                                Motor.TransientPosition,
                                Motor.TransientRotation,
                                _probedColliders,
                                Motor.CollidableLayers,
                                QueryTriggerInteraction.Ignore) > 0)
                            {
                                // If obstructions, just stick to crouching dimensions
                                Motor.SetCapsuleDimensions(0.5f, CrouchedCapsuleHeight, CrouchedCapsuleHeight * 0.5f);
                            }
                            else
                            {
                                // If no obstructions, uncrouch
                                MeshRoot.localScale = new Vector3(1f, 1f, 1f);
                                _isCrouching = false;
                            }
                        }
                        break;
                    }
            }
        }

        public void PostGroundingUpdate(float deltaTime)
        {
            // Handle landing and leaving ground
            if (Motor.GroundingStatus.IsStableOnGround && !Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLanded();
            }
            else if (!Motor.GroundingStatus.IsStableOnGround && Motor.LastGroundingStatus.IsStableOnGround)
            {
                OnLeaveStableGround();
            }
        }

        public bool IsColliderValidForCollisions(Collider coll)
        {
            if (IgnoredColliders.Count == 0)
            {
                return true;
            }

            if (IgnoredColliders.Contains(coll))
            {
                return false;
            }

            return true;
        }

        public void OnGroundHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void OnMovementHit(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, ref HitStabilityReport hitStabilityReport)
        {
        }

        public void AddVelocity(Vector3 velocity)
        {
            switch (CurrentCharacterState)
            {
                case CharacterState.Default:
                    {
                        _internalVelocityAdd += velocity;
                        break;
                    }
            }
        }

        public void ProcessHitStabilityReport(Collider hitCollider, Vector3 hitNormal, Vector3 hitPoint, Vector3 atCharacterPosition, Quaternion atCharacterRotation, ref HitStabilityReport hitStabilityReport)
        {
        }

        protected void OnLanded()
        {
        }

        protected void OnLeaveStableGround()
        {
        }

        public void OnDiscreteCollisionDetected(Collider hitCollider)
        {
        }
    }
}