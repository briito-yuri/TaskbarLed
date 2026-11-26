#!/bin/bash
set -euo pipefail

cd "$(dirname "$0")/../.."
dotnet_cmd="${DOTNET:-dotnet}"
output="$PWD/artifacts/TaskbarLed-osx-arm64"
app="$output/TaskbarLed.app"
bin="$app/Contents/MacOS"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT

"$dotnet_cmd" publish src/TaskbarLed.App.Avalonia -c Release -r osx-arm64 --self-contained -o "$stage/app"
"$dotnet_cmd" publish src/TaskbarLed.Hook -c Release -r osx-arm64 --self-contained -o "$stage/hook"
mkdir -p "$bin"
cp -R "$stage/app/." "$bin/"
for file in TaskbarLed.Hook TaskbarLed.Hook.dll TaskbarLed.Hook.deps.json TaskbarLed.Hook.runtimeconfig.json; do
    cp "$stage/hook/$file" "$bin/"
done
cat > "$app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key><string>io.github.briito-yuri.taskbarled</string>
    <key>CFBundleName</key><string>TaskbarLed</string>
    <key>CFBundleExecutable</key><string>TaskbarLed</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <key>LSUIElement</key><true/>
</dict>
</plist>
PLIST

codesign --force --deep --sign - "$app"
ditto -c -k --keepParent "$app" "$output/TaskbarLed-osx-arm64.zip"
printf '%s\n' "$app"
