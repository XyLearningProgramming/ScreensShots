using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace ScreenShotApp.Utils
{
	public class KeyStringHelper
	{
        public static string GetSelectKeyText(ModifierKeys modifier = ModifierKeys.None)
        {
            //Get the modifers as text.
            var modifiersText = Enum.GetValues(modifier.GetType()).OfType<ModifierKeys>()
                .Where(x => x != ModifierKeys.None && modifier.HasFlag(x))
                .Aggregate("", (current, mod) =>
                {
                    if(mod == ModifierKeys.Control)
                        return current + (string.IsNullOrWhiteSpace(current) ? "" : " + ") + "Ctrl";

                    return current + (string.IsNullOrWhiteSpace(current) ? "" : " + ") + mod;
                });

            return modifiersText;
        }

        public static string GetSelectKeyText(Key key, ModifierKeys modifier = ModifierKeys.None, bool isUppercase = false, bool ignoreNone = false)
        {
            //Key translation.
            switch(key)
            {
                case Key.Oem1:
                    key = Key.OemSemicolon;
                    break;
                case Key.Oem2:
                    key = Key.OemQuestion;
                    break;
                case Key.Oem3:
                    key = Key.OemTilde;
                    break;
                case Key.Oem4:
                    key = Key.OemOpenBrackets;
                    break;
                case Key.Oem5:
                    key = Key.OemPipe;
                    break;
                case Key.Oem6:
                    key = Key.OemCloseBrackets;
                    break;
                case Key.Oem7:
                    key = Key.OemComma;
                    break;
            }

            if(ignoreNone && key == Key.None)
                return "";

            //Get the modifers as text.
            var modifiersText = Enum.GetValues(modifier.GetType()).OfType<ModifierKeys>()
                .Where(x => x != ModifierKeys.None && modifier.HasFlag(x))
                .Aggregate("", (current, mod) =>
                {
                    if(mod == ModifierKeys.Control) //TODO: Custom mod.ToString();
                        return current + "Ctrl" + " + ";

                    return current + mod + " + ";
                });

            var result = GetCharFromKey(key);

            if(result == null || string.IsNullOrWhiteSpace(result.ToString()) || result < 32)
            {
                //Some keys need to be displayed differently.
                var keyText = key.ToString();

                switch(key)
                {
                    case Key.LeftCtrl:
                    case Key.RightCtrl:
                        keyText = "Ctrl";
                        break;
                    case Key.LeftShift:
                    case Key.RightShift:
                        keyText = "Shift";
                        break;
                    case Key.LeftAlt:
                    case Key.RightAlt:
                        keyText = "Alt";
                        break;
                    case Key.CapsLock:
                        keyText = "CapsLock";
                        break;
                    case Key.LWin:
                    case Key.RWin:
                        keyText = "Windows";
                        break;
                    case Key.Return:
                        keyText = "Enter";
                        break;
                    case Key.Next:
                        keyText = "PageDown";
                        break;
                    case Key.PrintScreen:
                        keyText = "PrintScreen";
                        break;
                    case Key.Back:
                        keyText = "Backspace";
                        break;

                    //Special localization
                    case Key.Space:
                        keyText = "Space";
                        break;
                }

                //Modifiers;
                return modifiersText + keyText;
            }

            //If there's any modifiers, it means that it's a command. So it should be treated as uppercase.
            if(modifiersText.Length > 0)
                isUppercase = true;

            return modifiersText + (isUppercase ? char.ToUpper(result.Value) : result);
        }

        public static char? GetCharFromKey(Key key, bool ignoreState = true)
        {
            var virtualKey = KeyInterop.VirtualKeyFromKey(key);
            var keyboardState = new byte[256];

            if(!ignoreState)
                GetKeyboardState(keyboardState);

            var scanCode = MapVirtualKey((uint)virtualKey, MapType.MapvkVkToVsc);
            var stringBuilder = new StringBuilder(2);

            var result = ToUnicode((uint)virtualKey, scanCode, keyboardState, stringBuilder, stringBuilder.Capacity, 0);

            switch(result)
            {
                case -1:
                case 0:
                    break;
                default: //Case 1
                    return stringBuilder[0];
            }

            return null;
        }

        public enum MapType : uint
        {
            MapvkVkToVsc = 0x0,
            MapvkVscToVk = 0x1,
            MapvkVkToChar = 0x2,
            MapvkVscToVkEx = 0x3,
        }

        [DllImport("user32.dll")]
        internal static extern bool GetKeyboardState(byte[] lpKeyState);

        [DllImport("user32.dll")]
        internal static extern uint MapVirtualKey(uint uCode, MapType uMapType);

        [DllImport("user32.dll")]
        internal static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex = 4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
    }
}
