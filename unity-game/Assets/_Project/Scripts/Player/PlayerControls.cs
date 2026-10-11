using UnityEngine;
using UnityEngine.InputSystem;

namespace Ataka
{
    public sealed class PlayerControls : MonoBehaviour
    {
        InputActionMap map;
        public InputAction Move { get; private set; }
        public InputAction MouseLook { get; private set; }
        public InputAction StickLook { get; private set; }
        public InputAction Interact { get; private set; }
        public InputAction Sprint { get; private set; }
        public InputAction Crouch { get; private set; }
        public InputAction Pause { get; private set; }

        void Awake()
        {
            map = new InputActionMap("Player");
            Move = map.AddAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddBinding("<Gamepad>/leftStick");
            MouseLook = map.AddAction("MouseLook", InputActionType.Value, "<Mouse>/delta");
            StickLook = map.AddAction("StickLook", InputActionType.Value, "<Gamepad>/rightStick");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonSouth");
            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Crouch = Button("Crouch", "<Keyboard>/leftCtrl", "<Gamepad>/buttonEast");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
        }

        InputAction Button(string name, string keyboard, string gamepad)
        {
            var action = map.AddAction(name, InputActionType.Button, keyboard);
            action.AddBinding(gamepad);
            return action;
        }

        void OnEnable() => map?.Enable();
        void OnDisable() => map?.Disable();
        void OnDestroy() => map?.Dispose();
    }
}
