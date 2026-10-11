using UnityEngine;

namespace Ataka
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerControls))]
    public sealed class FirstPersonController : MonoBehaviour
    {
        public Camera view;
        public float sensitivity = .085f;
        public float pitch;
        public bool crouched;
        public bool inputEnabled;
        public bool invertY;
        CharacterController body;
        PlayerControls controls;
        float verticalSpeed;
        int ignoreLookFrames;
        readonly Collider[] overlaps = new Collider[16];

        public void Initialize()
        {
            body = GetComponent<CharacterController>();
            controls = GetComponent<PlayerControls>();
        }

        void Awake() => Initialize();
        public void SuppressLook() { ignoreLookFrames = 2; }

        public bool CanStand()
        {
            var feet = transform.position;
            int count = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * .26f,
                feet + Vector3.up * 1.44f, .245f, overlaps, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!overlaps[i].transform.IsChildOf(transform)) return false;
            return count < overlaps.Length;
        }

        void Update()
        {
            if (!inputEnabled || Time.timeScale == 0) return;
            if (controls.Crouch.WasPressedThisFrame() && (!crouched || CanStand())) crouched = !crouched;
            float targetHeight = crouched ? 1.08f : 1.7f;
            body.height = targetHeight;
            body.center = new Vector3(0, targetHeight / 2, 0);
            var eye = view.transform.localPosition;
            eye.y = Mathf.Lerp(eye.y, crouched ? .94f : 1.55f, 1 - Mathf.Exp(-14 * Time.deltaTime));
            view.transform.localPosition = eye;

            if (ignoreLookFrames > 0) ignoreLookFrames--;
            else if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 mouse = controls.MouseLook.ReadValue<Vector2>();
                // Discard focus/warp spikes, without making mouse speed depend on FPS.
                if (mouse.sqrMagnitude > 36000000) mouse = Vector2.zero;
                var look = mouse * sensitivity + controls.StickLook.ReadValue<Vector2>() * (120 * Time.deltaTime);
                transform.Rotate(0, look.x, 0, Space.Self);
                pitch = Mathf.Clamp(pitch - look.y * (invertY ? -1 : 1), -82, 82);
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            var axis = Vector2.ClampMagnitude(controls.Move.ReadValue<Vector2>(), 1);
            var movement = transform.right * axis.x + transform.forward * axis.y;
            float speed = crouched ? 1.15f : controls.Sprint.IsPressed() ? 3.5f : 2;
            verticalSpeed = body.isGrounded ? -2 : Mathf.Max(verticalSpeed - 20 * Time.deltaTime, -20);
            body.Move((movement * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
        }

        public void Restore(Vector3 position, float yaw, float savedPitch, bool savedCrouch)
        {
            body.enabled = false;
            transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            pitch = Mathf.Clamp(savedPitch, -82, 82);
            crouched = savedCrouch;
            body.height = crouched ? 1.08f : 1.7f;
            body.center = Vector3.up * (body.height / 2);
            view.transform.localPosition = Vector3.up * (crouched ? .94f : 1.55f);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            verticalSpeed = 0;
            body.enabled = true;
            SuppressLook();
        }
    }
}
