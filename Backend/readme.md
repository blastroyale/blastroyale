# Game Logic Service

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

