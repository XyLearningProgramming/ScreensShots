using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace ScreenShotApp.ViewModels
{
	public class RootViewModel : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		#region fields
		private StartUpWindowViewModel startUpWindowViewModel;
		#endregion

		#region properties
		public StartUpWindowViewModel StartUpWindowViewModel { get => startUpWindowViewModel; }
		#endregion

		#region constructors
		public RootViewModel()
		{
			startUpWindowViewModel = new StartUpWindowViewModel(this);
		}
		#endregion
	}
}
