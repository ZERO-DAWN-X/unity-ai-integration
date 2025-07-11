# Unity Cursor Integration - Performance Monitor Testing Guide

This guide will walk you through testing the new **Real-time Performance Monitor** feature.

## 🚀 Quick Start Testing

### Step 1: Install the Package in Unity

1. **Open Unity Editor** (2019.4 or later)
2. **Open Package Manager**: `Window → Package Manager`
3. **Add from Git URL**: Click the "+" button → "Add package from git URL"
4. **Enter URL**: `https://github.com/ZERO-DAWN-X/unity-ai-integration.git`
5. **Install**: Click "Add" and wait for installation to complete

### Step 2: Verify Installation

1. **Check Menu Items**: Look for new menu items under:
   - `Window → Unity Cursor Integration → Performance Monitor`
   - `Window → Unity Cursor Integration → Install Performance Extension`
   - `Window → Unity Cursor Integration → Update Performance Settings`

2. **Open Performance Monitor**: 
   - Go to `Window → Unity Cursor Integration → Performance Monitor`
   - A new window should open showing the performance dashboard

## 🎮 Testing the Performance Monitor

### Test 1: Basic Performance Monitoring

1. **Open Performance Monitor Window**:
   ```
   Window → Unity Cursor Integration → Performance Monitor
   ```

2. **Verify Dashboard Elements**:
   - ✅ Current Performance section
   - ✅ Enable Monitoring toggle
   - ✅ Update Interval slider
   - ✅ Detailed View toggle

3. **Enter Play Mode**:
   - Press the Unity Play button
   - **Expected Result**: Performance Monitor should show:
     - Real FPS values
     - Frame time in milliseconds
     - Memory usage statistics
     - Draw calls and rendering stats

### Test 2: Performance Alerts System

1. **Open Performance Settings**:
   ```
   Window → Unity Cursor Integration → Update Performance Settings
   ```

2. **Set Low Thresholds** (for testing):
   - Minimum FPS: `45` (instead of 30)
   - Max Frame Time: `20` (instead of 33.33)
   - Max Draw Calls: `50` (instead of 1000)

3. **Create Performance Load**:
   - Add multiple GameObjects to your scene
   - Add lights, particle systems, or complex meshes
   - Enter Play Mode

4. **Expected Results**:
   - ✅ Performance alerts should appear in Unity Console
   - ✅ Warning/Error messages with optimization suggestions
   - ✅ Alert cooldown system (alerts every 5 seconds max)

### Test 3: Cursor Extension Installation

1. **Install Cursor Extension**:
   ```
   Window → Unity Cursor Integration → Install Performance Extension
   ```

2. **Verify Installation**:
   - ✅ Success dialog should appear
   - ✅ Extension files created in `.vscode/extensions/unity-performance-monitor/`
   - ✅ Configuration files created in `.vscode/` directory

3. **Check Generated Files**:
   ```
   YourProject/.vscode/
   ├── unity-performance-config.json
   ├── PERFORMANCE_MONITOR_README.md
   ├── unity-performance.json (auto-generated during play)
   └── extensions/unity-performance-monitor/
       ├── package.json
       ├── src/extension.js
       └── webview/dashboard.html
   ```

## 🔍 Testing in Cursor IDE

### Test 4: Cursor Integration

1. **Open Project in Cursor**:
   - Open your Unity project folder in Cursor
   - Restart Cursor to load the extension

2. **Verify Status Bar**:
   - ✅ Should see "Unity: Idle" or "Unity: Editor" in status bar
   - ✅ Click status bar item should show "Show Performance Panel" option

3. **Test Real-time Monitoring**:
   - Switch to Unity and enter Play Mode
   - Watch Cursor status bar update with:
     - ✅ FPS counter with colored indicators
     - ✅ Memory usage display
     - ✅ Click to open detailed panel

### Test 5: Performance Data Files

1. **Enter Play Mode in Unity**

2. **Check Generated Files**:
   ```bash
   # Check if performance data is being written
   cat .vscode/unity-performance.json
   ```

