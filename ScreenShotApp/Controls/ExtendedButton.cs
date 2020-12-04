using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ScreenShotApp.Controls
{
	/// <summary>
	/// Extended button can hold both a icon(brush) and text with specified size.
	/// This is adpated from Nick Manarin's ScreenToGif style controls
	/// https://github.com/NickeManarin/ScreenToGif/tree/master/ScreenToGif
	/// </summary>
	public class ExtendedButton : Button
	{
		static ExtendedButton()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ExtendedButton), new FrameworkPropertyMetadata(typeof(ExtendedButton)));
		}

		#region Properties

		public static readonly DependencyProperty IconProperty = DependencyProperty.Register(nameof(Icon), typeof(Brush), typeof(ExtendedButton));

		public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(ExtendedButton));

        public static readonly DependencyProperty ImageProperty = DependencyProperty.Register("Image", typeof(Image), typeof(ExtendedButton));

        /// <summary>
        /// Height of icon
        /// </summary>
		public static readonly DependencyProperty ContentHeightProperty = DependencyProperty.Register(nameof(ContentHeight), typeof(double), typeof(ExtendedButton), new FrameworkPropertyMetadata(double.NaN));

        /// <summary>
        /// Width of icon
        /// </summary>
		public static readonly DependencyProperty ContentWidthProperty = DependencyProperty.Register(nameof(ContentWidth), typeof(double), typeof(ExtendedButton), new FrameworkPropertyMetadata(double.NaN));
        
        /// <summary>
        /// Hint of shortcuts below the button. Auto collapsed if empty string
        /// </summary>
		public static readonly DependencyProperty KeyGestureProperty = DependencyProperty.Register(nameof(KeyGesture), typeof(string), typeof(ExtendedButton), new FrameworkPropertyMetadata(""));

		public static readonly DependencyProperty TextWrappingProperty = DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(ExtendedButton), new FrameworkPropertyMetadata(TextWrapping.NoWrap,
			FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

        /// <summary>
        /// The icon of the button as a brush.
        /// </summary>
        [Description("The icon of the button as a brush."), Category("Common")]
        public Brush Icon
        {
            get => (Brush)GetValue(IconProperty);
            set => SetCurrentValue(IconProperty, value);
        }

        /// <summary>
        /// The text of the button.
        /// </summary>
        [Description("The text of the button."), Category("Common")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetCurrentValue(TextProperty, value);
        }

        /// <summary>
        /// The height of the button content.
        /// </summary>
        [Description("The height of the button content."), Category("Common")]
        public double ContentHeight
        {
            get => (double)GetValue(ContentHeightProperty);
            set => SetCurrentValue(ContentHeightProperty, value);
        }

        /// <summary>
        /// The width of the button content.
        /// </summary>
        [Description("The width of the button content."), Category("Common")]
        public double ContentWidth
        {
            get => (double)GetValue(ContentWidthProperty);
            set => SetCurrentValue(ContentWidthProperty, value);
        }

        /// <summary>
        /// The KeyGesture of the button.
        /// </summary>
        [Description("The KeyGesture of the button."), Category("Common")]
        public string KeyGesture
        {
            get => (string)GetValue(KeyGestureProperty);
            set => SetCurrentValue(KeyGestureProperty, value);
        }

        /// <summary>
        /// The TextWrapping property controls whether or not text wraps 
        /// when it reaches the flow edge of its containing block box. 
        /// </summary>
        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public Image Image { get => (Image)GetValue(ImageProperty); set => SetValue(ImageProperty, value); }
        #endregion
    }
}
