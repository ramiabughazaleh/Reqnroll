using BoDi;
using FluentAssertions;
using Reqnroll.SystemTests.Drivers;
using Reqnroll.TestProjectGenerator;
using Reqnroll.TestProjectGenerator.Data;
using Reqnroll.TestProjectGenerator.Driver;
using Reqnroll.TestProjectGenerator.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Reqnroll.SystemTests;

/// <summary>
/// The purpose of these tests to verify that the tests generated by the MsTest
/// generator compile and can execute with MsTest.
/// </summary>
public class MsTestGeneratorTest
{
    private readonly ProjectsDriver _projectsDriver;
    private readonly ExecutionDriver _executionDriver;
    private readonly VSTestExecutionDriver _vsTestExecutionDriver;
    private readonly TestFileManager _testFileManager = new();
    private readonly FolderCleaner _folderCleaner;

    public MsTestGeneratorTest(ITestOutputHelper testOutputHelper)
    {
        var objectContainer = new ObjectContainer();
        objectContainer.RegisterInstanceAs(testOutputHelper);
        objectContainer.RegisterTypeAs<XUnitOutputConnector, IOutputWriter>();

        var testRunConfiguration = objectContainer.Resolve<TestRunConfiguration>();
        testRunConfiguration.ProgrammingLanguage = ProgrammingLanguage.CSharp;
        testRunConfiguration.ProjectFormat = ProjectFormat.New;
        testRunConfiguration.ConfigurationFormat = ConfigurationFormat.Json;
        testRunConfiguration.TargetFramework = TargetFramework.Net60;
        testRunConfiguration.UnitTestProvider = UnitTestProvider.MSTest;

        var currentVersionDriver = objectContainer.Resolve<CurrentVersionDriver>();
        currentVersionDriver.NuGetVersion = NuGetPackageVersion.Version;
        currentVersionDriver.ReqnrollNuGetVersion = NuGetPackageVersion.Version;

        _folderCleaner = objectContainer.Resolve<FolderCleaner>();
        _folderCleaner.EnsureOldRunFoldersCleaned();

        _projectsDriver = objectContainer.Resolve<ProjectsDriver>();
        _executionDriver = objectContainer.Resolve<ExecutionDriver>();
        _vsTestExecutionDriver = objectContainer.Resolve<VSTestExecutionDriver>();
    }

    private void ShouldAllScenariosPass(int expectedNrOfTests)
    {
        _executionDriver.ExecuteTests();

        _vsTestExecutionDriver.LastTestExecutionResult.Should().NotBeNull();
        _vsTestExecutionDriver.LastTestExecutionResult.Total.Should().Be(expectedNrOfTests, $"the run should contain {expectedNrOfTests} tests");
        _vsTestExecutionDriver.LastTestExecutionResult.Succeeded.Should().Be(expectedNrOfTests, "all tests should pass");
        _folderCleaner.CleanSolutionFolder();
    }

    [Theory]
    [InlineData("GeneratorAllInSample1.feature", 12)]
    [InlineData("GeneratorAllInSample2.feature", 5)]
    public void Generator_samples_can_be_handled(string featureFileName, int expectedNrOfTests)
    {
        var featureFileContent = _testFileManager.GetTestFileContent(featureFileName);
        _projectsDriver.AddFeatureFile(featureFileContent);
        _projectsDriver.AddPassingStepBinding();
        ShouldAllScenariosPass(expectedNrOfTests);
    }

    // This is an example of a test that verifies a special case


    /// <summary>
    /// MsTest v2.* defines the [DataRow] attribute with a ctor that cannot handle if the second
    /// parameter of the attribute is a string[]. This causes problems with single-column examples,
    /// because in this case the second parameter is a list of example block tags passed in as string[].
    /// </summary>
    [Fact]
    public void Generator_handles_tagged_examples_block_with_single_column()
    {
        _projectsDriver.AddScenario(
            """
            Scenario Outline: Sample Scenario Outline
                When <what> happens
            @example_tag
            Examples:
                | what |
                | foo  |
                | bar  |
            Examples: Second example without tags - in this case the tag list is null.
                | what |
                | baz  |
            """);
        _projectsDriver.AddPassingStepBinding();
        ShouldAllScenariosPass(3);
    }
}