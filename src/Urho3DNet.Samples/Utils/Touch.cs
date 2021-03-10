using System;

namespace Urho3DNet.Samples
{
    /// Mobile framework for Android/iOS
    /// Gamepad from NinjaSnowWar
    /// Touches patterns:
    /// - 1 finger touch  = pick object through raycast
    /// - 1 or 2 fingers drag  = rotate camera
    /// - 2 fingers sliding in opposite direction (up/down) = zoom in/out
    /// 
    /// Setup:
    /// - Call the update function 'UpdateTouches()' from HandleUpdate or equivalent update handler function
    internal class Touch : Object
    {
        public const float CAMERA_MIN_DIST = 1.0f;
        public const float CAMERA_INITIAL_DIST = 5.0f;
        public const float CAMERA_MAX_DIST = 20.0f;
        public const float GYROSCOPE_THRESHOLD = 0.1f;

        public Touch(Context context, float touchSensitivity) : base(context)
        {
            TouchSensitivity = touchSensitivity;
        }

        /// Touch sensitivity.
        public float TouchSensitivity { get; set; }

        /// Current camera zoom distance.
        public float CameraDistance { get; set; } = CAMERA_INITIAL_DIST;

        /// Zoom flag.
        public bool Zoom { get; set; }

        /// Gyroscope on/off flag.
        public bool UseGyroscope { get; set; }

        public void UpdateTouches(Controls controls) // Called from HandleUpdate
        {
            Zoom = false; // reset bool

            // Zoom in/out
            if (Context.Input.NumTouches == 2)
            {
                var touch1 = Context.Input.GetTouch(0);
                var touch2 = Context.Input.GetTouch(1);

                // Check for zoom pattern (touches moving in opposite directions and on empty space)
                if (touch1.TouchedElement == null && touch2.TouchedElement == null &&
                    (touch1.Delta.Y > 0 && touch2.Delta.Y < 0 || touch1.Delta.Y < 0 && touch2.Delta.Y > 0))
                    Zoom = true;
                else
                    Zoom = false;

                if (Zoom)
                {
                    var sens = 0;
                    // Check for zoom direction (in/out)
                    if (Math.Abs(touch1.Position.Y - touch2.Position.Y) >
                        Math.Abs(touch1.LastPosition.Y - touch2.LastPosition.Y))
                        sens = -1;
                    else
                        sens = 1;
                    CameraDistance += Math.Abs(touch1.Delta.Y - touch2.Delta.Y) * sens * TouchSensitivity / 50.0f;
                    CameraDistance =
                        MathDefs.Clamp(CameraDistance, CAMERA_MIN_DIST,
                            CAMERA_MAX_DIST); // Restrict zoom range to [1;20]
                }
            }

            // Gyroscope (emulated by SDL through a virtual joystick)
            if (UseGyroscope && Context.Input.NumJoysticks > 0) // numJoysticks = 1 on iOS & Android
            {
                var joystick = Context.Input.GetJoystickByIndex(0);
                if (joystick.NumAxes >= 2)
                {
                    if (joystick.GetAxisPosition(0) < -GYROSCOPE_THRESHOLD)
                        controls.Set(Character.CTRL_LEFT, true);
                    if (joystick.GetAxisPosition(0) > GYROSCOPE_THRESHOLD)
                        controls.Set(Character.CTRL_RIGHT, true);
                    if (joystick.GetAxisPosition(1) < -GYROSCOPE_THRESHOLD)
                        controls.Set(Character.CTRL_FORWARD, true);
                    if (joystick.GetAxisPosition(1) > GYROSCOPE_THRESHOLD)
                        controls.Set(Character.CTRL_BACK, true);
                }
            }
        }
    }
}