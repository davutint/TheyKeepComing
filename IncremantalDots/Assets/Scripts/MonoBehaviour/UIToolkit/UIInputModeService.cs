using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadWalls
{
    public enum UIInputMode
    {
        Pointer,
        Touch,
        Gamepad
    }

    /// <summary>
    /// Son anlamli girdiyi izler. Player-facing UI Toolkit ekranlari bu tek kaynaktan
    /// hit-area, focus ve cihaz ipucu siniflarini alir.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class UIInputModeService : MonoBehaviour
    {
        public event Action<UIInputMode> ModeChanged;

        public UIInputMode CurrentMode { get; private set; }

        private void Awake()
        {
            CurrentMode = Application.isMobilePlatform && Touchscreen.current != null
                ? UIInputMode.Touch
                : UIInputMode.Pointer;
        }

        private void Update()
        {
            if (HasTouchInput())
            {
                SetMode(UIInputMode.Touch);
                return;
            }

            if (HasGamepadInput())
            {
                SetMode(UIInputMode.Gamepad);
                return;
            }

            if (HasPointerOrKeyboardInput())
                SetMode(UIInputMode.Pointer);
        }

        private static bool HasTouchInput()
        {
            Touchscreen touch = Touchscreen.current;
            return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
        }

        private static bool HasGamepadInput()
        {
            Gamepad pad = Gamepad.current;
            if (pad == null)
                return false;

            return pad.buttonSouth.wasPressedThisFrame
                || pad.buttonEast.wasPressedThisFrame
                || pad.buttonWest.wasPressedThisFrame
                || pad.buttonNorth.wasPressedThisFrame
                || pad.leftShoulder.wasPressedThisFrame
                || pad.rightShoulder.wasPressedThisFrame
                || pad.leftStickButton.wasPressedThisFrame
                || pad.rightStickButton.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame
                || pad.selectButton.wasPressedThisFrame
                || pad.dpad.ReadValue().sqrMagnitude > 0.25f
                || pad.leftStick.ReadValue().sqrMagnitude > 0.36f
                || pad.rightStick.ReadValue().sqrMagnitude > 0.36f
                || pad.leftTrigger.ReadValue() > 0.55f
                || pad.rightTrigger.ReadValue() > 0.55f;
        }

        private static bool HasPointerOrKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                return true;

            Mouse mouse = Mouse.current;
            if (mouse == null)
                return false;

            return mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame
                || mouse.delta.ReadValue().sqrMagnitude > 0.5f
                || mouse.scroll.ReadValue().sqrMagnitude > 0.5f;
        }

        private void SetMode(UIInputMode mode)
        {
            if (CurrentMode == mode)
                return;

            CurrentMode = mode;
            ModeChanged?.Invoke(mode);
        }
    }
}

