

check_azure:
	az functionapp deployment list-publishing-credentials --name DevFirstLightBackend --resource-group Dev_Backend

update:
	cd Scripts && python update_dlls.py

zip:
	zip -r backend.zip .

deploy:
	az functionapp deployment source config-zip -g Dev_Backend -n DevFirstLightBackend --src ./backend.zip
	
jenkins: update zip deploy
	echo "Deployed to Azure"