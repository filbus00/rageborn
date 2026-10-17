#!/bin/bash
# Builds the exported Xcode project (Tools > ARPG > Build iOS Device first) in Release and installs it on the owner's
# iPhone 11 (device ids from Docs/11, "Builds and performance"). Run from the project folder.
set -e
cd "$(dirname "$0")/.."
xcodebuild -project Builds/iOS/Unity-iPhone.xcodeproj -scheme Unity-iPhone -configuration Release \
  -destination id=00008030-001025162EFB802E -derivedDataPath build -allowProvisioningUpdates \
  DEBUG_INFORMATION_FORMAT=dwarf -quiet
xcrun devicectl device install app --device 47676409-11D8-5F4B-90C2-7242B79A5ABC build/Build/Products/Release-iphoneos/Rageborn.app
