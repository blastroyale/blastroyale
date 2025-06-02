#!/bin/bash
BASEDIR=$(dirname "$0")
TEMPDIR=$BASEDIR/temp
GAMEVERSION=$1
APK="$TEMPDIR/$GAMEVERSION.apk"
IPA="$TEMPDIR/$GAMEVERSION.ipa"

PKG="com.firstlightgames.blastroyale"
mkdir -p $TEMPDIR

function download_file {
    URL=$1
    DOWNLOAD_PATH=$2
    DOWNLOAD_PATH_TEMP="$2.tmp"
    echo "Downloading $URL"
    curl --fail -s -o "$DOWNLOAD_PATH_TEMP" $URL
    if [ ! -f "$DOWNLOAD_PATH_TEMP" ]; then
        echo "Failed to download $URL"
        exit 1
    fi
    echo "Finished downloading $URL"
    mv $DOWNLOAD_PATH_TEMP $DOWNLOAD_PATH
}

function android_install {
    SERIAL=$1
    echo ""
    echo ""
    echo "Running android on $SERIAL"
    adb -s $SERIAL shell "pm uninstall $PKG"
    adb -s $SERIAL shell "rm -rf /data/app/$PKG-*"
    adb -s $SERIAL install -r -g -t "$APK"
    adb -s $SERIAL shell monkey -p $PKG -c android.intent.category.LAUNCHER 1
}

function ios_install {
    SERIAL=$1
    echo ""
    echo ""
    echo "Running ios on $SERIAL"
    ideviceinstaller -u $SERIAL uninstall $PKG
    ideviceinstaller -u $SERIAL install $IPA    
}



# Download android
if [ ! -e "$APK" ]; then
    url="https://game-builds-cdn-defzg2fxf6h7h5gp.z02.azurefd.net/android/Game-Builds/$GAMEVERSION/BlastRoyale.apk"
    download_file $url $APK &
else
    echo "Using downloaded apk!"
fi

# Download ios
if [ ! -e "$IPA" ]; then
    url="https://game-builds-cdn-defzg2fxf6h7h5gp.z02.azurefd.net/ios/Game-Builds/$GAMEVERSION/BlastRoyale.ipa"
    download_file $url $IPA &
else
    echo "Using downloaded ipa!"
fi


wait
if [ ! -f "$IPA" ]; then
    exit
fi

if [ ! -f "$APK" ]; then
    exit
fi

echo "Downloads finished!"



adb devices -l
for SERIAL in $(adb devices | grep -v List | cut -f 1); do
   android_install $SERIAL &
done

idevice_id
for SERIAL in $(idevice_id -l); do
    ios_install $SERIAL &
done

wait
