using System;
using System.Reflection;
using UnityEngine;
using System.Collections;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

// initial position 
// Vector3(462,0.140000001,117) // PlayerRig
// Vector3(0,0,0) RavanaPlayer

// position of brahma loka copy first line to player rig and second line to ravana player go
// Vector3(4337.3999,431.399994,1506.09998) 
// Vector3(0,0,0) 


// position mount meru
// Vector3(501,257.700012,1071.69995) - PlayerRig
// Vector3(0,2.5999999,27.5) - RavanaPlayer

namespace RavanaGame
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class RavanaPlayerController : MonoBehaviour
    {

        public PlayerControllerPublicProperties playerControllerPublicProperties;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;



        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _attackTimeoutDelta;
        private float _magicAttackTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDAttack;
        private int _animIDAttackDown;

        private AuraController auraController;

        public bool canMove = true;
        public bool noWeaponAttack = false;


        [SerializeField] private AudioClip attackAudioClip;
        [SerializeField] private bool drawGizmos = false;
        [SerializeField] private AudioSource audioSource;

        [SerializeField] private GameObject swordEquiped;
        [SerializeField] private GameObject swordUnequiped;




        private PlayerScoreEvolutionController playerScoreController;

        [SerializeField]
        private float maxDistance = 3.5f;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private RavanaInputs _input;

        private RavanaInputActions ravanaInputActions;
        private GameObject _mainCamera;

        private bool isSprint = false;

        private const float _threshold = 0.01f;

        // private bool _animator;

        private bool _hasWeapon = true;
        private bool _holdingWeapon = true;
        private bool _toggleHoldWeapon = false;
        private string _currentAnimation;

        public enum NoWeaponAnimations
        {
            DirectLeft,
            DirectRight,
            FrontKickLeftLeg,
            FrontKickRightLeg,
            LeftPunch,
            RightPunch,
            PunchingLeft,
            PunchingRight

        }

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var activeControl = ravanaInputActions.Ravana.Look.activeControl;
                if (activeControl != null && (activeControl.device is Mouse || activeControl.device is Keyboard))
                {
                    return true;
                }
#endif
                return false;
            }
        }


        public static Action EnterKeyPressed;

        private void Awake()
        {
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }


            ravanaInputActions = InputActionsSingleton.Instance;

            ravanaInputActions.Ravana.MissionMessageConfirm.performed += ctx =>
            {
                EnterKeyPressed?.Invoke();
            };



            ravanaInputActions.Ravana.Pause.performed += ctx => TogglePauseGame();
            ravanaInputActions.Ravana.GoToMainMenu.performed += ctx => GoToMainMenu();
            ravanaInputActions.Ravana.ToggleHoldWeapon.performed += ctx => ToggleHoldWeapon();

            audioSource = GameObject.Find("MainAudioSource").GetComponent<AudioSource>();
            audioSource.volume = 0.2f;
            swordUnequiped.SetActive(false);
        }

        private void OnEnable()
        {
            ravanaInputActions.Enable();
        }

        private void OnDisable()
        {
            ravanaInputActions.Ravana.Pause.performed -= ctx => TogglePauseGame();
            ravanaInputActions.Ravana.GoToMainMenu.performed -= ctx => GoToMainMenu();
            ravanaInputActions.Ravana.ToggleHoldWeapon.performed -= ctx => ToggleHoldWeapon();

            ravanaInputActions.Disable();
        }

        private void ToggleHoldWeapon()
        {
            if (_hasWeapon && _holdingWeapon)
            {
                _holdingWeapon = false;
                _toggleHoldWeapon = true;
                _animator.SetBool("ToggleHoldWeapon", _toggleHoldWeapon);
            }
            else if (_hasWeapon && !_holdingWeapon)
            {
                _holdingWeapon = true;
                _toggleHoldWeapon = true;
                _animator.SetBool("ToggleHoldWeapon", _toggleHoldWeapon);
            }
        }

        private void FinishToggleWeaponAnimation()
        {
            _toggleHoldWeapon = false;
            _animator.SetBool("ToggleHoldWeapon", _toggleHoldWeapon);
        }

        private void ReleaseWeapon()
        {
            swordEquiped.SetActive(false);
            swordUnequiped.SetActive(true);
            _animator.SetBool("HoldingWeapon", false);

        }

        private void GrabWeapon()
        {
            swordEquiped.SetActive(true);
            swordUnequiped.SetActive(false);
            _animator.SetBool("HoldingWeapon", true);
        }

        private void TogglePauseGame()
        {
            if (Time.timeScale != 0)
            {
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

        }

        public void GoToMainMenu()
        {
            Time.timeScale = 1;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SceneManager.LoadScene("MainMenu");
        }

        private void Start()
        {
            playerControllerPublicProperties.CinemachineCameraTarget = GameObject.Find("PlayerCameraRoot");
            _cinemachineTargetYaw = playerControllerPublicProperties.CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _animator = GetComponent<Animator>(); //TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<RavanaInputs>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
            Debug.LogError("Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = playerControllerPublicProperties.JumpTimeout;
            _fallTimeoutDelta = playerControllerPublicProperties.FallTimeout;
            _attackTimeoutDelta = playerControllerPublicProperties.AttackTimeout;
            _magicAttackTimeoutDelta = playerControllerPublicProperties.MagicAttackTimeout;


            auraController = this.transform.Find("AuraMesh").GetComponent<AuraController>();

            AudioClip[] footstepAudioClips = new AudioClip[10];

            for (int i = 0; i < 10; i++)
            {
                string fileName = $"Audio/Footsteps/Player_Footstep_{i + 1:D2}";
                footstepAudioClips[i] = Resources.Load<AudioClip>(fileName);
            }

            playerControllerPublicProperties.FootstepAudioClips = footstepAudioClips;
        }

        private void Update()
        {
            // _animator = TryGetComponent(out _animator);

            Vector3 forward = transform.TransformDirection(Vector3.forward);

            Debug.DrawRay(transform.position, forward * maxDistance, Color.red);

            if (canMove)
            {
                JumpAndGravity();
                GroundedCheck();
                if (!_input.magicAttack && !_input.attackDown)
                {
                    Move();
                }
                Attack();
            }

        }

        void OnDrawGizmos()
        {
            if (!drawGizmos) return;

            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Gizmos.color = Color.red;
            // Gizmos.DrawRay(transform.position, forward * maxDistance);
            float offset = 1.0f; // Offset from the ground
            float thickness = 0.1f; // Thickness of the line

            Vector3 startPosition = transform.position + new Vector3(0, offset, 0);

            // Draw the main line
            Gizmos.DrawRay(startPosition, forward * maxDistance);

            // Draw additional lines to simulate thickness
            for (float i = -thickness; i <= thickness; i += thickness / 2)
            {
                Gizmos.DrawRay(startPosition + new Vector3(i, 0, 0), forward * maxDistance);
                Gizmos.DrawRay(startPosition + new Vector3(0, 0, i), forward * maxDistance);
            }

            int numberOfLines = 40; // Number of lines to draw for the cone

            // Calculate the rotation for each line
            Quaternion rotationStep = Quaternion.AngleAxis(60f / (numberOfLines - 1), transform.up);

            // Start with a direction rotated 45 degrees to the left
            Vector3 direction = Quaternion.AngleAxis(-30f, transform.up) * transform.forward;

            // Draw the lines
            for (int i = 0; i < numberOfLines; i++)
            {
                Gizmos.DrawLine(transform.position + new Vector3(0, offset, 0), transform.position + direction * maxDistance);
                direction = rotationStep * direction;
            }
        }



        public bool isSwordAttack = false;

        private void Attack()
        {


            if (_input.attackDown && !isSwordAttack)
            {

                if (_animator)
                {

                    if (!_hasWeapon || !_holdingWeapon)
                    {

                        Array values = Enum.GetValues(typeof(NoWeaponAnimations));

                        NoWeaponAnimations randomAnimation = (NoWeaponAnimations)values.GetValue(UnityEngine.Random.Range(0, values.Length));

                        canMove = false;
                        noWeaponAttack = true;
                        CheckIfEnemyOnFront();
                        ChangeAnimationState(randomAnimation.ToString());
                        _input.attackDown = false;
                    }
                    else
                    {
                        _animator.SetBool("AttackDown", true);
                        isSwordAttack = true;
                        audioSource.PlayOneShot(attackAudioClip, 1f);

                    }
                }
            }

            if (isSwordAttack)
            {
                if (_attackTimeoutDelta >= 0.0f)
                {
                    _attackTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // reset the attack timeout timer
                    _attackTimeoutDelta = playerControllerPublicProperties.AttackTimeout;

                    // if we are not attacking, do not play attack animation
                    _input.attackDown = false;
                    isSwordAttack = false;
                    // update animator if using character
                    if (_animator)
                    {
                        // _animator.SetBool(_animIDAttackDown, false);
                        _animator.SetBool("AttackDown", false);

                    }
                }
            }
        }


        private void CheckIfEnemyOnFront()
        {
            // Define the forward direction and the maximum distance of the raycast
            Vector3 forward = transform.TransformDirection(Vector3.forward);

            // Debug.DrawRay(transform.position, forward * maxDistance, Color.red);

            // Perform the raycast
            RaycastHit hit;
            if (Physics.Raycast(transform.position, forward, out hit, maxDistance))
            {
                // Check if the hit GameObject has the "Enemy" tag and is within a 45-degree field of view
                if (hit.collider.gameObject.name.Contains("Skeleton") && Vector3.Angle(transform.forward, hit.transform.position - transform.position) < 45f)
                {

                    var skeletonAvatar = hit.collider.gameObject.GetComponent<SkeletonSword>();

                    // Check if the SkeletonAvatar GameObject was found
                    if (skeletonAvatar != null)
                    {
                        Type skeletonControllerType = skeletonAvatar.skeletonController.GetType();

                        // Get the MethodInfo object for the GetHit method
                        MethodInfo getHitMethod = skeletonControllerType.GetMethod("GetHit");

                        // Check if the GetHit method exists
                        if (getHitMethod != null)
                        {
                            Debug.Log("The GetHit method exists in the skeletonController.");
                            getHitMethod.Invoke(skeletonAvatar.skeletonController, new object[] { this.transform, true });
                        }
                        else
                        {
                            Debug.Log("The GetHit method does not exist in the skeletonController.");
                        }

                        // Get the SkeletonController component
                        Debug.Log("Enemy in front!");

                    }
                }
            }
        }

        private void ChangeAnimationState(string newAnimation)
        {
            if (_animator && _currentAnimation != newAnimation)
            {
                _currentAnimation = newAnimation;
                float randomTransitionDuration = UnityEngine.Random.Range(0.09f, 0.32f);
                _animator.CrossFadeInFixedTime(newAnimation, randomTransitionDuration);
            }
        }

        private void NoWeaponAttackEnd()
        {
            noWeaponAttack = false;
            ChangeAnimationState("Idle Walk Run Blend");
            canMove = true;
        }


        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDAttack = Animator.StringToHash("Attack");
            _animIDAttackDown = Animator.StringToHash("AttackDown");
            // _animIDAttack = Animator.StringToHash("Attack");
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(
                transform.position.x,
                transform.position.y - playerControllerPublicProperties.GroundedOffset,
                transform.position.z
            );
            playerControllerPublicProperties.Grounded = Physics.CheckSphere(
                spherePosition,
                playerControllerPublicProperties.GroundedRadius,
                playerControllerPublicProperties.GroundLayers,
                QueryTriggerInteraction.Ignore
            );

            // update animator if using character
            if (_animator)
            {
                _animator.SetBool(_animIDGrounded, playerControllerPublicProperties.Grounded);
            }

        }

        // private void UpdateCameraRotation(InputAction.CallbackContext context)
        // {
        //     Vector2 input = context.ReadValue<Vector2>();
        //     //Debug.Log("RavanaPlayerController CameraRotation =>" + input);

        //     if (input.sqrMagnitude >= _threshold && !playerControllerPublicProperties.LockCameraPosition)
        //     {
        //         //Don't multiply mouse input by Time.deltaTime;
        //         float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

        //         _cinemachineTargetYaw += input.x * deltaTimeMultiplier;
        //         _cinemachineTargetPitch += input.y * deltaTimeMultiplier;
        //     }

        //     // clamp our rotations so our values are limited 360 degrees
        //     _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        //     _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, playerControllerPublicProperties.BottomClamp, playerControllerPublicProperties.TopClamp);

        //     // Cinemachine will follow this target
        //     playerControllerPublicProperties.CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + playerControllerPublicProperties.CameraAngleOverride,
        //                        _cinemachineTargetYaw, 0.0f);
        // }

        private void CameraRotation()
        {


            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !playerControllerPublicProperties.LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, playerControllerPublicProperties.BottomClamp, playerControllerPublicProperties.TopClamp);

            // Cinemachine will follow this target
            playerControllerPublicProperties.CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + playerControllerPublicProperties.CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (_animator.GetBool("MagicAttack") || isSwordAttack || _toggleHoldWeapon) return;
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? playerControllerPublicProperties.SprintSpeed : playerControllerPublicProperties.MoveSpeed;

            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * playerControllerPublicProperties.SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * playerControllerPublicProperties.SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;

            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                  _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    playerControllerPublicProperties.RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_animator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }

        private void JumpAndGravity()
        {
            if (playerControllerPublicProperties.Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = playerControllerPublicProperties.FallTimeout;

                // update animator if using character
                if (_animator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(playerControllerPublicProperties.JumpHeight * -2f * playerControllerPublicProperties.Gravity);

                    // update animator if using character
                    if (_animator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = playerControllerPublicProperties.JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_animator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += playerControllerPublicProperties.Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (playerControllerPublicProperties.Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - playerControllerPublicProperties.GroundedOffset, transform.position.z),
                playerControllerPublicProperties.GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && canMove)
            {
                if (playerControllerPublicProperties.FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, playerControllerPublicProperties.FootstepAudioClips.Length);
                    // AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    audioSource.PlayOneShot(playerControllerPublicProperties.FootstepAudioClips[index], playerControllerPublicProperties.FootstepAudioVolume);
                }
            }
            //if (!canMove)
            //{
            //    audioSource.Stop();
            //}
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                // AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
                audioSource.PlayOneShot(playerControllerPublicProperties.LandingAudioClip, playerControllerPublicProperties.FootstepAudioVolume);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Debug.Log("RavanaPlayerController OnTriggerEnter =>" + other.gameObject.name);
        }

        public void TeleportTo(Transform destination)
        {
            // StopAgent();
            this.transform.position = destination.position;
            // EnableAgent();
        }

    }
}