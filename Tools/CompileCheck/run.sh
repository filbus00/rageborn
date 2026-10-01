#!/usr/bin/env bash
# Compile check and EditMode tests without Unity (for cloud sessions; see README.md).
#   Tools/CompileCheck/run.sh            compile against .NET Standard 2.1, then run every test
#   Tools/CompileCheck/run.sh Legendary  ... then only tests whose name contains "Legendary"
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
if ! command -v dotnet >/dev/null; then
  echo "Installing dotnet-sdk-8.0 from the Ubuntu archive..."
  sudo_cmd=""; [ "$(id -u)" -ne 0 ] && sudo_cmd="sudo"
  $sudo_cmd apt-get install -y -qq dotnet-sdk-8.0 >/dev/null
fi
echo "== Compile (netstandard2.1, as Unity) =="
dotnet build "$here/Editor/Editor.csproj" -nologo -v q 2>&1 | grep -E "error|Build succeeded|FAILED" | sed "s#$root/##" | sort -u
echo "== EditMode tests (net8.0) =="
dotnet build "$here/Runner/Runner.csproj" -nologo -v q -p:TF=net8.0 2>&1 | grep -E " error " | sed "s#$root/##" | sort -u || true
cd "$root"
dotnet "$here/Runner/bin/Debug/net8.0/Runner.dll" "$@" | sed "s#$root/##g"
