#!/bin/bash
set -euo pipefail

cd "$(dirname "$0")/../.."
dotnet_cmd="${DOTNET:-dotnet}"
rid="${1:-osx-arm64}"
case "$rid" in
    osx-arm64) host_arch=arm64 ;;
    osx-x64) host_arch=x86_64 ;;
    *) printf 'Usage: %s [osx-arm64|osx-x64]\n' "$0" >&2; exit 2 ;;
esac
output="$PWD/artifacts/TaskbarLed-$rid"
stage="$(mktemp -d)"
payload="$stage/payload"
app="$payload/Neon TSB Agents.app"
bin="$app/Contents/MacOS"
trap 'rm -rf "$stage"' EXIT

"$dotnet_cmd" publish src/TaskbarLed.App.Avalonia -c Release -r "$rid" --self-contained -o "$stage/app"
"$dotnet_cmd" publish src/TaskbarLed.Hook -c Release -r "$rid" --self-contained -o "$stage/hook"
mkdir -p "$bin" "$app/Contents/Resources"
iconset="$stage/NeonAgents.iconset"
mkdir -p "$iconset"
logo="$PWD/src/TaskbarLed.App.Avalonia/Assets/NeonAgents.png"
for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$logo" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" "$logo" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$app/Contents/Resources/NeonAgents.icns"
cp LICENSE "$app/Contents/Resources/LICENSE"
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
    <key>CFBundleName</key><string>Neon TSB Agents</string>
    <key>CFBundleDisplayName</key><string>Neon TSB Agents</string>
    <key>CFBundleIconFile</key><string>NeonAgents</string>
    <key>CFBundleExecutable</key><string>TaskbarLed</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <key>LSUIElement</key><true/>
</dict>
</plist>
PLIST

codesign --force --deep --sign - "$app"
rm -f "$output/TaskbarLed-$rid.zip"
ditto -c -k --keepParent "$app" "$output/TaskbarLed-$rid.zip"
printf '%s\n' "$output/TaskbarLed-$rid.zip"
pkgbuild --analyze --root "$payload" "$stage/Components.plist"
plutil -replace 0.BundleIsRelocatable -bool NO "$stage/Components.plist"
pkgbuild --root "$payload" --component-plist "$stage/Components.plist" --install-location /Applications \
    --identifier io.github.briito-yuri.taskbarled.installer "$stage/NeonAgents.pkg"
cat > "$stage/Distribution.xml" <<DIST
<?xml version="1.0" encoding="utf-8"?>
<installer-gui-script minSpecVersion="2">
    <title>Neon TSB Agents</title>
    <options customize="never" hostArchitectures="$host_arch"/>
    <volume-check><allowed-os-versions><os-version min="14.0"/></allowed-os-versions></volume-check>
    <choices-outline><line choice="neon"/></choices-outline>
    <choice id="neon" visible="false"><pkg-ref id="io.github.briito-yuri.taskbarled.installer"/></choice>
    <pkg-ref id="io.github.briito-yuri.taskbarled.installer">NeonAgents.pkg</pkg-ref>
</installer-gui-script>
DIST
productbuild --distribution "$stage/Distribution.xml" --package-path "$stage" "$stage/TaskbarLed-$rid.pkg"
mv -f "$stage/TaskbarLed-$rid.pkg" "$output/TaskbarLed-$rid.pkg"
printf '%s\n' "$output/TaskbarLed-$rid.pkg"
