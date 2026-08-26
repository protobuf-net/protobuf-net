#!/usr/bin/env bash
# Shows the version the current commit computes - i.e. the tag name to type into the GitHub
# Release UI. Nerdbank.GitVersioning derives it from src/version.json plus commit height; creating
# the release does not change it, and release.yml refuses a tag that disagrees with it.
#
# Run this on an up-to-date release branch (main, or a vN line): the version belongs to the commit
# you are on, and see the PublicRelease note below for what happens elsewhere.
set -euo pipefail
cd "$(dirname "$0")"

dotnet tool restore > /dev/null
# NuGetPackageVersion, deliberately, because that is the EXACT string release.yml's guard compares
# the tag against. This used to report the first three components of `Version`, which silently
# drops any prerelease label: on 4.0-alpha that produced "4.0.124" while the guard wanted
# "4.0.124-alpha", so following this script's own instruction failed the release. It went unnoticed
# because 3.3 and 3.4 were stable, where the two spellings coincide.
pkg=$(dotnet tool run nbgv -- get-version --project src --variable NuGetPackageVersion | tr -d '[:space:]')
v=$(dotnet tool run nbgv -- get-version --project src --variable Version | tr -d '[:space:]')
public=$(dotnet tool run nbgv -- get-version --project src --variable PublicRelease | tr -d '[:space:]')
where="$(git rev-parse --abbrev-ref HEAD) @ $(git rev-parse --short HEAD)"

echo "commit           : $where"
echo "computed version : $v"
echo "release tag      : $pkg"
echo ""

# off a release branch nbgv appends a commit id, so the version above is NOT what a release would
# publish - report that rather than let it be copied into the Release UI
if [ "$public" != "True" ]; then
    echo "WARNING: this ref is not a public release (see publicReleaseRefSpec in src/version.json)," >&2
    echo "WARNING: so the version above carries a commit id and is not a releasable tag." >&2
    echo "WARNING: switch to main or the vN release branch and re-run." >&2
    exit 0
fi

echo "Releases -> Draft a new release -> tag '$pkg' -> publish; release.yml does the rest."
case "$pkg" in
    *-*) echo "This is a PRERELEASE ($pkg); tick 'Set as a pre-release' in the Release UI." ;;
esac
