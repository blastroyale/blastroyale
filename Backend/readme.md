# Game Logic Service

This repo contains core service to run our game logic service.

The game logic service is a stateless webapp that runs game logic in a deterministic way on server.
More info: https://firstlightgames.atlassian.net/wiki/spaces/BB/pages/1914437633/Game+Logic+Service+Architecture

### Requirements

.net core 6
.net framework 4.8

### Project Structure

/BlastRoyale<PluginName> - Server-side Plugin projects.
/FunctionApp - Azure Function Network Layer implementation
/GameLogicService - Core logic service code
/ServerScripts - Developer scripts & utilities for debugging
/ServerSDK - SDK shared with the game-client. Required to use .net framework 4.8
/StandaloneServer - Standalone container that simulates playfab cloudscript calls for local testing.

### Playfab

The server uses playfab as its data storage, analytics and main functionality.

To help integrating with playfab, strongly suggest to setup Postman & Playfab Postman Schema
https://learn.microsoft.com/en-us/gaming/playfab/sdks/postman/postman-quickstart

#### Cloud Script

We use playfab function calls as our api gateway. Requests go from client to playfab cloudscript, which authorizes a request so then
its forwarded to our service.

#### Testing Locally

To test the server locally you need to run our Standalone Server, which includes a Playfab Cloudscript bridge as it
"pretends" to be playfab cloud script router.

To obtain more information how to run and test locally please refer to StandaloneServer/Readme.md
