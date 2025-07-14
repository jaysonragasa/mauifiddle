using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Newtonsoft.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace MAUIFiddle;

public partial class MainPage : ContentPage
{
	DateTime _lastExec = DateTime.MinValue;
	int delayBeforeExecute = 10; // Delay in milliseconds before executing the code

	public MainPage()
	{
		InitializeComponent();
		NewCSharpCode();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();
		_lastExec = DateTime.Now;
		
		//OnRunClicked(this, null);
		//LoadEditorHtml();
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

	private int _previousTextLength = 0;
	private void CodeEditor_TextChanged(object sender, TextChangedEventArgs e)
	{
		////int tabCount = GetCurrentLineTabIndentation((Editor)sender);
		////Debug.WriteLine(tabCount);

		var editor = (Editor)sender;

		//// Detect Enter key (new \n character added)
		//if (editor.Text.Length > _previousTextLength &&
		//	editor.CursorPosition > 0 &&
		//	editor.Text[editor.CursorPosition - 1] == '\r')
		//{
		//	// Count tabs in the current (previous) line
		//	int tabCount = GetTabCountInPreviousLine(editor);

		//	// Build the tab string
		//	string tabs = new string('\t', tabCount);

		//	// Insert tabs at current cursor position
		//	int cursorPos = editor.CursorPosition;
		//	editor.Text = editor.Text.Insert(cursorPos, tabs);

		//	// Move cursor after the inserted tabs
		//	editor.CursorPosition = cursorPos + tabs.Length;
		//}

		//_previousTextLength = editor.Text?.Length ?? 0;

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
		for(int i = 0; i < lines; i++)
		{
			sb.AppendLine((i + 1).ToString());
		}
		lineNumber.Text = sb.ToString();
	}

	private void ToolbarItem_Clicked(object sender, EventArgs e)
	{
		NewCSharpCode();
		//Shell.Current.CurrentPage.Navigation.PushModalAsync();
		//var a = new ContentPage().LoadFromXaml("asd");
	}

	void NewCSharpCode()
	{
		CodeEditor.Text = @"using System;

public class Say
{
	public static void Hello()
	{
		Console.WriteLine(""Hello World"");
	}
}

Say.Hello();";
	}

	private int GetTabCountInPreviousLine(Editor editor)
	{
		string text = editor.Text;
		int cursorIndex = editor.CursorPosition;

		if (string.IsNullOrEmpty(text) || cursorIndex <= 0)
			return 0;

		// Find start of the previous line
		int prevLineEnd = text.LastIndexOf('\r', Math.Max(0, cursorIndex - 2));
		int prevLineStart = (prevLineEnd == -1) ? 0 : text.LastIndexOf('\n', Math.Max(0, prevLineEnd - 1)) + 1;

		if (prevLineStart < 0) prevLineStart = 0;
		if (prevLineEnd < 0) prevLineEnd = text.Length;

		string prevLine = text.Substring(prevLineStart, prevLineEnd - prevLineStart + 1);
		return prevLine.Count(c => c == '\t');
	}

	public int GetCurrentLineTabIndentation(Editor editor)
	{
		// 1. Get the current state of the editor
		string text = editor.Text;
		int cursorPosition = editor.CursorPosition;

		// Handle edge cases where text is null, empty, or cursor is at the start
		if (string.IsNullOrEmpty(text) || cursorPosition == 0)
		{
			return 0;
		}

		// 2. Find the start of the current line.
		// We search backwards from the character *before* the cursor for a newline character ('\n').
		// If no newline is found, it means we are on the first line, so the start is index 0.
		// We add 1 to move past the newline character itself.
		int lineStartIndex = text.LastIndexOf('\r', cursorPosition - 1) + 1;

		// 3. Count the leading tabs from the start of the line.
		int tabCount = 0;
		for (int i = lineStartIndex; i < text.Length; i++)
		{
			if (text[i] == '\t') // The tab character
			{
				tabCount++;
			}
			else
			{
				// Stop counting as soon as a non-tab character is found
				break;
			}
		}

		return tabCount;
	}

	private void EditorStoppedTypingBehavior_StoppedTyping(object sender, string e)
	{
		OnRunClicked(sender, new EventArgs());
	}

	//async void LoadEditorHtml()
	//{
	//	var assembly = typeof(MainPage).GetTypeInfo().Assembly;
	//	using var stream = await FileSystem.OpenAppPackageFileAsync("editor.html");
	//	//var stream = assembly.GetManifestResourceStream("MAUIFiddle.editor.html");
	//	using var reader = new StreamReader(stream);
	//	var htmlString = reader.ReadToEnd();
	//	CodeEditor.Source = new HtmlWebViewSource { Html = htmlString };
	//}
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
		catch(Exception ex)
		{
			throw;
		}
		finally
		{
			Console.SetOut(originalOut);
		}
	}
}