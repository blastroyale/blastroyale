using System;
using System.Linq;

Console.WriteLine("Local Script Runner");

var allScripts = typeof(IScript).Assembly.GetTypes()
                             .Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                             .ToList();

var scripts = allScripts.Where(type => !typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();
var migrationScripts = allScripts.Where(type => typeof(VersionMigrationScript).IsAssignableFrom(type)).ToList();

Console.WriteLine("Select script to run:");
Console.WriteLine("0 - Data Migrations");
scripts.ForEach(script => Console.WriteLine($"{scripts.IndexOf(script)+1} - {script.Name}"));

var option = Int32.Parse(Console.ReadLine());

Type? chosenType = null;
if (option == 0)
{
	Console.WriteLine("Select a migration script to run:");
	migrationScripts.ForEach(script => Console.WriteLine($"{migrationScripts.IndexOf(script)} - {script.Name}"));
	option = Int32.Parse(Console.ReadLine());
	chosenType = migrationScripts[option];
} else {
	chosenType = scripts[option-1];
}
var chosen = (IScript)Activator.CreateInstance(chosenType);
Console.WriteLine("Executing script "+chosen.GetType().Name);
chosen.Execute(new ScriptParameters());

