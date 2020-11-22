using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenShotWindows.Utils
{
	public static class BitmapImagePointToColorExt
	{
		/// <summary>
		/// https://stackoverflow.com/questions/1176910/finding-specific-pixel-colors-of-a-bitmapimage
		/// </summary>
		/// <param name="image"></param>
		/// <param name="X"></param>
		/// <param name="Y"></param>
		/// <returns></returns>
		public static Color GetPixelColor(this BitmapImage image, int X, int Y)
		{
			if(X <= image.Width && Y <= image.Height) {
				byte[] pixel = new byte[4];
				new CroppedBitmap(image, new Int32Rect(X, Y, 1, 1)).CopyPixels(pixel,4,0);
				return Color.FromArgb(pixel[3], pixel[2], pixel[1], pixel[0]);
			}
			else return Colors.Transparent;
		}
	}

	//[StructLayout(LayoutKind.Sequential)]
	//public struct PixelColor
	//{
	//	public byte Blue;
	//	public byte Green;
	//	public byte Red;
	//	public byte Alpha;
	//}
}
