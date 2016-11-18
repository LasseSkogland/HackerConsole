using System;

namespace HackerConsole.Scripting {
	public interface IConsole {
		void AddCommandHandler(Func<string, string[], bool> handler, string[] commands);
		void SetCommandPrefix(string str);
		void Write(int index, string str, params object[] objects);
		int WriteLine(string str, params object[] objects);
		void SetFontColor(byte r, byte g, byte b);
		void SetBackgroundColor(byte r, byte g, byte b);
		void SetFontSize(uint size);
	}
}