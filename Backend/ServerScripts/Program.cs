using System;
using System.Linq;

Console.WriteLine("Local Script Runner");

var scripts = typeof(IScript).Assembly.GetTypes()
                             .Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                             .ToList();

Console.WriteLine("Select script to run:");
scripts.ForEach(script => Console.WriteLine($"{scripts.IndexOf(script)} - {script.Name}"));

var chosenType = scripts[Int32.Parse(Console.ReadLine())];
var chosen = (IScript)Activator.CreateInstance(chosenType);
Console.WriteLine("Executing script "+chosen.GetType().Name);
chosen.Execute(new ScriptParameters());