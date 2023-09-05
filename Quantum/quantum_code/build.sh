#!/bin/bash

# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")


helpFunction()
{
   echo ""
   echo "Usage: $0 debug|bots-debug"
   exit 1 # Exit script after printing help
}


symbols="TRACE%3BDEBUG"

if [ "$1" = "bots-debug" ]; then
  symbols="$symbols%3BBOT_DEBUG"
  echo "Debugging bots"
elif [ "$1" = "debug" ]; then
  echo "Standard debug build"
else 
  helpFunction
fi 
set -x

command="msbuild"
if ! command -v "msbuild" &> /dev/null
then
    command="msbuild.exe"
fi


$command "$SCRIPTPATH/quantum_code.sln" -restore -p:Configuration=Debug -p:RestorePackagesConfig=true -p:DefineConstants=$symbols