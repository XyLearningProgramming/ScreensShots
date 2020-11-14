using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ScreenShotApp.MVVMUtils
{
    /// <summary>
    /// Extension for all view models. Block changes when field and newValue are the same. If they aren't, raise the action sent in.
    /// </summary>
    /// <example>
	/// if(this.MutateVerbose(ref propertyName, value, e => PropertyChanged?.Invoke(this, e))) 
    /// {//Then do something}
    /// </example>
	public static class NotifyPropertyChangedExtension
    {
        public static bool MutateVerbose<TField>(this INotifyPropertyChanged _, ref TField field, TField newValue, Action<PropertyChangedEventArgs> raise, [CallerMemberName] string propertyName = null)
        {
            if(EqualityComparer<TField>.Default.Equals(field, newValue)) return false;
            field = newValue;
            raise?.Invoke(new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
