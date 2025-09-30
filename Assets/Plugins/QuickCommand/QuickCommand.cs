using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;
using System.Threading;

namespace QuickCmd 
{
	[AttributeUsage(AttributeTargets.Method)]
	public class CommandAttribute : Attribute
	{
		public string Name { get; private set; }

		public CommandAttribute(string name = null)
		{
			Name = name;
		}
	}

	public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
	{
		static T instance;

		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					instance = FindFirstObjectByType<T>();

					if (instance == null)
					{
						GameObject obj = new GameObject(typeof(T).Name);
						instance = obj.AddComponent<T>();
					}
				}
				return instance;
			}
		}

		/// <summary> 
		/// protected override void Awake() { base.Awake(); ... }
		/// </summary>
		protected virtual void Awake()
		{
			if (instance == null)
			{
				instance = this as T;
			}
			else if (instance != this)
			{
				Destroy(gameObject);
			}

			DontDestroyOnLoad(instance.transform.root.gameObject);
		}
	}

	public class QuickCommand : MonoSingleton<QuickCommand>
	{
		[SerializeField] int maxLogLength = 100;
		[SerializeField] KeyCode openKeyWithCtrl = KeyCode.BackQuote;

		[SerializeField][HideInInspector] GameObject commandView;
		[SerializeField][HideInInspector] InputField commandInput;
		[SerializeField][HideInInspector] Text logText;
		[SerializeField][HideInInspector] RectTransform[] fitLogRects;
		[SerializeField][HideInInspector] ScrollRect scrollRect;
		[SerializeField][HideInInspector] GameObject helperView;
		[SerializeField][HideInInspector] Text helperText;
		[SerializeField][HideInInspector] RectTransform[] fitHelperRects;
		[SerializeField][HideInInspector] GameObject watchView;
		[SerializeField][HideInInspector] Text[] watchTexts;
		[SerializeField][HideInInspector] RectTransform[] fitWatchRects;

		bool isShow;
		List<string> logEntries = new();
		Dictionary<string, MethodInfo> commandDic = new();
		List<(string functionName, string parameters)> helperTexts = new();
		int helperIndex;
		StringBuilder helperBuilder = new();
		float lastClickTime;
		float doubleClickThreshold = 0.3f;
		Dictionary<string, string> watchDic = new();
		string watchTempValue;
		StringBuilder text0Builder = new();
		StringBuilder text1Builder = new();
		SynchronizationContext syncContext;


		public static void SendWatch(string key, object value)
		{
			Instance.SendWatchInstance(key, value);
		}

		public void Send()
		{
			string text = commandInput.text;
			if (string.IsNullOrEmpty(text)) return;

			ExecuteCommand(text);
			commandInput.text = string.Empty;
			FillHelperTexts(text);
			ShowHelperView(false);
		}

		public void Clear()
		{
			logEntries.Clear();
			logText.text = string.Empty;
			RenewLogView();
		}

		public void Close() 
		{
			ShowCommand(false);
		}

		public void CommandInputChanged(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				ShowHelperView(false);
				return;
			}

			helperIndex = Mathf.Max(0, helperTexts.Count - 1);
			if (helperTexts.Count == 1 && helperIndex == 0 && 
				text.ToLower().StartsWith(helperTexts[0].functionName.ToLower()))
			{

			}
			else
			{
				FillHelperTexts(text);
			}
			helperIndex = Mathf.Max(0, helperTexts.Count - 1);


			if (helperTexts.Count > 0)
			{
				ShowHelperView(true);
			}
			else
			{
				ShowHelperView(false);
			}
		}

		public void ClickCommand() 
		{
			float currentTime = Time.time;

			if (currentTime - lastClickTime <= doubleClickThreshold)
			{
				ShowCommand(true);
			}
			lastClickTime = currentTime;
		}


		void OnEnable()
		{
			syncContext = SynchronizationContext.Current;
			isShow = commandView.activeInHierarchy;
			RegisterCommands();
			//Application.logMessageReceivedThreaded += AppendLogText;
			commandInput.onEndEdit.AddListener(OnEndEdit);
		}

		void OnDisable()
		{
			//Application.logMessageReceivedThreaded -= AppendLogText;
			commandInput.onEndEdit.RemoveListener(OnEndEdit);
		}

		void Update()
		{
			if (Input.anyKeyDown)
			{
				if (commandInput.isFocused)
				{
					if (Input.GetKeyDown(KeyCode.UpArrow))
					{
						ChangeHelperIndex(false);
					}
					else if (Input.GetKeyDown(KeyCode.DownArrow))
					{
						ChangeHelperIndex(true);
					}
					else if (Input.GetKeyDown(KeyCode.Tab))
					{
						WriteHelperTap();
					}
				}

				if (Input.GetKeyDown(openKeyWithCtrl) && 
					(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
				{
					ShowCommand(!isShow);
				}
			}
		}


		void OnEndEdit(string text)
		{
			if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
			{
				EventSystem.current.SetSelectedGameObject(commandInput.gameObject, null);
				commandInput.ActivateInputField();
			}
		}

		void AppendLogText(string text, string stackTrace, LogType type)
		{
			string curText = type switch
			{
				LogType.Log => $"{text}",
				LogType.Warning => $"<color=yellow>{text}</color>",
				_ => $"<color=red>{text}\n{stackTrace.Substring(0, Mathf.Max(0, stackTrace.Length - 1))}</color>"
			};

			logEntries.Add(curText);

			if (logEntries.Count > maxLogLength)
			{
				logEntries.RemoveAt(0);
			}

			syncContext.Post(state => 
			{
				if (logText != null) 
				{
					try
					{
						logText.text = string.Join("\n", logEntries);
					}
					catch {}
				}
				RenewLogView();
			}, null);
		}

		void RenewLogView()
		{
			Canvas.ForceUpdateCanvases();
			foreach (RectTransform fitLogRect in fitLogRects)
			{
				Fit(fitLogRect);
			}
			scrollRect.verticalNormalizedPosition = 0f;
		}

		void Fit(RectTransform Rect)
		{
			LayoutRebuilder.ForceRebuildLayoutImmediate(Rect);
		}

		void RegisterCommands()
		{
			IEnumerable<MethodInfo> methods = Assembly.GetExecutingAssembly()
				.GetTypes()
				.SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Static |
					BindingFlags.Public | BindingFlags.NonPublic))
				.Where(m => m.GetCustomAttribute<CommandAttribute>() != null);

			foreach (var method in methods)
			{
				string commandName = method.Name;
				commandDic[commandName] = method;
			}
		}

		void ExecuteCommand(string commandInput)
		{
			string[] splitInput = commandInput.Split(' ');
			string commandName = splitInput[0];
			string[] args = splitInput.Skip(1).ToArray();

			if (commandDic.TryGetValue(commandName, out MethodInfo method))
			{
				var component = FindFirstObjectByType(method.DeclaringType);
				ParameterInfo[] parameters = method.GetParameters();
				bool isSameParameter = parameters.Length == splitInput.Length - 1;

				if (component != null && isSameParameter)
				{
					AppendLogText($"<color=cyan>> {commandInput}</color>", string.Empty, LogType.Log);
					object[] parsedArgs = new object[parameters.Length];

					try
					{
						for (int i = 0; i < parameters.Length; i++)
						{
							Type paramType = parameters[i].ParameterType;
							parsedArgs[i] = Convert.ChangeType(args[i], paramType);
						}

						method.Invoke(component, parsedArgs);
					}
					catch (Exception e)
					{
						AppendLogText(e.Message, string.Empty, LogType.Exception);
					}
				}
			}
			else if (commandName == "/help")
			{
				ShowHelpLog();
			}
		}

		string GetUnityFriendlyTypeName(Type type)
		{
			if (type == typeof(int)) return "int";
			if (type == typeof(float)) return "float";
			if (type == typeof(double)) return "double";
			if (type == typeof(bool)) return "bool";
			if (type == typeof(string)) return "string";
			if (type == typeof(void)) return "void";
			return type.Name;
		}

		void ShowCommand(bool isShow) 
		{
			this.isShow = isShow;
			commandView.SetActive(isShow);
		}

		void FillHelperTexts(string text)
		{
			helperTexts.Clear();
			var exactMatches = new List<(string, string)>();
			var partialMatches = new List<(string, string)>();

			foreach (var (key, methodInfo) in commandDic)
			{
				if (key.ToLower().Contains(text.ToLower()))
				{
					ParameterInfo[] parameterInfos = methodInfo.GetParameters();
					var helperText = (key, parameterInfos.Length > 0 ? 
						string.Join(", ", parameterInfos.Select(p => $"{GetUnityFriendlyTypeName(p.ParameterType)} {p.Name}")) : 
						string.Empty);

					if (key.ToLower().StartsWith(text.ToLower()))
						exactMatches.Add(helperText);
					else
						partialMatches.Add(helperText);
				}
			}

			helperTexts.AddRange(partialMatches);
			helperTexts.AddRange(exactMatches);
		}

		void ShowHelperView(bool isShow) 
		{
			if (helperView.activeSelf != isShow)
			{
				helperView.SetActive(isShow);
			}
			if (!isShow) return;

			helperBuilder.Clear();

			for (int i = 0; i < helperTexts.Count; i++)
			{
				var (functionName, parameters) = helperTexts[i];
				string paramText = string.IsNullOrEmpty(parameters) ? string.Empty : $" ({parameters})";

				if (helperIndex == i)
				{
					helperBuilder.Append($"<color=yellow>{functionName}{paramText}</color>\n");
				}
				else
				{
					helperBuilder.Append($"{functionName}{paramText}\n");
				}
			}

			string newText = helperBuilder.ToString().TrimEnd('\n');

			if (helperText.text != newText)
			{
				helperText.text = newText;
				Canvas.ForceUpdateCanvases();
				foreach (RectTransform fitHelperRect in fitHelperRects)
				{
					Fit(fitHelperRect);
				}
			}
		}

		void ChangeHelperIndex(bool isUp)
		{
			helperIndex = Mathf.Clamp(isUp ? helperIndex + 1 : helperIndex - 1, 0, helperTexts.Count - 1);
			commandInput.caretPosition = commandInput.text.Length;
			ShowHelperView(helperTexts.Count > 0);
		}
	
		void WriteHelperTap()
		{
			if (helperTexts.Count == 0) return;

			var (functionName, parameters) = helperTexts[helperIndex];
			commandInput.text = functionName;
			commandInput.caretPosition = commandInput.text.Length;
			ShowHelperView(helperTexts.Count > 0);
		}

		void ShowHelpLog()
		{
			AppendLogText($"<color=cyan>> /help</color>", string.Empty, LogType.Log);
			foreach (var (key, methodInfo) in commandDic)
			{
				ParameterInfo[] parameterInfos = methodInfo.GetParameters();
				string parameters = parameterInfos.Length > 0 ? 
					$" ({string.Join(", ", parameterInfos.Select(p => GetUnityFriendlyTypeName(p.ParameterType)))})" : string.Empty;
				AppendLogText($"{key}{parameters}", string.Empty, LogType.Log);
			}
		}

		void SendWatchInstance(string key, object value)
		{
			watchTempValue = value.ToString();
			if (watchDic.TryGetValue(key, out string existingValue))
			{
				if (existingValue != watchTempValue)
				{
					watchDic[key] = watchTempValue;
					RenewWatchView(false);
				}
			}
			else
			{
				watchDic[key] = watchTempValue;
				RenewWatchView(true);
			}
		}

		void RenewWatchView(bool sizeChange)
		{
			bool hasElements = watchDic.Count > 0;

			if (watchView.activeSelf != hasElements)
			{
				watchView.SetActive(hasElements);
			}

			if (!hasElements) return;

			text0Builder.Clear();
			text1Builder.Clear();
			int index = 0;

			foreach (var (key, value) in watchDic)
			{
				string formattedText = $"<b><color=cyan>{key}</color></b> {value}\n";
				if (index % 2 == 0)
				{
					text0Builder.Append(formattedText);
				}
				else
				{
					text1Builder.Append(formattedText);
				}
				index++;
			}

			watchTexts[0].text = text0Builder.ToString().TrimEnd('\n');
			watchTexts[1].text = text1Builder.ToString().TrimEnd('\n');

			if (sizeChange)
			{
				Canvas.ForceUpdateCanvases();
				foreach (RectTransform fitWatchRect in fitWatchRects)
				{
					Fit(fitWatchRect);
				}
			}
		}
	}
}

