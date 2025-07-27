using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Mvvm.Input;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace MAUIFiddle;

public partial class MainPage : ContentPage
{
	#region commands
	public ICommand RunCommand { get; set; }
	public ICommand NewCommand { get; set; }
	public ICommand SaveCommand { get; set; }
	public ICommand OpenCommand { get; set; }
	#endregion

	public MainPage()
	{
		InitializeComponent();
		InitCommands();
		BindingContext = this;

		CodeEditor.Text = @"Print(""Start writing your code"")";

		fontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
		cbUseLegacyEditor.CheckedChanged += CbUseLegacyEditor_CheckedChanged;
	}

	#region command methods
	public async Task RunCode()
	{
		await ExecuteCodeAsync();
	}

	public async Task NewCodeAsync()
	{
		NewCSharpCode();
	}

	public async Task SaveCodeAsync()
	{
		//var code = CodeEditor.Text;
		//var fileName = "MAUIFiddle.cs";
		//var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);
		//await File.WriteAllTextAsync(filePath, code);
		//await Shell.Current.DisplayAlert("File Saved", $"Your code has been saved to {filePath}", "OK");

		var content = CodeEditor.Text;

		// Prompt the user for a file path using CommunityToolkit FileSaver
		var result = await FileSaver.Default.SaveAsync(
			"CodeFile.cs",
			new MemoryStream(Encoding.UTF8.GetBytes(content)),
			new CancellationToken()
		);

		if (result.IsSuccessful)
		{
			await DisplayAlert("Saved", $"File saved to: {result.FilePath}", "OK");
		}
		else
		{
			await DisplayAlert("Error", "File saving failed", "OK");
		}
	}

	public async Task OpenCodeAsync()
	{
		var result = await FilePicker.Default.PickAsync(new PickOptions
		{
			PickerTitle = "Select a code file"
		});

		if (result is not null && result.FileName != null)
		{
			using var stream = await result.OpenReadAsync();
			using var reader = new StreamReader(stream);

			var text = await reader.ReadToEndAsync();

			CodeEditor.Text = text;
		}
		else
		{
			await DisplayAlert("Error", "File opening cancelled or failed", "OK");
		}
	}
	#endregion

	#region overrides
	protected override void OnAppearing()
	{
		base.OnAppearing();

		LoadEditorHtml();
	}
	#endregion

	#region private methods
	void InitCommands()
	{
		RunCommand = new AsyncRelayCommand(RunCode);
		NewCommand = new AsyncRelayCommand(NewCodeAsync);
		SaveCommand = new AsyncRelayCommand(SaveCodeAsync);
		OpenCommand = new AsyncRelayCommand(OpenCodeAsync);
	}

	async Task ExecuteCodeAsync()
	{
		var code = CodeEditor.Text;

		try
		{
			var op = await Tissuevaluator.Tissueluate(code, default);

			OutputLabel.TextColor = Color.FromArgb("#FFDCD7BA");
			OutputLabel.Text = op;
		}
		catch (Exception ex)
		{
			OutputLabel.TextColor = Color.FromArgb("#FFE82424");
			OutputLabel.Text = ex.Message;
		}
	}

	async void LoadEditorHtml()
	{
		using var stream = await FileSystem.OpenAppPackageFileAsync("editorcodemirror.html");
		using var reader = new StreamReader(stream);
		var htmlString = reader.ReadToEnd();
		HtmlCodeEditor.Source = new HtmlWebViewSource { Html = htmlString };

		HtmlCodeEditor.Navigating += (s, e) =>
		{
			string url = e.Url ?? string.Empty;

			if (string.IsNullOrWhiteSpace(url)) return;

			Debug.WriteLine(url);

			if (url.StartsWith("callback://editor/?text="))
			{
				if (cbUseLegacyEditor.IsChecked)
				{
					e.Cancel = true;
					return;
				}

				var content = Uri.UnescapeDataString(url.Substring("callback://editor/?text=".Length));
				e.Cancel = true;

				CodeEditor.Text = content;
			}
			else if (url.StartsWith("callback://editor/?status=done"))
			{
				NewCSharpCode();
				e.Cancel = true;
			}
		};
	}

