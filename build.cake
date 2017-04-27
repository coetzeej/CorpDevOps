//Addins
// NEEDED #addin Cake.VersionReader
#addin Cake.FileHelpers
#tool "nuget:?package=NUnit.ConsoleRunner"
#tool "nuget:?package=OpenCover"
#tool "nuget:?package=GitVersion.CommandLine"

var target = Argument("target", "Default");
var buildType = Argument<string>("buildType", "develop");
var tools = "./tools";

var sln = "./src/CorpDevOps.sln";

var releaseFolder = "./src/CorpDevOps/bin/Release";
var releaseExe = "/webapp.exe";

var unitTestPaths = "./src/webapp.tests/bin/Debug/webapp.tests.dll";

var nuspecFile = "./src/CorpDevOps.nuspec";

var coverPath = "./CoverageResults.xml";

var testResultFile = "./TestResult.xml";
var testErrorFile = "./Errors.xml";
var testSucceeded = false; 

var sonarUrl = "https://github.com/SonarSource-VisualStudio/sonar-scanner-msbuild/releases/download/2.1/MSBuild.SonarQube.Runner-2.1.zip";
var sonarZipPath = tools + "/SonarQube.zip";
var sonarQubeServerUrl = "";
var sonarQubeProject = "";
var sonarQubeKey = "";

Task("Install")
	.Does(() =>{
		if(!FileExists(sonarZipPath))
		{
			Information("Downloading SonarQube");
			DownloadFile(sonarUrl, sonarZipPath);
		}
		if(!FileExists(tools + "/MSBuild.SonarQube.Runner.exe"))
		{
			Information("Extraction SonarQube");
			Unzip(tools + "/SonarQube.zip", tools + "/");
		}
});

Task("Configure")
	.IsDependentOn("Install")
	.Does(() =>
	{
		Information("TeamCity: " + TeamCity.IsRunningOnTeamCity);
		Information("AppVeyor: " + AppVeyor.IsRunningOnAppVeyor);
		Information("BuildType: " + buildType);
		Information("BuildNo: " + GitVersion(new GitVersionSettings {
        	UpdateAssemblyInfo = true
    	}).FullSemVer);
});

Task("Build")
	.IsDependentOn("Configure")
	.Does (() => {
		var version = GitVersion();		
		DotNetBuild (sln, c => c.Configuration = "Release");
		if (TeamCity.IsRunningOnTeamCity)
		{
			TeamCity.SetBuildNumber(version.FullSemVer);
		}
		else
		{
			
		}
});

Task("Test")
	.IsDependentOn("Build")
	.Does(() =>
	{
		var testAssemblies = GetFiles(unitTestPaths);
		OpenCover(tool => {
				tool.NUnit3(testAssemblies, new NUnit3Settings {
    				ErrorOutputFile = testErrorFile
    			});
			},
			new FilePath(coverPath),
			new OpenCoverSettings()
                .WithFilter("+:webapp")
    			.WithFilter("-:webapp.tests")
		);

		if(FileExists(testErrorFile) && FileReadLines(testErrorFile).Count() > 0)
		{
			Information("Unit tests failed");
			testSucceeded = false;
		}
		else
		{
			testSucceeded = true;
		}
});
	
Task("Publish")
	.WithCriteria(buildType == "master")
	.Does (() => {
		// if(!testSucceeded)
		// {
		// 	Error("Unit tests failed - Cannot push to Nuget");
		// 	throw new Exception("Unit tests failed");
		// }

		// CreateDirectory ("./nupkg/");
		// ReplaceRegexInFiles(nuspecFile, "0.0.0", version);
		
		// NuGetPack (nuspecFile, new NuGetPackSettings { 
		// 	Verbosity = NuGetVerbosity.Detailed,
		// 	OutputDirectory = "./nupkg/"
		// });	
});

Task("Deploy")
	.WithCriteria(buildType == "master")
	.IsDependentOn("Test")
	.Does (() => {
		// // Get the newest (by last write time) to publish
		// var newestNupkg = GetFiles ("nupkg/*.nupkg")
		// 	.OrderBy (f => new System.IO.FileInfo (f.FullPath).LastWriteTimeUtc)
		// 	.LastOrDefault();

		// var apiKey = EnvironmentVariable("NugetKey");

		// NuGetPush (newestNupkg, new NuGetPushSettings { 
		// 	Verbosity = NuGetVerbosity.Detailed,
		// 	Source = "https://www.nuget.org/api/v2/package/",
		// 	ApiKey = apiKey
		// });
});

Task("Default")
	.IsDependentOn("Test");

Task("Release")
    .IsDependentOn("Deploy");

TaskSetup((context, task) =>
{
	if (TeamCity.IsRunningOnTeamCity)
	{
		TeamCity.WriteStartBlock(task.Task.Name);
	}    
});

TaskTeardown((context, task) =>
{
	if (TeamCity.IsRunningOnTeamCity)
	{
		TeamCity.WriteEndBlock(task.Task.Name);
	}
});

RunTarget (target);
