[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9]+\.[0-9]+\.[0-9]+-[a-z0-9]+(?:-[a-z0-9]+)*\.[0-9]+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$tag = "v$Version"
$packageDirectory = "artifacts/packages/$Version"

function Invoke-NativeCommand {
    param(
        [Parameter(Mandatory)]
        [scriptblock]$Command,
        [Parameter(Mandatory)]
        [string]$FailureMessage
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw "$FailureMessage Exit code: $LASTEXITCODE."
    }
}

$changes = git status --porcelain
if ($LASTEXITCODE -ne 0) {
    throw "Unable to read the Git working tree. Exit code: $LASTEXITCODE."
}

if ($changes) {
    throw 'Commit or stash all changes before publishing.'
}

if (git tag --list $tag) {
    throw "Tag $tag already exists locally."
}

$remoteTag = git ls-remote --tags origin "refs/tags/$tag"
if ($LASTEXITCODE -ne 0) {
    throw "Unable to read tags from origin. Exit code: $LASTEXITCODE."
}

if ($remoteTag) {
    throw "Tag $tag already exists on origin."
}

Invoke-NativeCommand { dotnet build DotNetToolbox.slnx --configuration Release } 'Build failed.'
Invoke-NativeCommand { dotnet test DotNetToolbox.slnx --configuration Release --no-build --filter 'Category!=Integration' } 'Unit tests failed.'
Invoke-NativeCommand { dotnet pack DotNetToolbox.slnx --configuration Release --no-build --output $packageDirectory "-p:PackageVersion=$Version" } 'Package creation failed.'

$packages = @(Get-ChildItem "$packageDirectory/*.nupkg")
if ($packages.Count -eq 0) {
    throw 'No NuGet packages were created.'
}

foreach ($package in $packages) {
    Invoke-NativeCommand { dotnet nuget push $package.FullName --source github-garywu123 --skip-duplicate } "Publishing $($package.Name) failed."
}

Invoke-NativeCommand { git tag -a $tag -m "Preview $Version" } "Creating tag $tag failed."
Invoke-NativeCommand { git push origin $tag } "Pushing tag $tag failed."