using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics.CodeAnalysis;

namespace ScreenShotWindows.Utils.Interop
{
	internal class InteropStructs
	{
		[StructLayout(LayoutKind.Sequential)]
		internal struct POINT 
		{
			public int X;
			public int Y;
			public POINT(int x, int y)
			{
				X = x; Y = y;
			}
		}

		/// <summary>
		/// Int rectangle struct used to receive data from win32 rect
		/// </summary>
		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT 
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;

			public RECT(int left, int top, int right, int bottom) { Left = left; Right = right; Top = top; Bottom = bottom; }
			public RECT(System.Windows.Rect rect_)
			{
				Left = (int)rect_.Left;
				Top = (int)rect_.Top;
				Right = (int)rect_.Right;
				Bottom = (int)rect_.Bottom;
			}

			public int Width { get => Right - Left; set => Right = Left + value; }
			public int Height { get => Bottom - Top; set => Bottom = Top + value; }
			public System.Windows.Point GetLeftTopPoint()
			{
				return new System.Windows.Point(Left, Top);
			}
			public System.Windows.Size GetSize() => new System.Windows.Size(Width, Height);
		}

		/// <summary>
		/// An application-defined or library-defined callback function used with the SetWindowsHookEx function
		/// wParam: Specifies whether the message is sent by the current process. If the message is sent by the current process, it is nonzero; otherwise, it is NULL.
		/// lParam: A pointer to a CWPRETSTRUCT structure that contains details about the message.
		/// </summary>
		/// <param name="code">A code the hook procedure uses to determine how to process the message.</param>
		/// <param name="wParam">The virtual-key code of the key that generated the keystroke message.</param>
		/// <param name="lParam">The repeat count, scan code, extended-key flag, context code, previous key-state flag, and transition-state flag.</param>
		/// <returns></returns>
		internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
		internal delegate IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        internal delegate bool MonitorEnumProc(IntPtr hDesktop, IntPtr hdc, ref RECT pRect, int dwData);

