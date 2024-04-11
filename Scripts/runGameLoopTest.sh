#!/bin/bash
# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")
set -e
adb get-state 1>/dev/null 2>&1 && echo 'device attached' || (echo 'no device attached' && exit 1)

apk="$SCRIPTPATH/../app.apk"
scenarios=2

echo "Parsing APK $apk"
packageName=$(aapt dump badging $apk | grep package:\  | grep -Po "(?<= name=')[^']*")
versionCode=$(aapt dump badging $apk | grep package:\  | grep -Po "(?<= versionCode=')[^']*")
versionName=$(aapt dump badging $apk | grep package:\  | grep -Po "(?<= versionName=')[^']*")
logFileLocationInDevice="/data/data/$packageName/files/test.log"
logFileOnThisPC="$SCRIPTPATH/logs/${packageName}_version_${versionName}_build_${versionCode}.log"
mkdir -p "$SCRIPTPATH/logs/"
echo "Package Name:$packageName, Version Code:$versionCode, Version Name:$versionName"

echo "Stopping App"
adb shell am force-stop $packageName

set +e
echo "Uninstalling Old Version"
adb uninstall $packageName
set -e

echo "Installing the apk"
adb install -r -d -g $apk

echo "Remove old log file"
adb shell run-as $packageName "rm -rf $logFileLocationInDevice"

echo "Starting the app $packageName running the scenarios: $scenarios"
adb shell am start -a com.google.intent.action.TEST_LOOP -n $packageName/com.unity3d.player.UnityPlayerActivity --ei scenario $scenarios -d "file://$logFileLocationInDevice"
# Print the current log file
while sleep 3;
do
  if [[ $(adb shell pidof $packageName) ]]; then
      set +e
      clear && adb shell run-as $packageName "tail -n 50 $logFileLocationInDevice";
      set -e
  else
        break;
   fi
done

echo "Finished, copying log file to $logFileOnThisPC"
adb shell run-as $packageName "cat $logFileLocationInDevice" > $logFileOnThisPC