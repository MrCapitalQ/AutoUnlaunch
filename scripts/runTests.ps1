$currentDir = Get-Location

try {
    Set-Location $PSScriptRoot
    Set-Location ..

    # Clean previous code coverage data
    Get-ChildItem -Path . -Recurse | Where-Object { $_.Name -ilike "TestResults" } | Remove-Item -Force -Recurse

    # EnableMsixTooling MSBuild property is needed to build tests from dotnet CLI
    dotnet test `
        --collect:"XPlat Code Coverage" `
        /p:EnableMsixTooling=true `
        /p:Platform=x64 `
        -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile="**/*.g.cs,**/*.g.i.cs"
    if (!$?) { Exit $LASTEXITCODE }

    reportgenerator -reports:"./test/**/coverage.cobertura.xml" -targetdir:"./TestResults/CoverageReport" -reporttypes:Html
    if (!$?) { Exit $LASTEXITCODE }

    Start-Process ./TestResults/CoverageReport/index.html
}
finally {
    # Return to original directory
    Set-Location $currentDir
}
