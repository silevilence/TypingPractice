using System.IO;
using System.Text;
using System.Windows;
using TypingCore.Abstractions;
using TypingCore.Engine;
using TypingCore.Parsing;
using TypingCore.Persistence;
using TypingCore.Wpf.Services;
using TypingCore.Wpf.ViewModels;

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

		MainWindow window = BuildMainWindow();
		MainWindow = window;
		window.Show();
	}

	private static MainWindow BuildMainWindow()
	{
		string appDataDirectory = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"TypingPractice");
		Directory.CreateDirectory(appDataDirectory);

		string connectionString = $"Data Source={Path.Combine(appDataDirectory, "typing-practice.db")}";
		IArticleRepository articleRepository = new SqliteArticleRepository(connectionString);
		ISessionRepository sessionRepository = new SqliteSessionRepository(connectionString);
		IArticleImportService articleImportService = new ArticleImportService();
		IArticleTextLayoutBuilder articleTextLayoutBuilder = new ArticleTextLayoutBuilder();
		IFileDialogService fileDialogService = new FileDialogService();
		IClipboardService clipboardService = new ClipboardService();
		ISystemClock systemClock = new SystemClock();
		ICodeTableParser codeTableParser = new CodeTableParser();
		ICodeTableRepository codeTableRepository = new FileCodeTableRepository(
			Path.Combine(appDataDirectory, "CodeTables"),
			codeTableParser);
		CodeTableProvider codeTableProvider = new();

		ArticleLibraryViewModel articleLibrary = new(
			articleRepository,
			articleImportService,
			fileDialogService,
			clipboardService,
			systemClock);
		HistoryViewModel history = new(articleRepository, sessionRepository);
		IUserPreferencesRepository preferencesRepository = new JsonUserPreferencesRepository(
			Path.Combine(appDataDirectory, "preferences.json"));
		SettingsViewModel settings = new(
			preferencesRepository,
			ApplicationThemeManager.Apply);
		CodeTableManagerViewModel codeTableManager = new(
			codeTableRepository,
			codeTableProvider,
			fileDialogService,
			systemClock);
		MainViewModel mainViewModel = new(
			articleLibrary,
			history,
			settings,
			articleTextLayoutBuilder,
			systemClock,
			sessionRepository,
			codeTableManager,
			codeTableProvider);

		return new MainWindow(mainViewModel);
	}
}

