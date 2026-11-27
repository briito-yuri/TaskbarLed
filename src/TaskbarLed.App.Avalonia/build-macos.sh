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
app_identity="${NEON_MAC_APP_IDENTITY:-}"
installer_identity="${NEON_MAC_INSTALLER_IDENTITY:-}"
notary_profile="${NEON_MAC_NOTARY_PROFILE:-}"
if [[ -n "$app_identity$installer_identity$notary_profile" ]]; then
    if [[ -z "$app_identity" || -z "$installer_identity" || -z "$notary_profile" ]]; then
        printf '%s\n' 'Set NEON_MAC_APP_IDENTITY, NEON_MAC_INSTALLER_IDENTITY and NEON_MAC_NOTARY_PROFILE together.' >&2
        exit 2
    fi
fi
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

if [[ -n "$app_identity" ]]; then
    entitlements="$stage/RuntimeEntitlements.plist"
    cat > "$entitlements" <<'ENTITLEMENTS'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>com.apple.security.cs.allow-jit</key><true/>
    <key>com.apple.security.cs.allow-unsigned-executable-memory</key><true/>
</dict>
</plist>
ENTITLEMENTS
    signing=(--force --options runtime --timestamp --sign "$app_identity")
    while IFS= read -r -d '' native; do
        if [[ "$native" != "$bin/TaskbarLed" && "$native" != "$bin/TaskbarLed.Hook" ]] &&
            [[ "$(file -b "$native")" == *Mach-O* ]]; then
            codesign "${signing[@]}" "$native"
        fi
    done < <(find "$bin" -type f -print0)
    codesign "${signing[@]}" --entitlements "$entitlements" "$bin/TaskbarLed.Hook"
    codesign "${signing[@]}" --entitlements "$entitlements" "$app"
else
    codesign --force --deep --sign - "$app"
fi
codesign --verify --deep --strict "$app"
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
    <pkg-ref id="io.github.briito-yuri.taskbarled.installer">#NeonAgents.pkg</pkg-ref>
</installer-gui-script>
DIST
pkg="$stage/TaskbarLed-$rid.pkg"
productbuild --distribution "$stage/Distribution.xml" --package-path "$stage" "$pkg"
if [[ -n "$installer_identity" ]]; then
    productsign --sign "$installer_identity" "$pkg" "$stage/signed.pkg"
    mv -f "$stage/signed.pkg" "$pkg"
    notary_result="$stage/notarization.json"
    if ! xcrun notarytool submit "$pkg" --keychain-profile "$notary_profile" --wait --output-format json > "$notary_result"; then
        cat "$notary_result" >&2
        exit 1
    fi
    if [[ "$(plutil -extract status raw "$notary_result")" != Accepted ]]; then
        cat "$notary_result" >&2
        exit 1
    fi
    xcrun stapler staple "$pkg"
    xcrun stapler validate "$pkg"
    pkgutil --check-signature "$pkg"
    spctl --assess --type install --verbose=2 "$pkg"
fi

# Publish outputs only after the requested signing and notarization checks pass.
zip="$stage/TaskbarLed-$rid.zip"
ditto -c -k --keepParent "$app" "$zip"
mkdir -p "$output"
mv -f "$zip" "$output/"
mv -f "$pkg" "$output/"
printf '%s\n' "$output/TaskbarLed-$rid.zip" "$output/TaskbarLed-$rid.pkg"
