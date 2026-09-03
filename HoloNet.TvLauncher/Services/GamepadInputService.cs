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
    Refresh,
    Quit
}

/// <summary>
/// Which physical controller family is currently being polled, so the UI can show matching
/// button-prompt text/labels (e.g. "A" vs "Cross") instead of hardcoding one brand.
/// </summary>
public enum GamepadKind
{
    /// <summary>An Xbox-style pad polled via XInput.</summary>
    Xbox,

    /// <summary>A PlayStation (DualShock/DualSense) or other HID pad polled via DirectInput.</summary>
    PlayStation
}

public interface IGamepadService : IDisposable
{
    event EventHandler<GamepadButton>? ButtonPressed;

    /// <summary>
    /// Raised whenever <see cref="CurrentControllerKind"/> changes — e.g. a DualSense connects
    /// after an Xbox pad was active, or vice versa — so the UI can refresh its button-prompt
    /// text to match whichever pad is actually being read right now.
    /// </summary>
    event EventHandler<GamepadKind>? ControllerKindChanged;

    /// <summary>
    /// Which controller family is currently being polled. Defaults to <see cref="GamepadKind.PlayStation"/>
    /// before any input has been read yet, since that's this app's primary supported pad.
    /// </summary>
    GamepadKind CurrentControllerKind { get; }

    /// <summary>
    /// Must be called once the main window's native handle exists (e.g. in its Loaded event)
    /// and before <see cref="Start"/>, so DirectInput devices can be acquired in background
    /// cooperative mode.
    /// </summary>
    void AttachWindowHandle(IntPtr windowHandle);

    /// <summary>
    /// Forwards a WPF window message (via <c>HwndSource.AddHook</c> on the host window) so the
    /// Raw Input quit-combo fallback — see <see cref="RawInputQuitComboListener"/> — can see
    /// <c>WM_INPUT</c> messages. Harmless to call with any other message; only <c>WM_INPUT</c>
    /// is acted on. Call <see cref="AttachWindowHandle"/> first so Raw Input registration has
    /// already happened before messages start arriving.
    /// </summary>
    void ProcessWindowMessage(int msg, IntPtr lParam);

    void Start();

    void Stop();
}

/// <summary>
/// Tracks a button combo that must be held continuously for a configured duration before
/// firing once — used for the "quit current game" combo so it can't be triggered by a
/// single accidental press, and won't repeat-fire while still held afterwards. Shared between
/// <see cref="GamepadInputService"/>'s XInput/DirectInput polling and
/// <see cref="RawInputQuitComboListener"/>'s independent Raw Input detection path.
/// </summary>
internal sealed class ComboHoldTracker
{
    private DateTime? _heldSince;
    private bool _fired;

    /// <returns><c>true</c> exactly once per hold, the moment the threshold is reached.</returns>
    public bool Evaluate(bool isComboPressed, int thresholdMilliseconds)
    {
        if (!isComboPressed)
        {
            _heldSince = null;
            _fired = false;
            return false;
        }

        _heldSince ??= DateTime.UtcNow;

        if (_fired || (DateTime.UtcNow - _heldSince.Value).TotalMilliseconds < thresholdMilliseconds)
            return false;

        _fired = true;
        return true;
    }
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
///
/// Also detects a "quit current game" combo — Back+Start (XInput) / Share+Options
/// (DirectInput) held together for <see cref="TvLauncherOptions.QuitHoldMilliseconds"/> —
/// raised as <see cref="GamepadButton.Quit"/>. This keeps working even while another
/// application (the emulator) has window focus, since XInput polling is global and the
/// DirectInput device is acquired in background/non-exclusive mode. A third, independent
/// path — <see cref="RawInputQuitComboListener"/> — additionally detects just this quit combo
/// via the Windows Raw Input API, since DirectInput can go completely blind to a
/// Bluetooth-connected PS4/PS5 pad while certain emulators (e.g. PCSX2) are running; see the
/// README's "PS4/PS5 controller over Bluetooth + PCSX2" section for the full story.
/// </summary>
public sealed class GamepadInputService : IGamepadService
{
    private const int ErrorSuccess = 0;
    private const int MaxXInputControllers = 4;
    private const int DirectInputReinitIntervalPolls = 5; // ~0.5s at the default 100ms poll rate

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

    private readonly ComboHoldTracker _xInputQuitCombo = new();
    private readonly ComboHoldTracker _directInputQuitCombo = new();
    private readonly RawInputQuitComboListener _rawInputQuitCombo;

    public event EventHandler<GamepadButton>? ButtonPressed;
    public event EventHandler<GamepadKind>? ControllerKindChanged;

    public GamepadKind CurrentControllerKind { get; private set; } = GamepadKind.PlayStation;

    private void SetControllerKind(GamepadKind kind)
    {
        if (CurrentControllerKind == kind)
            return;

        CurrentControllerKind = kind;
        ControllerKindChanged?.Invoke(this, kind);
    }

