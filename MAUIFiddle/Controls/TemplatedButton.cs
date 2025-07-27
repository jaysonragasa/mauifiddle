using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace MAUIFiddle.Controls;

/// <summary>
/// A custom view control that supports command binding
/// </summary>
public class TemplatedButton : ContentView
{
	// visual feedback (scale)
	double minScale = 0.86d;

	/// <summary>
	/// Identifies the Command bindable property.
	/// </summary>
	public static readonly BindableProperty CommandProperty = BindableProperty.Create(
		propertyName: nameof(Command),
		returnType: typeof(ICommand),
		declaringType: typeof(TemplatedButton),
		defaultValue: null,
		defaultBindingMode: BindingMode.OneWay
		);

	/// <summary>
	/// Identifies the CommandParameter bindable property.
	/// </summary>
	public static readonly BindableProperty CommandParameterProperty = BindableProperty.Create(
		propertyName: nameof(CommandParameter),
		returnType: typeof(object),
		declaringType: typeof(TemplatedButton),
		defaultValue: null,
		defaultBindingMode: BindingMode.OneWay
		);

	/// <summary>
	/// Identifies the <see cref="BackgroundColor"/> bindable property.
	/// </summary>
	public static new readonly BindableProperty BackgroundColorProperty =
		BindableProperty.Create(
			nameof(BackgroundColor),
			typeof(Color),
			typeof(TemplatedButton),
			Color.FromArgb("#AFFFFFFF")
			);

	/// <summary>
	/// Identifies the <see cref="Title"/> bindable property.
	/// </summary>
	public static readonly BindableProperty TextProperty =
		BindableProperty.Create(
			nameof(Text),
			typeof(string),
			typeof(TemplatedButton),
			string.Empty);

	/// <summary>
	/// Gets or sets the command to invoke when the button is tapped.
	/// </summary
	public ICommand Command
	{
		get { return (ICommand)GetValue(CommandProperty); }
		set { SetValue(CommandProperty, value); }
	}

	/// <summary>
	/// Gets or sets an optional parameter to pass to the Command property.
	/// </summary>
	public object CommandParameter
	{
		get { return GetValue(CommandParameterProperty); }
		set { SetValue(CommandParameterProperty, value); }
	}

	/// <summary>
	/// Gets or sets the background color to be used for the container (inside the border)
	/// </summary>
	public new Color BackgroundColor
	{
		get => (Color)GetValue(BackgroundColorProperty);
		set => SetValue(BackgroundColorProperty, value);
	}

	/// <summary>
	/// Gets or sets the title of the attachment.
	/// </summary>
	/// <value>The title of the attachment as a string.</value>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Constructor for <see cref="TemplatedButton"/>.
	/// </summary>
	public TemplatedButton()
	{
		var tapGesture = new TapGestureRecognizer();
		tapGesture.Tapped += async (s, e) =>
		{
			if (Command == null)
				return;

			if (Command is IAsyncRelayCommand asyncCommand)
			{
				if (asyncCommand.CanExecute(CommandParameter))
				{
					SetScale(minScale);
					await asyncCommand.ExecuteAsync(CommandParameter);
					SetScale(1);
				}
			}
			else if (Command.CanExecute(CommandParameter))
			{
				SetScale(minScale);
				Command.Execute(CommandParameter);
				SetScale(1);
			}
		};

		GestureRecognizers.Add(tapGesture);
	}

	void SetScale(double scale) => MainThread.BeginInvokeOnMainThread(async () => { this.Scale = scale; await Task.Delay(1); });
}