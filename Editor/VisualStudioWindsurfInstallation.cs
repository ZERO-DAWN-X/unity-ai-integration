/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using SimpleJSON;
using IOPath = System.IO.Path;
using Debug = UnityEngine.Debug;

namespace Microsoft.Unity.VisualStudio.Editor {
	internal class VisualStudioWindsurfInstallation : VisualStudioInstallation {
		private static readonly IGenerator _generator = new SdkStyleProjectGeneration();

		public override bool SupportsAnalyzers {
			get {
				return true;
			}
		}

		public override Version LatestLanguageVersionSupported {
			get {
				return new Version(11, 0);
			}
		}

		private string GetExtensionPath() {
			var vscode = IsPrerelease ? ".vscode-insiders" : ".vscode";
			var extensionsPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), vscode, "extensions");
			if (!Directory.Exists(extensionsPath))
				return null;

			return Directory
				.EnumerateDirectories(extensionsPath, $"{MicrosoftUnityExtensionId}*") // publisherid.extensionid
				.OrderByDescending(n => n)
				.FirstOrDefault();
		}

		public override string[] GetAnalyzers() {
			var vstuPath = GetExtensionPath();
			if (string.IsNullOrEmpty(vstuPath))
				return Array.Empty<string>();

			return GetAnalyzers(vstuPath);
		}

		public override IGenerator ProjectGenerator {
			get {
				return _generator;
			}
		}

		private static bool IsCandidateForDiscovery(string path) {
#if UNITY_EDITOR_OSX
			return Directory.Exists(path) && Regex.IsMatch(path, ".*Windsurf.*.app$", RegexOptions.IgnoreCase);
#elif UNITY_EDITOR_WIN
			return File.Exists(path) && Regex.IsMatch(path, ".*Windsurf.*.exe$", RegexOptions.IgnoreCase);
#else
			return File.Exists(path) && path.EndsWith("windsurf", StringComparison.OrdinalIgnoreCase);
#endif
		}

		[Serializable]
		internal class VisualStudioCodeManifest {
			public string name;
			public string version;
		}

		public static bool TryDiscoverInstallation(string editorPath, out IVisualStudioInstallation installation) {
			installation = null;

			if (string.IsNullOrEmpty(editorPath))
				return false;

			if (!IsCandidateForDiscovery(editorPath))
				return false;

			Version version = null;
			var isPrerelease = false;

			try {
				var manifestBase = GetRealPath(editorPath);

#if UNITY_EDITOR_WIN
				// on Windows, editorPath is a file, resources as subdirectory
				manifestBase = IOPath.GetDirectoryName(manifestBase);
#elif UNITY_EDITOR_OSX
				// on Mac, editorPath is a directory
				manifestBase = IOPath.Combine(manifestBase, "Contents");
#else
				// on Linux, editorPath is a file, in a bin sub-directory
				var parent = Directory.GetParent(manifestBase);
				// but we can link to [vscode]/code or [vscode]/bin/code
				manifestBase = parent?.Name == "bin" ? parent.Parent?.FullName : parent?.FullName;
#endif

				if (manifestBase == null)
					return false;

				var manifestFullPath = IOPath.Combine(manifestBase, "resources", "app", "package.json");
				if (File.Exists(manifestFullPath)) {
					var manifest = JsonUtility.FromJson<VisualStudioCodeManifest>(File.ReadAllText(manifestFullPath));
					Version.TryParse(manifest.version.Split('-').First(), out version);
					isPrerelease = manifest.version.ToLower().Contains("insider");
				}
			} catch (Exception) {
				// do not fail if we are not able to retrieve the exact version number
			}

			isPrerelease = isPrerelease || editorPath.ToLower().Contains("insider");
			installation = new VisualStudioWindsurfInstallation() {
				IsPrerelease = isPrerelease,
				Name = "Windsurf" + (isPrerelease ? " - Insider" : string.Empty) + (version != null ? $" [{version.ToString(3)}]" : string.Empty),
				Path = editorPath,
				Version = version ?? new Version()
			};

			return true;
		}

		public static IEnumerable<IVisualStudioInstallation> GetVisualStudioInstallations() {
			var candidates = new List<string>();

#if UNITY_EDITOR_WIN
			var localAppPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs");
			var programFiles = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

			foreach (var basePath in new[] { localAppPath, programFiles }) {
				candidates.Add(IOPath.Combine(basePath, "Windsurf", "Windsurf.exe"));
			}
#elif UNITY_EDITOR_OSX
			var appPath = IOPath.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
			candidates.AddRange(Directory.EnumerateDirectories(appPath, "Windsurf*.app"));
#elif UNITY_EDITOR_LINUX
			// Well known locations
			candidates.Add("/usr/bin/windsurf");
			candidates.Add("/bin/windsurf");
			candidates.Add("/usr/local/bin/windsurf");

			// Preference ordered base directories relative to which desktop files should be searched
			candidates.AddRange(GetXdgCandidates());
#endif

			foreach (var candidate in candidates.Distinct()) {
				if (TryDiscoverInstallation(candidate, out var installation))
					yield return installation;
			}
		}

