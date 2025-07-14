#if ANDROID
using AndroidX.AppCompat.Widget;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;

namespace MAUIFiddle;

public class NoWrapEditorHandler : EditorHandler
{
	protected override void ConnectHandler(AppCompatEditText platformView)
	{
		base.ConnectHandler(platformView);

		// https://developer.android.com/reference/android/graphics/text/LineBreakConfig#LINE_BREAK_STYLE_NONE
		platformView.LineBreakStyle = 0;

		//https://developer.android.com/reference/android/widget/TextView#getLineBreakWordStyle()
		platformView.LineBreakWordStyle = 0;

		platformView.SetHorizontallyScrolling(true);
		//platformView.SetSingleLine(false);
		platformView.SetSingleLine(false);

		platformView.SetPadding(10, 0, 10, 0);
		platformView.Background = null;
	}
}
#endif