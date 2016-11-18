using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using HackerConsole.Scripting;
using SFML.Graphics;
using SFML.Window;
using static SFML.Window.Keyboard;
using KeyEventArgs = SFML.Window.KeyEventArgs;
using System.Linq;
using HackerConsole.Properties;
using SFML.System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HackerConsole {
	public class Console : IConsole {

		internal RenderWindow window;
		internal bool isAlive;

		internal List<string> consoleLines = new List<string>();
		internal List<string> commandHistory = new List<string>();

		internal string preCommand = "Command> ";

		public string CommandPrefix { get { return preCommand; } set { preCommand = value; } }

		internal string commandString = "";
		internal List<Func<string, string[], bool>> commandHandler = new List<Func<string, string[], bool>>();
		internal List<string> handlerCommands = new List<string>();
		internal int historyLine = 0;
		internal int consoleLine = 0;

		internal Text text;
		internal RectangleShape caret;
		internal int caretPosition = 0;
		public int CaretPosition {
			get { return caretPosition - CommandPrefix.Length; }
			set { caretPosition = CommandPrefix.Length + value; }
		}

		internal long flashCounter = 0;
		internal long flashDelay = TimeSpan.TicksPerMillisecond * 500;

		internal float characterWidth = 0;
		internal float characterHeight = 0;

		internal int maxWidth {
			get {
				return (int)Math.Floor(window.GetView().Size.X / characterWidth) - 1;
			}
		}

		internal int maxHeight {
			get {
				return (int)Math.Floor(window.GetView().Size.Y / characterHeight) - 1;
			}
		}

		public uint FontSize {
			get { return text.CharacterSize; }
			set {
				text.CharacterSize = value;
				text.DisplayedString = "A";
				characterWidth = text.GetLocalBounds().Width;
				characterHeight = text.GetLocalBounds().Height + 1;
			}
		}

		public Stream Font {
			set { text.Font = new Font(value); }
		}

		public Color FontColor {
			get { return text.Color; }
			set { text.Color = value; }
		}

		internal Color clearColor = Color.Black;
		public Color BackgroundColor {
			get { return clearColor; }
			set { clearColor = value; }
		}

		[DllImport("kernel32.dll")]
		static extern IntPtr LoadLibrary(string dllToLoad);

		public Console() {
			var files = Directory.GetFiles(".", "*", SearchOption.AllDirectories).ToList();
			LoadLibrary(files.Find(item => Path.GetFileName(item).Equals("csfml-audio-2.dll")));
			LoadLibrary(files.Find(item => Path.GetFileName(item).Equals("csfml-graphics-2.dll")));
			LoadLibrary(files.Find(item => Path.GetFileName(item).Equals("csfml-system-2.dll")));
			LoadLibrary(files.Find(item => Path.GetFileName(item).Equals("csfml-window-2.dll")));
			LoadLibrary(files.Find(item => Path.GetFileName(item).Equals("libsndfile-1.dll")));
			LoadLibrary(files.Find(item => Path.GetFileName(item).Equals("openal32.dll")));
			VideoMode vidMode = new VideoMode(800, 616);
			window = new RenderWindow(vidMode, "Hacker Console");
			window.SetKeyRepeatEnabled(true);
			window.TextEntered += TextInputHandler;
			window.KeyPressed += WindowOnKeyPressed;
			window.Closed += (sender, eventArgs) => { isAlive = false; };
			window.MouseWheelMoved += WindowOnMouseScroll;
			window.Resized += (sender, eventArgs) => {
				var view = window.GetView();
				view.Reset(new FloatRect(0, 0, eventArgs.Width, eventArgs.Height));
				window.SetView(view);
			};
			window.SetVerticalSyncEnabled(true);
			window.SetVisible(true);
			window.SetActive(true);
			text = new Text();
			Font = new MemoryStream(Properties.Resources.sourcecodepro_regular);
			FontSize = 12;
			caret = new RectangleShape(new Vector2f(2, characterHeight));
			flashCounter = DateTime.Now.Ticks;
			CaretPosition = 0;
		}

		public void Loop() {
			isAlive = true;
			while (isAlive && window.IsOpen) {
				window.Clear();
				window.DispatchEvents();
				Draw(window);
				window.Display();
			}
			window.SetVisible(false);
			window.Close();
		}

		public void Shutdown() {
			isAlive = false;
		}

		public void LoadPlugins(string path = "scripts") {
			if (Directory.Exists(path)) {
				var e = Directory.EnumerateFiles(path).GetEnumerator();
				string error;
				int i = WriteLine("Loading Plugins: ");
				while (e.MoveNext()) {
					window.Clear();
					window.DispatchEvents();
					Draw(window);
					if (!ScriptCompiler.Load(e.Current, out error, this)) {
						WriteLine(error);
					}
					WriteLine("{0} loaded successfully", e.Current);
					window.Display();
				}
				Write(i, " Done");
				window.Display();
			}
		}

		public void Clear() {
			consoleLines.Clear();
		}

		public void SetFontColor(byte r, byte g, byte b) {
			text.Color = new Color(r, g, b);
		}

		public void SetBackgroundColor(byte r, byte g, byte b) {
			clearColor = new Color(r, g, b);
		}

		public void SetFontSize(uint size) {
			FontSize = size;
		}

		public void SetCaretPosition(int position) {
			CaretPosition = position;
		}



		public void AddCommandHandler(Func<string, string[], bool> handler, string[] commands) {
			commandHandler.Add(handler);
			handlerCommands.AddRange(commands);
		}

		public void SetCommandPrefix(string str) {
			CommandPrefix = str;
		}

		public void Write(int index, string str, params object[] objects) {
			if (index < 0 || index >= consoleLines.Count) return;
			str = Format(str, objects);
			consoleLines[index] += str;
		}

		public int WriteLine(string str, params object[] objects) {
			str = Format(str, objects);
			if (str.Contains("\n")) {
				foreach (var item in str.Split('\n')) consoleLines.Add(item);
			} else {
				consoleLines.Add(str);
			}
			return consoleLines.Count - 1;
		}

		internal void DrawString(string str, float x, float y, RenderWindow window) {
			var textOrigin = text.Position;
			y = y * characterHeight;
			x = x * characterWidth;
			var pos = text.Position;
			pos.X = x;
			if (str.Length > maxWidth) {
				var index = 0;
				var unread = str.Length;
				while (unread > 0) {
					var strIndex = index * maxWidth;
					var strRead = maxWidth;
					if (strIndex >= str.Length) strIndex = (str.Length - 1) - unread;
					if (strIndex + maxWidth > str.Length) strRead = unread;
					text.DisplayedString = str.Substring(strIndex, strRead);
					pos.Y = y + (index * characterHeight);
					text.Position = pos;
					window.Draw(text);
					index += 1;
					unread -= maxWidth;
					consoleLine++;
				}
			} else {
				text.DisplayedString = str;
				pos.Y += y;
				text.Position = pos;
				window.Draw(text);
				consoleLine++;
			}
			text.Position = textOrigin;
		}

		internal void Draw(RenderWindow window) {
			var caretPos = caret.Position;
			caretPos.X = caretPosition * characterWidth + 2;
			caretPos.Y = ((maxHeight + .2f) * characterHeight);
			caret.Position = caretPos;
			if (DateTime.Now.Ticks - flashCounter > flashDelay) {
				flashCounter = DateTime.Now.Ticks;
				if (!caret.FillColor.Equals(text.Color))
					caret.FillColor = text.Color;
				else caret.FillColor = Color.Black;
			}
			window.Draw(caret);
			consoleLine = 0;
			if (consoleLines.Count > 0) foreach (var item in consoleLines.Skip(Math.Max(0, consoleLines.Count - (maxHeight - 1)))) {
					DrawString(item, 0, consoleLine, window);
				}
			DrawString(CommandPrefix + commandString, 0, maxHeight, window);
		}

		internal void WindowOnMouseScroll(object sender, MouseWheelEventArgs e) {

		}

		internal string AutoComplete(string str, string cmd = "") {
			List<string> autoList = new List<string>();
			if (cmd.Equals(string.Empty)) {
				autoList.AddRange(handlerCommands);
				var scriptCommands = ScriptCompiler.GetCommands();
				if (scriptCommands != null) {
					autoList.AddRange(scriptCommands);
				}
			} else {
				switch (cmd) {
					case "load-script":
						var e = Directory.EnumerateFiles("scripts", str + "*.cs", SearchOption.AllDirectories).GetEnumerator();
						while (e.MoveNext()) {
							autoList.Add(e.Current.Substring(8));
						}
						e = Directory.EnumerateFiles("scripts", str + "*.dll", SearchOption.AllDirectories).GetEnumerator();
						while (e.MoveNext()) {
							autoList.Add(e.Current.Substring(8));
						}
						break;
				}
			}

			int num = 0;
			foreach (var command in autoList) if (command.StartsWith(str)) num++;
			if (num == 0) return str;
			if (num == 1) return autoList.Find(item => item.StartsWith(str)) + " ";
			else {
				var i = WriteLine("Available: ");
				foreach (var command in autoList) {
					if (command.StartsWith(str)) {
						if (num > 1) Write(i, command + " ");
					}
				}
			}
			return str;
		}

		internal void WindowOnKeyPressed(object sender, KeyEventArgs e) {
			switch (e.Code) {
				case Key.Return:
					historyLine = 0;
					CaretPosition = 0;
					commandHistory.Add(commandString);
					var hasHandler = false;
					var command = commandString.Split((char)32);
					string[] args = null;
					if (command.Length > 1) {
						args = new string[command.Length - 1];
						Array.ConstrainedCopy(command, 1, args, 0, args.Length);
					}
					commandHandler.ForEach(item => {
						if (item(command[0].ToLower(), args)) hasHandler = true;
					});
					if (!hasHandler)
						if (!ScriptCompiler.RunCommand(command[0].ToLower(), args))
							WriteLine(commandString + " is not a valid command");
					commandString = "";
					return;
				case Key.BackSpace:
					if (commandString.Length > 0 && CaretPosition > 0) {
						commandString = commandString.Remove((CaretPosition) - 1, 1);
						CaretPosition--;
					}
					return;
				case Key.Down:
					if (historyLine > 1) {
						historyLine--;
						commandString = commandHistory[commandHistory.Count - historyLine];
					} else commandString = "";
					CaretPosition = commandString.Length;
					return;
				case Key.Up:
					if (historyLine < commandHistory.Count) {
						historyLine++;
						commandString = commandHistory[commandHistory.Count - historyLine];
					}
					CaretPosition = commandString.Length;
					return;
				case Key.Left:
					if (CaretPosition > 0) CaretPosition--;
					return;
				case Key.Right:
					if (CaretPosition < commandString.Length) CaretPosition++;
					return;
				case Key.Tab:
					if (commandString.Contains(" ")) {
						var last = commandString.LastIndexOf(" ") + 1;
						var auto = AutoComplete(commandString.Substring(last, commandString.Length - last), commandString.Substring(0, commandString.IndexOf(" ")));
						commandString = commandString.Substring(0, last) + auto;
					} else commandString = AutoComplete(commandString);
					CaretPosition = commandString.Length;
					return;
			}

		}

		internal void TextInputHandler(object sender, TextEventArgs e) {
			var ch = (Keys)e.Unicode[0];
			if (ch == Keys.Escape || ch == Keys.Enter || ch == Keys.Back || ch == Keys.Tab) return;
			CaretPosition++;
			commandString += e.Unicode;
		}

		internal string Format(string str, params object[] objects) {
			StringWriter writer = new StringWriter();
			writer.Write(str, objects);
			writer.Flush();
			str = writer.ToString();
			writer.Close();
			return str.Trim('\n');
		}
	}
}


