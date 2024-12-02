#!/bin/bash

echo "Publishing to Google Play"
 
# Unity environment variables replace the "\n" signs from the private key with spaces for some reason,
# so we replaces spaces with "\n" signs again so it works properly.

# You could also use shorter argument names here
# Also, you could put the "draft" and "internal" into environment variables if you want to never have to modify the script
# again and just control it with environment variables.
bundle exec fastlane run upload_to_play_store_internal_app_sharing package_name:"com.firstlightgames.blastroyale" aab:"../../BlastRoyale/BlastRoyale.aab" json_key:"./Secrets/service_account.json"