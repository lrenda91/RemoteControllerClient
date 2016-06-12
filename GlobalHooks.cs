using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Windows;

namespace Client.Windows {


    #region WINDOWS INPUT APIs
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 28)]
    public struct INPUT
    {
        [FieldOffset(0)]
        public Int32 dwType;
        [FieldOffset(4)]
        public MOUSEINPUT mi;
        [FieldOffset(4)]
        public KEYBDINPUT ki;
        [FieldOffset(4)]
        public HARDWAREINPUT hi;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MOUSEINPUT
    {
        public Int32 dx;
        public Int32 dy;
        public Int32 mouseData;
        public Int32 dwFlags;
        public Int32 time;
        public IntPtr dwExtraInfo;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KEYBDINPUT
    {
        public UInt16 wVk;
        public UInt16 wScan;
        public UInt32 dwFlags;
        public UInt32 time;
        public IntPtr dwExtraInfo;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct HARDWAREINPUT
    {
        public Int32 uMsg;
        public Int16 wParamL;
        public Int16 wParamH;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }
    #endregion

    public enum InputType : int
    {
        Mouse = 0,
        Keyboard = 1,
        Hardware = 2
    }

    public static class Win32
    {

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern UInt32 SendInput(UInt32 numberOfInputs, INPUT[] inputs, Int32 sizeOfInputStructure);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern short GetKeyState(int virtualKey);

    }


    public delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, ref KEYBDINPUT kinput);
    public delegate IntPtr LowLevelProcMouse (int nCode, IntPtr wParam, ref MOUSEINPUT minput);




    public class LowLevelKeyboardListener
    {

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x100;
        const int WM_KEYUP = 0x101;
        const int WM_SYSKEYDOWN = 0x104;
        const int WM_SYSKEYUP = 0x105;

        public const int KEYEVENTF_KEYDOWN = 0;
        public const int KEYEVENTF_EXTENDEDKEY = 1;
        public const int KEYEVENTF_KEYUP = 2;
        public const int KEYEVENTF_UNICODE = 4;
        public const int KEYEVENTF_SCANCODE = 8;

        public static bool IsExtendedKey(VirtualKeyCode keyCode)
        {
            if (keyCode == VirtualKeyCode.MENU ||
                keyCode == VirtualKeyCode.LMENU ||
                keyCode == VirtualKeyCode.RMENU ||
                keyCode == VirtualKeyCode.CONTROL ||
                keyCode == VirtualKeyCode.LCONTROL ||       //aggiunto da me
                keyCode == VirtualKeyCode.RCONTROL ||
                keyCode == VirtualKeyCode.INSERT ||
                keyCode == VirtualKeyCode.DELETE ||
                keyCode == VirtualKeyCode.HOME ||
                keyCode == VirtualKeyCode.END ||
                keyCode == VirtualKeyCode.PRIOR ||
                keyCode == VirtualKeyCode.NEXT ||
                keyCode == VirtualKeyCode.RIGHT ||
                keyCode == VirtualKeyCode.UP ||
                keyCode == VirtualKeyCode.LEFT ||
                keyCode == VirtualKeyCode.DOWN ||
                keyCode == VirtualKeyCode.NUMLOCK ||
                keyCode == VirtualKeyCode.CANCEL ||
                keyCode == VirtualKeyCode.SNAPSHOT ||
                keyCode == VirtualKeyCode.DIVIDE)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref KEYBDINPUT lParam);


        public event EventHandler<INPUT> KeyDown, KeyUp;
        public event EventHandler<INPUT[]> KeyUnicode;
        public event EventHandler<List<VirtualKeyCode>> KeyShortcut;

        private List<VirtualKeyCode> downKeys = new List<VirtualKeyCode>();

        //ascii in numpad
        private bool menuDown = false;
        private List<VirtualKeyCode> numpadDigitsKeys = new List<VirtualKeyCode>();

        private LowLevelProc proc;
        private IntPtr hookPtr = IntPtr.Zero;

        public LowLevelKeyboardListener()
        {
            proc = HookCallback;
        }

        public void HookKeyboard()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                IntPtr procPtr = Marshal.GetFunctionPointerForDelegate(proc);
                IntPtr moduleHandle = Win32.GetModuleHandle(curModule.ModuleName);
                hookPtr = Win32.SetWindowsHookEx(WH_KEYBOARD_LL, procPtr, moduleHandle, 0);
            }
        }

        ~LowLevelKeyboardListener()
        {
            UnHookKeyboard();
        }

        public void UnHookKeyboard()
        {
            if (hookPtr != IntPtr.Zero)
            {
                Win32.UnhookWindowsHookEx(hookPtr);
                hookPtr = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, ref KEYBDINPUT lParam)
        {
            if (nCode >= 0)
            {
                VirtualKeyCode code = (VirtualKeyCode)lParam.wVk;
                INPUT input = new INPUT
                {
                    dwType = (Int32)InputType.Keyboard,
                    ki = lParam
                };                
                int message = (int)wParam;
                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                {
                    if (code == VirtualKeyCode.MENU || code == VirtualKeyCode.LMENU || code == VirtualKeyCode.RMENU)
                    {
                        menuDown = true;
                    }
                    else if (code >= VirtualKeyCode.NUMPAD0 && code <= VirtualKeyCode.NUMPAD9 && menuDown)
                    {
                        if (code != downKeys[downKeys.Count - 1])
                        {
                            numpadDigitsKeys.Add(code);
                        }
                    }
                    else
                    {
                        numpadDigitsKeys.Clear();
                        menuDown = false;
                    }
                    OnKeyDown(input);
                    if (!downKeys.Contains(code))
                    {
                        downKeys.Add(code);
                    }
                }
                else if (message == WM_KEYUP || message == WM_SYSKEYUP)
                {
                    OnKeyUp(input);
                    if (downKeys.Count > 1)
                    {
                        OnKeyShortcut();
                    }
                    if (downKeys.Contains(code))
                    {
                        downKeys.Remove(code);
                    }
                    if (code == VirtualKeyCode.MENU || code == VirtualKeyCode.LMENU || code == VirtualKeyCode.RMENU)
                    {
                        OnUnicodeChar();
                        numpadDigitsKeys.Clear();
                        menuDown = false;
                    }
                }
            }
            int altDown = Win32.GetKeyState((int)VirtualKeyCode.MENU);
            bool isAltDown = altDown == -127 || altDown == -128;
            
            if (isAltDown && lParam.wVk == 9)
            {
                Console.WriteLine("alt tab");
                return new IntPtr(-1);
            }

            int ctrlDown = Win32.GetKeyState((int)VirtualKeyCode.LCONTROL);
            bool isCtrlDown = ctrlDown == -127 || ctrlDown == -128;
            if (isCtrlDown && isAltDown && lParam.wVk == (int) VirtualKeyCode.DELETE)
            {
                Console.WriteLine("ctrl alt canc");
                return new IntPtr(-1);
            }

            if (isAltDown && lParam.wVk ==(int) VirtualKeyCode.F4)
            {
                Console.WriteLine("alt F4");
                return new IntPtr(-1);
            }

            return CallNextHookEx(hookPtr, nCode, wParam, ref lParam);
        }

        private void OnKeyDown(INPUT input)
        {
            if (KeyDown != null)
            {
                VirtualKeyCode keyCode = (VirtualKeyCode)input.ki.wVk;
                input.ki.wScan = 0;
                input.ki.dwFlags = (UInt32) (IsExtendedKey(keyCode) ? KEYEVENTF_EXTENDEDKEY : KEYEVENTF_KEYDOWN);
                input.ki.time = 0;
                input.ki.dwExtraInfo = IntPtr.Zero;
                KeyDown(this, input);
            }
        }

        private void OnKeyUp(INPUT input)
        {
            if (KeyUp != null)
            {
                VirtualKeyCode keyCode = (VirtualKeyCode)input.ki.wVk;
                input.ki.wScan = 0;
                input.ki.dwFlags = (UInt32) (IsExtendedKey(keyCode) ? KEYEVENTF_KEYUP | KEYEVENTF_EXTENDEDKEY : KEYEVENTF_KEYUP);
                input.ki.time = 0;
                input.ki.dwExtraInfo = IntPtr.Zero;
                KeyUp(this, input);
            }
        }

        private void OnKeyShortcut()
        {
            if (KeyShortcut != null)
            {
                List<VirtualKeyCode> copy = new List<VirtualKeyCode>();
                List<VirtualKeyCode> extendedKeys = new List<VirtualKeyCode>();
                foreach (VirtualKeyCode vk in downKeys)
                {
                    copy.Add(vk);
                    if (IsExtendedKey(vk))
                    {
                        extendedKeys.Add(vk);
                    }
                }
                int numberOfExtKeys = extendedKeys.Count;
                //se ho premuto solo tasti speciali o nessun tasto speciale, non fare nulla!!
                //ex: CTRL+ALT oppure A+F
                if (numberOfExtKeys == copy.Count || numberOfExtKeys == 0)
                {
                    return;
                }
                KeyShortcut(this, copy);
            }
        }


        private void OnUnicodeChar()
        {
            if (KeyUnicode != null)
            {
                string s = string.Empty;
                int integerCode;
                foreach (VirtualKeyCode vk in numpadDigitsKeys)
                {
                    s += (vk - VirtualKeyCode.NUMPAD0);
                }
                if (Int32.TryParse(s, out integerCode))
                {
                    Console.WriteLine("UNICODE: code= " + integerCode);

                    KEYBDINPUT down = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)integerCode,
                        dwFlags = KEYEVENTF_UNICODE,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    };

                    KEYBDINPUT up = new KEYBDINPUT
                    {
                        wVk = 0,
                        wScan = (ushort)integerCode,
                        dwFlags = (KEYEVENTF_KEYUP | KEYEVENTF_UNICODE),
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    };

                    if ((integerCode & 0xFF00) == 0xE000)
                    {
                        up.dwFlags |= KEYEVENTF_EXTENDEDKEY;
                        down.dwFlags |= KEYEVENTF_EXTENDEDKEY;
                    }

                    INPUT[] inputs = new INPUT[]{
                        new INPUT {
                            dwType = (Int32) InputType.Keyboard,
                            ki = down
                        },
                        new INPUT {
                            dwType = (Int32) InputType.Keyboard,
                            ki = up
                        }
                    };

                    KeyUnicode(this, inputs);
                }
            }
        }

    }

    public class LowLevelMouseListener {

        public const int MOUSEEVENTF_MOVE = 0x1;
        public const int MOUSEEVENTF_LEFTDOWN = 0x2;
        public const int MOUSEEVENTF_LEFTUP = 0x4;
        public const int MOUSEEVENTF_RIGHTDOWN = 0x8;
        public const int MOUSEEVENTF_RIGHTUP = 0x10;
        public const int MOUSEEVENTF_MIDDLEDOWN = 0x20;
        public const int MOUSEEVENTF_MIDDLEUP = 0x40;
        public const int MOUSEEVENTF_WHEEL = 0x800;
        public const int MOUSEEVENTF_ABSOLUTE = 0x8000;


        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WH_MOUSE = 7;
        private const int WH_KEYBOARD = 2;
        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDBLCLK = 0x209;
        private const int WM_MOUSEWHEEL = 0x020A;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, ref MOUSEINPUT lParam);

        public event EventHandler<INPUT> MouseEvent, Move, LeftUp, LeftDown, RightUp, RightDown, MediumUp, MediumDown, 
            LeftDoubleClick, RightDoubleClick, MediumDoubleClick, Wheel;

        private LowLevelProcMouse proc;
        private IntPtr hookHandle = IntPtr.Zero;

        private int oldX;
        private int oldY;

        public LowLevelMouseListener() {
            proc = HookCallback;
            POINT currentPosition = new POINT();
            Win32.GetCursorPos(out currentPosition);
            oldX = currentPosition.X;
            oldY = currentPosition.Y;
        }

        public void HookMouse() {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule) {
                IntPtr procPtr = Marshal.GetFunctionPointerForDelegate(proc);
                IntPtr moduleHandle = Win32.GetModuleHandle(curModule.ModuleName);
                hookHandle = Win32.SetWindowsHookEx(WH_MOUSE_LL, procPtr, moduleHandle, 0);
            }
        }

        public void UnHookMouse() {
            Win32.UnhookWindowsHookEx(hookHandle);
        }

        ~LowLevelMouseListener() {
            UnHookMouse();
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, ref MOUSEINPUT lParam) {
            if (nCode >= 0) {
                INPUT input = new INPUT
                {
                    dwType = (Int32)InputType.Mouse,
                    mi = lParam
                };
                POINT p = new POINT();
                Win32.GetCursorPos(out p);
                input.mi.dx = (p.X - oldX);
                input.mi.dy = (p.Y - oldY);
                oldX = p.X;
                oldY = p.Y;
                //if (MouseEvent != null) MouseEvent(this, input);
                int message = (int)wParam;
                input.mi.time = 0;                
                input.mi.dwExtraInfo = IntPtr.Zero;
                switch (message) {
                    case WM_LBUTTONDOWN:
                        input.mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
                        input.mi.mouseData = 0;
                        if (LeftDown != null) LeftDown(this, input);
                        break;
                    case WM_LBUTTONUP:                 
                        input.mi.dwFlags = MOUSEEVENTF_LEFTUP;
                        input.mi.mouseData = 0;
                        if (LeftUp != null) LeftUp(this, input);
                        break;
                    case WM_RBUTTONDOWN:
                        input.mi.dwFlags = MOUSEEVENTF_RIGHTDOWN;
                        input.mi.mouseData = 0;
                        if (RightDown != null) RightDown(this, input);
                        break;
                    case WM_RBUTTONUP:
                        input.mi.dwFlags = MOUSEEVENTF_RIGHTUP;
                        input.mi.mouseData = 0;
                        if (RightUp != null) RightUp(this, input);
                        break;
                    case WM_MBUTTONDOWN:
                        input.mi.dwFlags = MOUSEEVENTF_MIDDLEDOWN;
                        input.mi.mouseData = 0;
                        if (MediumDown != null) MediumDown(this, input);
                        break;
                    case WM_MBUTTONUP:
                        input.mi.dwFlags = MOUSEEVENTF_MIDDLEUP;
                        input.mi.mouseData = 0;
                        if (MediumUp != null) MediumUp(this, input);
                        break;
                    case WM_MBUTTONDBLCLK:
                        input.mi.mouseData = 0;
                        if (MediumDoubleClick != null) MediumDoubleClick(this, input);
                        break;
                    case WM_LBUTTONDBLCLK:
                        input.mi.mouseData = 0;
                        if (LeftDoubleClick != null) LeftDoubleClick(this, input);
                        break;
                    case WM_RBUTTONDBLCLK:
                        input.mi.mouseData = 0;
                        if (RightDoubleClick != null) RightDoubleClick(this, input);
                        break;
                    case WM_MOUSEMOVE:
                        input.mi.mouseData = 0;
                        input.mi.dwFlags = MOUSEEVENTF_MOVE;
                        input.mi.dwExtraInfo = IntPtr.Zero;
                        if (Move != null) Move(this, input);
                        break;
                    case WM_MOUSEWHEEL:
                        input.mi.mouseData = (short)((input.mi.mouseData >> 16) & 0xffff);
                        input.mi.dwFlags = MOUSEEVENTF_WHEEL;
                        if (Wheel != null) Wheel(this, input);
                        break;
                }
                if (MouseEvent != null) MouseEvent(this, input);
            }
            return CallNextHookEx(hookHandle, nCode, wParam, ref lParam);
        }
    }
}