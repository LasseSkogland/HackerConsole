using System.Linq;
using System.IO;
using System;
using SFML.Graphics;

namespace HackerConsole {
	class Program {
		private static Console console;

		internal static readonly string[] commands = {
			"exit", "quit", "help", "load-script", "clear", "font"
		};
		
		static void Main(string[] args) {
			console = new Console();
			console.AddCommandHandler(HandleCommand, commands);
			console.LoadPlugins();
			console.Loop();
		}

		private static bool HandleCommand(string command, string[] args) {
			switch (command) {
				case "exit":
				case "quit":
					console.Shutdown();
					return true;
				case "clear":
					console.Clear();
					return true;
				case "load-script":
					string error;
					var scriptName = args[0];
					var scriptPath = "";
					if (File.Exists(scriptName))
						scriptPath = scriptName;
					else if (File.Exists("scripts\\" + scriptName)) {
						scriptPath = "scripts\\" + scriptName;
					}
					else {
						console.WriteLine("{0} not found", scriptName);
						return true;
					}
					if (!Scripting.ScriptCompiler.Load(scriptPath, out error, console)) {
						console.WriteLine(error);
						return true;
					}
					console.WriteLine("{0} loaded successfully", args[0]);

					return true;
				case "help":
					console.WriteLine("Available commands:");
					var commands = "";
					string[] scriptCommands;
					if (Scripting.ScriptCompiler.HasCommands())
						scriptCommands = Scripting.ScriptCompiler.GetCommands().Concat(Program.commands).ToArray();
					else scriptCommands = Program.commands;
					Array.Sort(scriptCommands, StringComparer.InvariantCulture);
					foreach (var cmd in scriptCommands) {
						commands += ", " + cmd;
					}
					console.WriteLine(commands.Substring(2));
					return true;
				case "font":
					if (args == null) {
						console.WriteLine("Font Size: {0}", console.FontSize);
					}
					else
						switch (args[0]) {
							case "color":
								if (args.Length < 4) {
									console.WriteLine("Usage: font color R G B\nR, G, B = byte value 0-255");
								}
								else {
									try {
										Color c = new Color(byte.Parse(args[1]), byte.Parse(args[2]), byte.Parse(args[3]));
										console.FontColor = c;
									}
									catch (Exception) {
										console.WriteLine("Usage: font color R G B\nR, G, B = byte value 0-255");
									}
								}
								return true;
							case "size":
								if (args.Length < 2) {
									console.WriteLine("Usage: font size SIZE\nSIZE = integer value");
								}
								try {
									console.FontSize = uint.Parse(args[1]);
								}
								catch (Exception) {
									console.WriteLine("Usage: font size SIZE\nSIZE = integer value");
								}
								return true;
						}
					return true;
				default:
					return false;
			}
		}
	}
}
