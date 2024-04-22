#!/bin/bash

echo "Publishing to Google Play"
 
# Unity environment variables replace the "\n" signs from the private key with spaces for some reason,
# so we replaces spaces with "\n" signs again so it works properly.
KEY_WITH_NEWLINES=$(echo $PLAYSTORE_KEY | jq '.private_key |= sub(" (?!PRIVATE|KEY)"; "\n"; "g")' -c -j)
 
# You could also use shorter argument names here
# Also, you could put the "draft" and "internal" into environment variables if you want to never have to modify the script
# again and just control it with environment variables.
fastlane supply --package_name "com.firstlightgames.blastroyale" --aab "$UNITY_PLAYER_PATH" --json_key_data "$KEY_WITH_NEWLINES" --track internal --release_status completed