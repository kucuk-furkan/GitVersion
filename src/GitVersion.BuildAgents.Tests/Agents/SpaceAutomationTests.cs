using GitVersion.Core.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace GitVersion.Agents.Tests;

[TestFixture]
public class SpaceAutomationTests : TestBase
{
    private IEnvironment environment;
    private SpaceAutomation buildServer;

    [SetUp]
    public void SetUp()
    {
        var sp = ConfigureServices(services => services.AddSingleton<SpaceAutomation>());
        this.environment = sp.GetRequiredService<IEnvironment>();
        this.buildServer = sp.GetRequiredService<SpaceAutomation>();
        this.environment.SetEnvironmentVariable(SpaceAutomation.EnvironmentVariableName, "true");
    }

    [TearDown]
    public void TearDown() => this.environment.SetEnvironmentVariable(SpaceAutomation.EnvironmentVariableName, null);

    [Test]
    public void CanApplyToCurrentContextShouldBeTrueWhenEnvironmentVariableIsSet()
    {
        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public void CanApplyToCurrentContextShouldBeFalseWhenEnvironmentVariableIsNotSet()
    {
        // Arrange
        this.environment.SetEnvironmentVariable(SpaceAutomation.EnvironmentVariableName, "");

        // Act
        var result = this.buildServer.CanApplyToCurrentContext();

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public void GetCurrentBranchShouldHandleBranches()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("JB_SPACE_GIT_BRANCH", "refs/heads/master");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/heads/master");
    }

    [Test]
    public void GetCurrentBranchShouldHandleTags()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("JB_SPACE_GIT_BRANCH", "refs/tags/1.0.0");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/tags/1.0.0");
    }

    [Test]
    public void GetCurrentBranchShouldHandlePullRequests()
    {
        // Arrange
        this.environment.SetEnvironmentVariable("JB_SPACE_GIT_BRANCH", "refs/pull/1/merge");

        // Act
        var result = this.buildServer.GetCurrentBranch(false);

        // Assert
        result.ShouldBe("refs/pull/1/merge");
    }

    [Test]
    public void ShouldSetBuildNumber()
    {
        // Arrange
        var vars = new TestableGitVersionVariables { FullSemVer = "1.0.0" };

        // Act
        var message = this.buildServer.SetBuildNumber(vars);

        // Assert
        message.ShouldBeEmpty();
    }
}
