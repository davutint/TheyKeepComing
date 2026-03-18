using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using System;
using System.Collections;

namespace SmallScaleInc.CharacterCreatorFantasy
{

    [RequireComponent(typeof(Animator))]
    public class GenericTopDownController2 : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// How the player’s movement is interpreted in world space.
        /// </summary>
        public enum MovementMode
        {
            /// <summary>
            /// WASD movement relative to where the mouse points.
            /// </summary>
            RelativeToMouse,

            /// <summary>
            /// Click anywhere (that isn’t UI) and the character moves toward that point.
            /// </summary>
            ClickToMove,

            /// <summary>
            /// WASD moves along cardinal axes (up/down/left/right), but character always faces the mouse.
            /// </summary>
            Cardinal,

            /// <summary>
            /// WASD moves along world axes, and facing also follows WASD direction (no mouse influence).
            /// </summary>
            WASDOnly
        }

        /// <summary>
        /// Basic attack style. Impacts which trigger names get sent to Animator.
        /// </summary>
        public enum AttackMode
        {
            Melee,
            Ranged
        }

        /// <summary>
        /// Which idle variant to play when standing still (1 = base idle; 2/3/4 map to the corresponding Animator bools).
        /// </summary>
        public enum IdleVariant
        {
            Idle1 = 1,
            Idle2 = 2,
            Idle3 = 3,
            Idle4 = 4
        }

        #endregion

        #region Inspector‐Exposed Fields

        [Header("Movement Settings")]
        [Tooltip("Choose how WASD/mouse/click input should be interpreted.")]
        public MovementMode movementMode = MovementMode.RelativeToMouse;

        [Tooltip("Walking speed (units/sec).")]
        public float walkSpeed = 2f;

        [Tooltip("Running speed (units/sec) when holding the Run key or equivalent.")]
        public float runSpeed = 4f;

        [Tooltip("If true, clicking UI elements will NOT trigger click‐to‐move targets.")]
        public bool ignoreUIClicks = true;

        [Tooltip("If true and you have a Rigidbody2D, movement will use physics (velocity) rather than directly setting transform.position.")]
        public bool usePhysicsMovement = false;

        [Tooltip("If usePhysicsMovement is true, the Rigidbody2D component to manipulate.")]
        public Rigidbody2D attachedRigidbody;

        [Header("Key Bindings")]
        [Tooltip("Key used for walking (if held, uses walkSpeed; otherwise runSpeed).")]
        public KeyCode walkKey = KeyCode.LeftControl;

        [Tooltip("Key used for crouching (hold to crouch).")]
        public KeyCode crouchKey = KeyCode.C;


        [Tooltip("Key used for toggling base vs. alternate speed multiplier.")]
        public KeyCode toggleSpeedKey = KeyCode.T;

        [Header("Mounting")]
        [Tooltip("Press this key to toggle mounted/unmounted state.")]
        public KeyCode mountKey = KeyCode.T;

        [Header("Attack Settings")]
        [Tooltip("Key or mouse button for the default (regular) attack.")]
        public KeyCode defaultAttackKey = KeyCode.Mouse1;
        [Tooltip("Select whether the character is Melee or Ranged. Affects which Animator trigger names are used.")]
        public AttackMode attackMode = AttackMode.Melee;

        [Tooltip("Key or mouse button for the primary attack (1 = left‐click by default).")]
        public KeyCode primaryAttackKey = KeyCode.Mouse0;

        [Tooltip("Key or mouse button for the secondary attack (2 = right‐click by default).")]
        public KeyCode secondaryAttackKey = KeyCode.Mouse1;

        [Tooltip("Enable number‐key mappings for Attack2 through Attack9 (1‐9).")]
        public bool enableNumberKeyAttacks = true;

        [Header("Idle Variant")]
        [Tooltip("Which idle variant (1–4) to use when standing still.")]
        public IdleVariant idleVariant = IdleVariant.Idle1;

        [Header("Playback Speed Options")]
        [Tooltip("Base playback speed multiplier (1.0 = normal).")]
        [Range(0.1f, 3f)]
        public float basePlaybackSpeed = 1f;

