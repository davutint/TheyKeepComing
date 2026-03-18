using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using System;
using System.Collections;

//Please note: this is a general purpose script used for all my assets. It is not recomended to be used in any other project. 
//For a more generic, general purpose character controller, please see the script "GenericTopDownController.cs" in the scripts folder.
namespace SmallScaleInc.CharacterCreatorFantasy
{
    [RequireComponent(typeof(Animator))]
    public class TopDownCharacterController : MonoBehaviour
    {
        public enum MovementMode
        {
            RelativeToMouse,   // WASD relative to mouse
            ClickToMove,       // click to send to point
            Cardinal,          // WASD world axes, face toward mouse
            WASDOnly           // WASD controls only, no mouse
        }

        private GearAnimatorSync[] gearSyncs;

        [Header("Movement")]
        public MovementMode movementMode = MovementMode.RelativeToMouse;
        public float walkSpeed = 2f;
        public float runSpeed  = 4f;

        [Header("Attack Mode")]
        public bool isMelee  = true;
        public bool isRanged = false;

        [Header("Idle Variants")]
        public bool Idle2 = false;
        public bool Idle3 = false;
        public bool Idle4 = false;

        [Header("States")]
        public bool isCrouching = false;
        public bool isMounted   = false;

        [Header("Playback Speed")]
        public bool Speed1x = true;
        public bool Speed2x = false;

        [Header("Playback Speed Toggles (UI)")]
        public Toggle speed1xUIToggle;
        public Toggle speed2xUIToggle;

        // trigger‐forwarding event
        public event Action<string> OnTriggerFired;

        Animator animator;
        Camera   mainCamera;

        // for click‐to‐move:
        private Vector3 clickTarget;
        private bool    hasClickTarget;
        private Vector2 lastFacingDir = Vector2.right;

        private static bool IsKeyPressed(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].isPressed;
        }

        private static bool WasKeyPressed(Key key)
        {
            return Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        }

        private static bool IsMouseButtonPressed(int button)
        {
            if (Mouse.current == null) return false;
            return button switch
            {
                0 => Mouse.current.leftButton.isPressed,
                1 => Mouse.current.rightButton.isPressed,
                2 => Mouse.current.middleButton.isPressed,
                _ => false
            };
        }

        private static bool WasMouseButtonPressed(int button)
        {
            if (Mouse.current == null) return false;
            return button switch
            {
                0 => Mouse.current.leftButton.wasPressedThisFrame,
                1 => Mouse.current.rightButton.wasPressedThisFrame,
                2 => Mouse.current.middleButton.wasPressedThisFrame,
                _ => false
            };
        }

        private static Vector2 GetMouseScreenPosition()
        {
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }


        void Awake()
        {
            animator   = GetComponent<Animator>();
            mainCamera = Camera.main;
            gearSyncs = GetComponentsInChildren<GearAnimatorSync>(true);

        }

    void Update()
    {
        // --- 1) Mouse world position and direction ---
        Vector3 mouseWorld = mainCamera.ScreenToWorldPoint(GetMouseScreenPosition());
        mouseWorld.z = transform.position.z;
        Vector2 mouseDir = (mouseWorld - transform.position).normalized;

        // --- 2) WASD key input ---
        bool w = IsKeyPressed(Key.W);
        bool s = IsKeyPressed(Key.S);
        bool a = IsKeyPressed(Key.A);
        bool d = IsKeyPressed(Key.D);
        bool walkHold = IsKeyPressed(Key.LeftCtrl);

        // --- 3) Handle click-to-move target selection ---
        if (movementMode == MovementMode.ClickToMove)
        {
            if (!EventSystem.current.IsPointerOverGameObject() && IsMouseButtonPressed(0))
            {
                clickTarget = mouseWorld;
                hasClickTarget = true;
            }
        }
        else
        {
            if (w || s || a || d)
                hasClickTarget = false;
        }

        // --- 4) Build movement vector based on mode ---
        Vector2 move = Vector2.zero;
        Vector2 forwardDir = Vector2.right; // temporary fallback

        switch (movementMode)
        {
            case MovementMode.RelativeToMouse:
                forwardDir = mouseDir;
                Vector2 rightDirRel = new Vector2(forwardDir.y, -forwardDir.x);
                if (w) move += forwardDir;
                if (s) move -= forwardDir;
                if (a) move -= rightDirRel;
                if (d) move += rightDirRel;
                break;

            case MovementMode.ClickToMove:
                if (hasClickTarget)
                {
                    Vector2 toT = (Vector2)(clickTarget - transform.position);
                    if (toT.magnitude < 0.1f)
                        hasClickTarget = false;
                    else
                        move = toT.normalized;
                }
                break;

            case MovementMode.Cardinal:
            case MovementMode.WASDOnly: // Shared movement logic
                if (w) move += Vector2.up;
                if (s) move += Vector2.down;
                if (a) move += Vector2.left;
                if (d) move += Vector2.right;
                break;
        }

        move = move.normalized;

        // --- 5) Determine forwardDir based on mode ---
        if (movementMode == MovementMode.ClickToMove)
        {
            if (hasClickTarget)
            {
                Vector2 toTarget = (Vector2)(clickTarget - transform.position);
                forwardDir = toTarget.normalized;
                lastFacingDir = forwardDir;
            }
            else
            {
                forwardDir = lastFacingDir;
            }
        }
        else if (movementMode == MovementMode.WASDOnly)
        {
            forwardDir = move.sqrMagnitude > 0.01f ? move.normalized : lastFacingDir;
            if (move.sqrMagnitude > 0.01f)
                lastFacingDir = forwardDir;
        }
        else
        {
            forwardDir = mouseDir;
            lastFacingDir = forwardDir;
        }

        // --- 6) Calculate right direction ---
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);

