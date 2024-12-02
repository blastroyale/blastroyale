#!/bin/bash
# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")

URL="$1"
PKG="com.firstlightgames.blastroyale"
adb devices

# Function to install APK on Android devices
function android_reboot {
    SERIAL=$1
    echo "Removing APK on Android device: $SERIAL"

    adb -s "$SERIAL" shell "pm uninstall --user 0 $PKG" || true

    echo "REBOOTING $SERIAL"
    adb -s "$SERIAL" reboot
    sleep 2
    echo "Waiting for device to boot"
    A=$(adb -s $SERIAL shell getprop sys.boot_completed | tr -d '\r')
    while [ "$A" != "1" ]; do
            sleep 2
            echo "Waiting"
            A=$(adb -s $SERIAL shell getprop sys.boot_completed | tr -d '\r')
    done
    echo "DEVICE BOOTED"
    if [[ -n "$URL" ]]
    then
        sleep 5
        adb -s "$SERIAL" shell pm clear com.android.vending
        adb -s "$SERIAL" shell am start -a android.intent.action.VIEW -d $URL
    fi
}

set -e
echo "Starting procedure..."
adb devices -l
for SERIAL in $(adb devices | grep -v List | cut -f 1); do
    android_reboot "$SERIAL" &
done
wait