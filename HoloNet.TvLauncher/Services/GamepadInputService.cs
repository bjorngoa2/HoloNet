using System.Runtime.InteropServices;
using HoloNet.TvLauncher.Configuration;
using Microsoft.Extensions.Options;
using SharpDX.DirectInput;

namespace HoloNet.TvLauncher.Services;

public enum GamepadButton
{
    Up,
    Down,
    Left,
    Right,
    Confirm,
    Cancel,
    Refresh
}

public interface IGamepadService : IDisposable
{
    event EventHandler<GamepadButton>? ButtonPressed;

    /// <summary>
    /// Must be called once the main window's native handle exists (e.g. in its Loaded event)
    /// and before <see cref="Start"/>, so DirectInput devices can be acquired in background
    /// cooperative mode.
    /// </summary>
    void AttachWindowHandle(IntPtr windowHandle);

    void Start();

    void Stop();
}

/// <summary>
/// Polls for gamepad input from two sources so both Xbox-style pads and PlayStation
/// (DualShock/DualSense) pads work out of the box, with no extra drivers:
///
/// 1. XInput (via <c>xinput1_4.dll</c>, ships with Windows) — used automatically for any
///    Xbox-compatible controller.
/// 2. DirectInput (via SharpDX.DirectInput) — Windows exposes PS4/PS5 controllers (and most
///    other HID gamepads) as DirectInput devices, not XInput, so this is required for them.
///
/// XInput is checked first each poll; if no XInput controller is connected, the first
/// attached DirectInput game controller is polled instead. Button indices for DirectInput
/// were determined empirically for a DualSense controller (Cross=1, Circle=2, Options=9,
/// matching the common PS4/PS5 DirectInput HID report layout) and are configurable via
/// <see cref="TvLauncherOptions.DirectInputButtonMappings"/> in case a different pad numbers
/// them differently.
/// </summary>
public sealed class GamepadInputService : IGamepadService
{
    private const int ErrorSuccess = 0;
    private const int MaxXInputControllers = 4;
    private const int DirectInputReinitIntervalPolls = 20; // ~2s at the default 100ms poll rate

