using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileConsole
{
	public class MobileConsole : MonoBehaviour
	{
		public enum Skin
		{
			Light,
			Dark
		}
		[SerializeField] private Skin _skin;
		
		[SerializeField] private bool _showOnError;

		private enum Corner
		{
			TopLeft,
			TopRight,
			BottomLeft,
			BottomRight
		}
		[SerializeField] private Corner _tapCorner;
		
		[Range(50, 500)]
		[SerializeField] private int _maxLogEntries = 100;

		private GameObject _console;
		private GameObject _logEntryPrefab;
		private ScrollRect _logScrollRect;
		private GameObject _logScrollbar;
		private RectTransform _logEntriesContainer;
		private Toggle _infoToggle;
		private Toggle _warningToggle;
		private Toggle _errorToggle;
		private Text _infoCountText;
		private Text _warningCountText;
		private Text _errorCountText;
		private Button _openButton;
		private Button _closeButton;
		private Button _clearButton;
		private ScrollRect _stackTraceScrollRect;
		private Text _stackTraceText;
		private RectTransform _pool;

		private class LogCall
		{
			public string LogString;
			public string StackTrace;
			public LogType Type;
		}
	
		private class LogEntry
		{
			public string LogString;
			public string StackTrace;
			public LogType Type;
			public DateTime TimeStamp;
			public int GameObjectInstanceId;
		}

		private Queue<LogCall> _pooledLogCalls = new Queue<LogCall>();
		private Queue<LogCall> _logCalls = new Queue<LogCall>();
		private Queue<LogEntry> _pooledLogEntries = new Queue<LogEntry>();
		private Queue<LogEntry> _logEntries = new Queue<LogEntry>();
		private LogEntry _selectedLogEntry;
		private Dictionary<int, GameObject> _logEntryGameObjects = new Dictionary<int, GameObject>();
		private Dictionary<LogType, bool> _filter = new Dictionary<LogType, bool>();
		private Dictionary<LogType, int> _counts = new Dictionary<LogType, int>();

		private bool _scrollToBottom;
		private bool _scrollToBottomOnShow;
		private int _clickCount;
		private float _lastClickTime;
	
		void Awake()
		{
			if (IsReleaseBuild() || IsDuplicate())
			{
				Destroy(gameObject);
			}
			else
			{
				DontDestroyOnLoad(gameObject);
				CreateEventSystemIfRequired();
				SetupReferences();
				FillPools();
				ApplySkin();
				SetupFilters();
				AddLogHandler();
			}
		}

		private bool IsReleaseBuild()
		{
			return !Application.isEditor && !Debug.isDebugBuild;
		}
		
		private bool IsDuplicate()
		{
			return FindObjectsOfType(GetType()).Length > 1;
		}
		
		private void CreateEventSystemIfRequired()
		{
			if (FindObjectOfType<EventSystem>() == null)
			{
				GameObject eventSystemGameObject = new GameObject();
				eventSystemGameObject.name = "EventSystem";
				eventSystemGameObject.AddComponent<EventSystem>();
				eventSystemGameObject.AddComponent<StandaloneInputModule>();
				eventSystemGameObject.transform.SetParent(FindDeepChild(transform, "Mobile Console Canvas"));
			}
		}

		private void SetupReferences()
		{
			Transform thisTransform = transform;
			_console = FindDeepChild(thisTransform, "Console").gameObject;
			_logEntryPrefab = (GameObject) Resources.Load("Mobile Console Log Entry");
			_logScrollRect = FindDeepChild(thisTransform, "Log").GetComponent<ScrollRect>();
			_logEntriesContainer = FindDeepChild(thisTransform, "Log Content").GetComponent<RectTransform>();
			_logScrollbar = FindDeepChild(thisTransform, "Log Scrollbar").gameObject;
			_infoToggle = FindDeepChild(thisTransform, "Info Toggle").GetComponent<Toggle>();
			_infoToggle.onValueChanged.AddListener(value => FilterChanged(LogType.Log, value));
			_warningToggle = FindDeepChild(thisTransform, "Warning Toggle").GetComponent<Toggle>();
			_errorToggle = FindDeepChild(thisTransform, "Error Toggle").GetComponent<Toggle>();
			_errorToggle.onValueChanged.AddListener(value => FilterChanged(LogType.Error, value));
			_infoCountText = FindDeepChild(thisTransform, "Info Count Text").GetComponent<Text>();
			_warningCountText = FindDeepChild(thisTransform, "Warning Count Text").GetComponent<Text>();
			_warningToggle.onValueChanged.AddListener(value => FilterChanged(LogType.Warning, value));
			_errorCountText = FindDeepChild(thisTransform, "Error Count Text").GetComponent<Text>();
			FindDeepChild(thisTransform, "Open Button Top Left").gameObject.SetActive(false);
			FindDeepChild(thisTransform, "Open Button Top Right").gameObject.SetActive(false);
			FindDeepChild(thisTransform, "Open Button Bottom Left").gameObject.SetActive(false);
			FindDeepChild(thisTransform, "Open Button Bottom Right").gameObject.SetActive(false);
			switch (_tapCorner)
			{
				case Corner.TopLeft:
					_openButton = FindDeepChild(thisTransform, "Open Button Top Left").GetComponent<Button>();
					break;
				case Corner.TopRight:
					_openButton = FindDeepChild(thisTransform, "Open Button Top Right").GetComponent<Button>();
					break;
				case Corner.BottomLeft:
					_openButton = FindDeepChild(thisTransform, "Open Button Bottom Left").GetComponent<Button>();
					break;
				case Corner.BottomRight:
					_openButton = FindDeepChild(thisTransform, "Open Button Bottom Right").GetComponent<Button>();
					break;
			}
			_openButton.onClick.AddListener(Open);
			_openButton.gameObject.SetActive(true);
			_closeButton = FindDeepChild(thisTransform, "Close Button").GetComponent<Button>();
			_closeButton.onClick.AddListener(Close);
			_clearButton = FindDeepChild(thisTransform, "Clear Button").GetComponent<Button>();
			_clearButton.onClick.AddListener(Clear);
			_stackTraceScrollRect = FindDeepChild(thisTransform, "StackTrace").GetComponent<ScrollRect>();
			_stackTraceText = FindDeepChild(thisTransform, "StackTrace Text").GetComponent<Text>();
			_pool = FindDeepChild(thisTransform, "Pool").GetComponent<RectTransform>();
		}

		private void FillPools()
		{
			for (int i = 0; i < _maxLogEntries; i++)
			{
				_pooledLogEntries.Enqueue(new LogEntry());
				
				GameObject logEntryGameObject = Instantiate(_logEntryPrefab);
				logEntryGameObject.GetComponent<RectTransform>().SetParent(_pool);
			}
		}
		
		private void ApplySkin()
		{
			foreach (Style style in GetComponentsInChildren<Style>(true))
			{
				style.SetSkin(_skin);
			}
		}
		
		private void SetupFilters()
		{
			_filter[LogType.Log] = true;
			_filter[LogType.Warning] = true;
			_filter[LogType.Error] = true;
		}

		private void AddLogHandler()
		{
			Application.logMessageReceivedThreaded += HandleLog;
		}
		
		private void HandleLog(string logString, string stackTrace, LogType type)
		{
			LogCall logCall;
			lock (_pooledLogCalls)
			{
				logCall = _pooledLogCalls.Count > 0 ? _pooledLogCalls.Dequeue() : new LogCall();
			}
			logCall.LogString = logString;
			logCall.StackTrace = stackTrace;
			logCall.Type = type == LogType.Exception || type == LogType.Assert ? LogType.Error : type;
			lock (_logCalls)
			{
				_logCalls.Enqueue(logCall);
			}
		}
	
		void Start()
		{
			UpdateCountTexts();
			ClearStackTrace();
			Close();
		}

		void Update()
		{
			CheckScrollToBottom();
			HandleLogCalls();
		}

		private void CheckScrollToBottom()
		{
			if (_scrollToBottom)
			{
				ScrollLogToBottom();
				_scrollToBottom = false;
			}
		}

		private void HandleLogCalls()
		{
			if (_logCalls.Count == 0)
			{
				return;
			}
			
			bool wasLogAtBottom = IsLogAtBottom();
			bool shouldShowDueToError = false;
			
			lock (_logCalls)
			{
				while (_logCalls.Count > 0)
				{
					EnsureFreeLogEntryInPool();
					
					LogCall logCall = _logCalls.Dequeue();
					HandleLogCall(logCall);
					lock (_pooledLogCalls)
					{
						_pooledLogCalls.Enqueue(logCall);
					}

					if (_showOnError && logCall.Type == LogType.Error)
					{
						shouldShowDueToError = true;
					}
				}
			}
			
			if (IsShown())
			{
				_scrollToBottom = wasLogAtBottom;
			}
			else
			{
				_scrollToBottomOnShow = shouldShowDueToError || wasLogAtBottom;
				
				if (shouldShowDueToError)
				{
					Show();
				}
			}
		}
		
		private void EnsureFreeLogEntryInPool()
		{
			while (_logEntries.Count >= _maxLogEntries)
			{
				LogEntry firstLogEntry = _logEntries.Dequeue();
				_pooledLogEntries.Enqueue(firstLogEntry);
				GameObject firstLogEntryGameObject = _logEntryGameObjects[firstLogEntry.GameObjectInstanceId];
				firstLogEntryGameObject.GetComponent<RectTransform>().SetParent(_pool);
				_logEntryGameObjects.Remove(firstLogEntry.GameObjectInstanceId);
				DecrementCount(firstLogEntry.Type);
				if (firstLogEntry == _selectedLogEntry)
				{
					ClearSelection();
				}
			}
		}

		private void HandleLogCall(LogCall logCall)
		{
			LogEntry logEntry = CreateLogEntry(logCall);
			_logEntries.Enqueue(logEntry);
					
			GameObject logEntryGameObject = CreateLogEntryGameObject(logEntry);
			logEntryGameObject.SetActive(_filter[logEntry.Type]);
			logEntry.GameObjectInstanceId = logEntryGameObject.GetInstanceID();
			_logEntryGameObjects[logEntry.GameObjectInstanceId] = logEntryGameObject;
					
			IncrementCount(logEntry.Type);
		}
		
		private bool IsLogAtBottom()
		{
			return !_logScrollbar.activeSelf || Math.Abs(_logScrollRect.verticalNormalizedPosition) < 0.01f;
		}

		private void ScrollLogToBottom()
		{
			_logScrollRect.verticalNormalizedPosition = 0.0f;
		}
		
		private LogEntry CreateLogEntry(LogCall logCall)
		{
			LogEntry logEntry = _pooledLogEntries.Dequeue();
			logEntry.LogString = logCall.LogString;
			logEntry.StackTrace = logCall.StackTrace;
			logEntry.Type = logCall.Type;
			logEntry.TimeStamp = DateTime.Now;
			logEntry.GameObjectInstanceId = -1;
			return logEntry;
		}
		
		private GameObject CreateLogEntryGameObject(LogEntry logEntry)
		{
			GameObject logEntryGameObject = _pool.GetChild(0).gameObject;
			logEntryGameObject.GetComponent<RectTransform>().SetParent(_logEntriesContainer);
			FindDeepChild(logEntryGameObject.transform, "Log Text").GetComponentInChildren<Text>().text = logEntry.LogString;
			FindDeepChild(logEntryGameObject.transform, "Time Text").GetComponentInChildren<Text>().text = logEntry.TimeStamp.ToLongTimeString();
			FindDeepChild(logEntryGameObject.transform, "Info Icon").gameObject.SetActive(logEntry.Type == LogType.Log);
			FindDeepChild(logEntryGameObject.transform, "Warning Icon").gameObject.SetActive(logEntry.Type == LogType.Warning);
			FindDeepChild(logEntryGameObject.transform, "Error Icon").gameObject.SetActive(logEntry.Type == LogType.Error);
			UpdateLogEntryGameObjectBasedOnSelected(logEntryGameObject, false);
			logEntryGameObject.GetComponent<Button>().onClick.RemoveAllListeners();
			logEntryGameObject.GetComponent<Button>().onClick.AddListener(() => SelectLogEntry(logEntry));
			return logEntryGameObject;
		}

		private void Close()
		{
			_console.SetActive(false);
			_openButton.gameObject.SetActive(true);
		}

		private void Open()
		{
			if (!IsShown())
			{
				if (Time.time - _lastClickTime < 0.4f)
				{
					_clickCount++;
					if (_clickCount >= 2)
					{
						Show();
					}
				}
				else
				{
					_clickCount = 0;
				}
				_lastClickTime = Time.time;
			}
		}

		private void Show()
		{
			_openButton.gameObject.SetActive(false);
			_console.SetActive(true);
			
			if (_scrollToBottomOnShow)
			{
				ScrollLogToBottom();
				_scrollToBottomOnShow = false;
			}
		}

		private bool IsShown()
		{
			return _console.activeSelf;
		}
		
		private void FilterChanged(LogType type, bool value)
		{
			_filter[type] = value;

			foreach (LogEntry logEntry in _logEntries)
			{
				_logEntryGameObjects[logEntry.GameObjectInstanceId].SetActive(_filter[logEntry.Type]);
			}
			
			if (!value && _selectedLogEntry != null && _selectedLogEntry.Type == type)
			{
				ClearSelection();
			}
		}
		
		private void IncrementCount(LogType type)
		{
			_counts[type] = _counts.ContainsKey(type) ? _counts[type] + 1 : 1;
			UpdateCountTexts();
		}
		
		private void DecrementCount(LogType type)
		{
			_counts[type] = _counts.ContainsKey(type) ? _counts[type] - 1 : 0;
			UpdateCountTexts();
		}
		
		private void ResetCounts()
		{
			_counts.Clear();
			UpdateCountTexts();
		}

		private void UpdateCountTexts()
		{
			_infoCountText.text = "" + (_counts.ContainsKey(LogType.Log) ? _counts[LogType.Log] : 0);
			_warningCountText.text = "" + (_counts.ContainsKey(LogType.Warning) ? _counts[LogType.Warning] : 0);
			_errorCountText.text = "" + (_counts.ContainsKey(LogType.Error) ? _counts[LogType.Error] : 0);
		}

		private void Clear()
		{
			ClearSelection();
			ReturnLogEntriesToPool();
			ReturnLogEntryGameObjectsToPool();
			ResetCounts();
		}

		private void ReturnLogEntriesToPool()
		{
			foreach (LogEntry logEntry in _logEntries)
			{
				_pooledLogEntries.Enqueue(logEntry);
			}
			_logEntries.Clear();
		}

		private void ReturnLogEntryGameObjectsToPool()
		{
			foreach (GameObject logEntryGameObject in _logEntryGameObjects.Values)
			{
				logEntryGameObject.GetComponent<RectTransform>().SetParent(_pool);
			}
			_logEntryGameObjects.Clear();
		}
	
		private void SelectLogEntry(LogEntry logEntry)
		{
			bool alreadySelected = _selectedLogEntry == logEntry;
			
			ClearSelection();

			if (!alreadySelected)
			{
				_selectedLogEntry = logEntry;
				GameObject logEntryGameObject = _logEntryGameObjects[logEntry.GameObjectInstanceId];
				UpdateLogEntryGameObjectBasedOnSelected(logEntryGameObject, true);
				ShowStackTrace(logEntry);
			}
		}

		private void ClearSelection()
		{
			if (_selectedLogEntry != null)
			{
				if (_logEntryGameObjects.ContainsKey(_selectedLogEntry.GameObjectInstanceId))
				{
					GameObject selectedLogEntryGameObject = _logEntryGameObjects[_selectedLogEntry.GameObjectInstanceId];
					UpdateLogEntryGameObjectBasedOnSelected(selectedLogEntryGameObject, false);
				}
				_selectedLogEntry = null;
				ClearStackTrace();
			}
		}

		private void UpdateLogEntryGameObjectBasedOnSelected(GameObject logEntryGameObject, bool selected)
		{
			if (selected)
			{
				FindDeepChild(logEntryGameObject.transform, "Background").GetComponent<Style>().SetStyle(Style.StyleType.SelectedBackground);
				FindDeepChild(logEntryGameObject.transform, "Log Text").GetComponent<Style>().SetStyle(Style.StyleType.SelectedText);
				FindDeepChild(logEntryGameObject.transform, "Time Text").GetComponent<Style>().SetStyle(Style.StyleType.SelectedText);
			}
			else
			{
				FindDeepChild(logEntryGameObject.transform, "Background").GetComponent<Style>().SetStyle(
					logEntryGameObject.transform.GetSiblingIndex() % 2 == 0 ? Style.StyleType.LightBackground : Style.StyleType.DarkBackground);
				FindDeepChild(logEntryGameObject.transform, "Log Text").GetComponent<Style>().SetStyle(Style.StyleType.Text);
				FindDeepChild(logEntryGameObject.transform, "Time Text").GetComponent<Style>().SetStyle(Style.StyleType.Text);
			}
			FindDeepChild(logEntryGameObject.transform, "Time Text").gameObject.SetActive(selected);
		}
	
		private void ClearStackTrace()
		{
			_stackTraceText.text = "\n";
		}
	
		private void ShowStackTrace(LogEntry logEntry)
		{
			_stackTraceText.text = logEntry.LogString + "\n" + logEntry.StackTrace;
			_stackTraceScrollRect.verticalNormalizedPosition = 1.0f;
		}
		
		private Transform FindDeepChild(Transform parent, string name)
		{
			foreach (Transform child in parent)
			{
				if (child.name == name)
				{
					return child;
				}
				else
				{
					var result = FindDeepChild(child, name);
					if (result != null)
					{
						return result;
					}
				}
			}
			return null;
		}
	}
}
