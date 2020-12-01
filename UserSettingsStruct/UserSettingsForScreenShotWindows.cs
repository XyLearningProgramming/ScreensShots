using System;
using System.Collections.Generic;
using System.Text;

namespace UserSettingsStruct
{
	/// <summary>
	/// Class used to initialze ScreenShotWindows according to user's preferences 
	/// </summary>
	public struct UserSettingsForScreenShotWindows
	{
		public bool IsShowingReferenceLine;
		public double WhiteDipAnimDuration;
		public string ImageFolderPath;
		public bool IsShowingWhiteDip;
		public string SaveFormatPreferred;
	}
}
