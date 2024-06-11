using System;
using System.Linq;
using CommandLine;
using Quantum;

[Verb("i", HelpText = "Interactive")]
class InteractiveOptions
{
	//normal options here
}

[Verb("run", HelpText = "Run a script")]
class RunScriptOptions
{
	[Option('n', "name", Required = true, HelpText = "Script name")]
	public string ScriptName { get; set; }
}

class Program
{
	static int Main(string[] args) =>
		Parser.Default.ParseArguments<InteractiveOptions, RunScriptOptions>(args)
			.MapResult(
				(InteractiveOptions options) => Interactive(),
				(RunScriptOptions options) => RunScript(options),
				errors => 1);

	public static int RunScript(RunScriptOptions opts)
	{
		var allScripts = typeof(IScript).Assembly.GetTypes()
			.Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
			.ToList();

		var scripts = allScripts.Where(type => !typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();
		var exists = scripts.Exists(type => type.Name == opts.ScriptName);
		if (!exists)
		{
			Log.Error("Script not found!");
		}

		var script = scripts.FirstOrDefault(type => type.Name == opts.ScriptName);
		var chosen = (IScript) Activator.CreateInstance(script);
		Console.WriteLine("Executing script " + chosen.GetType().Name);
		chosen.Execute(new ScriptParameters());
		return 0;
	}


	public static int Interactive()
	{
		var allScripts = typeof(IScript).Assembly.GetTypes()
			.Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
			.ToList();

		var scripts = allScripts.Where(type => !typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();
		var migrationScripts = allScripts.Where(type => typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();

		Environment.GetCommandLineArgs();
		Console.WriteLine("Select script to run:");
		Console.WriteLine("0 - Data Migrations");
		scripts.ForEach(script => Console.WriteLine($"{scripts.IndexOf(script) + 1} - {script.Name}"));

		var option = Int32.Parse(Console.ReadLine());

		Type? chosenType = null;
		if (option == 0)
		{
			Console.WriteLine("Select a migration script to run:");
			migrationScripts.ForEach(script => Console.WriteLine($"{migrationScripts.IndexOf(script)} - {script.Name}"));
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