#!/bin/bash
# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")

APK="$1"
PKG="com.firstlightgames.blastroyale"
adb devices

# Function to install APK on Android devices
function android_install {
    SERIAL=$1
    echo "Installing APK on Android device: $SERIAL"
    adb -s "$SERIAL" shell "pm uninstall $PKG" > /dev/null 2>&1
    adb -s "$SERIAL" shell "rm -rf /data/app/$PKG-*" > /dev/null 2>&1
    adb -s "$SERIAL" install -r -g -t "$APK" && echo "Successfully installed APK on $SERIAL." || echo "Failed to install APK on $SERIAL."
    adb -s "$SERIAL" shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1
}

set -e
echo "Starting installation on Android devices..."
adb devices -l
for SERIAL in $(adb devices | grep -v List | cut -f 1); do
    android_install "$SERIAL" &
done
wait