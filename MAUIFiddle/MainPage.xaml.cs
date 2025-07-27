using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace MAUIFiddle;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		CodeEditor.Text = @"Print(""Start writing your code"")";

		fontSizeSlider.ValueChanged += FontSizeSlider_ValueChanged;
	}

	private async void FontSizeSlider_ValueChanged(object? sender, ValueChangedEventArgs e)
	{
		await HtmlCodeEditor.EvaluateJavaScriptAsync($"setEditorFontSize({e.NewValue});");
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		LoadEditorHtml();
	}

	private async void OnRunClicked(object sender, EventArgs e)
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

	private void ToolbarItem_Clicked(object sender, EventArgs e)
	{
		NewCSharpCode();
	}

	async void NewCSharpCode()
	{
		string code = @"using System;

public class Say
{
	public static void Hello()
	{
		Console.WriteLine(""Hello World"");
	}
}

Say.Hello();";

		code = System.Web.HttpUtility.UrlEncode(code);
		await HtmlCodeEditor.EvaluateJavaScriptAsync($"setEditorText(\"{code}\");");
	}

	private void EditorStoppedTypingBehavior_StoppedTyping(object sender, string e)
	{
		OnRunClicked(sender, new EventArgs());
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
}

public class ScriptGlobals
{
	public CancellationToken CancellationToken { get; set; }
	public Action<string> Print = Console.WriteLine;
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