	async void NewCSharpCode()
	{
		string code = @"using System;

public class Say
{
	public static void Hello()
		=> Console.Write(""Hello"");
}
Say.Hello();
Print("" World!"");
PrintLn(""This is a simple C# code example. You can also directly call the CurrentPage too!"");
ContentPage cp = new ContentPage();
Image img = new Image() { Source = ""dotnet_bot.png"", HeightRequest = 185d, Aspect = Microsoft.Maui.Aspect.AspectFit };
Label label = new Label() { Text = ""Welcome to &#10;.NET Multi-platform App UI"", HorizontalOptions = LayoutOptions.Center };
Button btn = new() { Text = ""Back"", HorizontalOptions = LayoutOptions.Center }; btn.Clicked += (s, e) => CurrentPage.Navigation.PopAsync();
VerticalStackLayout vsl = new() { Spacing = 25 };
vsl.Children.Add(img);
vsl.Children.Add(label);
vsl.Children.Add(btn);
cp.Content = vsl;
// CurrentPage.Navigation.PushAsync(cp);
// CurrentPage.Navigation.PushModalAsync(cp);";

		if (cbUseLegacyEditor.IsChecked)
			CodeEditor.Text = code;
		else
			await SendCodeToHtmlEditor(code);
	}
	#endregion

	#region event triggered methods
	private async void FontSizeSlider_ValueChanged(object? sender, ValueChangedEventArgs e)
	{
		await HtmlCodeEditor.EvaluateJavaScriptAsync($"setEditorFontSize({e.NewValue});");
	}

	/// <summary>
	/// Creates a lazy line number display for the code editor.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
	{
		var editor = (Editor)sender;

		// lines
		int lines = 0;
		lines = editor.Text.Split(
#if ANDROID
			'\n'
#elif WINDOWS
			'\r'
#endif
			).Count();
		StringBuilder sb = new StringBuilder();
		for (int i = 0; i < lines; i++)
		{
			sb.AppendLine((i + 1).ToString());
		}
		lineNumber.Text = sb.ToString();
	}

	private async void EditorStoppedTypingBehavior_StoppedTyping(object sender, string e)
	{
		if (cbAutoRun.IsChecked)
			await ExecuteCodeAsync();

		if (cbUseLegacyEditor.IsChecked)
			await SendCodeToHtmlEditor(CodeEditor.Text);
	}

	async Task SendCodeToHtmlEditor(string code)
	{
		try
		{
			code = System.Web.HttpUtility.UrlEncode(code);
			//code = System.Web.HttpUtility.HtmlEncode(code);
			await HtmlCodeEditor.EvaluateJavaScriptAsync($"setEditorText(\"{code}\");");
		}
		catch (Exception ex)
		{
			// just ignore something for now
			Debug.WriteLine(ex.Message);
		}
	}

	private async void CbUseLegacyEditor_CheckedChanged(object? sender, CheckedChangedEventArgs e)
	{
		bool ischecked = e.Value;

		if (!ischecked)
		{
			await SendCodeToHtmlEditor(CodeEditor.Text);
		}
	}
	#endregion
}

public class ScriptGlobals
{
	public CancellationToken CancellationToken { get; set; }
	public Action<string> PrintLn = Console.WriteLine;
	public Action<string> Print = Console.Write;
	public AppShell AppShell = (AppShell)App.Current.MainPage;
	public ContentPage CurrentPage = (ContentPage)Shell.Current.CurrentPage;
}

public class Tissuevaluator
{
	static CancellationTokenSource _cts = new CancellationTokenSource();

	private static ScriptOptions BuildDefaultScriptOptions()
	{
		var referencedAssemblies = new[]
		{
			typeof(object).Assembly,                            // System.Private.CoreLib
            typeof(Enumerable).Assembly,                        // System.Linq
            typeof(List<>).Assembly,                            // System.Collections
            typeof(Uri).Assembly,                               // System.Net
            typeof(HttpClient).Assembly,                        // System.Net.Http
            typeof(Regex).Assembly,                             // System.Text.RegularExpressions
            typeof(JsonConvert).Assembly,                       // Newtonsoft.Json
			typeof(System.Reflection.Assembly).Assembly,		// System.Reflection

			//typeof(Microsoft.Maui.Controls.View).Assembly,
			typeof(Microsoft.Maui.Controls.VisualElement).Assembly,
			typeof(Microsoft.Maui.Controls.Xaml.Extensions).Assembly
		};

		var defaultNamespaces = new[]
		{
			// Base
            "System",
			"System.IO",
			"System.Text",
			"System.Linq",
			"System.Collections",
			"System.Collections.Generic",
			"System.Threading",
			"System.Threading.Tasks",
			"System.Reflection",

            // Regex + Net + Http
            "System.Text.RegularExpressions",
			"System.Net",
			"System.Net.Http",

            // JSON
            "Newtonsoft.Json",
			"Newtonsoft.Json.Linq",

            // MAUI UI types
            "Microsoft.Maui.Controls",
			"Microsoft.Maui.Controls.Xaml",
			//"Microsoft.Maui.Essentials", // Note: Essentials is now merged
		};

		return ScriptOptions.Default
			.AddReferences(referencedAssemblies)
			.AddImports(defaultNamespaces);
	}

	public static void Cancel()
	{
		_cts?.Cancel();
	}

	public static async Task<string> Tissueluate(string code, CancellationToken cancellationToken)
	{
		var output = new StringBuilder();

		using var writer = new StringWriter(output);
		Console.SetOut(writer);
		var originalOut = Console.Out;

		var _cts = new CancellationTokenSource();

		var globals = new ScriptGlobals
		{
			CancellationToken = _cts.Token,
			Print = Console.WriteLine
		};

		try
		{
			//var script = CSharpScript.Create(
			//	code: code,
			//	options: BuildDefaultScriptOptions(),
			//	globalsType: typeof(ScriptGlobals)
			//	);
			//var state = await script.RunAsync(globals, globals.CancellationToken);
			await CSharpScript.EvaluateAsync(
				code: code,
				options: BuildDefaultScriptOptions(),
				globalsType: typeof(ScriptGlobals),
				globals: globals,
				cancellationToken: cancellationToken
			);

			return output.ToString();
		}
		catch (Exception ex)
		{
			throw;
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}
}