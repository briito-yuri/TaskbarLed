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
version="${TASKBARLED_VERSION:-0.0.0}"
if [[ ! "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    printf '%s\n' 'TASKBARLED_VERSION must be MAJOR.MINOR.PATCH.' >&2
    exit 2
fi
output="$PWD/artifacts/TaskbarLed-$rid"
stage="$(mktemp -d)"
payload="$stage/payload"
app="$payload/TaskbarLed.app"
bin="$app/Contents/MacOS"
trap 'rm -rf "$stage"' EXIT

"$dotnet_cmd" publish src/TaskbarLed.App.Avalonia -c Release -r "$rid" --self-contained -p:Version="$version" -o "$stage/app"
"$dotnet_cmd" publish src/TaskbarLed.Hook -c Release -r "$rid" --self-contained -p:Version="$version" -o "$stage/hook"
mkdir -p "$bin" "$app/Contents/Resources"
iconset="$stage/TaskbarLedIcon.iconset"
mkdir -p "$iconset"
logo="$PWD/src/TaskbarLed.App.Avalonia/Assets/TaskbarLedIcon.png"
for size in 16 32 128 256 512; do
    sips -z "$size" "$size" "$logo" --out "$iconset/icon_${size}x${size}.png" >/dev/null
    double=$((size * 2))
    sips -z "$double" "$double" "$logo" --out "$iconset/icon_${size}x${size}@2x.png" >/dev/null
done
iconutil -c icns "$iconset" -o "$app/Contents/Resources/TaskbarLedIcon.icns"
cp LICENSE "$app/Contents/Resources/LICENSE"
cp -R "$stage/app/." "$bin/"
for file in TaskbarLed.Hook TaskbarLed.Hook.dll TaskbarLed.Hook.deps.json TaskbarLed.Hook.runtimeconfig.json; do
    cp "$stage/hook/$file" "$bin/"
done
cat > "$app/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleIdentifier</key><string>io.github.llorenzojorge.taskbarled</string>
    <key>CFBundleName</key><string>TaskbarLed</string>
    <key>CFBundleDisplayName</key><string>TaskbarLed</string>
    <key>CFBundleIconFile</key><string>TaskbarLedIcon</string>
    <key>CFBundleShortVersionString</key><string>$version</string>
    <key>CFBundleVersion</key><string>$version</string>
    <key>CFBundleExecutable</key><string>TaskbarLed</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <key>LSUIElement</key><true/>
</dict>
</plist>
PLIST

codesign --force --deep --sign - "$app"
codesign --verify --deep --strict "$app"
pkgbuild --analyze --root "$payload" "$stage/Components.plist"
plutil -replace 0.BundleIsRelocatable -bool NO "$stage/Components.plist"
pkgbuild --root "$payload" --component-plist "$stage/Components.plist" --install-location /Applications \
    --version "$version" --identifier io.github.llorenzojorge.taskbarled.installer "$stage/TaskbarLedComponent.pkg"
cat > "$stage/Distribution.xml" <<DIST
<?xml version="1.0" encoding="utf-8"?>
<installer-gui-script minSpecVersion="2">
    <title>TaskbarLed</title>
    <options customize="never" hostArchitectures="$host_arch"/>
    <volume-check><allowed-os-versions><os-version min="14.0"/></allowed-os-versions></volume-check>
    <choices-outline><line choice="taskbarled"/></choices-outline>
    <choice id="taskbarled" visible="false"><pkg-ref id="io.github.llorenzojorge.taskbarled.installer"/></choice>
    <pkg-ref id="io.github.llorenzojorge.taskbarled.installer">#TaskbarLedComponent.pkg</pkg-ref>
</installer-gui-script>
DIST
pkg="$stage/TaskbarLed-$rid.pkg"
productbuild --distribution "$stage/Distribution.xml" --package-path "$stage" "$pkg"
zip="$stage/TaskbarLed-$rid.zip"
ditto -c -k --keepParent "$app" "$zip"
mkdir -p "$output"
mv -f "$zip" "$output/"
mv -f "$pkg" "$output/"
printf '%s\n' "$output/TaskbarLed-$rid.zip" "$output/TaskbarLed-$rid.pkg"
