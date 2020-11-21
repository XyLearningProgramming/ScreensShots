using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Permissions;
using System.Text;
using System.Threading;

namespace ScreenShotWindows.Utils.Interop
{
	internal static class InteropErrorHandlerExt
	{
		private static int lastError = 0;
		internal static TResult HandleError<TResult>(this TResult result, [CallerMemberName] string memberName="", [CallerLineNumber] int lineNumber = 0)
		{
			int currentError = Marshal.GetLastWin32Error();
			if(lastError != currentError)
			{
				LogSystemShared.LogWriter.WriteLine($"Win32 error occurred: No.{currentError} with result type {nameof(TResult)}.", memberName: memberName, sourceLineNumber: lineNumber);
				lastError = currentError;
			}
			return result;
		}
	}
	internal class InteropMethods
	{
        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working). The system assigns a slightly higher priority to the thread that creates the foreground window than it does to other threads.
        /// </summary>
        /// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetForegroundWindow();
		public static IntPtr GetForegroundWindow_()=> GetForegroundWindow().HandleError();

		/// <summary>
		/// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window is the area on top of which other windows are painted.
		/// </summary>
		/// <returns></returns>
        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetDesktopWindow();
        public static IntPtr GetDesktopWindow_() => GetDesktopWindow().HandleError();


        /// <summary>
        /// The GetWindowDC function retrieves the device context (DC) for the entire window, including title bar, menus, and scroll bars.
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetWindowDC(IntPtr window);
		public static IntPtr GetWindowDC_(IntPtr window) => GetWindowDC(window).HandleError();

