# ReportGenerator - *dotnet-reportgenerator-globaltool*
ReportGenerator converts coverage reports generated by coverlet, OpenCover, dotCover, Visual Studio, NCover, Cobertura, JaCoCo, Clover, gcov, or lcov into human readable reports in various formats. The reports show the coverage quotas and also visualize which lines of your source code have been covered.

## Available packages

|**Package**|**Platforms**|**Installation/Usage**|
|:----------|:------------|:---------------------|
|[ReportGenerator](https://www.nuget.org/packages/ReportGenerator)|.NET Core<br/>.NET Framework 4.7|Use this package if your project is based on *.NET Framework* or *.NET Core* and you want to use *ReportGenerator* via the command line or a build script.|
|[dotnet-reportgenerator-globaltool](https://www.nuget.org/packages/dotnet-reportgenerator-globaltool)|.NET Core|Use this package if your project is based on *.NET Core* and you want to use *ReportGenerator* as a (global) 'DotnetTool'.|
|[ReportGenerator.Core](https://www.nuget.org/packages/ReportGenerator.Core)|.NET Standard 2.0|Use this package if you want to write a custom **plugin** for *ReportGenerator* or if you want to call/execute *ReportGenerator* within your code base.|

## Usage

### Installation
```
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.4.8

dotnet tool install dotnet-reportgenerator-globaltool --tool-path tools --version 5.4.8

dotnet new tool-manifest
dotnet tool install dotnet-reportgenerator-globaltool --version 5.4.8
```

### Execution
```
reportgenerator [options]
tools\reportgenerator.exe [options]
dotnet reportgenerator [options]
```

## Additional information
- [Get started](https://reportgenerator.io/getstarted)
- [Command line parameters](https://reportgenerator.io/usage)