        // --- 7) Move the character ---
        float speed = walkHold ? walkSpeed : runSpeed;
        transform.position += (Vector3)(move * speed * Time.deltaTime);

        // --- Handle crouch key input (hold C to crouch) ---
        if (!isMounted)
        {
            isCrouching = IsKeyPressed(Key.C);

            // QUICK FIX: ensure we're on base Idle before crouching
            if (isCrouching)
            {
                Idle2 = false;
                Idle3 = false;
                Idle4 = false;
                animator.SetBool("UseIdle2", false);
                animator.SetBool("UseIdle3", false);
                animator.SetBool("UseIdle4", false);
            }
        }



        // --- 8) Set animator direction and state ---
        int dirIdx = GetDirectionIndex(forwardDir);
        animator.SetFloat("Direction", dirIdx);
        animator.SetInteger("DirIndex", dirIdx);
        animator.SetBool("IsCrouching", isCrouching);
        animator.SetBool("IsMounted", isMounted);

        // --- 9) Idle variant booleans ---
        bool useIdle2 = false, useIdle3 = false, useIdle4 = false;
        if (!isCrouching && !isMounted)
        {
            if      (Idle4) useIdle4 = true;
            else if (Idle3) useIdle3 = true;
            else if (Idle2) useIdle2 = true;
        }
        animator.SetBool("UseIdle2", useIdle2);
        animator.SetBool("UseIdle3", useIdle3);
        animator.SetBool("UseIdle4", useIdle4);

        // --- 10) Movement direction flags for animation ---
        bool isMoving = move.sqrMagnitude > 0.01f;
        float dotF = Vector2.Dot(move, forwardDir);
        float dotB = Vector2.Dot(move, -forwardDir);
        float dotR = Vector2.Dot(move, rightDir);
        float dotL = Vector2.Dot(move, -rightDir);
        const float TH = 0.5f;

        bool mF = isMoving && dotF > TH;
        bool mB = isMoving && dotB > TH;
        bool mR = isMoving && dotR > TH;
        bool mL = isMoving && dotL > TH;

        animator.SetBool("IsRun",          mF && !walkHold);
        animator.SetBool("IsWalk",         mF &&  walkHold);
        animator.SetBool("IsRunBackwards", mB);
        animator.SetBool("IsStrafeRight",   mL);
        animator.SetBool("IsStrafeLeft",  mR);

        // --- 11) Attack triggers (unchanged) ---
        if (WasMouseButtonPressed(1))
        {
            string trig = isMoving
                ? (isRanged ? "AttackRun2" : "AttackRun")
                : (isRanged ? "Attack3"    : "Attack1");
            animator.SetTrigger(trig);
            OnTriggerFired?.Invoke(trig);
        }

        if (WasKeyPressed(Key.Digit1)) { animator.SetTrigger("Attack2"); OnTriggerFired?.Invoke("Attack2"); }
        if (WasKeyPressed(Key.Digit2)) { animator.SetTrigger("Attack1"); OnTriggerFired?.Invoke("Attack1"); }
        if (WasKeyPressed(Key.Digit3)) { animator.SetTrigger("Attack4"); OnTriggerFired?.Invoke("Attack4"); }
        if (WasKeyPressed(Key.Digit4)) { animator.SetTrigger("Attack5"); OnTriggerFired?.Invoke("Attack5"); }
        if (WasKeyPressed(Key.Digit5)) { animator.SetTrigger("Special1"); OnTriggerFired?.Invoke("Special1"); }
        if (WasKeyPressed(Key.Digit6)) { animator.SetTrigger("Special2"); OnTriggerFired?.Invoke("Special2"); }
        if (WasKeyPressed(Key.Digit7)) { animator.SetTrigger("Taunt");    OnTriggerFired?.Invoke("Taunt"); }
        if (WasKeyPressed(Key.Digit8)) { animator.SetTrigger("Die");      OnTriggerFired?.Invoke("Die"); }
        if (WasKeyPressed(Key.Digit9)) { animator.SetTrigger("TakeDamage");OnTriggerFired?.Invoke("TakeDamage"); }

