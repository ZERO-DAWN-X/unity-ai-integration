# Unity AI Integration

![Unity AI Integration](Documentation~/Images/hero.jpg)

> **A powerful Unity package that seamlessly integrates Cursor AI Editor and Windsurf (Codium) with Unity development workflow**

---

## Overview

Transform your Unity development experience with intelligent code editing, debugging, and project management through Cursor AI Editor integration.

### What This Package Does

- **Automatic Detection** - Finds Cursor and Windsurf installations across all platforms
- **Smart Project Generation** - Creates optimized `.csproj` and `.sln` files for IntelliSense  
- **Debugging Support** - Full Unity debugging capabilities within both editors
- **Workspace Configuration** - Automatically configures `.vscode` settings for optimal workflow
- **Multi-Editor Support** - Seamlessly switch between Cursor and Windsurf

---

## Quick Start

### Installation

1. Open Unity Editor
2. Navigate to **Window → Package Manager**
3. Click the **"+"** button (top-left corner)
4. Select **"Add package from git URL"**
5. Enter: `https://github.com/ZERO-DAWN-X/unity-ai-integration.git`
6. Click **Add**

### Setup

Once installed, the package automatically:
- Detects your Cursor and Windsurf installations
- Configures Unity to use your preferred AI editor as the default script editor
- Sets up project files for optimal IntelliSense support

---

## Features

![Feature Showcase](Documentation~/Images/Showcase.jpg)

### Core Integration
- **Cross-Platform Support** - Windows, macOS, and Linux compatibility
- **Multi-Editor Support** - Seamlessly works with both Cursor and Windsurf (Codium)
- **Version Detection** - Automatically identifies editor versions and configurations
- **Hot Reload** - Instant script compilation and error detection

### Developer Experience
- **Enhanced IntelliSense** - Full C# language support with Unity-specific completions
- **Integrated Debugging** - Attach debugger directly to Unity Editor
- **Clean Workspace** - Hides Unity meta files and unnecessary clutter
- **Extension Recommendations** - Suggests essential Unity development extensions
- **AI-Powered Development** - Leverage AI assistance in both Cursor and Windsurf

---

## Platform Support

### Cursor AI Editor
| Platform | Installation Path | Status |
|----------|------------------|---------|
| Windows | `%LOCALAPPDATA%\Programs\cursor\cursor.exe` | ✅ Supported |
| macOS | `/Applications/Cursor.app` | ✅ Supported |
| Linux | `/usr/bin/cursor` | ✅ Supported |

### Windsurf (Codium) Editor
| Platform | Installation Path | Status |
|----------|------------------|---------|
| Windows | `%LOCALAPPDATA%\Programs\Windsurf\Windsurf.exe` | ✅ Supported |
| macOS | `/Applications/Windsurf.app` | ✅ Supported |
| Linux | `/usr/bin/Windsurf` | ✅ Supported |

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
- **Supported Editors**: Cursor AI Editor or Windsurf (Codium) - any recent version
- **Dependencies**: `com.unity.test-framework@1.1.9`

---

## Compatibility Notes

### Version 2.0.24+ Important Update

### New Features in Version 2.0.25+

🚀 **Asset Management System**
- **Asset Analyzer** - Comprehensive project asset analysis and dependency tracking
- **Asset Optimizer** - Automatic texture, model, and audio optimization for better performance
- **Smart Compression** - Platform-specific optimization (mobile vs desktop)
- **Usage Detection** - Identifies unused assets and optimization opportunities

🔧 **Enhanced Developer Tools**
- **Intelligent Asset Processing** - Batch optimization with progress tracking
- **Memory Optimization** - Automatic texture size limiting and compression
- **Platform-Specific Settings** - Optimized configurations for Android/iOS builds
- **Audio Optimization** - Smart audio compression based on clip length and usage

---

## Troubleshooting

### Common Issues

**Editor not detected automatically?**
- Ensure Cursor or Windsurf is installed in standard directories
- Check that Unity has permission to access the installation path

**IntelliSense not working?**
- Verify that project files (.sln/.csproj) are generated
- Restart your editor after Unity project generation

**Debugging connection fails?**
- Confirm Unity Editor is running
- Check that Visual Studio Tools for Unity extension is installed in your editor

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

Built upon Unity's Visual Studio Tools integration framework, adapted for Cursor AI Editor and Windsurf compatibility.

### Original Sources

This package is based on the original work by:
- **boxqkrtm** at [com.unity.ide.cursor](https://github.com/boxqkrtm/com.unity.ide.cursor)
- **Asuta** at [com.unity.ide.windsurf](https://github.com/Asuta/com.unity.ide.windsurf)

**Special thanks to:**
- **boxqkrtm** - Original Unity Cursor integration implementation
- **Asuta** - Windsurf (Codium) integration implementation
- **Microsoft & Unity Technologies** - Visual Studio Tools for Unity framework
- **Cursor Team** - Cursor AI Editor development
- **Windsurf Team** - Windsurf AI Editor development

### Modifications & Enhancements

This repository (`unity-ai-integration`) includes:
- Enhanced README with professional documentation
- Visual assets and improved presentation
- Streamlined installation process
- Additional platform compatibility notes
- Comprehensive troubleshooting guides
- **Multi-Editor Support** - Unified support for both Cursor and Windsurf
- **Asset Management Tools** - Advanced asset analysis and optimization
- **Scene Management Tools** - Scene hierarchy analysis and optimization

---

**Repository Maintainer:** [ZERO-DAWN-X](https://github.com/ZERO-DAWN-X)  
**Original Sources:** 
- [boxqkrtm/com.unity.ide.cursor](https://github.com/boxqkrtm/com.unity.ide.cursor)
- [Asuta/com.unity.ide.windsurf](https://github.com/Asuta/com.unity.ide.windsurf)
