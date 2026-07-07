using System.Diagnostics;
using Velopack;
using Velopack.Sources;

namespace TypingCore.Wpf.Services;

internal static class VelopackUpdateService
{
	private const string GitHubRepositoryUrl = "https://github.com/silevilence/TypingPractice";
	private const string ReleaseChannel = "win";

	public static async Task CheckAndDownloadLatestAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			GithubSource updateSource = new(GitHubRepositoryUrl, string.Empty, false, null);
			UpdateManager updateManager = new(updateSource, new UpdateOptions
			{
				ExplicitChannel = ReleaseChannel,
			});

			if (!updateManager.IsInstalled)
			{
				return;
			}

			UpdateInfo? updateInfo = await updateManager.CheckForUpdatesAsync();
			if (updateInfo is null)
			{
				return;
			}

			await updateManager.DownloadUpdatesAsync(updateInfo, null, cancellationToken);
			updateManager.WaitExitThenApplyUpdates(updateInfo.TargetFullRelease, silent: true, restart: false, []);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			Debug.WriteLine($"Velopack update check failed: {ex}");
		}
	}
}