    public GamepadInputService(IOptions<TvLauncherOptions> options)
    {
        _options = options.Value;
        _timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(Math.Max(_options.GamepadPollIntervalMs, 16))
        };
        _timer.Tick += (_, _) => Poll();
        _rawInputQuitCombo = new RawInputQuitComboListener(
            () => _options.DirectInputButtonMappings,
            () => _options.QuitHoldMilliseconds,
            () => ButtonPressed?.Invoke(this, GamepadButton.Quit));
    }

    public void AttachWindowHandle(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
        _rawInputQuitCombo.Attach(windowHandle);
    }

    public void Start() => _timer.Start();

    public void Stop() => _timer.Stop();

    /// <summary>
    /// Forwards a WPF window message to the Raw Input quit-combo listener — must be wired up by
    /// the host window (e.g. via <c>HwndSource.AddHook</c>) once <see cref="AttachWindowHandle"/>
    /// has registered for Raw Input, so <c>WM_INPUT</c> messages actually reach this service.
    /// </summary>
    public void ProcessWindowMessage(int msg, IntPtr lParam) => _rawInputQuitCombo.HandleWindowMessage(msg, lParam);

    private void Poll()
    {
        if (TryPollXInput())
        {
            SetControllerKind(GamepadKind.Xbox);
            return;
        }

        if (TryPollDirectInput())
            SetControllerKind(GamepadKind.PlayStation);
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

        if (_xInputQuitCombo.Evaluate(
                pressedNow.HasFlag(XInputButtons.Back) && pressedNow.HasFlag(XInputButtons.Start),
                _options.QuitHoldMilliseconds))
        {
            ButtonPressed?.Invoke(this, GamepadButton.Quit);
        }

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

    private bool TryPollDirectInput()
    {
        if (_directInputJoystick is null && !TryInitializeDirectInput())
            return false;

        try
        {
            _directInputJoystick!.Poll();
            var state = _directInputJoystick.GetCurrentState();
            HandleDirectInputState(state);
            return true;
        }
        catch (SharpDX.SharpDXException)
        {
            // The device was unplugged, lost (e.g. Bluetooth sleep/reconnect), or went stale —
            // drop it AND the cached DirectInput object so the next attempt does a fully fresh
            // COM device enumeration rather than reusing a list that may still reference the
            // now-dead instance.
            _directInputJoystick?.Dispose();
            _directInputJoystick = null;
            _directInput?.Dispose();
            _directInput = null;
            _pollsSinceDirectInputInitAttempt = 0;
            return false;
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

            // Prefer the most recently attached device rather than always devices[0] — after a
            // Bluetooth reconnect, Windows can briefly list a stale/ghost instance ahead of the
            // real, currently-live one, so favour the last entry in the enumeration.
            var deviceInfo = devices[^1];
            var joystick = new Joystick(_directInput, deviceInfo.InstanceGuid);

            if (_windowHandle != IntPtr.Zero)
                joystick.SetCooperativeLevel(_windowHandle, CooperativeLevel.NonExclusive | CooperativeLevel.Background);

            joystick.Acquire();

            // Guard against a known DirectInput quirk where the very first read after Acquire()
            // can report a stale/phantom "pressed" bit before real HID reports start arriving —
            // seed the baseline from an actual poll so the first real Poll() only reports genuine
            // edges, not a false transition from the assumed all-false starting state. This also
            // verifies the device actually responds — some stale/ghost instances Acquire() fine
            // but throw immediately on the first real Poll(), which the catch below handles.
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
            // Acquisition or the verification poll failed — drop the DirectInput object too so
            // the next attempt re-enumerates from scratch instead of retrying the same stale list.
            _directInput?.Dispose();
            _directInput = null;
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

        if (_directInputQuitCombo.Evaluate(IsQuitComboPressed(buttons, mappings), _options.QuitHoldMilliseconds))
            ButtonPressed?.Invoke(this, GamepadButton.Quit);

        _previousDirectInputButtons = (bool[])buttons.Clone();
    }

    private static bool IsQuitComboPressed(bool[] buttons, IReadOnlyDictionary<string, List<int>> mappings)
    {
        return TryGetPressed(buttons, mappings, "Share") && TryGetPressed(buttons, mappings, "Refresh");
    }

    private static bool TryGetPressed(bool[] buttons, IReadOnlyDictionary<string, List<int>> mappings, string key)
    {
        if (!mappings.TryGetValue(key, out var indices))
            return false;

        foreach (var index in indices)
        {
            if (index >= 0 && index < buttons.Length && buttons[index])
                return true;
        }

        return false;
    }

    private void RaiseButtonIfConfigured(bool[] buttons, IReadOnlyDictionary<string, List<int>> mappings, string key, GamepadButton button)
    {
        if (!mappings.TryGetValue(key, out var indices))
            return;

        var isPressed = false;
        var wasPressed = false;

        foreach (var index in indices)
        {
            if (index < 0 || index >= buttons.Length)
                continue;

            isPressed |= buttons[index];
            wasPressed |= index < _previousDirectInputButtons.Length && _previousDirectInputButtons[index];
        }

        RaiseOnRisingEdge(isPressed, wasPressed, button);
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
        _rawInputQuitCombo.Dispose();
    }
}
