#!/usr/bin/env bash
# Shows the version the current commit computes - i.e. the tag name to type into the GitHub
# Release UI. Nerdbank.GitVersioning derives it from src/version.json plus commit height; creating
# the release does not change it, and release.yml refuses a tag that disagrees with it.
#
# Run this on up-to-date main: the version belongs to the commit you are on.
set -euo pipefail
cd "$(dirname "$0")"

dotnet tool restore > /dev/null
v=$(dotnet tool run nbgv -- get-version --project src --variable Version | tr -d '[:space:]')
tag=$(echo "$v" | cut -d. -f1-3)
where="$(git rev-parse --abbrev-ref HEAD) @ $(git rev-parse --short HEAD)"

echo "commit           : $where"
echo "computed version : $v"
echo "release tag      : $tag"
echo ""
echo "Releases -> Draft a new release -> tag '$tag' -> publish; release.yml does the rest."