3. **Expected JSON Structure**:
   ```json
   {
     "type": "unity_performance",
     "timestamp": "2024-01-15 10:30:45.123",
     "data": {
       "fps": 60.0,
       "frameTime": 16.67,
       "memory": {
         "total": "512 MB",
         "used": "256 MB",
         "gc": "128 MB"
       },
       "rendering": {
         "drawCalls": 45,
         "triangles": 12500,
         "vertices": 8750
       }
     }
   }
   ```

## 🧪 Advanced Testing Scenarios

### Test 6: Performance Stress Testing

1. **Create Performance Test Scene**:
   ```csharp
   // Create a script to stress test performance
   public class PerformanceStressTester : MonoBehaviour
   {
       public GameObject prefab;
       public int objectCount = 1000;
       
       void Start()
       {
           for (int i = 0; i < objectCount; i++)
           {
               Instantiate(prefab, Random.insideUnitSphere * 10, Quaternion.identity);
           }
       }
   }
   ```

2. **Expected Results**:
   - ✅ FPS should drop significantly
   - ✅ Performance alerts should trigger
   - ✅ Suggestions should appear in console
   - ✅ Cursor status bar should show red indicators

### Test 7: Memory Usage Testing

1. **Create Memory Stress Test**:
   ```csharp
   public class MemoryStressTester : MonoBehaviour
   {
       void Update()
       {
           // Allocate memory each frame (bad practice - for testing only)
           var largeArray = new byte[1024 * 1024]; // 1MB allocation
       }
   }
   ```

2. **Expected Results**:
   - ✅ Memory usage should increase rapidly
   - ✅ GC memory alerts should trigger
   - ✅ Memory optimization suggestions in console

## 🔧 Troubleshooting Tests

### Common Issues and Solutions

**Issue**: Performance Monitor window shows "No performance data available"
- **Solution**: Enter Play Mode in Unity
- **Test**: Verify monitoring is enabled in settings

**Issue**: Cursor extension not loading
- **Solution**: Restart Cursor after installation
- **Test**: Check `.vscode/extensions/` directory exists

**Issue**: No performance files generated
- **Solution**: Check Unity Console for errors
- **Test**: Verify `.vscode` directory has write permissions

**Issue**: Alerts not showing
- **Solution**: Lower thresholds in Performance Settings
- **Test**: Create artificial performance bottlenecks

## ✅ Testing Checklist

Mark each item as you test:

### Basic Functionality
- [ ] Package installs successfully from Git URL
- [ ] Performance Monitor window opens
- [ ] Performance data displays during Play Mode
- [ ] Settings window allows threshold configuration

### Performance Monitoring
- [ ] FPS tracking works accurately
- [ ] Memory usage updates in real-time
- [ ] Draw calls and rendering stats display
- [ ] Color-coded performance indicators work

### Alert System
- [ ] Performance alerts trigger correctly
- [ ] Optimization suggestions appear in console
- [ ] Alert cooldown prevents spam
- [ ] Different severity levels (warning/critical) work

### Cursor Integration
- [ ] Extension installs without errors
- [ ] Status bar shows Unity performance data
- [ ] Performance panel opens from status bar
- [ ] Real-time updates work in Cursor

### File Generation
- [ ] `.vscode` configuration files created
- [ ] Performance JSON files generate during Play
- [ ] Extension files properly structured
- [ ] README and config files present

## 📊 Expected Performance Metrics

### Typical Values for Testing:
- **Good Performance**: 55+ FPS, <20ms frame time
- **Acceptable**: 30-55 FPS, 20-33ms frame time  
- **Poor Performance**: <30 FPS, >33ms frame time
- **Memory**: Varies by project, watch for rapid increases
- **Draw Calls**: <1000 for mobile, <5000 for desktop

## 🎯 Success Criteria

The Performance Monitor feature is working correctly if:

1. ✅ **Real-time monitoring** displays accurate FPS, memory, and rendering data
2. ✅ **Smart alerts** trigger with helpful optimization suggestions
3. ✅ **Cursor integration** shows performance data in status bar and panels
4. ✅ **Configuration system** allows customizing thresholds
5. ✅ **File generation** creates all necessary `.vscode` files automatically

---

**Need Help?** Check the Unity Console for detailed error messages and refer to the generated `PERFORMANCE_MONITOR_README.md` in your `.vscode` directory.

**Found a Bug?** Please report issues with:
- Unity version
- Cursor version
- Error messages from Unity Console
- Generated file contents 