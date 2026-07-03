using System.Reflection;

namespace TypingCore.Tests;

public class CoreAbstractionsContractTests
{
    private static readonly Assembly CoreAssembly = Assembly.Load("TypingCore");

    [Theory]
    [InlineData("TypingCore.Abstractions.IKeyInputEvent")]
    [InlineData("TypingCore.Abstractions.ITypingSession")]
    [InlineData("TypingCore.Abstractions.IStatisticsProvider")]
    [InlineData("TypingCore.Abstractions.ICodeTableProvider")]
    [InlineData("TypingCore.Abstractions.IArticleRepository")]
    [InlineData("TypingCore.Abstractions.ISessionRepository")]
    [InlineData("TypingCore.Abstractions.ITypingSessionSnapshot")]
    [InlineData("TypingCore.Abstractions.IStatisticsSnapshot")]
    [InlineData("TypingCore.Abstractions.ICodeLookupResult")]
    [InlineData("TypingCore.Abstractions.IArticleRecord")]
    [InlineData("TypingCore.Abstractions.ISessionRecord")]
    public void Required_interface_contracts_exist(string typeName)
    {
        Type? type = CoreAssembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.True(type!.IsInterface, $"{typeName} should be an interface.");
    }

    [Theory]
    [InlineData("TypingCore.Abstractions.KeyInputKey")]
    [InlineData("TypingCore.Abstractions.TypingSessionState")]
    public void Required_enum_contracts_exist(string typeName)
    {
        Type? type = CoreAssembly.GetType(typeName);

        Assert.NotNull(type);
        Assert.True(type!.IsEnum, $"{typeName} should be an enum.");
    }
}