using System.Text;
using System.Windows;

namespace TypingCore.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
	/// <inheritdoc />
	protected override void OnStartup(StartupEventArgs e)
	{
		Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		base.OnStartup(e);
	}
}

