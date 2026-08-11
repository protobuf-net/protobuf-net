# Shows the version the current commit computes - i.e. the tag name to type into the GitHub
# Release UI. Nerdbank.GitVersioning derives it from src/version.json plus commit height; creating
# the release does not change it, and release.yml refuses a tag that disagrees with it.
#
# Run this on up-to-date main: the version belongs to the commit you are on.
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    dotnet tool restore | Out-Null
    $v = (dotnet tool run nbgv -- get-version --project src --variable Version).Trim()
    $tag = ($v.Split('.')[0..2]) -join '.'
    $where = "$(git rev-parse --abbrev-ref HEAD) @ $(git rev-parse --short HEAD)"

    Write-Output "commit           : $where"
    Write-Output "computed version : $v"
    Write-Output "release tag      : $tag"
    Write-Output ""
    Write-Output "Releases -> Draft a new release -> tag '$tag' -> publish; release.yml does the rest."
}
finally { Pop-Location }
