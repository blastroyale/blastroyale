#!/bin/bash

# Absolute path to this script, e.g. /home/user/bin/foo.sh
SCRIPT=$(readlink -f "$0")
# Absolute path this script is in, thus /home/user/bin
SCRIPTPATH=$(dirname "$SCRIPT")


helpFunction()
{
   echo ""
   echo "Usage: $0 debug|bots-debug|release"
   exit 1 # Exit script after printing help
}

command="msbuild"
if ! command -v "msbuild" &> /dev/null
then
    command="msbuild.exe"
fi

symbols="TRACE%3BDEBUG"

if [ "$1" = "release" ]; then
  echo "Release build"
  set -x
  $command "$SCRIPTPATH/quantum_code.sln" -restore -p:Configuration=Release -p:RestorePackagesConfig=true -p:DefineConstants=$symbols
  exit
elif [ "$1" = "releasep" ]; then
    echo "Release build"
    set -x
    $command "$SCRIPTPATH/quantum_code.sln" -restore -p:Configuration=ReleaseProfiler -p:RestorePackagesConfig=true -p:DefineConstants=$symbols
    exit
elif [ "$1" = "bots-debug" ]; then
  symbols="$symbols%3BBOT_DEBUG"
  echo "Debugging bots"
elif [ "$1" = "debug" ]; then
  echo "Standard debug build"
else 
  helpFunction
fi 
set -x


$command "$SCRIPTPATH/quantum_code.sln" -restore -p:Configuration=Debug -p:RestorePackagesConfig=true -p:DefineConstants=$symbols