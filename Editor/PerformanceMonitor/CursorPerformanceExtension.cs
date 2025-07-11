using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Microsoft.Unity.VisualStudio.Editor.PerformanceMonitor
{
    /// <summary>
    /// Cursor-specific performance extension that creates custom VS Code extensions and widgets
    /// Generates real-time performance overlays and commands for Cursor IDE
    /// </summary>
    public static class CursorPerformanceExtension
    {
        private const string EXTENSION_NAME = "unity-performance-monitor";
        private const string EXTENSION_VERSION = "1.0.0";
        
        [MenuItem("Window/Unity Cursor Integration/Install Performance Extension")]
        public static void InstallCursorExtension()
        {
            try
            {
                string projectPath = Application.dataPath.Replace("/Assets", "");
                string extensionPath = Path.Combine(projectPath, ".vscode", "extensions", EXTENSION_NAME);
                
                CreateExtensionStructure(extensionPath);
                CreateExtensionManifest(extensionPath);
                CreateExtensionScript(extensionPath);
                CreatePerformanceWebview(extensionPath);
                CreateStatusBarProvider(extensionPath);
                
                Debug.Log($"[Performance Monitor] Cursor extension installed successfully at: {extensionPath}");
                Debug.Log("[Performance Monitor] Restart Cursor to see the performance widgets!");
                
                EditorUtility.DisplayDialog("Performance Extension", 
                    "Cursor Performance Extension installed successfully!\n\n" +
                    "Features:\n" +
                    "• Real-time FPS counter in status bar\n" +
                    "• Performance alerts and notifications\n" +
                    "• Detailed performance panel\n" +
                    "• Custom commands for monitoring\n\n" +
                    "Please restart Cursor to activate the extension.", "OK");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Performance Monitor] Failed to install Cursor extension: {e.Message}");
            }
        }

        private static void CreateExtensionStructure(string extensionPath)
        {
            Directory.CreateDirectory(extensionPath);
            Directory.CreateDirectory(Path.Combine(extensionPath, "src"));
            Directory.CreateDirectory(Path.Combine(extensionPath, "media"));
            Directory.CreateDirectory(Path.Combine(extensionPath, "webview"));
        }

        private static void CreateExtensionManifest(string extensionPath)
        {
            var manifest = new
            {
                name = EXTENSION_NAME,
                displayName = "Unity Performance Monitor",
                description = "Real-time Unity performance monitoring for Cursor",
                version = EXTENSION_VERSION,
                publisher = "ZERO-DAWN-X",
                engines = new { vscode = "^1.60.0" },
                categories = new[] { "Other", "Debuggers" },
                activationEvents = new[] { "*" },
                main = "./src/extension.js",
                contributes = new
                {
                    commands = new[]
                    {
                        new
                        {
                            command = "unityPerformance.showPanel",
                            title = "Unity Performance: Show Performance Panel"
                        },
                        new
                        {
                            command = "unityPerformance.toggleMonitoring",
                            title = "Unity Performance: Toggle Monitoring"
                        },
                        new
                        {
                            command = "unityPerformance.clearAlerts",
                            title = "Unity Performance: Clear Performance Alerts"
                        }
                    },
                    statusBarItems = new[]
                    {
                        new
                        {
                            id = "unityPerformance.fps",
                            alignment = "right",
                            priority = 100
                        }
                    }
                },
                scripts = new
                {
                    vscode_prepublish = "npm run compile",
                    compile = "tsc -p ./",
                    watch = "tsc -watch -p ./"
                },
                devDependencies = new
                {
                    typescript = "^4.4.4",
                    vscode = "^1.60.0"
                }
            };

            string manifestJson = EditorJsonUtility.ToJson(manifest, true);
            File.WriteAllText(Path.Combine(extensionPath, "package.json"), manifestJson);
        }

        private static void CreateExtensionScript(string extensionPath)
        {
            string extensionScript = @"
const vscode = require('vscode');
const fs = require('fs');
const path = require('path');

let statusBarItem;
let performancePanel;
let isMonitoring = true;
let performanceData = null;

function activate(context) {
    console.log('Unity Performance Monitor extension activated');

    // Create status bar item for FPS
    statusBarItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Right, 100);
    statusBarItem.command = 'unityPerformance.showPanel';
    statusBarItem.show();
    context.subscriptions.push(statusBarItem);

    // Register commands
    let showPanelCommand = vscode.commands.registerCommand('unityPerformance.showPanel', showPerformancePanel);
    let toggleCommand = vscode.commands.registerCommand('unityPerformance.toggleMonitoring', toggleMonitoring);
    let clearAlertsCommand = vscode.commands.registerCommand('unityPerformance.clearAlerts', clearAlerts);

    context.subscriptions.push(showPanelCommand, toggleCommand, clearAlertsCommand);

    // Start monitoring Unity performance files
    startPerformanceMonitoring();

    // Update status bar every 500ms
    setInterval(updateStatusBar, 500);
}

function startPerformanceMonitoring() {
    const workspaceFolder = vscode.workspace.workspaceFolders?.[0];
    if (!workspaceFolder) return;

    const performanceFile = path.join(workspaceFolder.uri.fsPath, '.vscode', 'unity-performance.json');
    const alertsFile = path.join(workspaceFolder.uri.fsPath, '.vscode', 'unity-performance-alerts.json');

    // Watch for performance updates
    const watcher = vscode.workspace.createFileSystemWatcher(performanceFile);
    watcher.onDidChange(() => {
        readPerformanceData(performanceFile);
    });

    // Watch for alerts
    const alertWatcher = vscode.workspace.createFileSystemWatcher(alertsFile);
    alertWatcher.onDidChange(() => {
        readAlerts(alertsFile);
    });

    // Initial read
    readPerformanceData(performanceFile);
}

function readPerformanceData(filePath) {
    try {
        if (fs.existsSync(filePath)) {
            const data = fs.readFileSync(filePath, 'utf8');
            performanceData = JSON.parse(data);
        }
    } catch (error) {
        console.log('Error reading performance data:', error);
    }
}

function readAlerts(filePath) {
    try {
        if (fs.existsSync(filePath)) {
            const data = fs.readFileSync(filePath, 'utf8');
            const alerts = JSON.parse(data);
            showPerformanceAlerts(alerts);
        }
    } catch (error) {
        console.log('Error reading alerts:', error);
    }
}

function updateStatusBar() {
    if (!isMonitoring || !performanceData) {
        statusBarItem.text = '$(pulse) Unity: Idle';
        statusBarItem.tooltip = 'Unity Performance Monitor - Click to open panel';
        return;
    }

    const fps = performanceData.data?.fps || 0;
    const memory = performanceData.data?.memory?.used || 'N/A';
    const isPlaying = performanceData.data?.system?.isPlaying || false;

    if (isPlaying) {
        const fpsColor = fps >= 55 ? '$(check)' : fps >= 30 ? '$(warning)' : '$(error)';
        statusBarItem.text = `${fpsColor} Unity: ${fps.toFixed(1)} FPS | ${memory}`;
        statusBarItem.tooltip = `Unity Performance Monitor
FPS: ${fps.toFixed(1)}
Memory: ${memory}
Status: Playing
Click to open detailed panel`;
    } else {
        statusBarItem.text = '$(debug-pause) Unity: Editor';
        statusBarItem.tooltip = 'Unity Performance Monitor - Editor Mode';
    }
}

function showPerformancePanel() {
    if (performancePanel) {
        performancePanel.reveal();
        return;
    }

    performancePanel = vscode.window.createWebviewPanel(
        'unityPerformance',
        'Unity Performance Monitor',
        vscode.ViewColumn.Beside,
        {
            enableScripts: true,
            retainContextWhenHidden: true
        }
    );

    performancePanel.webview.html = getWebviewContent();

    performancePanel.onDidDispose(() => {
        performancePanel = undefined;
    });

    // Send updates to webview
    const updateInterval = setInterval(() => {
        if (performancePanel && performanceData) {
            performancePanel.webview.postMessage({
                type: 'update',
                data: performanceData
            });
        }
    }, 500);

    performancePanel.onDidDispose(() => {
        clearInterval(updateInterval);
    });
}

function showPerformanceAlerts(alerts) {
    if (alerts.summary?.criticalCount > 0) {
        vscode.window.showErrorMessage(
            `Unity Performance: ${alerts.summary.criticalCount} critical performance issues detected!`,
            'Show Details'
        ).then(selection => {
            if (selection === 'Show Details') {
                showPerformancePanel();
            }
        });
    } else if (alerts.summary?.warningCount > 0) {
        vscode.window.showWarningMessage(
            `Unity Performance: ${alerts.summary.warningCount} performance warnings detected.`,
            'Show Details'
        ).then(selection => {
            if (selection === 'Show Details') {
                showPerformancePanel();
            }
        });
    }
}

function toggleMonitoring() {
    isMonitoring = !isMonitoring;
    vscode.window.showInformationMessage(
        `Unity Performance Monitoring ${isMonitoring ? 'enabled' : 'disabled'}`
    );
}

function clearAlerts() {
    vscode.window.showInformationMessage('Performance alerts cleared');
}

function getWebviewContent() {
    return `<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Unity Performance Monitor</title>
    <style>
        body { font-family: var(--vscode-font-family); background: var(--vscode-editor-background); color: var(--vscode-editor-foreground); padding: 20px; }
        .metric-card { background: var(--vscode-editorWidget-background); border: 1px solid var(--vscode-editorWidget-border); border-radius: 4px; padding: 15px; margin: 10px 0; }
        .metric-title { font-size: 14px; font-weight: bold; margin-bottom: 8px; }
        .metric-value { font-size: 24px; font-weight: bold; }
        .fps-good { color: #4CAF50; }
        .fps-warning { color: #FF9800; }
        .fps-critical { color: #F44336; }
        .grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 15px; }
        .status-bar { background: var(--vscode-statusBar-background); color: var(--vscode-statusBar-foreground); padding: 10px; margin: -20px -20px 20px -20px; font-weight: bold; }
    </style>
</head>
<body>
    <div class=""status-bar"" id=""statusBar"">Unity Performance Monitor - Waiting for data...</div>
    
    <div class=""grid"">
        <div class=""metric-card"">
            <div class=""metric-title"">Frame Rate</div>
            <div class=""metric-value"" id=""fps"">-- FPS</div>
            <div>Frame Time: <span id=""frameTime"">-- ms</span></div>
        </div>
        
        <div class=""metric-card"">
            <div class=""metric-title"">Memory Usage</div>
            <div class=""metric-value"" id=""memory"">-- MB</div>
            <div>GC: <span id=""gcMemory"">-- MB</span></div>
        </div>
        
        <div class=""metric-card"">
            <div class=""metric-title"">Rendering</div>
            <div class=""metric-value"" id=""drawCalls"">-- Calls</div>
            <div>Triangles: <span id=""triangles"">--</span></div>
        </div>
        
        <div class=""metric-card"">
            <div class=""metric-title"">System Load</div>
            <div class=""metric-value"" id=""cpuUsage"">--%</div>
            <div>GPU: <span id=""gpuUsage"">--%</span></div>
        </div>
    </div>
    
    <script>
        const vscode = acquireVsCodeApi();
        
        window.addEventListener('message', event => {
            const message = event.data;
            if (message.type === 'update') {
                updateMetrics(message.data);
            }
        });
        
        function updateMetrics(data) {
            const perfData = data.data;
            
            if (perfData.system?.isPlaying) {
                document.getElementById('statusBar').textContent = 'Unity Performance Monitor - Game Running';
                
                const fps = perfData.fps || 0;
                const fpsElement = document.getElementById('fps');
                fpsElement.textContent = fps.toFixed(1) + ' FPS';
                fpsElement.className = 'metric-value ' + (fps >= 55 ? 'fps-good' : fps >= 30 ? 'fps-warning' : 'fps-critical');
                
                document.getElementById('frameTime').textContent = (perfData.frameTime || 0).toFixed(2) + ' ms';
                document.getElementById('memory').textContent = perfData.memory?.used || 'N/A';
                document.getElementById('gcMemory').textContent = perfData.memory?.gc || 'N/A';
                document.getElementById('drawCalls').textContent = (perfData.rendering?.drawCalls || 0).toLocaleString();
                document.getElementById('triangles').textContent = (perfData.rendering?.triangles || 0).toLocaleString();
                document.getElementById('cpuUsage').textContent = (perfData.system?.cpuUsage || 0).toFixed(1) + '%';
                document.getElementById('gpuUsage').textContent = (perfData.system?.gpuUsage || 0).toFixed(1) + '%';
            } else {
                document.getElementById('statusBar').textContent = 'Unity Performance Monitor - Editor Mode';
                document.getElementById('fps').textContent = 'Editor Mode';
                document.getElementById('fps').className = 'metric-value';
            }
        }
    </script>
</body>
</html>`;
}

function deactivate() {}

module.exports = {
    activate,
    deactivate
};
";

            File.WriteAllText(Path.Combine(extensionPath, "src", "extension.js"), extensionScript);
        }

        private static void CreatePerformanceWebview(string extensionPath)
        {
            // Create a separate HTML file for the performance webview
            string webviewHtml = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Unity Performance Dashboard</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { 
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #1e1e1e 0%, #2d2d30 100%);
            color: #ffffff;
            height: 100vh;
            overflow-x: hidden;
        }
        .dashboard { padding: 20px; }
        .header { text-align: center; margin-bottom: 30px; }
        .title { font-size: 28px; font-weight: 300; margin-bottom: 10px; }
        .subtitle { font-size: 14px; opacity: 0.7; }
        .metrics-grid { 
            display: grid; 
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); 
            gap: 20px; 
            margin-bottom: 30px;
        }
        .metric-card { 
            background: rgba(255, 255, 255, 0.05);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 12px;
            padding: 20px;
            backdrop-filter: blur(10px);
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }
        .metric-card:hover { 
            transform: translateY(-2px);
            box-shadow: 0 8px 25px rgba(0, 0, 0, 0.3);
        }
        .metric-header { 
            display: flex; 
            align-items: center; 
            margin-bottom: 15px;
        }
        .metric-icon { 
            width: 24px; 
            height: 24px; 
            margin-right: 10px;
            opacity: 0.8;
        }
        .metric-title { 
            font-size: 14px; 
            font-weight: 600; 
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }
        .metric-value { 
            font-size: 32px; 
            font-weight: 700; 
            margin-bottom: 8px;
            background: linear-gradient(45deg, #00d4ff, #00ff88);
            -webkit-background-clip: text;
            -webkit-text-fill-color: transparent;
        }
        .metric-subtitle { 
            font-size: 12px; 
            opacity: 0.6;
        }
        .chart-container { 
            background: rgba(255, 255, 255, 0.05);
            border: 1px solid rgba(255, 255, 255, 0.1);
            border-radius: 12px;
            padding: 20px;
            margin-bottom: 20px;
        }
        .fps-excellent { color: #4CAF50; }
        .fps-good { color: #8BC34A; }
        .fps-warning { color: #FF9800; }
        .fps-critical { color: #F44336; }
        .status-indicator { 
            display: inline-block;
            width: 8px;
            height: 8px;
            border-radius: 50%;
            margin-right: 8px;
        }
        .status-playing { background: #4CAF50; }
        .status-paused { background: #FF9800; }
        .status-stopped { background: #9E9E9E; }
    </style>
</head>
<body>
    <div class=""dashboard"">
        <div class=""header"">
            <h1 class=""title"">Unity Performance Dashboard</h1>
            <p class=""subtitle"" id=""statusText"">Real-time performance monitoring for Unity development</p>
        </div>
        
        <div class=""metrics-grid"">
            <div class=""metric-card"">
                <div class=""metric-header"">
                    <span class=""metric-icon"">⚡</span>
                    <span class=""metric-title"">Frame Rate</span>
                </div>
                <div class=""metric-value"" id=""fpsValue"">--</div>
                <div class=""metric-subtitle"">FPS | <span id=""frameTimeValue"">-- ms</span> frame time</div>
            </div>
            
            <div class=""metric-card"">
                <div class=""metric-header"">
                    <span class=""metric-icon"">💾</span>
                    <span class=""metric-title"">Memory</span>
                </div>
                <div class=""metric-value"" id=""memoryValue"">--</div>
                <div class=""metric-subtitle"">Used | <span id=""gcMemoryValue"">--</span> GC allocated</div>
            </div>
            
            <div class=""metric-card"">
                <div class=""metric-header"">
                    <span class=""metric-icon"">🎨</span>
                    <span class=""metric-title"">Rendering</span>
                </div>
                <div class=""metric-value"" id=""drawCallsValue"">--</div>
                <div class=""metric-subtitle"">Draw calls | <span id=""trianglesValue"">--</span> triangles</div>
            </div>
            
            <div class=""metric-card"">
                <div class=""metric-header"">
                    <span class=""metric-icon"">⚙️</span>
                    <span class=""metric-title"">System</span>
                </div>
                <div class=""metric-value"" id=""cpuValue"">--%</div>
                <div class=""metric-subtitle"">CPU | <span id=""gpuValue"">--%</span> GPU estimated</div>
            </div>
        </div>
    </div>
    
    <script>
        // This would be enhanced with actual charting libraries in a real implementation
        console.log('Unity Performance Dashboard loaded');
    </script>
</body>
</html>";

            File.WriteAllText(Path.Combine(extensionPath, "webview", "dashboard.html"), webviewHtml);
        }

        private static void CreateStatusBarProvider(string extensionPath)
        {
            // Create TypeScript definition file
            string tsConfig = @"{
  ""compilerOptions"": {
    ""module"": ""commonjs"",
    ""target"": ""es6"",
    ""outDir"": ""out"",
    ""lib"": [""es6""],
    ""sourceMap"": true,
    ""rootDir"": ""src"",
    ""strict"": true
  },
  ""exclude"": [""node_modules"", "".vscode-test""]
}";

            File.WriteAllText(Path.Combine(extensionPath, "tsconfig.json"), tsConfig);
        }

        [MenuItem("Window/Unity Cursor Integration/Update Performance Settings")]
        public static void ShowPerformanceSettings()
        {
            var window = EditorWindow.CreateInstance<PerformanceSettingsWindow>();
            window.titleContent = new GUIContent("Performance Settings");
            window.ShowUtility();
        }
    }

    /// <summary>
    /// Settings window for configuring performance monitoring thresholds
    /// </summary>
    public class PerformanceSettingsWindow : EditorWindow
    {
        private PerformanceNotificationSystem.PerformanceThresholds thresholds;

        private void OnEnable()
        {
            thresholds = PerformanceNotificationSystem.GetThresholds();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Performance Monitor Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Performance Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");

            thresholds.minFPS = EditorGUILayout.FloatField("Minimum FPS", thresholds.minFPS);
            thresholds.maxFrameTime = EditorGUILayout.FloatField("Max Frame Time (ms)", thresholds.maxFrameTime);
            thresholds.maxMemoryUsage = EditorGUILayout.LongField("Max Memory Usage (bytes)", thresholds.maxMemoryUsage);
            thresholds.maxDrawCalls = EditorGUILayout.IntField("Max Draw Calls", thresholds.maxDrawCalls);
            thresholds.maxCPUUsage = EditorGUILayout.FloatField("Max CPU Usage (%)", thresholds.maxCPUUsage);
            thresholds.maxGPUUsage = EditorGUILayout.FloatField("Max GPU Usage (%)", thresholds.maxGPUUsage);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Settings"))
            {
                PerformanceNotificationSystem.SetThresholds(thresholds);
                EditorUtility.DisplayDialog("Settings Saved", "Performance monitoring thresholds have been updated.", "OK");
            }

            if (GUILayout.Button("Reset to Defaults"))
            {
                thresholds = new PerformanceNotificationSystem.PerformanceThresholds();
                PerformanceNotificationSystem.SetThresholds(thresholds);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);
            
            var currentMetrics = PerformanceMonitor.GetCurrentMetrics();
            if (currentMetrics != null)
            {
                EditorGUILayout.LabelField($"FPS: {currentMetrics.fps:F1}");
                EditorGUILayout.LabelField($"Memory: {FormatBytes(currentMetrics.usedMemory)}");
                EditorGUILayout.LabelField($"Draw Calls: {currentMetrics.drawCalls}");
            }
            else
            {
                EditorGUILayout.LabelField("No performance data available");
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }
} 