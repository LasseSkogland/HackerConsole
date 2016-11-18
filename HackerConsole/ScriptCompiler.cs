using System;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;

namespace HackerConsole.Scripting {
	public static class ScriptCompiler {
		internal static Dictionary<string, ICommandScript> scriptCommands = new Dictionary<string, ICommandScript>();

		public static bool TestScript(string fileName, out string output) {
			if (!fileName.EndsWith(".cs")) {
				output = "this only supports .cs extension";
				return false;
			}
			CompilerResults results = BuildAssembly(fileName);
			return !HasErrors(results.Errors, out output);
		}

		public static bool Load(string fileName, out string errors, params object[] loadArgs) {
			if (fileName.EndsWith(".dll")) {
				try {
					return ProcessScript(Assembly.LoadFrom(fileName), out errors, loadArgs);
				}
				catch (Exception) {
					errors = "Failed to load .dll";
					return false;
				}
			} else {
				var result = BuildAssembly(fileName);
				if (HasErrors(result.Errors, out errors)) return false;
				return ProcessScript(result.CompiledAssembly, out errors, loadArgs);
			}
		}

		public static bool Load(Assembly assembly, out string error, params object[] loadArgs) {
			return ProcessScript(assembly, out error, loadArgs);
		}

		public static bool RunCommand(string command, string[] args) {
			if (scriptCommands.ContainsKey(command)) {
				scriptCommands[command].RunScript(args);
				return true;
			}
			return false;
		}

		public static bool HasCommands() {
			if (scriptCommands.Count > 0) return true;
			return false;
		}

		public static string[] GetCommands() {
			if (!HasCommands()) return null;
			var commands = new string[scriptCommands.Count];
			scriptCommands.Keys.CopyTo(commands, 0);
			return commands;
		}

		internal static bool HasErrors(CompilerErrorCollection errors, out string error) {
			if (errors.HasWarnings && !errors.HasErrors) {
				error = string.Format("Script Valid\nWarnings:\n");
				foreach (var err in errors) {
					error += string.Format("{0}\n", err.ToString());
				}
				return false;
			} else if (errors.HasErrors) {
				error = string.Format("Script Invalid\nErrors:\n");
				foreach (var err in errors) {
					error += string.Format("{0}\n", err.ToString());
				}
				return true;
			}
			error = "Script Valid";
			return false;
		}

		internal static CompilerResults BuildAssembly(string fileName) {
			var csBuilder = new Microsoft.CSharp.CSharpCodeProvider();
			var options = new CompilerParameters();
			options.GenerateExecutable = false;
			options.GenerateInMemory = true;
			options.IncludeDebugInformation = false;
			options.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
			return csBuilder.CompileAssemblyFromFile(options, fileName);
		}

		internal static bool ProcessScript(Assembly result, out string error, params object[] loadArgs) {
			foreach (Type type in result.GetTypes()) {
				foreach (Type iface in type.GetInterfaces()) {
					if (iface == typeof(ICommandScript)) {
						ConstructorInfo constructor = type.GetConstructor(Type.EmptyTypes);
						if (constructor != null && constructor.IsPublic) {
							var script = constructor.Invoke(null) as ICommandScript;
							if (script != null) {
								script.Load(loadArgs);
								if (!scriptCommands.ContainsKey(script.GetCommandName())) {
									scriptCommands.Add(script.GetCommandName(), script);
								} else {
									scriptCommands.Remove(script.GetCommandName());
									scriptCommands.Add(script.GetCommandName(), script);
								}
								error = "";
								return true;
							} else {
								error = "Script Invalid\nCould not invoke constructor";
								return false;
							}
						} else {
							error = "Script Invalid\nScript does not have valid Contructor";
							return false;
						}
					} else {
						error = "Script Invalid\nScript does not inherit ICommandScript";
						return false;
					}
				}
			}
			error = "";
			return false;
		}
	}
}