using HackerConsole.Scripting;

class ExampleScript : ICommandScript {
	IConsole console;

	public void Load(object[] args) {
		this.console = (IConsole) args[0];
	}

	public void RunScript(string[] args) {
		console.WriteLine("Length of args: {0}", args.Length);
	}

	public string GetCommandName() {
		return "example";
	}
}
