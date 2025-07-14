#if WINDOWS
using Microsoft.Maui.Handlers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MAUIFiddle;

public class NoWrapEditorHandler : EditorHandler
{
	protected override void ConnectHandler(TextBox platformView)
	{
		base.ConnectHandler(platformView);
		platformView.TextWrapping = TextWrapping.NoWrap;
	}
}
#endif
