# Unity Cursor Integration

![Unity Cursor Integration](Documentation~/Images/hero.jpg)

> **A powerful Unity package that seamlessly integrates Cursor AI Editor with Unity development workflow**

---

## Overview

Transform your Unity development experience with intelligent code editing, debugging, and project management through Cursor AI Editor integration.

### What This Package Does

- **Automatic Detection** - Finds Cursor installations across all platforms
- **Smart Project Generation** - Creates optimized `.csproj` and `.sln` files for IntelliSense  
- **Debugging Support** - Full Unity debugging capabilities within Cursor
- **Workspace Configuration** - Automatically configures `.vscode` settings for optimal workflow

---

## Quick Start

### Installation

1. Open Unity Editor
2. Navigate to **Window → Package Manager**
3. Click the **"+"** button (top-left corner)
4. Select **"Add package from git URL"**
5. Enter: `https://github.com/boxqkrtm/com.unity.ide.cursor.git`
6. Click **Add**

### Setup

Once installed, the package automatically:
- Detects your Cursor installation
- Configures Unity to use Cursor as the default script editor
- Sets up project files for optimal IntelliSense support

---

## Features

![Feature Showcase](Documentation~/Images/Showcase.jpg)

### Core Integration
- **Cross-Platform Support** - Windows, macOS, and Linux compatibility
- **Version Detection** - Automatically identifies Cursor version and configuration
- **Hot Reload** - Instant script compilation and error detection

### Developer Experience
- **Enhanced IntelliSense** - Full C# language support with Unity-specific completions
- **Integrated Debugging** - Attach debugger directly to Unity Editor
- **Clean Workspace** - Hides Unity meta files and unnecessary clutter
- **Extension Recommendations** - Suggests essential Unity development extensions

---

## Platform Support

| Platform | Installation Path | Status |
|----------|------------------|---------|
| Windows | `%LOCALAPPDATA%\Programs\cursor\cursor.exe` | ✅ Supported |
| macOS | `/Applications/Cursor.app` | ✅ Supported |
| Linux | `/usr/bin/cursor` | ✅ Supported |

---

## Configuration

The package automatically creates and manages:

```
YourProject/
├── .vscode/
│   ├── launch.json      # Debugger configuration
│   ├── settings.json    # Workspace preferences  
│   └── extensions.json  # Recommended extensions
└── *.sln *.csproj      # Project files for IntelliSense
```

---

## Requirements

- **Unity Version**: 2019.4 or later
- **Cursor Editor**: Any recent version
- **Dependencies**: `com.unity.test-framework@1.1.9`

---

## Compatibility Notes

### Version 2.0.24+ Important Update

The package name changed from `com.unity.ide.cursor` to `com.boxqkrtm.ide.cursor` to comply with Unity's attribution guidelines.

**If updating from older versions:**
1. Remove the existing package first
2. Clear Package Manager cache
3. Install the new version to avoid conflicts

---

## Troubleshooting

### Common Issues

**Cursor not detected automatically?**
- Ensure Cursor is installed in standard directories
- Check that Unity has permission to access the installation path

**IntelliSense not working?**
- Verify that project files (.sln/.csproj) are generated
- Restart Cursor after Unity project generation

**Debugging connection fails?**
- Confirm Unity Editor is running
- Check that Visual Studio Tools for Unity extension is installed in Cursor

---

## Contributing

We welcome contributions! Please feel free to:
- Report bugs and issues
- Suggest new features
- Submit pull requests
- Improve documentation

---

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

---

## Acknowledgments

Built upon Unity's Visual Studio Tools integration framework, adapted for Cursor AI Editor compatibility.