        // --- 12) Playback speed ---
        if (speed1xUIToggle != null && speed2xUIToggle != null)
        {
            // Read UI
            Speed1x = speed1xUIToggle.isOn;
            Speed2x = speed2xUIToggle.isOn;

            // Enforce only one can be true
            if (Speed1x && speed2xUIToggle.isOn)
                speed2xUIToggle.isOn = false;
            if (Speed2x && speed1xUIToggle.isOn)
                speed1xUIToggle.isOn = false;
        }

        // Push into Animator
        animator.SetBool("Speed1x", Speed1x);
        animator.SetBool("Speed2x", Speed2x);

        // Adjust the actual playback
        animator.speed = Speed2x ? 2f : 1f;
        foreach (var gear in gearSyncs)
        gear.SyncFromPlayer(animator);
    }


        int GetDirectionIndex(Vector2 d)
        {
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            if      (angle >= 337.5f || angle <  22.5f) return 0;  // E
            if      (angle <   67.5f)                   return 4;  // NE
            if      (angle <  112.5f)                   return 3;  // N
            if      (angle <  157.5f)                   return 5;  // NW
            if      (angle <  202.5f)                   return 1;  // W
            if      (angle <  247.5f)                   return 7;  // SW
            if      (angle <  292.5f)                   return 2;  // S
                                                        return 6;  // SE
        }


        /// <summary>
        /// Call this from *any* weapon‐toggle’s OnValueChanged.
        /// Pass `true` (via the Static bool field) if it’s a ranged toggle,
        /// or `false` if it’s a non-ranged toggle.
        /// </summary>
        public void OnWeaponToggleChanged(bool isRangedWeapon)
        {
            isRanged = isRangedWeapon;
            isMelee  = !isRangedWeapon;
        }

        /// <summary>
        /// Call this to switch your Idle variant.
        /// Pass 1 for the base Idle, 2 for Idle2, 3 for Idle3, 4 for Idle4.
        /// </summary>
        public void SwitchIdleVariant(int variant)
        {
            // stop any in‐flight switch
            StopCoroutine(nameof(_SwitchIdleCoroutine));
            StartCoroutine(_SwitchIdleCoroutine(variant));
        }

        private IEnumerator _SwitchIdleCoroutine(int variant)
        {
            // 1) Clear all at once (this will force the Animator back to the default Idle)
            Idle2 = Idle3 = Idle4 = false;
            animator.SetBool("UseIdle2", false);
            animator.SetBool("UseIdle3", false);
            animator.SetBool("UseIdle4", false);

            // 2) Wait one frame so the Animator actually sees “all false”
            yield return null;

            // 3) Now turn on the one you want
            switch (variant)
            {
                case 1:
                    // default Idle — nothing more to do
                    break;
                case 2:
                    Idle2 = true;
                    animator.SetBool("UseIdle2", true);
                    break;
                case 3:
                    Idle3 = true;
                    animator.SetBool("UseIdle3", true);
                    break;
                case 4:
                    Idle4 = true;
                    animator.SetBool("UseIdle4", true);
                    break;
            }
        }

        // these go on each Toggle’s OnValueChanged(Boolean) UnityEvent:
        public void OnIdle1Toggled(bool isOn)
        {
            if (!isOn) return;
            SwitchIdleVariant(1);
        }
        public void OnIdle2Toggled(bool isOn)
        {
            if (!isOn) return;
            SwitchIdleVariant(2);
        }
        public void OnIdle3Toggled(bool isOn)
        {
            if (!isOn) return;
            SwitchIdleVariant(3);
        }
        public void OnIdle4Toggled(bool isOn)
        {
            if (!isOn) return;
            SwitchIdleVariant(4);
        }

        /// <summary>
        /// Call this from your “mount” Toggle’s OnValueChanged(Boolean) event.
        /// Pass `true` when you want to enter mounted state, `false` to dismount.
        /// </summary>
        public void OnMountToggled(bool isOn)
        {
            isMounted = isOn;

            // QUICK FIX: force Idle variant off before mounting
            if (isMounted)
            {
                Idle2 = false;
                Idle3 = false;
                Idle4 = false;
                animator.SetBool("UseIdle2", false);
                animator.SetBool("UseIdle3", false);
                animator.SetBool("UseIdle4", false);
            }
        }


        public void OnMovementModeDropdownChanged(int value)
        {
            switch (value)
            {
                case 0:
                    movementMode = MovementMode.RelativeToMouse;
                    break;
                case 1:
                    movementMode = MovementMode.ClickToMove;
                    break;
                case 2:
                    movementMode = MovementMode.Cardinal;
                    break;
                case 3:
                    movementMode = MovementMode.WASDOnly;
                    break;
                default:
                    Debug.LogWarning("Invalid movement mode index from dropdown.");
                    break;
            }
        }

    }
}
