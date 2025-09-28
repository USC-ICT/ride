using System;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace Ride.IO
{
    /// <summary>
    /// Represents a wrapper around UnityEngine.KeyCode, designed to provide a strongly-typed abstraction for key input
    /// within the RIDE framework. RideKeyCode simplifies interoperability with Unity's input system while enabling future
    /// input backend replacement or mocking. Static fields are provided to mirror all defined Unity KeyCodes for ergonomic use,
    /// and implicit conversions allow seamless usage with Unity APIs that expect KeyCode.
    /// ref: <a href="https://docs.unity3d.com/ScriptReference/KeyCode.html">UnityEngine.KeyCode</a>.
    /// </summary>
    [Serializable]
    [DebuggerDisplay("{ToString()}")]
    public readonly struct RideKeyCode : IEquatable<RideKeyCode>
    {
        private readonly KeyCode key;

        public RideKeyCode(KeyCode _key)
        {
            key = _key;
        }

        public static readonly int MaxDefinedKeyCode = Enum.GetValues(typeof(KeyCode))
                                                        .Cast<int>()
                                                        .Max() + 1;

        public KeyCode UnityKey => key;

        public bool IsModifier =>
            key == KeyCode.LeftShift || key == KeyCode.RightShift ||
            key == KeyCode.LeftControl || key == KeyCode.RightControl ||
            key == KeyCode.LeftAlt || key == KeyCode.RightAlt ||
            key == KeyCode.LeftCommand || key == KeyCode.RightCommand;

        public bool IsArrow =>
            key == KeyCode.LeftArrow || key == KeyCode.RightArrow ||
            key == KeyCode.UpArrow || key == KeyCode.DownArrow;

        public bool IsMouse =>
            key >= KeyCode.Mouse0 && key <= KeyCode.Mouse6;

        public bool IsFunctionKey =>
            key >= KeyCode.F1 && key <= KeyCode.F15;

        public override string ToString() => key.ToString();

        public bool Equals(RideKeyCode other) => key == other.key;
        public override bool Equals(object obj) => obj is RideKeyCode other && Equals(other);
        public override int GetHashCode() => (int)key;

        public static implicit operator RideKeyCode(KeyCode key) => new RideKeyCode(key);
        public static implicit operator KeyCode(RideKeyCode rideKey) => rideKey.key;

        public static bool operator ==(RideKeyCode left, RideKeyCode right) => left.Equals(right);
        public static bool operator !=(RideKeyCode left, RideKeyCode right) => !left.Equals(right);

        public static readonly RideKeyCode None = new(KeyCode.None);
        public static readonly RideKeyCode Backspace = new(KeyCode.Backspace);
        public static readonly RideKeyCode Tab = new(KeyCode.Tab);
        public static readonly RideKeyCode Clear = new(KeyCode.Clear);
        public static readonly RideKeyCode Return = new(KeyCode.Return);
        public static readonly RideKeyCode Pause = new(KeyCode.Pause);
        public static readonly RideKeyCode Escape = new(KeyCode.Escape);
        public static readonly RideKeyCode Space = new(KeyCode.Space);
        public static readonly RideKeyCode Exclaim = new(KeyCode.Exclaim);
        public static readonly RideKeyCode DoubleQuote = new(KeyCode.DoubleQuote);
        public static readonly RideKeyCode Hash = new(KeyCode.Hash);
        public static readonly RideKeyCode Dollar = new(KeyCode.Dollar);
        public static readonly RideKeyCode Percent = new(KeyCode.Percent);
        public static readonly RideKeyCode Ampersand = new(KeyCode.Ampersand);
        public static readonly RideKeyCode Quote = new(KeyCode.Quote);
        public static readonly RideKeyCode LeftParen = new(KeyCode.LeftParen);
        public static readonly RideKeyCode RightParen = new(KeyCode.RightParen);
        public static readonly RideKeyCode Asterisk = new(KeyCode.Asterisk);
        public static readonly RideKeyCode Plus = new(KeyCode.Plus);
        public static readonly RideKeyCode Comma = new(KeyCode.Comma);
        public static readonly RideKeyCode Minus = new(KeyCode.Minus);
        public static readonly RideKeyCode Period = new(KeyCode.Period);
        public static readonly RideKeyCode Slash = new(KeyCode.Slash);
        public static readonly RideKeyCode Alpha0 = new(KeyCode.Alpha0);
        public static readonly RideKeyCode Alpha1 = new(KeyCode.Alpha1);
        public static readonly RideKeyCode Alpha2 = new(KeyCode.Alpha2);
        public static readonly RideKeyCode Alpha3 = new(KeyCode.Alpha3);
        public static readonly RideKeyCode Alpha4 = new(KeyCode.Alpha4);
        public static readonly RideKeyCode Alpha5 = new(KeyCode.Alpha5);
        public static readonly RideKeyCode Alpha6 = new(KeyCode.Alpha6);
        public static readonly RideKeyCode Alpha7 = new(KeyCode.Alpha7);
        public static readonly RideKeyCode Alpha8 = new(KeyCode.Alpha8);
        public static readonly RideKeyCode Alpha9 = new(KeyCode.Alpha9);
        public static readonly RideKeyCode Colon = new(KeyCode.Colon);
        public static readonly RideKeyCode Semicolon = new(KeyCode.Semicolon);
        public static readonly RideKeyCode Less = new(KeyCode.Less);
        public static readonly RideKeyCode EqualsKey = new(KeyCode.Equals);
        public static readonly RideKeyCode Greater = new(KeyCode.Greater);
        public static readonly RideKeyCode Question = new(KeyCode.Question);
        public static readonly RideKeyCode At = new(KeyCode.At);
        public static readonly RideKeyCode LeftBracket = new(KeyCode.LeftBracket);
        public static readonly RideKeyCode Backslash = new(KeyCode.Backslash);
        public static readonly RideKeyCode RightBracket = new(KeyCode.RightBracket);
        public static readonly RideKeyCode Caret = new(KeyCode.Caret);
        public static readonly RideKeyCode Underscore = new(KeyCode.Underscore);
        public static readonly RideKeyCode BackQuote = new(KeyCode.BackQuote);
        public static readonly RideKeyCode A = new(KeyCode.A);
        public static readonly RideKeyCode B = new(KeyCode.B);
        public static readonly RideKeyCode C = new(KeyCode.C);
        public static readonly RideKeyCode D = new(KeyCode.D);
        public static readonly RideKeyCode E = new(KeyCode.E);
        public static readonly RideKeyCode F = new(KeyCode.F);
        public static readonly RideKeyCode G = new(KeyCode.G);
        public static readonly RideKeyCode H = new(KeyCode.H);
        public static readonly RideKeyCode I = new(KeyCode.I);
        public static readonly RideKeyCode J = new(KeyCode.J);
        public static readonly RideKeyCode K = new(KeyCode.K);
        public static readonly RideKeyCode L = new(KeyCode.L);
        public static readonly RideKeyCode M = new(KeyCode.M);
        public static readonly RideKeyCode N = new(KeyCode.N);
        public static readonly RideKeyCode O = new(KeyCode.O);
        public static readonly RideKeyCode P = new(KeyCode.P);
        public static readonly RideKeyCode Q = new(KeyCode.Q);
        public static readonly RideKeyCode R = new(KeyCode.R);
        public static readonly RideKeyCode S = new(KeyCode.S);
        public static readonly RideKeyCode T = new(KeyCode.T);
        public static readonly RideKeyCode U = new(KeyCode.U);
        public static readonly RideKeyCode V = new(KeyCode.V);
        public static readonly RideKeyCode W = new(KeyCode.W);
        public static readonly RideKeyCode X = new(KeyCode.X);
        public static readonly RideKeyCode Y = new(KeyCode.Y);
        public static readonly RideKeyCode Z = new(KeyCode.Z);
        public static readonly RideKeyCode Delete = new(KeyCode.Delete);
        public static readonly RideKeyCode Keypad0 = new(KeyCode.Keypad0);
        public static readonly RideKeyCode Keypad1 = new(KeyCode.Keypad1);
        public static readonly RideKeyCode Keypad2 = new(KeyCode.Keypad2);
        public static readonly RideKeyCode Keypad3 = new(KeyCode.Keypad3);
        public static readonly RideKeyCode Keypad4 = new(KeyCode.Keypad4);
        public static readonly RideKeyCode Keypad5 = new(KeyCode.Keypad5);
        public static readonly RideKeyCode Keypad6 = new(KeyCode.Keypad6);
        public static readonly RideKeyCode Keypad7 = new(KeyCode.Keypad7);
        public static readonly RideKeyCode Keypad8 = new(KeyCode.Keypad8);
        public static readonly RideKeyCode Keypad9 = new(KeyCode.Keypad9);
        public static readonly RideKeyCode KeypadPeriod = new(KeyCode.KeypadPeriod);
        public static readonly RideKeyCode KeypadDivide = new(KeyCode.KeypadDivide);
        public static readonly RideKeyCode KeypadMultiply = new(KeyCode.KeypadMultiply);
        public static readonly RideKeyCode KeypadMinus = new(KeyCode.KeypadMinus);
        public static readonly RideKeyCode KeypadPlus = new(KeyCode.KeypadPlus);
        public static readonly RideKeyCode KeypadEnter = new(KeyCode.KeypadEnter);
        public static readonly RideKeyCode KeypadEquals = new(KeyCode.KeypadEquals);
        public static readonly RideKeyCode UpArrow = new(KeyCode.UpArrow);
        public static readonly RideKeyCode DownArrow = new(KeyCode.DownArrow);
        public static readonly RideKeyCode RightArrow = new(KeyCode.RightArrow);
        public static readonly RideKeyCode LeftArrow = new(KeyCode.LeftArrow);
        public static readonly RideKeyCode Insert = new(KeyCode.Insert);
        public static readonly RideKeyCode Home = new(KeyCode.Home);
        public static readonly RideKeyCode End = new(KeyCode.End);
        public static readonly RideKeyCode PageUp = new(KeyCode.PageUp);
        public static readonly RideKeyCode PageDown = new(KeyCode.PageDown);
        public static readonly RideKeyCode F1 = new(KeyCode.F1);
        public static readonly RideKeyCode F2 = new(KeyCode.F2);
        public static readonly RideKeyCode F3 = new(KeyCode.F3);
        public static readonly RideKeyCode F4 = new(KeyCode.F4);
        public static readonly RideKeyCode F5 = new(KeyCode.F5);
        public static readonly RideKeyCode F6 = new(KeyCode.F6);
        public static readonly RideKeyCode F7 = new(KeyCode.F7);
        public static readonly RideKeyCode F8 = new(KeyCode.F8);
        public static readonly RideKeyCode F9 = new(KeyCode.F9);
        public static readonly RideKeyCode F10 = new(KeyCode.F10);
        public static readonly RideKeyCode F11 = new(KeyCode.F11);
        public static readonly RideKeyCode F12 = new(KeyCode.F12);
        public static readonly RideKeyCode F13 = new(KeyCode.F13);
        public static readonly RideKeyCode F14 = new(KeyCode.F14);
        public static readonly RideKeyCode F15 = new(KeyCode.F15);
        public static readonly RideKeyCode Numlock = new(KeyCode.Numlock);
        public static readonly RideKeyCode CapsLock = new(KeyCode.CapsLock);
        public static readonly RideKeyCode ScrollLock = new(KeyCode.ScrollLock);
        public static readonly RideKeyCode RightShift = new(KeyCode.RightShift);
        public static readonly RideKeyCode LeftShift = new(KeyCode.LeftShift);
        public static readonly RideKeyCode RightControl = new(KeyCode.RightControl);
        public static readonly RideKeyCode LeftControl = new(KeyCode.LeftControl);
        public static readonly RideKeyCode RightAlt = new(KeyCode.RightAlt);
        public static readonly RideKeyCode LeftAlt = new(KeyCode.LeftAlt);
        public static readonly RideKeyCode Mouse0 = new(KeyCode.Mouse0);
        public static readonly RideKeyCode Mouse1 = new(KeyCode.Mouse1);
        public static readonly RideKeyCode Mouse2 = new(KeyCode.Mouse2);
        public static readonly RideKeyCode Mouse3 = new(KeyCode.Mouse3);
        public static readonly RideKeyCode Mouse4 = new(KeyCode.Mouse4);
        public static readonly RideKeyCode Mouse5 = new(KeyCode.Mouse5);
        public static readonly RideKeyCode Mouse6 = new(KeyCode.Mouse6);
        public static readonly RideKeyCode JoystickButton0 = new(KeyCode.JoystickButton0);
        public static readonly RideKeyCode JoystickButton1 = new(KeyCode.JoystickButton1);
        public static readonly RideKeyCode JoystickButton2 = new(KeyCode.JoystickButton2);
        public static readonly RideKeyCode JoystickButton3 = new(KeyCode.JoystickButton3);
        public static readonly RideKeyCode JoystickButton4 = new(KeyCode.JoystickButton4);
        public static readonly RideKeyCode JoystickButton5 = new(KeyCode.JoystickButton5);
        public static readonly RideKeyCode JoystickButton6 = new(KeyCode.JoystickButton6);
        public static readonly RideKeyCode JoystickButton7 = new(KeyCode.JoystickButton7);
        public static readonly RideKeyCode JoystickButton8 = new(KeyCode.JoystickButton8);
        public static readonly RideKeyCode JoystickButton9 = new(KeyCode.JoystickButton9);
        public static readonly RideKeyCode JoystickButton10 = new(KeyCode.JoystickButton10);
        public static readonly RideKeyCode JoystickButton11 = new(KeyCode.JoystickButton11);
        public static readonly RideKeyCode JoystickButton12 = new(KeyCode.JoystickButton12);
        public static readonly RideKeyCode JoystickButton13 = new(KeyCode.JoystickButton13);
        public static readonly RideKeyCode JoystickButton14 = new(KeyCode.JoystickButton14);
        public static readonly RideKeyCode JoystickButton15 = new(KeyCode.JoystickButton15);
        public static readonly RideKeyCode JoystickButton16 = new(KeyCode.JoystickButton16);
        public static readonly RideKeyCode JoystickButton17 = new(KeyCode.JoystickButton17);
        public static readonly RideKeyCode JoystickButton18 = new(KeyCode.JoystickButton18);
        public static readonly RideKeyCode JoystickButton19 = new(KeyCode.JoystickButton19);
        public static readonly RideKeyCode Joystick1Button0 = new(KeyCode.Joystick1Button0);
        public static readonly RideKeyCode Joystick1Button1 = new(KeyCode.Joystick1Button1);
        public static readonly RideKeyCode Joystick1Button2 = new(KeyCode.Joystick1Button2);
        public static readonly RideKeyCode Joystick1Button3 = new(KeyCode.Joystick1Button3);
        public static readonly RideKeyCode Joystick1Button4 = new(KeyCode.Joystick1Button4);
        public static readonly RideKeyCode Joystick1Button5 = new(KeyCode.Joystick1Button5);
        public static readonly RideKeyCode Joystick1Button6 = new(KeyCode.Joystick1Button6);
        public static readonly RideKeyCode Joystick1Button7 = new(KeyCode.Joystick1Button7);
        public static readonly RideKeyCode Joystick1Button8 = new(KeyCode.Joystick1Button8);
        public static readonly RideKeyCode Joystick1Button9 = new(KeyCode.Joystick1Button9);
        public static readonly RideKeyCode Joystick1Button10 = new(KeyCode.Joystick1Button10);
        public static readonly RideKeyCode Joystick1Button11 = new(KeyCode.Joystick1Button11);
        public static readonly RideKeyCode Joystick1Button12 = new(KeyCode.Joystick1Button12);
        public static readonly RideKeyCode Joystick1Button13 = new(KeyCode.Joystick1Button13);
        public static readonly RideKeyCode Joystick1Button14 = new(KeyCode.Joystick1Button14);
        public static readonly RideKeyCode Joystick1Button15 = new(KeyCode.Joystick1Button15);
        public static readonly RideKeyCode Joystick1Button16 = new(KeyCode.Joystick1Button16);
        public static readonly RideKeyCode Joystick1Button17 = new(KeyCode.Joystick1Button17);
        public static readonly RideKeyCode Joystick1Button18 = new(KeyCode.Joystick1Button18);
        public static readonly RideKeyCode Joystick1Button19 = new(KeyCode.Joystick1Button19);
        public static readonly RideKeyCode Joystick2Button0 = new(KeyCode.Joystick2Button0);
        public static readonly RideKeyCode Joystick2Button1 = new(KeyCode.Joystick2Button1);
        public static readonly RideKeyCode Joystick2Button2 = new(KeyCode.Joystick2Button2);
        public static readonly RideKeyCode Joystick2Button3 = new(KeyCode.Joystick2Button3);
        public static readonly RideKeyCode Joystick2Button4 = new(KeyCode.Joystick2Button4);
        public static readonly RideKeyCode Joystick2Button5 = new(KeyCode.Joystick2Button5);
        public static readonly RideKeyCode Joystick2Button6 = new(KeyCode.Joystick2Button6);
        public static readonly RideKeyCode Joystick2Button7 = new(KeyCode.Joystick2Button7);
        public static readonly RideKeyCode Joystick2Button8 = new(KeyCode.Joystick2Button8);
        public static readonly RideKeyCode Joystick2Button9 = new(KeyCode.Joystick2Button9);
        public static readonly RideKeyCode Joystick2Button10 = new(KeyCode.Joystick2Button10);
        public static readonly RideKeyCode Joystick2Button11 = new(KeyCode.Joystick2Button11);
        public static readonly RideKeyCode Joystick2Button12 = new(KeyCode.Joystick2Button12);
        public static readonly RideKeyCode Joystick2Button13 = new(KeyCode.Joystick2Button13);
        public static readonly RideKeyCode Joystick2Button14 = new(KeyCode.Joystick2Button14);
        public static readonly RideKeyCode Joystick2Button15 = new(KeyCode.Joystick2Button15);
        public static readonly RideKeyCode Joystick2Button16 = new(KeyCode.Joystick2Button16);
        public static readonly RideKeyCode Joystick2Button17 = new(KeyCode.Joystick2Button17);
        public static readonly RideKeyCode Joystick2Button18 = new(KeyCode.Joystick2Button18);
        public static readonly RideKeyCode Joystick2Button19 = new(KeyCode.Joystick2Button19);
        public static readonly RideKeyCode Joystick3Button0 = new(KeyCode.Joystick3Button0);
        public static readonly RideKeyCode Joystick3Button1 = new(KeyCode.Joystick3Button1);
        public static readonly RideKeyCode Joystick3Button2 = new(KeyCode.Joystick3Button2);
        public static readonly RideKeyCode Joystick3Button3 = new(KeyCode.Joystick3Button3);
        public static readonly RideKeyCode Joystick3Button4 = new(KeyCode.Joystick3Button4);
        public static readonly RideKeyCode Joystick3Button5 = new(KeyCode.Joystick3Button5);
        public static readonly RideKeyCode Joystick3Button6 = new(KeyCode.Joystick3Button6);
        public static readonly RideKeyCode Joystick3Button7 = new(KeyCode.Joystick3Button7);
        public static readonly RideKeyCode Joystick3Button8 = new(KeyCode.Joystick3Button8);
        public static readonly RideKeyCode Joystick3Button9 = new(KeyCode.Joystick3Button9);
        public static readonly RideKeyCode Joystick3Button10 = new(KeyCode.Joystick3Button10);
        public static readonly RideKeyCode Joystick3Button11 = new(KeyCode.Joystick3Button11);
        public static readonly RideKeyCode Joystick3Button12 = new(KeyCode.Joystick3Button12);
        public static readonly RideKeyCode Joystick3Button13 = new(KeyCode.Joystick3Button13);
        public static readonly RideKeyCode Joystick3Button14 = new(KeyCode.Joystick3Button14);
        public static readonly RideKeyCode Joystick3Button15 = new(KeyCode.Joystick3Button15);
        public static readonly RideKeyCode Joystick3Button16 = new(KeyCode.Joystick3Button16);
        public static readonly RideKeyCode Joystick3Button17 = new(KeyCode.Joystick3Button17);
        public static readonly RideKeyCode Joystick3Button18 = new(KeyCode.Joystick3Button18);
        public static readonly RideKeyCode Joystick3Button19 = new(KeyCode.Joystick3Button19);
        public static readonly RideKeyCode Joystick4Button0 = new(KeyCode.Joystick4Button0);
        public static readonly RideKeyCode Joystick4Button1 = new(KeyCode.Joystick4Button1);
        public static readonly RideKeyCode Joystick4Button2 = new(KeyCode.Joystick4Button2);
        public static readonly RideKeyCode Joystick4Button3 = new(KeyCode.Joystick4Button3);
        public static readonly RideKeyCode Joystick4Button4 = new(KeyCode.Joystick4Button4);
        public static readonly RideKeyCode Joystick4Button5 = new(KeyCode.Joystick4Button5);
        public static readonly RideKeyCode Joystick4Button6 = new(KeyCode.Joystick4Button6);
        public static readonly RideKeyCode Joystick4Button7 = new(KeyCode.Joystick4Button7);
        public static readonly RideKeyCode Joystick4Button8 = new(KeyCode.Joystick4Button8);
        public static readonly RideKeyCode Joystick4Button9 = new(KeyCode.Joystick4Button9);
        public static readonly RideKeyCode Joystick4Button10 = new(KeyCode.Joystick4Button10);
        public static readonly RideKeyCode Joystick4Button11 = new(KeyCode.Joystick4Button11);
        public static readonly RideKeyCode Joystick4Button12 = new(KeyCode.Joystick4Button12);
        public static readonly RideKeyCode Joystick4Button13 = new(KeyCode.Joystick4Button13);
        public static readonly RideKeyCode Joystick4Button14 = new(KeyCode.Joystick4Button14);
        public static readonly RideKeyCode Joystick4Button15 = new(KeyCode.Joystick4Button15);
        public static readonly RideKeyCode Joystick4Button16 = new(KeyCode.Joystick4Button16);
        public static readonly RideKeyCode Joystick4Button17 = new(KeyCode.Joystick4Button17);
        public static readonly RideKeyCode Joystick4Button18 = new(KeyCode.Joystick4Button18);
        public static readonly RideKeyCode Joystick4Button19 = new(KeyCode.Joystick4Button19);
        public static readonly RideKeyCode Joystick5Button0 = new(KeyCode.Joystick5Button0);
        public static readonly RideKeyCode Joystick5Button1 = new(KeyCode.Joystick5Button1);
        public static readonly RideKeyCode Joystick5Button2 = new(KeyCode.Joystick5Button2);
        public static readonly RideKeyCode Joystick5Button3 = new(KeyCode.Joystick5Button3);
        public static readonly RideKeyCode Joystick5Button4 = new(KeyCode.Joystick5Button4);
        public static readonly RideKeyCode Joystick5Button5 = new(KeyCode.Joystick5Button5);
        public static readonly RideKeyCode Joystick5Button6 = new(KeyCode.Joystick5Button6);
        public static readonly RideKeyCode Joystick5Button7 = new(KeyCode.Joystick5Button7);
        public static readonly RideKeyCode Joystick5Button8 = new(KeyCode.Joystick5Button8);
        public static readonly RideKeyCode Joystick5Button9 = new(KeyCode.Joystick5Button9);
        public static readonly RideKeyCode Joystick5Button10 = new(KeyCode.Joystick5Button10);
        public static readonly RideKeyCode Joystick5Button11 = new(KeyCode.Joystick5Button11);
        public static readonly RideKeyCode Joystick5Button12 = new(KeyCode.Joystick5Button12);
        public static readonly RideKeyCode Joystick5Button13 = new(KeyCode.Joystick5Button13);
        public static readonly RideKeyCode Joystick5Button14 = new(KeyCode.Joystick5Button14);
        public static readonly RideKeyCode Joystick5Button15 = new(KeyCode.Joystick5Button15);
        public static readonly RideKeyCode Joystick5Button16 = new(KeyCode.Joystick5Button16);
        public static readonly RideKeyCode Joystick5Button17 = new(KeyCode.Joystick5Button17);
        public static readonly RideKeyCode Joystick5Button18 = new(KeyCode.Joystick5Button18);
        public static readonly RideKeyCode Joystick5Button19 = new(KeyCode.Joystick5Button19);
        public static readonly RideKeyCode Joystick6Button0 = new(KeyCode.Joystick6Button0);
        public static readonly RideKeyCode Joystick6Button1 = new(KeyCode.Joystick6Button1);
        public static readonly RideKeyCode Joystick6Button2 = new(KeyCode.Joystick6Button2);
        public static readonly RideKeyCode Joystick6Button3 = new(KeyCode.Joystick6Button3);
        public static readonly RideKeyCode Joystick6Button4 = new(KeyCode.Joystick6Button4);
        public static readonly RideKeyCode Joystick6Button5 = new(KeyCode.Joystick6Button5);
        public static readonly RideKeyCode Joystick6Button6 = new(KeyCode.Joystick6Button6);
        public static readonly RideKeyCode Joystick6Button7 = new(KeyCode.Joystick6Button7);
        public static readonly RideKeyCode Joystick6Button8 = new(KeyCode.Joystick6Button8);
        public static readonly RideKeyCode Joystick6Button9 = new(KeyCode.Joystick6Button9);
        public static readonly RideKeyCode Joystick6Button10 = new(KeyCode.Joystick6Button10);
        public static readonly RideKeyCode Joystick6Button11 = new(KeyCode.Joystick6Button11);
        public static readonly RideKeyCode Joystick6Button12 = new(KeyCode.Joystick6Button12);
        public static readonly RideKeyCode Joystick6Button13 = new(KeyCode.Joystick6Button13);
        public static readonly RideKeyCode Joystick6Button14 = new(KeyCode.Joystick6Button14);
        public static readonly RideKeyCode Joystick6Button15 = new(KeyCode.Joystick6Button15);
        public static readonly RideKeyCode Joystick6Button16 = new(KeyCode.Joystick6Button16);
        public static readonly RideKeyCode Joystick6Button17 = new(KeyCode.Joystick6Button17);
        public static readonly RideKeyCode Joystick6Button18 = new(KeyCode.Joystick6Button18);
        public static readonly RideKeyCode Joystick6Button19 = new(KeyCode.Joystick6Button19);
        public static readonly RideKeyCode Joystick7Button0 = new(KeyCode.Joystick7Button0);
        public static readonly RideKeyCode Joystick7Button1 = new(KeyCode.Joystick7Button1);
        public static readonly RideKeyCode Joystick7Button2 = new(KeyCode.Joystick7Button2);
        public static readonly RideKeyCode Joystick7Button3 = new(KeyCode.Joystick7Button3);
        public static readonly RideKeyCode Joystick7Button4 = new(KeyCode.Joystick7Button4);
        public static readonly RideKeyCode Joystick7Button5 = new(KeyCode.Joystick7Button5);
        public static readonly RideKeyCode Joystick7Button6 = new(KeyCode.Joystick7Button6);
        public static readonly RideKeyCode Joystick7Button7 = new(KeyCode.Joystick7Button7);
        public static readonly RideKeyCode Joystick7Button8 = new(KeyCode.Joystick7Button8);
        public static readonly RideKeyCode Joystick7Button9 = new(KeyCode.Joystick7Button9);
        public static readonly RideKeyCode Joystick7Button10 = new(KeyCode.Joystick7Button10);
        public static readonly RideKeyCode Joystick7Button11 = new(KeyCode.Joystick7Button11);
        public static readonly RideKeyCode Joystick7Button12 = new(KeyCode.Joystick7Button12);
        public static readonly RideKeyCode Joystick7Button13 = new(KeyCode.Joystick7Button13);
        public static readonly RideKeyCode Joystick7Button14 = new(KeyCode.Joystick7Button14);
        public static readonly RideKeyCode Joystick7Button15 = new(KeyCode.Joystick7Button15);
        public static readonly RideKeyCode Joystick7Button16 = new(KeyCode.Joystick7Button16);
        public static readonly RideKeyCode Joystick7Button17 = new(KeyCode.Joystick7Button17);
        public static readonly RideKeyCode Joystick7Button18 = new(KeyCode.Joystick7Button18);
        public static readonly RideKeyCode Joystick7Button19 = new(KeyCode.Joystick7Button19);
        public static readonly RideKeyCode Joystick8Button0 = new(KeyCode.Joystick8Button0);
        public static readonly RideKeyCode Joystick8Button1 = new(KeyCode.Joystick8Button1);
        public static readonly RideKeyCode Joystick8Button2 = new(KeyCode.Joystick8Button2);
        public static readonly RideKeyCode Joystick8Button3 = new(KeyCode.Joystick8Button3);
        public static readonly RideKeyCode Joystick8Button4 = new(KeyCode.Joystick8Button4);
        public static readonly RideKeyCode Joystick8Button5 = new(KeyCode.Joystick8Button5);
        public static readonly RideKeyCode Joystick8Button6 = new(KeyCode.Joystick8Button6);
        public static readonly RideKeyCode Joystick8Button7 = new(KeyCode.Joystick8Button7);
        public static readonly RideKeyCode Joystick8Button8 = new(KeyCode.Joystick8Button8);
        public static readonly RideKeyCode Joystick8Button9 = new(KeyCode.Joystick8Button9);
        public static readonly RideKeyCode Joystick8Button10 = new(KeyCode.Joystick8Button10);
        public static readonly RideKeyCode Joystick8Button11 = new(KeyCode.Joystick8Button11);
        public static readonly RideKeyCode Joystick8Button12 = new(KeyCode.Joystick8Button12);
        public static readonly RideKeyCode Joystick8Button13 = new(KeyCode.Joystick8Button13);
        public static readonly RideKeyCode Joystick8Button14 = new(KeyCode.Joystick8Button14);
        public static readonly RideKeyCode Joystick8Button15 = new(KeyCode.Joystick8Button15);
        public static readonly RideKeyCode Joystick8Button16 = new(KeyCode.Joystick8Button16);
        public static readonly RideKeyCode Joystick8Button17 = new(KeyCode.Joystick8Button17);
        public static readonly RideKeyCode Joystick8Button18 = new(KeyCode.Joystick8Button18);
        public static readonly RideKeyCode Joystick8Button19 = new(KeyCode.Joystick8Button19);
    }
}
