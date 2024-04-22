#!/bin/bash
 
echo "Publishing to Apple Connect"

# Unity environment variables replace the "\n" signs from the private key with spaces for some reason,
# so we replaces spaces with "\n" signs again so it works properly.
KEY_WITH_NEWLINES=$(echo $APPLE_CONNECT_KEY | jq '.key |= sub(" (?!PRIVATE|KEY)"; "\n"; "g")' -c -j)

echo $KEY_WITH_NEWLINES > api_key.json
 
# The force option skips a manual approval check you'd otherwise need to do
fastlane deliver --ipa "$UNITY_PLAYER_PATH" --api_key_path api_key.json --submission_information "{\"export_compliance_uses_encryption\": false }" --force