        [Tooltip("Alternate playback speed multiplier (e.g. 2x speed).")]
        [Range(0.1f, 3f)]
        public float alternatePlaybackSpeed = 2f;

        [Tooltip("If true, pressing the Toggle Speed key will swap between base and alternate playback speeds.")]
        public bool allowPlaybackSpeedToggle = true;

        [Header("States (Read‐Only in Inspector)")]
        [Tooltip("True when crouching (hold crouchKey).")]
        [SerializeField]
        private bool isCrouching = false;
        [Tooltip("True when mounted; toggles each time mountKey is pressed.")]
        [SerializeField]
        private bool isMounted = false;

        [Header("Debug & Events")]
        [Tooltip("If true, will draw a gizmo line from character to current click target (for debugging ClickToMove).")]
        public bool showClickTargetGizmo = false;

        /// <summary>
        /// Event that fires whenever we send an Animator trigger (e.g. any Attack or Special or Die, etc.).
        /// Subscribers can react if they need to know which trigger was fired.
        /// </summary>
        public event Action<string> OnAnimationTriggerSent;

        #endregion

        #region Private/Internal Fields

        private Animator animator;
        private Camera   mainCamera;

        // For ClickToMove logic:
        private Vector3 clickTarget;
        private bool    hasClickTarget = false;

        // Remember last facing direction when movementMode prevents immediate facing updates:
        private Vector2 lastFacingDir = Vector2.right;

        // For playback speed toggle:
        private bool isUsingAlternateSpeed = false;

        #endregion

