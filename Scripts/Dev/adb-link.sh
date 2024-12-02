#!/bin/bash
# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")

LINK="$1"
adb devices
set -e
adb devices -l
for SERIAL in $(adb devices | grep -v List | cut -f 1); do
     echo "Opening link APK on Android device: $SERIAL"
    adb -s "$SERIAL" shell am start -a android.intent.action.VIEW -d $LINK
done