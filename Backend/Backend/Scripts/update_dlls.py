import os
import shutil

# Simple python script to copy required DLLS from unity client & quantum to backend.

_unityPath = os.getcwd()+"/../../../Library/ScriptAssemblies/"
_quantumLibPath = os.getcwd()+"/../../../Assets/Libs/Photon/Quantum/Assemblies/"
_destPath = os.getcwd()+"/../../Lib/"


def copy_assembly(path, assembly_name):
	shutil.copy(os.path.join(path, assembly_name), os.path.join(_destPath, assembly_name))
	print("Copied "+assembly_name)


def run():
	copy_assembly(_quantumLibPath, "quantum.code.dll")
	copy_assembly(_unityPath, "FirstLight.DataExtensions.dll")
	copy_assembly(_unityPath, "FirstLight.Game.dll")
	copy_assembly(_unityPath, "FirstLight.Services.dll")


if __name__ == "__main__":
	run()
