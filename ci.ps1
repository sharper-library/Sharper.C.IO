$TestsRegex = '\.Tests$'

$dotnetCliUrl = 'https://dotnetcli.blob.core.windows.net/dotnet/preview/Binaries/Latest/dotnet-dev-win-x64.latest.zip'

if (Test-Path '.dotnet-cli/dotnet.exe') {
    $dotnet = '.dotnet-cli/dotnet.exe'
}
else {
    $dotnet = 'dotnet.exe'
}

function ExtractZip($srcZip, $destDir) {
    Add-Type -Assembly "System.IO.Compression.FileSystem"
    [IO.Compression.ZipFile]::ExtractToDirectory($srcZip, $destDir)
}

function AllProjects() {
    Get-ChildItem */project.json
}

function PackageProjects() {
    AllProjects | Where {$_.Directory.Name -notmatch $TestsRegex}
}

function TestProjects() {
    AllProjects | Where {$_.Directory.Name -match $TestsRegex}
}

function CleanCmd() {
    AllProjects | %{$_.Directory} | %{
        if (Test-Path $_/bin) {Remove-Item -Recurse $_/bin}
        if (Test-Path $_/obj) {Remove-Item -Recurse $_/obj}
    }
    if (Test-Path artifacts) {Remove-Item -Recurse artifacts}
}

function EnsureDotnetCliCmd() {
    if (!(Test-Path '.dotnet-cli/dotnet.exe'))
    {
        Invoke-WebRequest $dotnetCliUrl -OutFile 'dotnet-cli.zip'
        ExtractZip 'dotnet-cli.zip' "$pwd/.dotnet-cli"
        $dotnet = '.dotnet-cli/dotnet.exe'
    }
}

function InstallCmd() {
    & $dotnet restore
}

function BuildCmd() {
    Write-Host "Building projects:"
    PackageProjects | %{Write-Host "   $_"}
    if ($env:BUILD_BUILDNUMBER) {
      $env:DOTNET_BUILD_VERSION = $env:BUILD_BUILDNUMBER
    }
    else {
      $env:DOTNET_BUILD_VERSION = 'z'
    }
    PackageProjects | %{
      Write-Host "Building $_"
      & $dotnet pack $_.Directory -c Release
    }
}

function TestCmd() {
    $codes = (TestProjects) | %{& $dotnet test $_ | Write-Host; $LASTEXITCODE}
    $code = ($codes | Measure-Object -Sum).Sum
    exit $code
}

function RegisterCmd() {
    PackageProjects | %{
        Get-ChildItem -Recurse *.nupkg | %{
            nuget add $_ -Source "$env:USERPROFILE/.nuget/packages"
        }
    }
}

function RunCommand($name) {
    switch ($name) {
        ensurecli {EnsureDotnetCliCmd}
        clean {CleanCmd}
        install {InstallCmd}
        build {BuildCmd}
        test {TestCmd}
        register {RegisterCmd}
        all {CleanCmd; InstallCmd; BuildCmd; RegisterCmd}
    }
}

$args | %{RunCommand $_}
