using Scripts;

Console.WriteLine("Local Script Runner");

var scripts = typeof(Program).Assembly.GetTypes()
                             .Where(type => typeof(IScript).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                             .Select(type => (IScript)Activator.CreateInstance(type))
                             .ToList();

Console.WriteLine("Select script to run:");
scripts.ForEach(script => Console.WriteLine($"{scripts.IndexOf(script)} - {script.GetType().Name}"));

var chosen = scripts[Int32.Parse(Console.ReadLine())];
Console.WriteLine("Executing script "+chosen.GetType().Name);
chosen.Execute(new ScriptParameters());