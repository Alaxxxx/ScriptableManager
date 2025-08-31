# ScriptableObject Manager

A comprehensive Unity Editor tool for managing, organizing, and analyzing ScriptableObjects in your project with advanced features and an intuitive interface.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
[![Release](https://img.shields.io/github/v/release/Alaxxxx/ScriptableManager?style=flat-square)](https://github.com/Alaxxxx/ScriptableManager/releases)
[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-green.svg)](https://unity3d.com/get-unity/download)
[![GitHub last commit](https://img.shields.io/github/last-commit/Alaxxxx/ScriptableManager)](https://github.com/Alaxxxx/ScriptableManager/commits/main)


## 🚀 Features

### Core Features

- **🔍 Advanced Search & Filtering**: Search by name or ScriptableObject type
- **⭐ Favorites System**: Mark frequently used ScriptableObjects as favorites
- **🕒 Recently Modified**: Track and quickly access recently changed assets
- **📊 Smart Sorting**: Sort by name, type, date (newest/oldest) with dropdown options
- **🎯 Drag & Drop Support**: Drag ScriptableObjects directly to Inspector fields
- **📁 Multi-Selection**: Select multiple assets with Ctrl/Shift for batch operations
- **🔗 Dependency Analysis**: View what each ScriptableObject uses and what uses it
- **🎮 Scene Reference Scanner**: Find GameObjects in scenes that reference your assets
- **📱 Three-Panel Interface**: Organized layout with resizable panels
- **⚙️ Customizable Settings**: Configure excluded paths
- **📈 Statistics Panel**: Overview of your ScriptableObject usage and distribution
- **🎨 Rich Preview**: Detailed asset information with thumbnails and metadata

## 📥 Installation

<details>
<summary><strong>1. Install via Git URL (Recommended)</strong></summary>
<br>

This method installs the package directly from GitHub and allows you to update it easily.

1. In Unity, open the **Package Manager** (`Window > Package Manager`).
2. Click the **+** button and select **"Add package from git URL..."**.
3. Enter the following URL and click "Add":
   ```
   https://github.com/Alaxxxx/SriptableManager.git
   ```

</details>

<details>
<summary><strong>2. Install via .unitypackage</strong></summary>
<br>

Ideal if you prefer a specific, stable version of the asset.

1. Go to the [**Releases**](https://github.com/Alaxxxx/SriptableManager/releases) page.
2. Download the `.unitypackage` file from the latest release.
3. In your Unity project, go to **`Assets > Import Package > Custom Package...`** and select the downloaded file.

</details>

## 🎮 Usage

### Opening the Tool

- **Menu**: `Tools → → ScriptableObject Manager`

### Interface Overview

<img width="1191" height="792" alt="Interface Overview" src="https://github.com/user-attachments/assets/ca5e8912-bcc8-4a69-95a2-d31609a0df3e" />

### Basic Operations

#### Searching & Filtering

- **Text Search**: Type in the search box to find assets by name
- **Type Filter**: Use the dropdown to filter by specific ScriptableObject types
- **Favorites Only**: Toggle to show only starred assets
- **Recent Filter**: Automatically shows assets modified in the last 24 hours

#### Working with Assets

- **Single Click**: Select an asset
- **Double Click**: Ping the asset in Project window
- **Right Click**: Open context menu with batch operations
- **Drag**: Drag assets to Inspector fields or scenes
- **Star Icon**: Toggle favorite status

#### Multi-Selection

- **Ctrl + Click**: Add/remove from selection

### Advanced Features

#### Dependency Analysis

The right panel shows comprehensive dependency information:

- **Uses (Dependencies)**: Assets this ScriptableObject depends on
- **Used By (Referencers)**: Assets that reference this ScriptableObject
- **Scene References**: GameObjects in scenes that use this asset

#### Scene Scanning

- Click the 🔍 button next to scene assets to scan for references
- View detailed results showing which GameObjects reference your ScriptableObject
- Quick navigation to referenced objects

#### Batch Operations

Select multiple assets to perform batch operations with right click:

- **Add to Favorites**: Star multiple assets at once
- **Remove from Favorites**: Unstar selected assets
- **Delete Assets**: Move multiple assets to trash

## ⚙️ Configuration

### Settings Panel

Access via the gear icon (⚙️) in the toolbar:

#### Excluded Paths

Default excluded paths:

```
Assets/Plugins/
Packages/
```

Add folder paths to exclude from scanning.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
