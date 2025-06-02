#!/bin/bash

# Function to install APK on Android devices
function android_install {
    SERIAL=$1
    echo "Installing APK on Android device: $SERIAL"
    adb -s "$SERIAL" shell "pm uninstall $PKG" > /dev/null 2>&1
    adb -s "$SERIAL" shell "rm -rf /data/app/$PKG-*" > /dev/null 2>&1
    adb -s "$SERIAL" install -r -g -t "$APK" && echo "Successfully installed APK on $SERIAL." || echo "Failed to install APK on $SERIAL."
    adb -s "$SERIAL" shell monkey -p "$PKG" -c android.intent.category.LAUNCHER 1
}

# Function to install IPA on iOS devices
function ios_install {
    SERIAL=$1
    echo "Installing IPA on iOS device: $SERIAL"
    ideviceinstaller -u "$SERIAL" uninstall "$PKG" > /dev/null 2>&1
    ideviceinstaller -u "$SERIAL" install "$IPA" && echo "Successfully installed IPA on $SERIAL." || echo "Failed to install IPA on $SERIAL."
}

BASEDIR=$(dirname "$0")
TEMPDIR="$BASEDIR/temp"
GAMEVERSION=""
APK=""
APK_TMP=""
IPA=""
IPA_TMP=""

PKG="com.firstlightgames.blastroyale"
mkdir -p "$TEMPDIR"

# Set download preferences based on arguments
ANDROID=true
IOS=true

# Process command-line arguments
while [[ "$#" -gt 0 ]]; do
    case "$1" in
        -a) IOS=false; echo "Running in Android-only mode." ;;
        -i) ANDROID=false; echo "Running in iOS-only mode." ;;
        *) GAMEVERSION="$1";;
    esac
    shift  # Move to the next argument
done

# Check if GAMEVERSION is set
if [ -z "$GAMEVERSION" ]; then
    echo "Error: GAMEVERSION is required."
    echo "Usage: $0 [-a | -i] <GAMEVERSION>"
    exit 1
fi

APK="$TEMPDIR/$GAMEVERSION.apk"
APK_TMP="$TEMPDIR/$GAMEVERSION.apk.tmp"
IPA="$TEMPDIR/$GAMEVERSION.ipa"
IPA_TMP="$TEMPDIR/$GAMEVERSION.ipa.tmp"

if $ANDROID; then
    if [ -e "$APK" ]; then
        echo "Using existing APK: $APK"
    else
        ANDROID_URL="https://game-builds-cdn-defzg2fxf6h7h5gp.z02.azurefd.net/android/Game-Builds/$GAMEVERSION/BlastRoyale.apk"
        URLS+=("$ANDROID_URL")
        OUTPUTS+=("-o" "$APK_TMP")
    fi
fi

if $IOS; then
    if [ -e "$IPA" ]; then
        echo "Using existing IPA: $IPA"
    else
        IOS_URL="https://game-builds-cdn-defzg2fxf6h7h5gp.z02.azurefd.net/ios/Game-Builds/$GAMEVERSION/BlastRoyale.ipa"
        URLS+=("$IOS_URL")
        OUTPUTS+=("-o" "$IPA_TMP")
    fi
fi

# Download files using curl --parallel if URLs are set
if [ ${#URLS[@]} -gt 0 ]; then
    echo "Starting downloads..."
    curl --parallel --progress-bar "${OUTPUTS[@]}" "${URLS[@]}"
else
    echo "No new downloads needed."
fi

# Move temporary files to final location after successful download
if [ -f "$APK_TMP" ]; then
    mv "$APK_TMP" "$APK"
    echo "APK download completed successfully."
elif [ ! -e "$APK" ]; then
    echo "Failed to download APK."
    exit 1
fi

if [ -f "$IPA_TMP" ]; then
    mv "$IPA_TMP" "$IPA"
    echo "IPA download completed successfully."
elif [ ! -e "$IPA" ]; then
    echo "Failed to download IPA."
    exit 1
fi

echo "All downloads finished successfully!"

# Install on Android devices
if [ -e "$APK" ] && $ANDROID; then
    echo "Starting installation on Android devices..."
    adb devices -l
    for SERIAL in $(adb devices | grep -v List | cut -f 1); do
        android_install "$SERIAL" &
    done
else
    echo "No APK found for installation on Android devices."
fi

# Install on iOS devices
if [ -e "$IPA" ] && $IOS; then
    echo "Starting installation on iOS devices..."
    for SERIAL in $(idevice_id -l); do
        ios_install "$SERIAL" &
    done
else
    echo "No IPA found for installation on iOS devices."
fi

wait
echo "Installation process completed!"