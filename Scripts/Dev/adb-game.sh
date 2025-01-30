#!/bin/bash
# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")

APK="$1"
PKG="com.firstlightgames.blastroyale"
adb devices

# Function to install APK on Android devices
function android_open {
    SERIAL=$1
    adb -s "$SERIAL" shell am force-stop $PKG
    adb -s "$SERIAL" shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1
}

set -e
echo "Starting installation on Android devices..."
adb devices -l
for SERIAL in $(adb devices | grep -v List | cut -f 1); do
    android_open "$SERIAL" &
done
wait