        // Delegate to filter which windows to include 
        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		internal enum HookType
		{
			MOUSE_LL = 14,
			KEYBOARD_LL = 13,
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct MOUSEHOOKSTRUCT
		{
			public Interop.InteropStructs.POINT pt;
			public IntPtr hwnd; // A handle to the window that processed the message specified by the message value.
			public uint wHitTestCode;
			public IntPtr dwExtraInfo;
		}

		// dark magic linked to Interop.InteropStructs.MOUSEHOOKSTRUCT. Do not change a bit
		internal enum MouseHookMessageType
		{
			LeftButtonDown = 0x0201, // 513
			LeftButtonUp = 0x0202,
			MouseMove = 0x0200,
			MouseWheel = 0x020A,
			RightButtonDown = 0x0204,
			RightButtonUp = 0x0205,
            LeftButtonDoubleClick = 0x0203,
        }

		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		internal class ImageCodecInfoPrivate
		{
			[MarshalAs(UnmanagedType.Struct)]
			public Guid Clsid;
			[MarshalAs(UnmanagedType.Struct)]
			public Guid FormatID;

			public IntPtr CodecName = IntPtr.Zero;
			public IntPtr DllName = IntPtr.Zero;
			public IntPtr FormatDescription = IntPtr.Zero;
			public IntPtr FilenameExtension = IntPtr.Zero;
			public IntPtr MimeType = IntPtr.Zero;

			public int Flags;
			public int Version;
			public int SigCount;
			public int SigSize;

			public IntPtr SigPattern = IntPtr.Zero;
			public IntPtr SigMask = IntPtr.Zero;
		}

        internal enum WINDOWCOMPOSITIONATTRIB
        {
            WCA_ACCENT_POLICY = 19
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINCOMPATTRDATA
        {
            public WINDOWCOMPOSITIONATTRIB Attribute;
            public IntPtr Data;
            public int DataSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RTL_OSVERSIONINFOEX
        {
            internal uint dwOSVersionInfoSize;
            internal uint dwMajorVersion;
            internal uint dwMinorVersion;
            internal uint dwBuildNumber;
            internal uint dwPlatformId;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            internal string szCSDVersion;
        }

        /// <summary>
        /// http://kenneththorman.blogspot.com/2010/08/c-net-active-windows-size-helper.html
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWINFO
        {
            public uint cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public ushort wCreatorVersion;

            public WINDOWINFO(Boolean? filler)
             : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
            {
                cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
            }

        }

		[Flags]
		internal enum CHILDWINDOWEXTFlAG
        {
            CWP_ALL = 0X0000,
            CWP_SKIPDISABLED= 0x0002,
            CWP_SKIPINVISIBLE = 0x0001,
            CWP_SKIPTRANSPARENT = 0x0004,
        }



        [ComImport, Guid("0000000C-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
		internal interface IStream
		{
			int Read([In] IntPtr buf, [In] int len);

			int Write([In] IntPtr buf, [In] int len);

			[return: MarshalAs(UnmanagedType.I8)]
			long Seek([In, MarshalAs(UnmanagedType.I8)] long dlibMove, [In] int dwOrigin);

			void SetSize([In, MarshalAs(UnmanagedType.I8)] long libNewSize);

			[return: MarshalAs(UnmanagedType.I8)]
			long CopyTo([In, MarshalAs(UnmanagedType.Interface)] IStream pstm, [In, MarshalAs(UnmanagedType.I8)] long cb, [Out, MarshalAs(UnmanagedType.LPArray)] long[] pcbRead);

			void Commit([In] int grfCommitFlags);

			void Revert();

			void LockRegion([In, MarshalAs(UnmanagedType.I8)] long libOffset, [In, MarshalAs(UnmanagedType.I8)] long cb, [In] int dwLockType);

			void UnlockRegion([In, MarshalAs(UnmanagedType.I8)] long libOffset, [In, MarshalAs(UnmanagedType.I8)] long cb, [In] int dwLockType);

			void Stat([In] IntPtr pStatstg, [In] int grfStatFlag);

			[return: MarshalAs(UnmanagedType.Interface)]
			IStream Clone();
		}


        [SuppressMessage("ReSharper", "InconsistentNaming")]
        internal class StreamConsts
        {
            public const int LOCK_WRITE = 0x1;
            public const int LOCK_EXCLUSIVE = 0x2;
            public const int LOCK_ONLYONCE = 0x4;
            public const int STATFLAG_DEFAULT = 0x0;
            public const int STATFLAG_NONAME = 0x1;
            public const int STATFLAG_NOOPEN = 0x2;
            public const int STGC_DEFAULT = 0x0;
            public const int STGC_OVERWRITE = 0x1;
            public const int STGC_ONLYIFCURRENT = 0x2;
            public const int STGC_DANGEROUSLYCOMMITMERELYTODISKCACHE = 0x4;
            public const int STREAM_SEEK_SET = 0x0;
            public const int STREAM_SEEK_CUR = 0x1;
            public const int STREAM_SEEK_END = 0x2;
            public const int E_FAIL = unchecked((int)0x80004005);
        }

        internal class ComStreamFromDataStream : IStream
        {
            protected Stream DataStream;

            // to support seeking ahead of the stream length... ??
            private long _virtualPosition = -1;

            internal ComStreamFromDataStream(Stream dataStream)
            {
                this.DataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            }

            private void ActualizeVirtualPosition()
            {
                if(_virtualPosition == -1) return;

                if(_virtualPosition > DataStream.Length)
                    DataStream.SetLength(_virtualPosition);

                DataStream.Position = _virtualPosition;

                _virtualPosition = -1;
            }

            public virtual IStream Clone()
            {
                NotImplemented();
                return null;
            }

            public virtual void Commit(int grfCommitFlags)
            {
                DataStream.Flush();
                ActualizeVirtualPosition();
            }

            public virtual long CopyTo(IStream pstm, long cb, long[] pcbRead)
            {
                const int bufsize = 4096; // one page
                var buffer = Marshal.AllocHGlobal(bufsize);
                if(buffer == IntPtr.Zero) throw new OutOfMemoryException();
                long written = 0;

                try
                {
                    while(written < cb)
                    {
                        var toRead = bufsize;
                        if(written + toRead > cb) toRead = (int)(cb - written);
                        var read = Read(buffer, toRead);
                        if(read == 0) break;
                        if(pstm.Write(buffer, read) != read)
                        {
                            throw EFail("Wrote an incorrect number of bytes");
                        }
                        written += read;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                if(pcbRead != null && pcbRead.Length > 0)
                {
                    pcbRead[0] = written;
                }

                return written;
            }

            public virtual Stream GetDataStream() => DataStream;

            public virtual void LockRegion(long libOffset, long cb, int dwLockType)
            {
            }

            protected static ExternalException EFail(string msg) => throw new ExternalException(msg, StreamConsts.E_FAIL);

            protected static void NotImplemented() => throw new NotImplementedException();

            public virtual int Read(IntPtr buf, int length)
            {
                var buffer = new byte[length];
                var count = Read(buffer, length);
                Marshal.Copy(buffer, 0, buf, length);
                return count;
            }

            public virtual int Read(byte[] buffer, int length)
            {
                ActualizeVirtualPosition();
                return DataStream.Read(buffer, 0, length);
            }

            public virtual void Revert() => NotImplemented();

            public virtual long Seek(long offset, int origin)
            {
                var pos = _virtualPosition;
                if(_virtualPosition == -1)
                {
                    pos = DataStream.Position;
                }
                var len = DataStream.Length;

                switch(origin)
                {
                    case StreamConsts.STREAM_SEEK_SET:
                        if(offset <= len)
                        {
                            DataStream.Position = offset;
                            _virtualPosition = -1;
                        }
                        else
                        {
                            _virtualPosition = offset;
                        }
                        break;
                    case StreamConsts.STREAM_SEEK_END:
                        if(offset <= 0)
                        {
                            DataStream.Position = len + offset;
                            _virtualPosition = -1;
                        }
                        else
                        {
                            _virtualPosition = len + offset;
                        }
                        break;
                    case StreamConsts.STREAM_SEEK_CUR:
                        if(offset + pos <= len)
                        {
                            DataStream.Position = pos + offset;
                            _virtualPosition = -1;
                        }
                        else
                        {
                            _virtualPosition = offset + pos;
                        }
                        break;
                }

                return _virtualPosition != -1 ? _virtualPosition : DataStream.Position;
            }

            public virtual void SetSize(long value) => DataStream.SetLength(value);

            public virtual void Stat(IntPtr pstatstg, int grfStatFlag) => NotImplemented();

            public virtual void UnlockRegion(long libOffset, long cb, int dwLockType)
            {
            }

            public virtual int Write(IntPtr buf, int length)
            {
                var buffer = new byte[length];
                Marshal.Copy(buf, buffer, 0, length);
                return Write(buffer, length);
            }

            public virtual int Write(byte[] buffer, int length)
            {
                ActualizeVirtualPosition();
                DataStream.Write(buffer, 0, length);
                return length;
            }
        }
    }
}
