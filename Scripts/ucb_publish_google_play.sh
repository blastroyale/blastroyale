#!/bin/bash
 
# Passed in as environment variables from CI, you must get this from Google and put
# it in the environment variable PLAYSTORE_KEY in the Unity build config.
# $PLAYSTORE_KEY - The JSON file with the credentials for the Google Developer account
 
# You can also just hardcode your package name, e.g. com.candycrush.game or whatever here...
PACKAGE_NAME=$(cat "$WORKSPACE/build.json" | jq -j '.[].bundleid')
 
# Unity environment variables replace the "\n" signs from the private key with spaces for some reason,
# so we replaces spaces with "\n" signs again so it works properly.
KEY_WITH_NEWLINES=$(echo $PLAYSTORE_KEY | jq '.private_key |= sub(" (?!PRIVATE|KEY)"; "\n"; "g")' -c -j)

echo "DEBUG PRINT KEYS"
echo $KEY_WITH_NEWLINES
echo $PLAYSTORE_KEY
 
# You could also use shorter argument names here, but DO NOT use -e for --release-status, there's some error there where
# fastlane thinks -e should mean the -env option and fails.
# Also, you could put the "draft" and "internal" into environment variables if you want to never have to modify the script
# again and just control it with environment variables.
# Add to auto release to track: --release-status draft --track internal
fastlane supply --package_name "$PACKAGE_NAME" --aab "$UNITY_PLAYER_PATH" --json_key_data "$KEY_WITH_NEWLINES"