#if UNITY_EDITOR_LINUX
		private static readonly Regex DesktopFileExecEntry = new Regex(@"Exec=(\S+)", RegexOptions.Singleline | RegexOptions.Compiled);

		private static IEnumerable<string> GetXdgCandidates()
		{
			var envdirs = Environment.GetEnvironmentVariable("XDG_DATA_DIRS");
			if (string.IsNullOrEmpty(envdirs))
				yield break;

			var dirs = envdirs.Split(':');
			foreach(var dir in dirs)
			{
				Match match = null;

				try
				{
					var desktopFile = IOPath.Combine(dir, "applications/windsurf.desktop");
					if (!File.Exists(desktopFile))
						continue;
				
					var content = File.ReadAllText(desktopFile);
					match = DesktopFileExecEntry.Match(content);
				}
				catch
				{
					// do not fail if we cannot read desktop file
				}

				if (match == null || !match.Success)
					continue;

				yield return match.Groups[1].Value;
				break;
			}
		}

		[System.Runtime.InteropServices.DllImport ("libc")]
		private static extern int readlink(string path, byte[] buffer, int buflen);

		internal static string GetRealPath(string path)
		{
			byte[] buf = new byte[512];
			int ret = readlink(path, buf, buf.Length);
			if (ret == -1) return path;
			char[] cbuf = new char[512];
			int chars = System.Text.Encoding.Default.GetChars(buf, 0, ret, cbuf, 0);
			return new string(cbuf, 0, chars);
		}
#else
		internal static string GetRealPath(string path) {
			return path;
		}
