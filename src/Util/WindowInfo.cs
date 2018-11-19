using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowTitleWatcher.Internal;

namespace WindowTitleWatcher.Util
{
    /// <summary>
    /// Provides access to a window's information.
    /// </summary>
    public class WindowInfo
    {
        #region imports
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(IntPtr hWnd, ref RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr hWnd, ShowWindowEnum flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

        [DllImport("user32.dll")]
        //[return: MarshalAs(UnmanagedType.SysUInt)]
        private static extern uint SendInput(uint nInputs, [MarshalAs(UnmanagedType.LPArray), In] INPUT[] pInputs, int cbSize);
        #endregion imports

        #region structs
#pragma warning disable 649
        internal struct INPUT
        {
            public UInt32 Type;
            public MOUSEKEYBDHARDWAREINPUT Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT Mouse;
        }

        internal struct MOUSEINPUT
        {
            public Int32 X;
            public Int32 Y;
            public UInt32 MouseData;
            public UInt32 Flags;
            public UInt32 Time;
            public IntPtr ExtraInfo;
        }
#pragma warning restore 649
        #endregion structs

        #region enums
        [Flags()]
        private enum RedrawWindowFlags : uint
        {
            /// <summary>
            /// Invalidates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_INVALIDATE invalidates the entire window.
            /// </summary>
            Invalidate = 0x1,

            /// <summary>Causes the OS to post a WM_PAINT message to the window regardless of whether a portion of the window is invalid.</summary>
            InternalPaint = 0x2,

            /// <summary>
            /// Causes the window to receive a WM_ERASEBKGND message when the window is repainted.
            /// Specify this value in combination with the RDW_INVALIDATE value; otherwise, RDW_ERASE has no effect.
            /// </summary>
            Erase = 0x4,

            /// <summary>
            /// Validates the rectangle or region that you specify in lprcUpdate or hrgnUpdate.
            /// You can set only one of these parameters to a non-NULL value. If both are NULL, RDW_VALIDATE validates the entire window.
            /// This value does not affect internal WM_PAINT messages.
            /// </summary>
            Validate = 0x8,

            NoInternalPaint = 0x10,

            /// <summary>Suppresses any pending WM_ERASEBKGND messages.</summary>
            NoErase = 0x20,

            /// <summary>Excludes child windows, if any, from the repainting operation.</summary>
            NoChildren = 0x40,

            /// <summary>Includes child windows, if any, in the repainting operation.</summary>
            AllChildren = 0x80,

            /// <summary>Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND and WM_PAINT messages before the RedrawWindow returns, if necessary.</summary>
            UpdateNow = 0x100,

            /// <summary>
            /// Causes the affected windows, which you specify by setting the RDW_ALLCHILDREN and RDW_NOCHILDREN values, to receive WM_ERASEBKGND messages before RedrawWindow returns, if necessary.
            /// The affected windows receive WM_PAINT messages at the ordinary time.
            /// </summary>
            EraseNow = 0x200,

            Frame = 0x400,

            NoFrame = 0x800
        }
        #endregion enums

        private enum ShowWindowEnum
        {
            Hide = 0,
            ShowNormal = 1,
            ShowMinimized = 2,
            ShowMaximized = 3,
            Maximize = 3,
            ShowNormalNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActivate = 7,
            ShowNoActivate = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimized = 11,
        };

        public readonly IntPtr Handle;
        private readonly WindowPoller Poller;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public int ProcessId
        {
            get
            {
                uint pid;
                GetWindowThreadProcessId(Handle, out pid);

                return (int)pid;
            }
        }
        public string ProcessName
        {
            get
            {
                return Process.GetProcessById(ProcessId).ProcessName;
            }
        }

        public bool IsVisible
        {
            get
            {
                return Poller.Poll().IsVisible;
            }
        }

        public string Title
        {
            get
            {
                return Poller.Poll().Title;
            }
        }

        /// <summary>
        /// Ctor. Only requires a pointer to a control.
        /// </summary>
        /// <param name="hWnd"></param>
        public WindowInfo(IntPtr hWnd)
        {
            Handle = hWnd;
            Poller = new WindowPoller(hWnd);
        }

        /// <summary>
        /// Retrieves the location and size of this window.
        /// </summary>
        /// <returns></returns>
        public Rectangle GetRectangle()
        {
            var rct = new RECT();
            if (!GetWindowRect(Handle, ref rct))
            {
                return Rectangle.Empty;
            }

            return new Rectangle()
            {
                X = rct.Left,
                Y = rct.Top,
                Width = rct.Right - rct.Left,
                Height = rct.Bottom - rct.Top,
            };
        }

        /// <summary>
        /// Bring this window to the foreground.
        /// </summary>
        /// <returns></returns>
        public bool SetForeground()
        {
            return SetForegroundWindow(Handle);
        }

        /// <summary>
        /// Returns true if the window had been minimized and was restored,
        /// false if the window was already restored, and null if it failed.
        /// </summary>
        /// <param name="win"></param>
        /// <returns></returns>
        public bool? RestoreWindow()
        {
            if (!IsIconic(Handle))
            {
                return false;
            }

            var result = ShowWindow(Handle, ShowWindowEnum.Restore);
            if (result)
            {
                return true;
            }

            // TODO: This bit doesn't actually repaint the window; we probably need to wait for some events before, but I don't have the time to investigate this now.
            UpdateWindow(Handle);
            RedrawWindow(Handle, IntPtr.Zero, IntPtr.Zero, RedrawWindowFlags.Frame | RedrawWindowFlags.UpdateNow | RedrawWindowFlags.Invalidate);
            return null;
        }

        /// <summary>
        /// Easy way to send a click to another window. For more complex scenarios you can use specialized libraries,
        /// such as InputSimulator (on NuGet). This is just a simple implementation for mouse clicks.
        /// </summary>
        /// <param name="clientPoint">The point to click on, relative to the client window.</param>
        /// <remarks>
        /// This function was swiped from StackOverflow:
        /// https://stackoverflow.com/questions/10355286/programmatically-mouse-click-in-another-window#10355905
        /// </remarks>
        public void ClickOnPoint(Point clientPoint)
        {
            var oldPos = Cursor.Position;

            /// get screen coordinates
            ClientToScreen(Handle, ref clientPoint);

            /// set cursor on coords, and press mouse
            Cursor.Position = new Point(clientPoint.X, clientPoint.Y);

            var inputMouseDown = new INPUT();
            inputMouseDown.Type = 0; /// input type mouse
            inputMouseDown.Data.Mouse.Flags = 0x0002; /// left button down

            var inputMouseUp = new INPUT();
            inputMouseUp.Type = 0; /// input type mouse
            inputMouseUp.Data.Mouse.Flags = 0x0004; /// left button up

            var inputs = new INPUT[] { inputMouseDown, inputMouseUp };
            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));

            /// return mouse 
            Cursor.Position = oldPos;
        }
    }
}
