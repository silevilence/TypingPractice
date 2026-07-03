using TypingCore.Abstractions;
using TypingCore.Models;

namespace TypingCore.Tests.Models;

public class SessionStatisticsTests
{
    [Fact]
    public void SessionStatistics_exposes_cpm_and_wpm_through_contract()
    {
        SessionStatistics statistics = new(
            320,
            260,
            52,
            4.1,
            3,
            0.1,
            0.04,
            TimeSpan.FromSeconds(95));

        IStatisticsSnapshot snapshot = statistics;

        Assert.Equal(320, snapshot.KeystrokesPerMinute);
        Assert.Equal(260, snapshot.CharactersPerMinute);
        Assert.Equal(52, snapshot.WordsPerMinute);
        Assert.Equal(4.1, snapshot.AverageCodeLength);
        Assert.Equal(3, snapshot.BackspaceCount);
        Assert.Equal(0.1, snapshot.BackspaceRate);
        Assert.Equal(0.04, snapshot.ErrorRate);
        Assert.Equal(TimeSpan.FromSeconds(95), snapshot.Elapsed);
    }
}