    #region XInput interop

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint dwPacketNumber;
        public XInputGamepad Gamepad;
    }

    [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
    private static extern int XInputGetState(int dwUserIndex, out XInputState pState);

    [Flags]
    private enum XInputButtons : ushort
    {
        DPadUp = 0x0001,
        DPadDown = 0x0002,
        DPadLeft = 0x0004,
        DPadRight = 0x0008,
        Start = 0x0010,
        Back = 0x0020,
        A = 0x1000,
        B = 0x2000
    }

    #endregion

    private readonly TvLauncherOptions _options;
    private readonly System.Windows.Threading.DispatcherTimer _timer;

    private IntPtr _windowHandle;
    private DirectInput? _directInput;
    private Joystick? _directInputJoystick;
    private int _pollsSinceDirectInputInitAttempt;

    private ushort _previousXInputButtons;
    private bool _previousXInputStickUp;
    private bool _previousXInputStickDown;
    private bool _previousXInputStickLeft;
    private bool _previousXInputStickRight;

    private bool[] _previousDirectInputButtons = [];
    private int _previousDirectInputPov = -1;
    private bool _previousDirectInputStickUp;
    private bool _previousDirectInputStickDown;
    private bool _previousDirectInputStickLeft;
    private bool _previousDirectInputStickRight;

    public event EventHandler<GamepadButton>? ButtonPressed;

    public GamepadInputService(IOptions<TvLauncherOptions> options)
    {
        _options = options.Value;
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(_options.GamepadPollIntervalMs, 16))
        };
        _timer.Tick += (_, _) => Poll();
    }

    public void AttachWindowHandle(IntPtr windowHandle) => _windowHandle = windowHandle;

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    private void Poll()
    {
        if (TryPollXInput())
            return;

        TryPollDirectInput();
    }

    #region XInput polling

    private bool TryPollXInput()
    {
        for (var i = 0; i < MaxXInputControllers; i++)
        {
            if (XInputGetState(i, out var state) != ErrorSuccess)
                continue;

            HandleXInputState(state.Gamepad);
            return true;
        }

        return false;
    }

    private void HandleXInputState(XInputGamepad gamepad)
    {
        var pressedNow = (XInputButtons)gamepad.wButtons;
        var pressedBefore = (XInputButtons)_previousXInputButtons;

        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.DPadUp), pressedBefore.HasFlag(XInputButtons.DPadUp), GamepadButton.Up);
        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.DPadDown), pressedBefore.HasFlag(XInputButtons.DPadDown), GamepadButton.Down);
        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.DPadLeft), pressedBefore.HasFlag(XInputButtons.DPadLeft), GamepadButton.Left);
        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.DPadRight), pressedBefore.HasFlag(XInputButtons.DPadRight), GamepadButton.Right);
        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.A), pressedBefore.HasFlag(XInputButtons.A), GamepadButton.Confirm);
        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.B), pressedBefore.HasFlag(XInputButtons.B), GamepadButton.Cancel);
        RaiseOnRisingEdge(pressedNow.HasFlag(XInputButtons.Start), pressedBefore.HasFlag(XInputButtons.Start), GamepadButton.Refresh);

        _previousXInputButtons = gamepad.wButtons;

        var deadzone = _options.GamepadStickDeadzone;
        var normalizedX = gamepad.sThumbLX / 32767.0;
        var normalizedY = gamepad.sThumbLY / 32767.0;

        RaiseOnStickEdge(normalizedY > deadzone, ref _previousXInputStickUp, GamepadButton.Up);
        RaiseOnStickEdge(normalizedY < -deadzone, ref _previousXInputStickDown, GamepadButton.Down);
        RaiseOnStickEdge(normalizedX < -deadzone, ref _previousXInputStickLeft, GamepadButton.Left);
        RaiseOnStickEdge(normalizedX > deadzone, ref _previousXInputStickRight, GamepadButton.Right);
    }

    #endregion

    #region DirectInput polling

    private void TryPollDirectInput()
    {
        if (_directInputJoystick is null && !TryInitializeDirectInput())
            return;

        try
        {
            _directInputJoystick!.Poll();
            var state = _directInputJoystick.GetCurrentState();
            HandleDirectInputState(state);
        }
        catch (SharpDX.SharpDXException)
        {
            // The device was unplugged or lost — drop it and try to re-acquire on a later poll.
            _directInputJoystick?.Dispose();
            _directInputJoystick = null;
        }
    }

    private bool TryInitializeDirectInput()
    {
        // Re-enumerating/creating a DirectInput device is relatively expensive, so only retry
        // periodically rather than on every single poll tick while no controller is present.
        if (_pollsSinceDirectInputInitAttempt++ % DirectInputReinitIntervalPolls != 0)
            return false;

        try
        {
            _directInput ??= new DirectInput();

            var devices = _directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
            if (devices.Count == 0)
                return false;

            var joystick = new Joystick(_directInput, devices[0].InstanceGuid);

            if (_windowHandle != IntPtr.Zero)
                joystick.SetCooperativeLevel(_windowHandle, CooperativeLevel.NonExclusive | CooperativeLevel.Background);

            joystick.Acquire();

            // Guard against a known DirectInput quirk where the very first read after Acquire()
            // can report a stale/phantom "pressed" bit before real HID reports start arriving —
            // seed the baseline from an actual poll so the first real Poll() only reports genuine
            // edges, not a false transition from the assumed all-false starting state.
            joystick.Poll();
            var initialState = joystick.GetCurrentState();

            _directInputJoystick = joystick;
            _previousDirectInputButtons = (bool[])initialState.Buttons.Clone();
            _previousDirectInputPov = initialState.PointOfViewControllers.Length > 0
                ? initialState.PointOfViewControllers[0]
                : -1;
            return true;
        }
        catch (SharpDX.SharpDXException)
        {
            return false;
        }
    }

    private void HandleDirectInputState(JoystickState state)
    {
        HandleDirectInputPov(state.PointOfViewControllers.Length > 0 ? state.PointOfViewControllers[0] : -1);
        HandleDirectInputButtons(state.Buttons);
        HandleDirectInputStick(state.X, state.Y);
    }

    private void HandleDirectInputPov(int pov)
    {
        // Standard hat-switch encoding: hundredths of a degree, 0=Up, 9000=Right, 18000=Down,
        // 27000=Left, -1=centered. A small tolerance allows for slightly worn/imprecise pads.
        const int tolerance = 4000;

        var wasUp = IsNearAngle(_previousDirectInputPov, 0, tolerance);
        var wasRight = IsNearAngle(_previousDirectInputPov, 9000, tolerance);
        var wasDown = IsNearAngle(_previousDirectInputPov, 18000, tolerance);
        var wasLeft = IsNearAngle(_previousDirectInputPov, 27000, tolerance);

        RaiseOnRisingEdge(IsNearAngle(pov, 0, tolerance), wasUp, GamepadButton.Up);
        RaiseOnRisingEdge(IsNearAngle(pov, 9000, tolerance), wasRight, GamepadButton.Right);
        RaiseOnRisingEdge(IsNearAngle(pov, 18000, tolerance), wasDown, GamepadButton.Down);
        RaiseOnRisingEdge(IsNearAngle(pov, 27000, tolerance), wasLeft, GamepadButton.Left);

        _previousDirectInputPov = pov;
    }

    private static bool IsNearAngle(int pov, int targetDegreesHundredths, int tolerance)
        => pov >= 0 && Math.Abs(pov - targetDegreesHundredths) <= tolerance;

    private void HandleDirectInputButtons(bool[] buttons)
    {
        var mappings = _options.DirectInputButtonMappings;

        RaiseButtonIfConfigured(buttons, mappings, "Confirm", GamepadButton.Confirm);
        RaiseButtonIfConfigured(buttons, mappings, "Cancel", GamepadButton.Cancel);
        RaiseButtonIfConfigured(buttons, mappings, "Refresh", GamepadButton.Refresh);

        _previousDirectInputButtons = (bool[])buttons.Clone();
    }

    private void RaiseButtonIfConfigured(bool[] buttons, IReadOnlyDictionary<string, int> mappings, string key, GamepadButton button)
    {
        if (!mappings.TryGetValue(key, out var index) || index < 0 || index >= buttons.Length)
            return;

        var wasPressed = index < _previousDirectInputButtons.Length && _previousDirectInputButtons[index];
        RaiseOnRisingEdge(buttons[index], wasPressed, button);
    }

    private void HandleDirectInputStick(int x, int y)
    {
        var deadzone = _options.GamepadStickDeadzone;
        var normalizedX = (x - 32767) / 32767.0;
        var normalizedY = (32767 - y) / 32767.0; // DirectInput Y grows downward; invert so "up" is positive.

        RaiseOnStickEdge(normalizedY > deadzone, ref _previousDirectInputStickUp, GamepadButton.Up);
        RaiseOnStickEdge(normalizedY < -deadzone, ref _previousDirectInputStickDown, GamepadButton.Down);
        RaiseOnStickEdge(normalizedX < -deadzone, ref _previousDirectInputStickLeft, GamepadButton.Left);
        RaiseOnStickEdge(normalizedX > deadzone, ref _previousDirectInputStickRight, GamepadButton.Right);
    }

    #endregion

    private void RaiseOnRisingEdge(bool isPressedNow, bool wasPressed, GamepadButton button)
    {
        if (isPressedNow && !wasPressed)
            ButtonPressed?.Invoke(this, button);
    }

    private void RaiseOnStickEdge(bool isPressedNow, ref bool wasPressed, GamepadButton button)
    {
        if (isPressedNow && !wasPressed)
            ButtonPressed?.Invoke(this, button);

        wasPressed = isPressedNow;
    }

    public void Dispose()
    {
        _timer.Stop();
        _directInputJoystick?.Dispose();
        _directInput?.Dispose();
    }
}
