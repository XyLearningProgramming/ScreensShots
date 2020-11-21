using ScreenShotWindows.Utils.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace ScreenShotWindows.Utils
{
    [Flags]
    internal enum ImageCodecFlags
    {
        Encoder = 0x00000001,
        Decoder = 0x00000002,
        SupportBitmap = 0x00000004,
        SupportVector = 0x00000008,
        SeekableEncode = 0x00000010,
        BlockingDecode = 0x00000020,
        Builtin = 0x00010000,
        System = 0x00020000,
        User = 0x00040000
    }

    internal class ImageCodecInfo
	{
        private string _dllName;

        public Guid Clsid { get; set; }

        public Guid FormatID { get; set; }

        public string CodecName { get; set; }

        public string DllName
        {
            [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
            get
            {
                if(_dllName != null)
                {
                    new System.Security.Permissions.FileIOPermission(System.Security.Permissions.FileIOPermissionAccess.PathDiscovery, _dllName).Demand();
                }
                return _dllName;
            }
            [SuppressMessage("Microsoft.Security", "CA2103:ReviewImperativeSecurity")]
            set
            {
                if(value != null)
                {
                    new System.Security.Permissions.FileIOPermission(System.Security.Permissions.FileIOPermissionAccess.PathDiscovery, value).Demand();
                }
                _dllName = value;
            }
        }

        public string FormatDescription { get; set; }

        public string FilenameExtension { get; set; }

        public string MimeType { get; set; }

        public ImageCodecFlags Flags { get; set; }

        public int Version { get; set; }

        public byte[][] SignaturePatterns { get; set; }

        public byte[][] SignatureMasks { get; set; }

        public static ImageCodecInfo[] GetImageDecoders()
        {
            ImageCodecInfo[] imageCodecs;

            InteropMethods.Gdip.GdipGetImageDecodersSize_(out var numDecoders, out var size).GdipExceptionHandler();

            var memory = Marshal.AllocHGlobal(size);

            try
            {
                InteropMethods.Gdip.GdipGetImageDecoders_(numDecoders, size, memory).GdipExceptionHandler();

                imageCodecs = ConvertFromMemory(memory, numDecoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        public static ImageCodecInfo[] GetImageEncoders()
        {
            ImageCodecInfo[] imageCodecs;

            InteropMethods.Gdip.GdipGetImageEncodersSize_(out var numEncoders, out var size).GdipExceptionHandler();

            var memory = Marshal.AllocHGlobal(size);

            try
            {
                InteropMethods.Gdip.GdipGetImageEncoders_(numEncoders, size, memory).GdipExceptionHandler();
                imageCodecs = ConvertFromMemory(memory, numEncoders);
            }
            finally
            {
                Marshal.FreeHGlobal(memory);
            }

            return imageCodecs;
        }

        public static ImageCodecInfo[] ConvertFromMemory(IntPtr memoryStart, int numCodecs)
        {
            var codecs = new ImageCodecInfo[numCodecs];

            int index;

            for(index = 0; index < numCodecs; index++)
            {
                var curcodec = (IntPtr)((long)memoryStart + Marshal.SizeOf(typeof(InteropStructs.ImageCodecInfoPrivate)) * index);
                var codecp = new InteropStructs.ImageCodecInfoPrivate();
                InteropMethods.PtrToStructure(curcodec, codecp);

                codecs[index] = new ImageCodecInfo
                {
                    Clsid = codecp.Clsid,
                    FormatID = codecp.FormatID,
                    CodecName = Marshal.PtrToStringUni(codecp.CodecName),
                    DllName = Marshal.PtrToStringUni(codecp.DllName),
                    FormatDescription = Marshal.PtrToStringUni(codecp.FormatDescription),
                    FilenameExtension = Marshal.PtrToStringUni(codecp.FilenameExtension),
                    MimeType = Marshal.PtrToStringUni(codecp.MimeType),
                    Flags = (ImageCodecFlags)codecp.Flags,
                    Version = codecp.Version,
                    SignaturePatterns = new byte[codecp.SigCount][],
                    SignatureMasks = new byte[codecp.SigCount][]
                };

                for(var j = 0; j < codecp.SigCount; j++)
                {
                    codecs[index].SignaturePatterns[j] = new byte[codecp.SigSize];
                    codecs[index].SignatureMasks[j] = new byte[codecp.SigSize];

                    Marshal.Copy((IntPtr)((long)codecp.SigMask + j * codecp.SigSize), codecs[index].SignatureMasks[j], 0, codecp.SigSize);
                    Marshal.Copy((IntPtr)((long)codecp.SigPattern + j * codecp.SigSize), codecs[index].SignaturePatterns[j], 0, codecp.SigSize);
                }
            }

            return codecs;
        }
    }
}