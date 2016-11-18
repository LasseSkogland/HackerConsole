using System.Security.Cryptography.X509Certificates;

namespace HackerConsole.Scripting {
	public interface ICommandScript {
		void Load(object[] args);
		void RunScript(string[] args);
		string GetCommandName();
	}
}