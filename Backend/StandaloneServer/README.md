# Standalone Server

This server is to be run locally for local development.
It only includes the "ExecuteCommand" functionality at this stage, PlayerSetup has to be done in Azure.

The standalone server simulates the whole "Playfab Cloudscript Execution" flow for testing purposes.
### Requirements

Docker

### How to Run

In your terminal of choice:

`docker-compose build` and
`docker-compose up`

This should spin up an instance of the server and its required dependencies such as database.

### How to Connect

After running the local server, in your unity, find the First Light Games menu on the top bar.

`First Light Games -> Backend -> Local Server`

After clicking this menu for this session all logic requests will be sent to the local backend instead.


### Debugging & Local App

You can debug directly from Rider/Visual studio if you want. For that, open your Docker app and close the Backend container (but leave postgres running).
After that you can simply run the project directly from Rider by clickin in the "Run" button. 

### Blast Hub

You can define to which blast hub your local server points in case you want
to test some specific integration.

For local runs that can be done in the StandaloneServer Properties -> launchSettings.json
