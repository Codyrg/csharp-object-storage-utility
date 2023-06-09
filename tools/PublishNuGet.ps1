param(
    [bool]$allowUncommittedChanges = $false
)

$readme = "readme.md"

$solutionName = "SimpleObjectStorage.sln"
$simpleObjectStorageFolder = "SimpleObjectStorage"
$libraryCsproj = "./$simpleObjectStorageFolder/SimpleObjectStorage.csproj"
$releaseFolder = "$simpleObjectStorageFolder/bin/Release"

# checks
Write-Host "Checking that this script is being run from the root of the repository..." -ForegroundColor Gray
if (!(Test-Path $solutionName)) {
    Write-Host "`tThis script must be run from the root of the repository." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Checking that the user is on the master branch..." -ForegroundColor Gray
$branch = git rev-parse --abbrev-ref HEAD
if ($branch -ne "master") {
    Write-Host "`tThe user must be on the master branch to release." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Checking that the user has no uncommitted changes..." -ForegroundColor Gray
$uncommittedChanges = git status --porcelain
if ($uncommittedChanges -and !$allowUncommittedChanges) {
    Write-Host "`tThe user must have no uncommitted changes to release." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

if ($uncommittedChanges) {
    Write-Host "Adding uncommitted changes..." -ForegroundColor Gray
    git add .
    Write-Host "`tOK" -ForegroundColor Green
}

Write-Host "Checking that the user has no unpushed changes..." -ForegroundColor Gray
$unpushedChanges = git cherry -v
if ($unpushedChanges) {
    Write-Host "`tThe user must have no unpushed changes to release." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Checking that the user has the NuGet key..." -ForegroundColor Gray
$nugetKey = $env:NUGET_KEY
if (!$nugetKey) {
    Write-Host "`tThe user must have the NuGet key in the $nugetKeyEnvName environment variable." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

# run tests
Write-Host "Running unit tests..." -ForegroundColor Gray
./tools/RunTests.ps1
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tOne or more unit tests failed." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

# release
# clear out the release folder if it exists
Write-Host "Clearing out the release folder..." -ForegroundColor Green
if (Test-Path $releaseFolder) {
    Get-ChildItem -Path $releaseFolder -Recurse | Remove-Item -Force -Recurse
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Incrementing the version..." -ForegroundColor Gray
./tools/SemVer.ps1 -mode build
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tFailed to increment the version." -ForegroundColor Red
    exit 1
}
# git add cs proj
git add $libraryCsproj 
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Getting the version from the csproj file..." -ForegroundColor Gray
$version = ""
$semVer = Get-Content $libraryCsproj | Select-String -Pattern "<SemVer>(\d+\.\d+\.\d+)</SemVer>"
if ($semVer) {
    $version = $semVer.Matches.Groups[1].Value
    Write-Host "`tOK" -ForegroundColor Green
} else {
    Write-Host "`tFailed to get the version from the csproj file." -ForegroundColor Red
    exit 1
}

# Replace Version="<old version>" with Version="<new version>" in the readme
Write-Host "Replacing the version in the readme..." -ForegroundColor Gray
$readmeText = Get-Content $readme
$readmeText = $readmeText -replace "Version=""\d+\.\d+\.\d+""", "Version=""$version"""
$readmeText | Set-Content $readme
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Building the library..." -ForegroundColor Gray
dotnet build $libraryCsproj -c Release
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tFailed to build the library." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Pushing the readme..." -ForegroundColor Gray
git add $readme
git commit -m "Update readme for version $version"
git push
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tFailed to push the readme." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

# find the nupkg file if it exists
Write-Host "Finding the nupkg folder..." -ForegroundColor Gray
$nupkgFile = Get-ChildItem -Path $releaseFolder -Filter "*.nupkg" -Recurse
if ($nupkgFile) {
    Write-Host "`tOK" -ForegroundColor Green
} else {
    Write-Host "`tFailed to find the nupkg folder." -ForegroundColor Red
    exit 1
}

# parse the version from the nupkg file
Write-Host "Parsing the version from the nupkg file..." -ForegroundColor Gray
$version = $nupkgFile.Name -replace "AppFiles.", ""
$version = $version -replace ".nupkg", ""
if ($version) {
    Write-Host "`tOK" -ForegroundColor Green
} else {
    Write-Host "`tFailed to parse the version from the nupkg file." -ForegroundColor Red
    exit 1
}


Write-Host "Pushing the NuGet package..." -ForegroundColor Gray
dotnet nuget push $nupkgFile.FullName --api-key $env:NUGET_KEY --source https://api.nuget.org/v3/index.json
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tFailed to push the NuGet package." -ForegroundColor Red
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green

Write-Host "Tagging the version." -ForegroundColor Gray
git tag -a $version -m "Version $version"
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tFailed to tag the version." -ForegroundColor Red
    exit 1
}
Write-Host "`tPush the tag." -ForegroundColor Green


Write-Host "Push the tag..." -ForegroundColor Gray
git push origin $version
if ($LASTEXITCODE -ne 0) {
    Write-Host "`tFailed to push the tag." -ForegroundColor Gray
    exit 1
}
Write-Host "`tOK" -ForegroundColor Green
exit 0
