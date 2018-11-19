﻿using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace WindowTitleWatcher.Util
{
    /// <summary>
    /// Utility class for enumerating and finding specific windows to be used
    /// in Watcher instances.
    /// </summary>
    public class Windows
    {
        #region imports
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        #endregion imports

        public delegate bool EnumCallback(WindowInfo windowHandle);

        /// <summary>
        /// Enumerates all windows synchronously and applies the given callback
        /// until it returns false or the end is reached.
        /// </summary>
        /// <param name="callback">The callback to apply to each window.</param>
        public static void ForEach(EnumCallback callback)
        {
            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindow(hWnd))
                {
                    // skip this
                    return true;
                }
                return callback(new WindowInfo(hWnd));
            }, IntPtr.Zero);
        }

        public static WindowInfo GetForegroundWindowInfo()
        {
            return new WindowInfo(GetForegroundWindow());
        }

        /// <summary>
        /// Finds the first out of all windows for which the filter returns true.
        /// </summary>
        /// <param name="filter">The predicate to apply.</param>
        /// <returns>The window or null if none match.</returns>
        public static WindowInfo FindFirst(Predicate<WindowInfo> filter)
        {
            WindowInfo result = null;
            ForEach(window =>
            {
                if (filter(window))
                {
                    result = window;
                    return false;
                }
                return true;
            });
            return result;
        }
    }
}