		/// <summary>
		/// The CreateCompatibleDC function creates a memory device context (DC) compatible with the specified device.
		/// </summary>
		/// <param name="hdc">A handle to an existing DC.</param>
		/// <returns></returns>
		[DllImport(InteropConstants.Gdi32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CreateCompatibleDC(IntPtr hdc);
		public static IntPtr CreateCompatibleDC_(IntPtr hdc) => CreateCompatibleDC(hdc).HandleError();

		[DllImport(InteropConstants.Gdi32, CharSet = CharSet.Auto, SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteDC(IntPtr hdc);
		public static bool DeleteDC_(IntPtr hdc) => DeleteDC(hdc).HandleError();

		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int ReleaseDC(IntPtr window, IntPtr dc);
		public static int ReleaseDC_(IntPtr window, IntPtr dc) => ReleaseDC(window, dc).HandleError();

		/// <summary>
		/// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
		/// </summary>
		/// <param name="hWnd"></param>
		/// <param name="lpRect"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GetWindowRect(IntPtr hWnd, out InteropStructs.RECT lpRect);
		public static void GetWindowRect_(IntPtr hWnd, out InteropStructs.RECT lpRect)
		{
			if(GetWindowRect(hWnd, out lpRect) == false) hWnd.HandleError();
		}

		/// <summary>
		/// Installs an application-defined hook procedure into a hook chain.
		/// SetLastError: true to indicate that the callee will call SetLastError; otherwise, false. The default is false. The runtime marshaler calls GetLastError and caches the value returned to prevent it from being overwritten by other API calls.You can retrieve the error code by calling Marshal.GetLastWin32Error().
		/// </summary>
		/// <param name="idHook"></param>
		/// <param name="lpfn">A pointer to the hook procedure.</param>
		/// <param name="hMod"></param>
		/// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated. For desktop apps, if this parameter is zero</param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int idHook, InteropStructs.HookProc lpfn, IntPtr hMod, uint dwThreadId);
		public static IntPtr SetWindowsHookEx_(int idHook, InteropStructs.HookProc lpfn, IntPtr hMod, uint dwThreadId) => SetWindowsHookEx(idHook, lpfn, hMod, dwThreadId).HandleError();
		/// <summary>
		/// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function.
		/// </summary>
		/// <param name="hhk"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool UnhookWindowsHookEx(IntPtr hhk);
		public static bool UnhookWindowsHookEx_(IntPtr hhk)=>UnhookWindowsHookEx(hhk).HandleError();

		/// <summary>
		/// Retrieves a module handle for the specified module. The module must have been loaded by the calling process.
		/// </summary>
		/// <param name="lpModuleName"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.Kernel32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);
		public static IntPtr GetModuleHandle_(string lpModuleName) => GetModuleHandle(lpModuleName).HandleError();

		/// <summary>
		/// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information.
		/// </summary>
		/// <param name="hhk"></param>
		/// <param name="nCode"></param>
		/// <param name="wParam"></param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
		public static IntPtr CallNextHookEx_(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam)=>CallNextHookEx(hhk,nCode,wParam,lParam).HandleError();

		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GetCursorPos(out InteropStructs.POINT pt);
		public static bool GetCursorPos_(out InteropStructs.POINT pt) => GetCursorPos(out pt).HandleError();

		#region image ops
		/// <summary>
		/// The CreateCompatibleBitmap function creates a bitmap compatible with the device that is associated with the specified device context.
		/// </summary>
		/// <param name="hDC"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		/// <returns>If the function succeeds, the return value is a handle to the compatible bitmap (DDB).</returns>
		[DllImport(InteropConstants.Gdi32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int width, int height);
		public static IntPtr CreateCompatibleBitmap_(IntPtr hDC, int width, int height) => CreateCompatibleBitmap(hDC, width, height).HandleError();

		/// <summary>
		/// The SelectObject function selects an object into the specified device context (DC). The new object replaces the previous object of the same type.
		/// </summary>
		/// <param name="hdc"></param>
		/// <param name="hgdiobj"></param>
		/// <returns></returns>
		public static IntPtr SelectObject_(IntPtr hdc, IntPtr hgdiobj) => SelectObject(hdc, hgdiobj).HandleError();
		[DllImport(InteropConstants.Gdi32, ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

		/// <summary>
		/// The BitBlt function performs a bit-block transfer of the color data corresponding to a rectangle of pixels from the specified source device context into a destination device context.
		/// </summary>
		/// <param name="hDC"></param>
		/// <param name="x"></param>
		/// <param name="y"></param>
		/// <param name="nWidth"></param>
		/// <param name="nHeight"></param>
		/// <param name="hSrcDC"></param>
		/// <param name="xSrc"></param>
		/// <param name="ySrc"></param>
		/// <param name="dwRop">A raster-operation code. These codes define how the color data for the source rectangle is to be combined with the color data for the destination rectangle to achieve the final color.</param>
		/// <returns></returns>
		[DllImport(InteropConstants.Gdi32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
		public static bool BitBlt_(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop) => BitBlt(hDC, x, y, nWidth, nHeight, hSrcDC, xSrc, ySrc, dwRop).HandleError();

		#endregion


		#region multiple screens interface
		[DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);
        public static int GetWindowText_(IntPtr hWnd, StringBuilder strText, int maxCount) => GetWindowText(hWnd, strText, maxCount).HandleError();
        public static string GetWindowText_(IntPtr hWnd)
		{
            int size = GetWindowTextLength_(hWnd);
			if(size > 0)
			{
                StringBuilder sb = new StringBuilder(size+1);
                GetWindowText_(hWnd, sb, sb.Capacity);
                return sb.ToString();
			}
			else
			{
                return string.Empty;
			}
		}

        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr hWnd);
        public static bool IsWindowVisible_(IntPtr hWnd) => IsWindowVisible(hWnd).HandleError();

        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern int GetWindowTextLength(IntPtr hWnd);
        public static int GetWindowTextLength_(IntPtr hWnd)=>GetWindowTextLength(hWnd).HandleError();

        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool EnumWindows(InteropStructs.EnumWindowsProc enumProc, IntPtr lParam);
        public static bool EnumWindows_(InteropStructs.EnumWindowsProc enumProc, IntPtr lParam) => EnumWindows(enumProc, lParam).HandleError();

        /// <summary>
        /// Gets the window info.
        /// </summary>
        /// <param name="hwnd">The HWND.</param>
        /// <param name="pwi">The pwi.</param>
        /// <returns></returns>
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetWindowInfo(IntPtr hwnd, ref InteropStructs.WINDOWINFO pwi);
        public static bool GetWindowInfo_(IntPtr hwnd, out InteropStructs.WINDOWINFO pwi) 
        {
            pwi = new InteropStructs.WINDOWINFO() { cbSize = (UInt32)Marshal.SizeOf(typeof(InteropStructs.WINDOWINFO)) };
            return GetWindowInfo(hwnd, ref pwi).HandleError();
        }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lpRect, InteropStructs.MonitorEnumProc callback, int dwData);
        public static bool EnumDisplayMonitors_(IntPtr hdc, IntPtr lpRect, InteropStructs.MonitorEnumProc callback, int dwData) => EnumDisplayMonitors(hdc, lpRect, callback, dwData).HandleError();
        #endregion

        [ReflectionPermission(SecurityAction.Assert, Unrestricted = true),
 SecurityPermission(SecurityAction.Assert, Flags = SecurityPermissionFlag.UnmanagedCode)]
		internal static void PtrToStructure(IntPtr lparam, object data) => Marshal.PtrToStructure(lparam, data);

        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, InteropStructs.POINT pt, InteropStructs.CHILDWINDOWEXTFlAG uFlags);
        public static IntPtr ChildWindowFromPointEx_(IntPtr hwndParent, InteropStructs.POINT pt, InteropStructs.CHILDWINDOWEXTFlAG uFlags) => ChildWindowFromPointEx(hwndParent, pt, uFlags).HandleError();

        [DllImport(InteropConstants.User32, CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PtInRect([In] ref InteropStructs.RECT rect, InteropStructs.POINT pt);
        public static bool PtInRect_(ref InteropStructs.RECT rect, InteropStructs.POINT pt) => PtInRect(ref rect, pt).HandleError();

        internal class Gdip
        {
            private const string ThreadDataSlotName = "system.drawing.threaddata";

            private static IntPtr InitToken;

            private static bool Initialized => InitToken != IntPtr.Zero;

            internal const int
                Ok = 0,
                GenericError = 1,
                InvalidParameter = 2,
                OutOfMemory = 3,
                ObjectBusy = 4,
                InsufficientBuffer = 5,
                NotImplemented = 6,
                Win32Error = 7,
                WrongState = 8,
                Aborted = 9,
                FileNotFound = 10,
                ValueOverflow = 11,
                AccessDenied = 12,
                UnknownImageFormat = 13,
                FontFamilyNotFound = 14,
                FontStyleNotFound = 15,
                NotTrueTypeFont = 16,
                UnsupportedGdiplusVersion = 17,
                GdiplusNotInitialized = 18,
                PropertyNotFound = 19,
                PropertyNotSupported = 20,
                E_UNEXPECTED = unchecked((int)0x8000FFFF);

            static Gdip()
            {
                Initialize();
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct StartupInput
            {
                private int GdiplusVersion;

                private readonly IntPtr DebugEventCallback;

                private bool SuppressBackgroundThread;

                private bool SuppressExternalCodecs;

                public static StartupInput GetDefault()
                {
                    var result = new StartupInput
                    {
                        GdiplusVersion = 1,
                        SuppressBackgroundThread = false,
                        SuppressExternalCodecs = false
                    };
                    return result;
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct StartupOutput
            {
                private readonly IntPtr hook;

                private readonly IntPtr unhook;
            }

            [ResourceExposure(ResourceScope.None)]
            [ResourceConsumption(ResourceScope.AppDomain, ResourceScope.AppDomain)]
            [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals")]
            private static void Initialize()
            {
                var input = StartupInput.GetDefault();

                GdiplusStartup(out InitToken, ref input, out _).GdipExceptionHandler();

                var currentDomain = AppDomain.CurrentDomain;
                currentDomain.ProcessExit += OnProcessExit;

                if(!currentDomain.IsDefaultAppDomain())
                {
                    currentDomain.DomainUnload += OnProcessExit;
                }
            }

            [PrePrepareMethod]
            [ResourceExposure(ResourceScope.AppDomain)]
            [ResourceConsumption(ResourceScope.AppDomain)]
            private static void OnProcessExit(object sender, EventArgs e) => Shutdown();

            [SuppressMessage("Microsoft.Reliability", "CA2001:AvoidCallingProblematicMethods")]
            [ResourceExposure(ResourceScope.AppDomain)]
            [ResourceConsumption(ResourceScope.AppDomain)]
            private static void Shutdown()
            {
                if(Initialized)
                {
                    ClearThreadData();
                    // unhook our shutdown handlers as we do not need to shut down more than once
                    var currentDomain = AppDomain.CurrentDomain;
                    currentDomain.ProcessExit -= OnProcessExit;
                    if(!currentDomain.IsDefaultAppDomain())
                    {
                        currentDomain.DomainUnload -= OnProcessExit;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static void ClearThreadData()
            {
                var slot = Thread.GetNamedDataSlot(ThreadDataSlotName);
                Thread.SetData(slot, null);
            }

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageGetFrameDimensionsCount(HandleRef image, out int count);

            internal static Exception StatusException(int status)
            {
                switch(status)
                {
                    case GenericError: return new ExternalException("GdiplusGenericError");
                    case InvalidParameter: return new ArgumentException("GdiplusInvalidParameter");
                    case OutOfMemory: return new OutOfMemoryException("GdiplusOutOfMemory");
                    case ObjectBusy: return new InvalidOperationException("GdiplusObjectBusy");
                    case InsufficientBuffer: return new OutOfMemoryException("GdiplusInsufficientBuffer");
                    case NotImplemented: return new NotImplementedException("GdiplusNotImplemented");
                    case Win32Error: return new ExternalException("GdiplusGenericError");
                    case WrongState: return new InvalidOperationException("GdiplusWrongState");
                    case Aborted: return new ExternalException("GdiplusAborted");
                    case FileNotFound: return new FileNotFoundException("GdiplusFileNotFound");
                    case ValueOverflow: return new OverflowException("GdiplusOverflow");
                    case AccessDenied: return new ExternalException("GdiplusAccessDenied");
                    case UnknownImageFormat: return new ArgumentException("GdiplusUnknownImageFormat");
                    case PropertyNotFound: return new ArgumentException("GdiplusPropertyNotFoundError");
                    case PropertyNotSupported: return new ArgumentException("GdiplusPropertyNotSupportedError");
                    case FontFamilyNotFound: return new ArgumentException("GdiplusFontFamilyNotFound");
                    case FontStyleNotFound: return new ArgumentException("GdiplusFontStyleNotFound");
                    case NotTrueTypeFont: return new ArgumentException("GdiplusNotTrueTypeFont_NoName");
                    case UnsupportedGdiplusVersion: return new ExternalException("GdiplusUnsupportedGdiplusVersion");
                    case GdiplusNotInitialized: return new ExternalException("GdiplusNotInitialized");
                }

                return new ExternalException("GdiplusUnknown");
            }


            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
            private static extern int GdipCreateBitmapFromHBITMAP(HandleRef hbitmap, HandleRef hpalette, out IntPtr bitmap);
            public static int GdipCreateBitmapFromHBITMAP_(HandleRef hbitmap, HandleRef hpalette, out IntPtr bitmap) => GdipCreateBitmapFromHBITMAP(hbitmap, hpalette, out bitmap).HandleError();

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int GdipGetImageEncodersSize(out int numEncoders, out int size);
            public static int GdipGetImageEncodersSize_(out int numEncoders, out int size) => GdipGetImageEncodersSize(out numEncoders, out size).HandleError();

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int GdipGetImageEncoders(int numEncoders, int size, IntPtr encoders);
            public static int GdipGetImageEncoders_(int numEncoders, int size, IntPtr encoders) => GdipGetImageEncoders(numEncoders, size, encoders).HandleError();

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int GdipGetImageDecodersSize(out int numDecoders, out int size);
            public static int GdipGetImageDecodersSize_(out int numDeoders, out int size) => GdipGetImageEncodersSize(out numDeoders, out size).HandleError();

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Auto)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int GdipGetImageDecoders(int numDecoders, int size, IntPtr decoders);
            public static int GdipGetImageDecoders_(int numDecoders, int size, IntPtr decoders) => GdipGetImageEncoders(numDecoders, size, decoders).HandleError();


            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int GdipSaveImageToStream(HandleRef image, InteropStructs.IStream stream, ref Guid classId, HandleRef encoderParams);
            public static int GdipSaveImageToStream_(HandleRef image, InteropStructs.IStream stream, ref Guid classId, HandleRef encoderParams) => GdipSaveImageToStream(image, stream, ref classId, encoderParams).HandleError();

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageGetFrameDimensionsList(HandleRef image, IntPtr buffer, int count);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageGetFrameCount(HandleRef image, ref Guid dimensionId, int[] count);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetPropertyItemSize(HandleRef image, int propid, out int size);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetPropertyItem(HandleRef image, int propid, int size, IntPtr buffer);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateHBITMAPFromBitmap(HandleRef nativeBitmap, out IntPtr hbitmap, int argbBackground);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageSelectActiveFrame(HandleRef image, ref Guid dimensionId, int frameIndex);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateBitmapFromFile(string filename, out IntPtr bitmap);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipImageForceValidation(HandleRef image);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, EntryPoint = "GdipDisposeImage", CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            private static extern int IntGdipDisposeImage(HandleRef image);

            internal static int GdipDisposeImage(HandleRef image)
            {
                if(!Initialized) return Ok;
                var result = IntGdipDisposeImage(image);
                return result;
            }

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Process)]
            private static extern int GdiplusStartup(out IntPtr token, ref StartupInput input, out StartupOutput output);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.None)]
            internal static extern int GdipGetImageRawFormat(HandleRef image, ref Guid format);

            [DllImport(InteropConstants.User32)]
            internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref InteropStructs.WINCOMPATTRDATA data);

            [DllImport(InteropConstants.Gdiplus, SetLastError = true, ExactSpelling = true, CharSet = CharSet.Unicode)]
            [ResourceExposure(ResourceScope.Machine)]
            internal static extern int GdipCreateBitmapFromStream(InteropStructs.IStream stream, out IntPtr bitmap);


            [DllImport(InteropConstants.NTdll)]
            internal static extern int RtlGetVersion(out InteropStructs.RTL_OSVERSIONINFOEX lpVersionInformation);

        }// end of Gdip inner class
    } // end of interop methods

	public static class GdipExceptionExt
	{
		public static void GdipExceptionHandler(this int status, [CallerMemberName] string CallerMemberName="",[CallerLineNumber]int CallerLineNum=0)
		{
			if(status != InteropMethods.Gdip.Ok)
			{
				Exception e = InteropMethods.Gdip.StatusException(status);
				LogSystemShared.LogWriter.WriteLine(e.Message, "Gdip Error", memberName: CallerMemberName, sourceLineNumber: CallerLineNum);
                // what if I just ignore gdip exceptions for now?
                //throw e;
			}
		}
	}
}