#endif

		public override void CreateExtraFiles(string projectDirectory) {
			var vscodeDirectory = IOPath.Combine(projectDirectory, ".vscode");
			var enablePatch = EditorPrefs.GetBool("windsurf_enable_integration", true);

			CreateLaunchFile(vscodeDirectory, enablePatch);
			CreateSettingsFile(vscodeDirectory, enablePatch);
			CreateRecommendedExtensionsFile(vscodeDirectory, enablePatch);
		}

		private const string DefaultLaunchFileContent = @"{
	// Use IntelliSense to learn about possible attributes.
	// Hover to view descriptions of existing attributes.
	// For more information, visit: https://go.microsoft.com/fwlink/?linkid=830387
	""version"": ""0.2.0"",
	""configurations"": [
		{
			""name"": ""Attach to Unity"",
			""type"": ""vstuc"",
			""request"": ""attach""
		}
	]
}";

		private static void CreateLaunchFile(string vscodeDirectory, bool enablePatch) {
			if (!Directory.Exists(vscodeDirectory))
				Directory.CreateDirectory(vscodeDirectory);

			var launchFile = IOPath.Combine(vscodeDirectory, "launch.json");

			if (File.Exists(launchFile) && enablePatch) {
				PatchLaunchFile(launchFile);
				return;
			}

			if (!File.Exists(launchFile))
				File.WriteAllText(launchFile, DefaultLaunchFileContent);
		}

		private static void PatchLaunchFile(string launchFile) {
			try {
				var content = File.ReadAllText(launchFile);
				var launch = JSON.Parse(content);

				var configurations = launch["configurations"];
				if (configurations == null) {
					launch["configurations"] = new JSONArray();
					configurations = launch["configurations"];
				}

				var attachConfiguration = configurations.Children.FirstOrDefault(config => config["type"] == "vstuc");
				if (attachConfiguration == null) {
					var newConfig = new JSONObject();
					newConfig["name"] = "Attach to Unity";
					newConfig["type"] = "vstuc";
					newConfig["request"] = "attach";
					configurations.Add(newConfig);
				}

				WriteAllTextFromJObject(launchFile, launch);
			} catch (Exception e) {
				Debug.LogWarning($"[Windsurf] Failed to patch launch.json: {e.Message}");
			}
		}

		private void CreateSettingsFile(string vscodeDirectory, bool enablePatch) {
			if (!Directory.Exists(vscodeDirectory))
				Directory.CreateDirectory(vscodeDirectory);

			var settingsFile = IOPath.Combine(vscodeDirectory, "settings.json");

			if (File.Exists(settingsFile) && enablePatch) {
				PatchSettingsFile(settingsFile);
				return;
			}

			if (!File.Exists(settingsFile)) {
				var settings = new JSONObject();
				
				// Windsurf-specific settings
				settings["windsurf.enable"] = true;
				settings["windsurf.cascade.autoActivate"] = true;
				settings["windsurf.tab.enable"] = true;
				
				// General Unity settings
				settings["files.exclude"] = new JSONObject {
					["**/.DS_Store"] = true,
					["**/.git"] = true,
					["**/.gitmodules"] = true,
					["**/*.booproj"] = true,
					["**/*.pidb"] = true,
					["**/*.suo"] = true,
					["**/*.user"] = true,
					["**/*.userprefs"] = true,
					["**/*.unityproj"] = true,
					["**/*.dll"] = true,
					["**/*.exe"] = true,
					["**/.*"] = true,
					["**/*.meta"] = true,
					["**/*.pdb"] = true,
					["**/Library"] = true,
					["**/ProjectSettings"] = true,
					["**/Temp"] = true,
					["bin/"] = true,
					["obj/"] = true,
					["UpgradeLog*.XML"] = true,
					["UpgradeLog*.htm"] = true
				};

				settings["files.associations"] = new JSONObject {
					["*.cs"] = "csharp",
					["*.shader"] = "hlsl",
					["*.compute"] = "hlsl"
				};

				WriteAllTextFromJObject(settingsFile, settings);
			}
		}

		private void PatchSettingsFile(string settingsFile) {
			try {
				var content = File.ReadAllText(settingsFile);
				var settings = JSON.Parse(content);

				// Enable Windsurf features
				settings["windsurf.enable"] = true;
				settings["windsurf.cascade.autoActivate"] = true;
				settings["windsurf.tab.enable"] = true;

				WriteAllTextFromJObject(settingsFile, settings);
			} catch (Exception e) {
				Debug.LogWarning($"[Windsurf] Failed to patch settings.json: {e.Message}");
			}
		}

		private const string MicrosoftUnityExtensionId = "visualstudiotoolsforunity.vstuc";
		private const string DefaultRecommendedExtensionsContent = @"{
	""recommendations"": [
		""visualstudiotoolsforunity.vstuc""
	]
}";

		private static void CreateRecommendedExtensionsFile(string vscodeDirectory, bool enablePatch) {
			if (!Directory.Exists(vscodeDirectory))
				Directory.CreateDirectory(vscodeDirectory);

			var extensionFile = IOPath.Combine(vscodeDirectory, "extensions.json");

			if (File.Exists(extensionFile) && enablePatch) {
				PatchRecommendedExtensionsFile(extensionFile);
				return;
			}

			if (!File.Exists(extensionFile))
				File.WriteAllText(extensionFile, DefaultRecommendedExtensionsContent);
		}

		private static void PatchRecommendedExtensionsFile(string extensionFile) {
			try {
				var content = File.ReadAllText(extensionFile);
				var extensions = JSON.Parse(content);

				var recommendations = extensions["recommendations"];
				if (recommendations == null) {
					extensions["recommendations"] = new JSONArray();
					recommendations = extensions["recommendations"];
				}

				var hasUnityExtension = recommendations.Children.Any(ext => ext.Value == MicrosoftUnityExtensionId);
				if (!hasUnityExtension) {
					recommendations.Add(MicrosoftUnityExtensionId);
				}

				WriteAllTextFromJObject(extensionFile, extensions);
			} catch (Exception e) {
				Debug.LogWarning($"[Windsurf] Failed to patch extensions.json: {e.Message}");
			}
		}

		private static void WriteAllTextFromJObject(string file, JSONNode node) {
			try {
				File.WriteAllText(file, node.ToString(2));
			} catch (Exception e) {
				Debug.LogWarning($"[Windsurf] Failed to write file {file}: {e.Message}");
			}
		}

		private Process FindRunningWindsurfWithSolution(string solutionPath) {
			var processes = new List<Process>();

			try {
#if UNITY_EDITOR_WIN
				processes.AddRange(Process.GetProcessesByName("Windsurf"));
				processes.AddRange(Process.GetProcessesByName("Windsurf Helper"));
#elif UNITY_EDITOR_OSX
				processes.AddRange(Process.GetProcessesByName("windsurf"));
				processes.AddRange(Process.GetProcessesByName("Windsurf"));
#else
				processes.AddRange(Process.GetProcessesByName("windsurf"));
#endif

				foreach (var process in processes) {
					try {
						var commandLine = GetProcessCommandLine(process);
						if (!string.IsNullOrEmpty(commandLine) && commandLine.Contains(solutionPath)) {
							return process;
						}
					} catch (Exception ex) {
						Debug.LogError($"[Windsurf] Error checking process: {ex}");
					}
				}
			} catch (Exception ex) {
				Debug.LogError($"[Windsurf] Error finding processes: {ex}");
			}

			return null;
		}

		public override bool Open(string path, int line, int column, string solution) {
			var directory = IOPath.GetDirectoryName(solution);
			var projectName = IOPath.GetFileNameWithoutExtension(solution);

			try {
				var existingProcess = FindRunningWindsurfWithSolution(directory);
				if (existingProcess != null) {
					var arguments = $"--goto \"{path}:{line}:{column}\"";
					Process.Start(new ProcessStartInfo {
						FileName = Path,
						Arguments = arguments,
						UseShellExecute = false,
						CreateNoWindow = true
					});
					return true;
				}
			} catch (Exception ex) {
				Debug.LogError($"[Windsurf] Error using existing instance: {ex}");
			}

			var args = $"\"{directory}\" --goto \"{path}:{line}:{column}\"";
			return CodeEditor.OSOpenFile(Path, args);
		}

		private static ProcessStartInfo ProcessStartInfoFor(string application, string arguments) {
			return new ProcessStartInfo {
				FileName = application,
				Arguments = arguments,
				UseShellExecute = false,
				CreateNoWindow = true
			};
		}

		public static void Initialize() {
			var editorPath = EditorPrefs.GetString("kScriptsDefaultApp");
			if (TryDiscoverInstallation(editorPath, out var installation)) {
				CodeEditor.Register(installation);
			}
		}

		private string GetWindsurfStoragePath() {
			var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string windsurfStoragePath;

#if UNITY_EDITOR_OSX
			windsurfStoragePath = Path.Combine(userProfile, "Library", "Application Support", "Windsurf", "User", "workspaceStorage");
#elif UNITY_EDITOR_LINUX
			windsurfStoragePath = Path.Combine(userProfile, ".config", "Windsurf", "User", "workspaceStorage");
#else
			windsurfStoragePath = Path.Combine(userProfile, "AppData", "Roaming", "Windsurf", "User", "workspaceStorage");
#endif

			Debug.Log($"[Windsurf] Looking for workspaces in: {windsurfStoragePath}");

			if (Directory.Exists(windsurfStoragePath))
			{
				foreach (var workspaceDir in Directory.GetDirectories(windsurfStoragePath))
				{
					var workspaceStateFile = Path.Combine(workspaceDir, "workspace.json");
					if (File.Exists(workspaceStateFile))
					{
						try
						{
							var workspaceContent = File.ReadAllText(workspaceStateFile);
							var workspaceData = JSON.Parse(workspaceContent);
							
							if (workspaceData["folder"] != null)
							{
								var folderUri = workspaceData["folder"].Value;
								if (folderUri.StartsWith("file://"))
								{
									var folderPath = Uri.UnescapeDataString(folderUri.Substring(7));
									if (folderPath.Contains("Unity") || Directory.Exists(Path.Combine(folderPath, "Assets")))
									{
										return folderPath;
									}
								}
							}
						}
						catch (Exception ex)
						{
							Debug.LogWarning($"[Windsurf] Error reading workspace state file: {ex.Message}");
						}
					}
				}
			}
			else
			{
				Debug.LogWarning($"[Windsurf] Workspace storage directory not found: {windsurfStoragePath}");
			}

			return null;
		}

		private string GetProcessCommandLine(Process process)
		{
			try
			{
#if UNITY_EDITOR_WIN
				using (var searcher = new System.Management.ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}"))
				{
					foreach (System.Management.ManagementObject obj in searcher.Get())
					{
						return obj["CommandLine"]?.ToString();
					}
				}
#endif
			}
			catch (Exception ex)
			{
				Debug.LogError($"[Windsurf] Error getting command line: {ex.Message}");
			}

			return null;
		}
	}
} 