namespace Tasketier.Cli.Tests;

public sealed class ArgumentsTests
{
    [Fact]
    public void The_first_word_is_the_command_and_the_rest_is_the_text()
    {
        var arguments = new Arguments(["add", "Buy", "milk", "@fri"]);

        Assert.Equal("add", arguments.Command);
        Assert.Equal("Buy milk @fri", arguments.RestText);
    }

    [Fact]
    public void Options_take_a_value_unless_the_next_word_is_an_option()
    {
        var arguments = new Arguments(["list", "--view", "inbox", "--json", "--data", @"C:\x"]);

        Assert.Equal("inbox", arguments.Value("view"));
        Assert.True(arguments.Has("json"));
        Assert.Equal(string.Empty, arguments.Value("json"));
        Assert.Equal(@"C:\x", arguments.Value("data"));
        Assert.Null(arguments.Value("filter"));
        Assert.Empty(arguments.Rest);
    }

    [Fact]
    public void No_arguments_means_an_empty_command()
    {
        Assert.Equal(string.Empty, new Arguments([]).Command);
    }

    [Fact]
    public void A_repeated_option_is_a_usage_error()
    {
        Assert.Throws<UsageException>(() => new Arguments(["list", "--view", "a", "--view", "b"]));
    }
}
