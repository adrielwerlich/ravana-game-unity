using System;
using UnityEngine;
using System.Collections;
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
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [SerializeField] private AudioClip attackAudioClip;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to attack again. Set to 0f to instantly attack again")]
        public float AttackTimeout = .5f;
        public float MagicAttackTimeout = .8f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

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

        [SerializeField] private AudioSource audioSource;

        public bool canMove = true;
        public bool noWeaponAttack = false;

        [SerializeField] private GameObject greenSpell;
        [SerializeField] private GameObject blueSpell;
        [SerializeField] private GameObject[] spells;
        [SerializeField] private GameObject swordEquiped;
        [SerializeField] private GameObject swordUnequiped;

        [SerializeField] private AudioClip magicSpellAudioClip;

        private LevelController levelController;

        private PlayerScoreEvolutionController playerScoreController;


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


            ravanaInputActions = new RavanaInputActions();

            ravanaInputActions.Ravana.MissionMessageConfirm.performed += ctx =>
            {
                EnterKeyPressed?.Invoke();
            };

            ravanaInputActions.Ravana.MagicAttack.performed += ctx => MagicAttack();

            ravanaInputActions.Ravana.Pause.performed += ctx => TogglePauseGame();
            ravanaInputActions.Ravana.GoToMainMenu.performed += ctx => GoToMainMenu();
            ravanaInputActions.Ravana.ToggleHoldWeapon.performed += ctx => ToggleHoldWeapon();



            swordUnequiped.SetActive(false);
        }

        private void OnEnable()
        {
            ravanaInputActions.Enable();
        }

        private void OnDisable()
        {
            ravanaInputActions.Ravana.MagicAttack.performed -= ctx => MagicAttack();
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
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

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
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _attackTimeoutDelta = AttackTimeout;
            _magicAttackTimeoutDelta = MagicAttackTimeout;

            levelController = GameObject.Find("LevelController").GetComponent<LevelController>();

            playerScoreController = this.gameObject.GetComponent<PlayerScoreEvolutionController>();

        }

        private void Update()
        {
            // _animator = TryGetComponent(out _animator);

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

        private bool isMagicAttack = false;

        private void MagicAttack()
        {
            if (levelController.currentLevel > 1)
            {
                isMagicAttack = true;
            }
            //Debug.Log("RavanaPlayerController MagicAttack ");
        }

        public bool isSwordAttack = false;
        private bool magicAnimationOn = false;
        private void Attack()
        {

            if (isMagicAttack && !magicAnimationOn)
            {
                if (_animator)
                {
                    _animator.SetBool("MagicAttack", true);
                    //isMagicAttack = true;
                    magicAnimationOn = true;
                    audioSource.clip = magicSpellAudioClip;
                    audioSource.pitch = 2.0f;
                    audioSource.Play();

                    // GameObject spellInstance = Instantiate(spells[1], transform.position + transform.forward + Vector3.up, transform.rotation);
                    GameObject spellInstance = Instantiate(spells[Random.Range(0, spells.Length)], transform.position + transform.forward + Vector3.up, transform.rotation);
                    // GameObject spellInstance = Instantiate(greenSpell, transform.position + transform.forward + Vector3.up, transform.rotation);
                    spellInstance.gameObject.SetActive(true);

                    playerScoreController.ReduceScore(Random.Range(1, 3));
                }
            }

            if (isMagicAttack)
            {
                if (_magicAttackTimeoutDelta >= 0.0f)
                {
                    _magicAttackTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // reset the attack timeout timer
                    _magicAttackTimeoutDelta = MagicAttackTimeout;

                    // if we are not attacking, do not play attack animation
                    //_input.magicAttack = false;
                    isMagicAttack = false;
                    magicAnimationOn = false;
                    // update animator if using character
                    if (_animator)
                    {
                        _animator.SetBool("MagicAttack", false);
                    }
                }
            }

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
                    _attackTimeoutDelta = AttackTimeout;

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
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_animator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }

        }

        private void UpdateCameraRotation(InputAction.CallbackContext context)
        {
            Vector2 input = context.ReadValue<Vector2>();
            //Debug.Log("RavanaPlayerController CameraRotation =>" + input);

            if (input.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += input.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += input.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                               _cinemachineTargetYaw, 0.0f);
        }

        private void CameraRotation()
        {


            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            if (isMagicAttack || isSwordAttack || _toggleHoldWeapon) return;
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;

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
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
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
                    RotationSmoothTime);

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


        //private void Move(InputAction.CallbackContext context)
        //{
        //    if (isMagicAttack || isSwordAttack) return;

        //        Vector2 inputMove = context.ReadValue<Vector2>();

        //    // set target speed based on move speed, sprint speed and if sprint is pressed
        //    float targetSpeed = isSprint ? SprintSpeed : MoveSpeed;

        //    // if there is no input, set the target speed to 0
        //    if (inputMove == Vector2.zero) targetSpeed = 0.0f;

        //    if (inputMove != Vector2.zero)
        //    {
        //        _targetRotation = Mathf.Atan2(inputMove.x, inputMove.y) * Mathf.Rad2Deg;
        //        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
        //                    RotationSmoothTime);

        //        // rotate to face input direction relative to camera position
        //        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        //    }

        //    float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        //    float speedOffset = 0.1f;
        //    float inputMagnitude = inputMove.magnitude; // : 1f;

        //    // accelerate or decelerate to target speed
        //    if (currentHorizontalSpeed < targetSpeed - speedOffset ||
        //        currentHorizontalSpeed > targetSpeed + speedOffset)
        //    {
        //        _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
        //            Time.deltaTime * SpeedChangeRate);

        //        _speed = Mathf.Round(_speed * 1000f) / 1000f;
        //    }
        //    else
        //    {
        //        _speed = targetSpeed;
        //    }

        //    _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        //    if (_animationBlend < 0.01f) _animationBlend = 0f;

        //    // normalise input direction
        //    Vector3 inputDirection = new Vector3(inputMove.x, 0.0f, inputMove.y).normalized;

        //    // if there is a move input rotate player when the player is moving
        //    if (inputMove != Vector2.zero)
        //    {
        //        _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
        //                          _mainCamera.transform.eulerAngles.y;
        //        float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
        //            RotationSmoothTime);

        //        // rotate to face input direction relative to camera position
        //        transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        //    }

        //    Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        //    // move the player
        //    _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
        //                     new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        //    // update animator if using character
        //    if (_animator)
        //    {
        //        _animator.SetFloat(_animIDSpeed, _animationBlend);
        //        _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        //    }
        //}

        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

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
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

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
                _jumpTimeoutDelta = JumpTimeout;

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
                _verticalVelocity += Gravity * Time.deltaTime;
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

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && canMove)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = UnityEngine.Random.Range(0, FootstepAudioClips.Length);
                    // AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                    audioSource.PlayOneShot(FootstepAudioClips[index], FootstepAudioVolume);
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
                audioSource.PlayOneShot(LandingAudioClip, FootstepAudioVolume);
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