        private static Vector2 GetMouseScreenPosition()
        {
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        private static bool GetMouseButtonDown(int button)
        {
            return button switch
            {
                0 => GetKeyDown(KeyCode.Mouse0),
                1 => GetKeyDown(KeyCode.Mouse1),
                2 => GetKeyDown(KeyCode.Mouse2),
                3 => GetKeyDown(KeyCode.Mouse3),
                4 => GetKeyDown(KeyCode.Mouse4),
                _ => false
            };
        }

        private static bool GetKey(KeyCode keyCode)
        {
            if (TryGetMouseButton(keyCode, out ButtonControl button))
                return button.isPressed;
            if (TryGetKeyboardKey(keyCode, out Key key))
                return Keyboard.current != null && Keyboard.current[key].isPressed;
            return false;
        }

        private static bool GetKeyDown(KeyCode keyCode)
        {
            if (TryGetMouseButton(keyCode, out ButtonControl button))
                return button.wasPressedThisFrame;
            if (TryGetKeyboardKey(keyCode, out Key key))
                return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
            return false;
        }

        private static bool TryGetMouseButton(KeyCode keyCode, out ButtonControl button)
        {
            button = null;
            if (Mouse.current == null) return false;

            switch (keyCode)
            {
                case KeyCode.Mouse0:
                    button = Mouse.current.leftButton;
                    return true;
                case KeyCode.Mouse1:
                    button = Mouse.current.rightButton;
                    return true;
                case KeyCode.Mouse2:
                    button = Mouse.current.middleButton;
                    return true;
                case KeyCode.Mouse3:
                    button = Mouse.current.backButton;
                    return true;
                case KeyCode.Mouse4:
                    button = Mouse.current.forwardButton;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetKeyboardKey(KeyCode keyCode, out Key key)
        {
            key = Key.None;
            if (keyCode == KeyCode.LeftCommand || keyCode == KeyCode.LeftApple || keyCode == KeyCode.LeftWindows)
            {
                key = Key.LeftMeta;
                return true;
            }
            if (keyCode == KeyCode.RightCommand || keyCode == KeyCode.RightApple || keyCode == KeyCode.RightWindows)
            {
                key = Key.RightMeta;
                return true;
            }
            switch (keyCode)
            {
                case KeyCode.None:
                    return false;
                case KeyCode.LeftControl:
                    key = Key.LeftCtrl;
                    return true;
                case KeyCode.RightControl:
                    key = Key.RightCtrl;
                    return true;
                case KeyCode.LeftShift:
                    key = Key.LeftShift;
                    return true;
                case KeyCode.RightShift:
                    key = Key.RightShift;
                    return true;
                case KeyCode.LeftAlt:
                    key = Key.LeftAlt;
                    return true;
                case KeyCode.RightAlt:
                    key = Key.RightAlt;
                    return true;
                case KeyCode.Alpha0:
                    key = Key.Digit0;
                    return true;
                case KeyCode.Alpha1:
                    key = Key.Digit1;
                    return true;
                case KeyCode.Alpha2:
                    key = Key.Digit2;
                    return true;
                case KeyCode.Alpha3:
                    key = Key.Digit3;
                    return true;
                case KeyCode.Alpha4:
                    key = Key.Digit4;
                    return true;
                case KeyCode.Alpha5:
                    key = Key.Digit5;
                    return true;
                case KeyCode.Alpha6:
                    key = Key.Digit6;
                    return true;
                case KeyCode.Alpha7:
                    key = Key.Digit7;
                    return true;
                case KeyCode.Alpha8:
                    key = Key.Digit8;
                    return true;
                case KeyCode.Alpha9:
                    key = Key.Digit9;
                    return true;
            }

            return Enum.TryParse(keyCode.ToString(), out key);
        }

        #region Unity Callbacks

        private void Awake()
        {
            animator = GetComponent<Animator>();
            mainCamera = Camera.main;

            if (usePhysicsMovement && attachedRigidbody == null)
            {
                attachedRigidbody = GetComponent<Rigidbody2D>();
                if (attachedRigidbody == null)
                    Debug.LogWarning("usePhysicsMovement is true but no Rigidbody2D was assigned or found. Movement will fallback to transform.position.");
            }
        }

    private void Update()
    {
        // 1) Toggle mount and playback‐speed
        HandleMountToggleInput();    // mountKey (T) flips isMounted on/off
        HandleSpeedToggleInput();    // toggleSpeedKey (e.g. T) flips base/alternate speed

        // 2) Mouse world position & direction
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(GetMouseScreenPosition());
        mouseWorld.z = transform.position.z;
        Vector2 mouseDir = (mouseWorld - transform.position).normalized;

        // 3) WASD / arrow keys
        bool w = GetKey(KeyCode.W)    || GetKey(KeyCode.UpArrow);
        bool s = GetKey(KeyCode.S)    || GetKey(KeyCode.DownArrow);
        bool a = GetKey(KeyCode.A)    || GetKey(KeyCode.LeftArrow);
        bool d = GetKey(KeyCode.D)    || GetKey(KeyCode.RightArrow);
        bool walkHold  = GetKey(walkKey);

        // 4) Click-to-move targeting
        if (movementMode == MovementMode.ClickToMove)
        {
            if (GetMouseButtonDown(0) &&
                (!ignoreUIClicks || EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
            {
                clickTarget    = mouseWorld;
                hasClickTarget = true;
            }
        }
        else if (w || s || a || d)
        {
            hasClickTarget = false;
        }

        // 5) Build raw movement vector
        Vector2 rawMove   = Vector2.zero;
        Vector2 forwardDir = Vector2.right;

        switch (movementMode)
        {
            case MovementMode.RelativeToMouse:
                forwardDir = mouseDir;
                Vector2 rightRel = new Vector2(forwardDir.y, -forwardDir.x);
                if (w) rawMove += forwardDir;
                if (s) rawMove -= forwardDir;
                if (a) rawMove -= rightRel;
                if (d) rawMove += rightRel;
                break;

            case MovementMode.ClickToMove:
                if (hasClickTarget)
                {
                    Vector2 toT = ((Vector2)clickTarget - (Vector2)transform.position);
                    if (toT.magnitude < 0.1f) hasClickTarget = false;
                    else                       rawMove = toT.normalized;
                }
                break;

            case MovementMode.Cardinal:
            case MovementMode.WASDOnly:
                if (w) rawMove += Vector2.up;
                if (s) rawMove += Vector2.down;
                if (a) rawMove += Vector2.left;
                if (d) rawMove += Vector2.right;
                break;
        }

        Vector2 moveDir = rawMove.normalized;

        // 6) Determine facing direction
        if (movementMode == MovementMode.ClickToMove)
        {
            if (hasClickTarget)
            {
                forwardDir = ((Vector2)clickTarget - (Vector2)transform.position).normalized;
                lastFacingDir = forwardDir;
            }
            else forwardDir = lastFacingDir;
        }
        else if (movementMode == MovementMode.WASDOnly)
        {
            if (moveDir.sqrMagnitude > 0.01f)
            {
                forwardDir = moveDir;
                lastFacingDir = forwardDir;
            }
            else forwardDir = lastFacingDir;
        }
        else
        {
            forwardDir = mouseDir;
            lastFacingDir = forwardDir;
        }

        // 7) Right‐vector for strafing/backwards
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);

        // 8) Move (physics or direct)
        float speed = walkHold ? walkSpeed : runSpeed;
        if (usePhysicsMovement && attachedRigidbody != null)
            attachedRigidbody.linearVelocity = moveDir * speed;
        else
            transform.position += (Vector3)moveDir * speed * Time.deltaTime;

        // 9) Crouch (hold) — cannot crouch while mounted
        bool newCrouch = GetKey(crouchKey) && !isMounted;
        if (newCrouch && !isCrouching)
            ResetAllIdleVariants();
        isCrouching = newCrouch;

        // 10) Push core states to Animator
        int dirIdx = GetDirectionIndex(forwardDir);
        animator.SetFloat("Direction", dirIdx);
        animator.SetInteger("DirIndex", dirIdx);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsMounted",  isMounted);

        // 11) Idle‐variant bools (only when not crouching/mounted)
        bool useIdle2 = false, useIdle3 = false, useIdle4 = false;
        if (!isCrouching && !isMounted)
        {
            switch (idleVariant)
            {
                case IdleVariant.Idle2: useIdle2 = true; break;
                case IdleVariant.Idle3: useIdle3 = true; break;
                case IdleVariant.Idle4: useIdle4 = true; break;
            }
        }
        animator.SetBool("UseIdle2", useIdle2);
        animator.SetBool("UseIdle3", useIdle3);
        animator.SetBool("UseIdle4", useIdle4);

        // 12) Movement‐direction flags for walk/run/back/strafe
        bool isMoving = moveDir.sqrMagnitude > 0.01f;
        float dotF = Vector2.Dot(moveDir, forwardDir);
        float dotB = Vector2.Dot(moveDir, -forwardDir);
        float dotR = Vector2.Dot(moveDir, rightDir);
        float dotL = Vector2.Dot(moveDir, -rightDir);
        const float TH = 0.5f;

        animator.SetBool("IsRun",          isMoving && dotF > TH && !walkHold);
        animator.SetBool("IsWalk",         isMoving && dotF > TH &&  walkHold);
        animator.SetBool("IsRunBackwards", isMoving && dotB > TH);
        animator.SetBool("IsStrafeLeft",   isMoving && dotL > TH);
        animator.SetBool("IsStrafeRight",  isMoving && dotR > TH);

        // 13) ATTACK INPUT
        // — default attack only on defaultAttackKey (Mouse1)
        if (GetKeyDown(defaultAttackKey))
        {
            string trig = isMoving
                ? (attackMode == AttackMode.Ranged ? "AttackRun2" : "AttackRun")
                : (attackMode == AttackMode.Ranged ? "Attack3"    : "Attack1");
            animator.SetTrigger(trig);
            OnAnimationTriggerSent?.Invoke(trig);
        }

        // — number keys 1–9 exactly as requested
        if (GetKeyDown(KeyCode.Alpha1)) { animator.SetTrigger("Attack2");    OnAnimationTriggerSent?.Invoke("Attack2"); }
        if (GetKeyDown(KeyCode.Alpha2)) { animator.SetTrigger("Attack1");    OnAnimationTriggerSent?.Invoke("Attack1"); }
        if (GetKeyDown(KeyCode.Alpha3)) { animator.SetTrigger("Attack4");    OnAnimationTriggerSent?.Invoke("Attack4"); }
        if (GetKeyDown(KeyCode.Alpha4)) { animator.SetTrigger("Attack5");    OnAnimationTriggerSent?.Invoke("Attack5"); }
        if (GetKeyDown(KeyCode.Alpha5)) { animator.SetTrigger("Special1");   OnAnimationTriggerSent?.Invoke("Special1"); }
        if (GetKeyDown(KeyCode.Alpha6)) { animator.SetTrigger("Special2");   OnAnimationTriggerSent?.Invoke("Special2"); }
        if (GetKeyDown(KeyCode.Alpha7)) { animator.SetTrigger("Taunt");      OnAnimationTriggerSent?.Invoke("Taunt"); }
        if (GetKeyDown(KeyCode.Alpha8)) { animator.SetTrigger("Die");        OnAnimationTriggerSent?.Invoke("Die"); }
        if (GetKeyDown(KeyCode.Alpha9)) { animator.SetTrigger("TakeDamage"); OnAnimationTriggerSent?.Invoke("TakeDamage"); }

        // 14) APPLY PLAYBACK SPEED
        float targetSpeed = isUsingAlternateSpeed ? alternatePlaybackSpeed : basePlaybackSpeed;
        animator.SetBool("Speed1x", Mathf.Approximately(targetSpeed, basePlaybackSpeed));
        animator.SetBool("Speed2x", Mathf.Approximately(targetSpeed, alternatePlaybackSpeed));
        animator.speed = targetSpeed;
    }


        private void OnDrawGizmos()
        {
            if (showClickTargetGizmo && movementMode == MovementMode.ClickToMove && hasClickTarget)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, clickTarget);
                Gizmos.DrawWireSphere(clickTarget, 0.1f);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Handles toggling mount/unmount when mountKey is pressed.
        /// </summary>
        private void HandleMountToggleInput()
        {
            if (GetKeyDown(mountKey))
            {
                isMounted = !isMounted;
                if (isMounted)
                {
                    // If mounting, ensure no idle variants or crouch stay active
                    isCrouching = false;
                    ResetAllIdleVariants();
                }
                animator.SetBool("IsMounted", isMounted);
            }
        }

        /// <summary>
        /// Handles toggling Animator playback speed between base and alternate when toggleSpeedKey is pressed.
        /// </summary>
        private void HandleSpeedToggleInput()
        {
            if (!allowPlaybackSpeedToggle)
                return;

            if (GetKeyDown(toggleSpeedKey))
            {
                isUsingAlternateSpeed = !isUsingAlternateSpeed;
            }
        }

        /// <summary>
        /// Resets all idle‐variant bools in the Animator to false.
        /// Use whenever we forcibly return to the base idle state.
        /// </summary>
        private void ResetAllIdleVariants()
        {
            animator.SetBool("UseIdle2", false);
            animator.SetBool("UseIdle3", false);
            animator.SetBool("UseIdle4", false);
            idleVariant = IdleVariant.Idle1;
        }

        /// <summary>
        /// Checks primary/secondary attack buttons and number keys (if enabled),
        /// then fires the appropriate Animator trigger and raises OnAnimationTriggerSent.
        /// </summary>
        private void HandleAttackInput(bool isMoving)
        {
            // PRIMARY ATTACK (e.g. Mouse0)
            if (GetKeyDown(primaryAttackKey))
            {
                string trig;
                if (isMoving)
                {
                    // If moving: “AttackRun” for Melee; “AttackRun2” for Ranged
                    trig = (attackMode == AttackMode.Ranged) ? "AttackRun2" : "AttackRun";
                }
                else
                {
                    // If standing: “Attack3” for Ranged; “Attack1” for Melee
                    trig = (attackMode == AttackMode.Ranged) ? "Attack3" : "Attack1";
                }
                animator.SetTrigger(trig);
                OnAnimationTriggerSent?.Invoke(trig);
            }

            // SECONDARY ATTACK (e.g. Mouse1) – always uses a different trigger ("Attack2" for Melee, "Attack4" for Ranged), but you can customize further if needed
            if (GetKeyDown(secondaryAttackKey))
            {
                string trig = (attackMode == AttackMode.Ranged) ? "Attack4" : "Attack2";
                animator.SetTrigger(trig);
                OnAnimationTriggerSent?.Invoke(trig);
            }

            // OPTIONAL: Number‐key driven special moves / taunt / die / takeDamage
            if (!enableNumberKeyAttacks)
                return;

            if (GetKeyDown(KeyCode.Alpha3))
            {
                // e.g. “Attack5”
                animator.SetTrigger("Attack5");
                OnAnimationTriggerSent?.Invoke("Attack5");
            }
            if (GetKeyDown(KeyCode.Alpha4))
            {
                animator.SetTrigger("Special1");
                OnAnimationTriggerSent?.Invoke("Special1");
            }
            if (GetKeyDown(KeyCode.Alpha5))
            {
                animator.SetTrigger("Special2");
                OnAnimationTriggerSent?.Invoke("Special2");
            }
            if (GetKeyDown(KeyCode.Alpha6))
            {
                animator.SetTrigger("Taunt");
                OnAnimationTriggerSent?.Invoke("Taunt");
            }
            if (GetKeyDown(KeyCode.Alpha7))
            {
                animator.SetTrigger("Die");
                OnAnimationTriggerSent?.Invoke("Die");
            }
            if (GetKeyDown(KeyCode.Alpha8))
            {
                animator.SetTrigger("TakeDamage");
                OnAnimationTriggerSent?.Invoke("TakeDamage");
            }
            // etc. Extend as needed; customizing in Inspector by toggling enableNumberKeyAttacks = false will disable all these.
        }

        /// <summary>
        /// Converts a normalized 2D direction into one of eight direction indices (0–7).
        /// Matches the same mapping your Animator expects:
        ///   0: East, 1: West, 2: South, 3: North, 4: NE, 5: NW, 6: SE, 7: SW
        /// (Actually order depends on your build, but matches your original GetDirectionIndex logic.)
        /// </summary>
        private int GetDirectionIndex(Vector2 dir)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;

            if (angle >= 337.5f || angle < 22.5f)   return 0; // East
            if (angle < 67.5f)                      return 4; // NE
            if (angle < 112.5f)                     return 3; // North
            if (angle < 157.5f)                     return 5; // NW
            if (angle < 202.5f)                     return 1; // West
            if (angle < 247.5f)                     return 7; // SW
            if (angle < 292.5f)                     return 2; // South
            return 6; // SE
        }

        #endregion

        #region Public API

        /// <summary>
        /// At runtime, you can call this to switch to a different Idle variant (1–4).  
        /// Starts a coroutine that resets all variants to false, waits one frame, then enables the one you chose.
        /// </summary>
        public void SwitchIdleVariant(IdleVariant variant)
        {
            StopCoroutine(nameof(_SwitchIdleCoroutine));
            StartCoroutine(_SwitchIdleCoroutine((int)variant));
        }

        private IEnumerator _SwitchIdleCoroutine(int variant)
        {
            ResetAllIdleVariants();
            yield return null; // wait one frame so Animator sees “all false”

            idleVariant = (IdleVariant)variant;
            switch (variant)
            {
                case 1:
                    // Default idle: nothing else to do
                    break;
                case 2:
                    animator.SetBool("UseIdle2", true);
                    break;
                case 3:
                    animator.SetBool("UseIdle3", true);
                    break;
                case 4:
                    animator.SetBool("UseIdle4", true);
                    break;
            }
        }

        /// <summary>
        /// Call this to change attack mode at runtime (Melee or Ranged).
        /// </summary>
        public void SetAttackMode(AttackMode newMode)
        {
            attackMode = newMode;
        }

        /// <summary>
        /// If you want to explicitly force a crouch on/off from other scripts:
        /// Will also reset idle variants if turning crouch on.
        /// </summary>
        public void ForceCrouch(bool crouchOn)
        {
            if (crouchOn && !isCrouching)
            {
                ResetAllIdleVariants();
            }

            isCrouching = crouchOn;
            animator.SetBool("IsCrouching", isCrouching);
        }

        /// <summary>
        /// If you want to explicitly mount/dismount from other scripts:
        /// </summary>
        public void ForceMount(bool mountOn)
        {
            isMounted = mountOn;
            if (isMounted)
            {
                isCrouching = false;
                ResetAllIdleVariants();
            }
            animator.SetBool("IsMounted", isMounted);
        }

        /// <summary>
        /// Directly set the playback speed multiplier (overrides toggle logic).
        /// </summary>
        public void SetPlaybackSpeed(float speed)
        {
            basePlaybackSpeed = speed;
            isUsingAlternateSpeed = false;
            animator.speed = speed;
        }

        #endregion
    }
}
