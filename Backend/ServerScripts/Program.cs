using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CommandLine;
using Quantum;
using Scripts.Base;

[Verb("i", HelpText = "Interactive")]
class InteractiveOptions
{
	//normal options here
}

class Program
{
	/// <summary>
	/// 
	/// </summary>
	/// <returns>Key: OptionsType Value: CommandType</returns>
	static Dictionary<Type, Type> GetOptions()
	{
		Dictionary<Type, Type> genTypes = new Dictionary<Type, Type>();
		foreach (var t in typeof(IScript).Assembly.GetTypes()
					 .Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract))
		{
			foreach (Type intType in t.GetInterfaces())
			{
				if (intType.IsGenericType && intType.GetGenericTypeDefinition()
					== typeof(IScriptCommandLineOptions<>))
				{
					genTypes.Add(intType.GetGenericArguments()[0], t);
				}
			}
		}

		return genTypes;
	}

	static int Main(string[] args)
	{
		var types = new List<Type>()
		{
			typeof(InteractiveOptions),
		};
		var commandOptions = GetOptions();
		types.AddRange(commandOptions.Keys);

		var taks = Parser.Default.ParseArguments(args, types.ToArray())
			.WithParsedAsync(async (opt) =>
			{
				switch (opt)
				{
					case InteractiveOptions interactiveOptions:
						Interactive();
						break;
					default:
						var commandType = commandOptions[opt.GetType()];
						var command = Activator.CreateInstance(commandType);
						var someObject = (Task) commandType.InvokeMember(
							nameof(IScriptCommandLineOptions<object>.RunWithOptions), BindingFlags.InvokeMethod, null,
							command, new object[] { opt });
						await someObject;
						break;
				}
			});
		taks.Wait();
		return 1;
	}

	public static int Interactive()
	{
		var allScripts = typeof(IScript).Assembly.GetTypes()
			.Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
			.ToList();

		var scripts = allScripts.Where(type => !typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();
		var migrationScripts = allScripts.Where(type => typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();

		System.Environment.GetCommandLineArgs();
		Console.WriteLine("Select script to run:");
		Console.WriteLine("0 - Data Migrations");
		scripts.ForEach(script => Console.WriteLine($"{scripts.IndexOf(script) + 1} - {script.Name}"));

		var option = Int32.Parse(Console.ReadLine());

		Type? chosenType = null;
		if (option == 0)
		{
			Console.WriteLine("Select a migration script to run:");
			migrationScripts.ForEach(script =>
				Console.WriteLine($"{migrationScripts.IndexOf(script)} - {script.Name}"));
			option = Int32.Parse(Console.ReadLine());
			chosenType = migrationScripts[option];
		}
		else
		{
			chosenType = scripts[option - 1];
		}

		var chosen = (IScript) Activator.CreateInstance(chosenType);
		Console.WriteLine("Executing script " + chosen.GetType().Name);
		chosen.Execute(new ScriptParameters());
		return 0;
	}
}