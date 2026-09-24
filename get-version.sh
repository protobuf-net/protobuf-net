#!/usr/bin/env bash
# Shows the version the current commit computes - i.e. the tag name to type into the GitHub
# Release UI. Nerdbank.GitVersioning derives it from src/version.json plus commit height; creating
# the release does not change it, and release.yml refuses a tag that disagrees with it.
#
# Reports NuGetPackageVersion, which is what release.yml's guard compares against. That is the
# same as the three-part version for a stable release from main, and NOT the same for a
# prerelease - where the tag carries the suffix ("0.1.1-alpha", not "0.1.1").
#
# Run this on up-to-date main: the version belongs to the commit you are on.
set -euo pipefail
cd "$(dirname "$0")"

dotnet tool restore > /dev/null
tag=$(dotnet tool run nbgv -- get-version --project src --variable NuGetPackageVersion | tr -d '[:space:]')
where="$(git rev-parse --abbrev-ref HEAD) @ $(git rev-parse --short HEAD)"

echo "commit           : $where"
echo "release tag      : $tag"
echo ""
echo "Releases -> Draft a new release -> tag '$tag' -> publish; release.yml does the rest."
