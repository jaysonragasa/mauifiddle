using System.Windows.Input;

namespace MAUIFiddle;

public class EditorStoppedTypingBehavior : Behavior<Editor>
{
	private CancellationTokenSource _cts;

	public static readonly BindableProperty CommandProperty =
		BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(EditorStoppedTypingBehavior));

	public ICommand Command
	{
		get => (ICommand)GetValue(CommandProperty);
		set => SetValue(CommandProperty, value);
	}

	public static readonly BindableProperty DelayMillisecondsProperty =
		BindableProperty.Create(nameof(DelayMilliseconds), typeof(int), typeof(EditorStoppedTypingBehavior), 1000);

	public int DelayMilliseconds
	{
		get => (int)GetValue(DelayMillisecondsProperty);
		set => SetValue(DelayMillisecondsProperty, value);
	}

	public event EventHandler<string> StoppedTyping;

	protected override void OnAttachedTo(Editor bindable)
	{
		base.OnAttachedTo(bindable);
		bindable.TextChanged += OnTextChanged;
	}

	protected override void OnDetachingFrom(Editor bindable)
	{
		base.OnDetachingFrom(bindable);
		bindable.TextChanged -= OnTextChanged;
	}

	private void OnTextChanged(object sender, TextChangedEventArgs e)
	{
		_cts?.Cancel();
		_cts = new CancellationTokenSource();
		var token = _cts.Token;

		var editor = (Editor)sender;
		string newText = e.NewTextValue;

		Task.Run(async () =>
		{
			try
			{
				await Task.Delay(DelayMilliseconds, token);
				if (!token.IsCancellationRequested)
				{
					MainThread.BeginInvokeOnMainThread(() =>
					{
						if (Command?.CanExecute(newText) == true)
							Command.Execute(newText);

						StoppedTyping?.Invoke(editor, newText);
					});
				}
			}
			catch (TaskCanceledException) { /* No action needed */ }
		});
	}
}