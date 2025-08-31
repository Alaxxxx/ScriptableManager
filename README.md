# ScriptableObject Manager

<p align="center">
  <a href="https://github.com/Alaxxxx/ScriptableManager/stargazers"><img src="https://img.shields.io/github/stars/Alaxxxx/ScriptableManager?style=flat-square&logo=github&color=FFC107" alt="GitHub Stars"></a>
  &nbsp;
  <a href="https://github.com/Alaxxxx?tab=followers"><img src="https://img.shields.io/github/followers/Alaxxxx?style=flat-square&logo=github&label=Followers&color=282c34" alt="GitHub Followers"></a>
  &nbsp;
  <a href="https://github.com/Alaxxxx/ScriptableManager/commits/main"><img src="https://img.shields.io/github/last-commit/Alaxxxx/ScriptableManager?style=flat-square&logo=github&color=blueviolet" alt="Last Commit"></a>
</p>
<p align="center">
  <a href="https://github.com/Alaxxxx/ScriptableManager/releases"><img src="https://img.shields.io/github/v/release/Alaxxxx/ScriptableManager?style=flat-square" alt="Release"></a>
  &nbsp;
  <a href="https://unity.com/"><img src="https://img.shields.io/badge/Unity-2021.3+-2296F3.svg?style=flat-square&logo=unity" alt="Unity Version"></a>
  &nbsp;
  <a href="https://github.com/Alaxxxx/ScriptableManager/blob/main/LICENSE"><img src="https://img.shields.io/github/license/Alaxxxx/ScriptableManager?style=flat-square" alt="License"></a>
</p>

A comprehensive Unity Editor tool for managing, organizing, and analyzing ScriptableObjects in your project with advanced features and an intuitive interface.


## ✨ Features

The tool is built around three core pillars: Organization, Workflow Integration, and Deep Analysis.

### 🗂️ Organization & Management
- **Advanced Search & Filtering**: Instantly find any asset by name or `ScriptableObject` type.
- **Favorites System**: Bookmark your most-used assets for one-click access.
- **Recently Modified**: A dedicated view to track and access recently changed assets.
- **Smart Sorting**: Flexible sorting options by name, type, or modification date.
- **Statistics Panel**: Get a high-level overview of your project's `ScriptableObject` usage and type distribution.

### ⚡ Workflow & Integration
- **Drag & Drop Support**: Drag assets directly from the manager to any Inspector field.
- **Multi-Selection**: Use `Ctrl` to select multiple assets for batch operations (delete, add/remove from favorites).
- **Rich Preview Panel**: See detailed asset information with thumbnails, metadata, and a preview of the object's data.
- **Customizable Settings**: Configure excluded paths to keep your workspace clean and relevant.

### 🔎 Analysis & Insights
- **Dependency Analysis**: A powerful two-way view to see what each asset **Uses** (dependencies) and what it is **Used By** (referencers).
- **Scene Reference Scanner**: Find every single `GameObject` in your scenes that holds a reference to a selected `ScriptableObject`.

<br>

## 📥 Installation

<details>
<summary><strong>1. Install via Git URL (Recommended)</strong></summary>
<br>

This method installs the package directly from GitHub and allows you to update it easily.

1. In Unity, open the **Package Manager** (`Window > Package Manager`).
2. Click the **+** button and select **"Add package from git URL..."**.
3. Enter the following URL and click "Add":
   ```
   https://github.com/Alaxxxx/ScriptableManager.git
   ```

</details>

<details>
<summary><strong>2. Install via .unitypackage</strong></summary>
<br>

Ideal if you prefer a specific, stable version of the asset.

1. Go to the [**Releases**](https://github.com/Alaxxxx/ScriptableManager/releases) page.
2. Download the `.unitypackage` file from the latest release.
3. In your Unity project, go to **`Assets > Import Package > Custom Package...`** and select the downloaded file.

</details>

<br>

## 🎮 Usage & Configuration

### Opening the Manager
You can open the main window via the Unity menu: `Tools > ScriptableObject Manager`.

### Interface Overview
The interface is designed for efficiency with three resizable panels: the category/filter list on the left, the asset list in the center, and the details/preview panel on the right.

<img width="1191" height="792" alt="Interface Overview" src="https://github.com/user-attachments/assets/ca5e8912-bcc8-4a69-95a2-d31609a0df3e" />

### Core Workflow

#### 🔍 Searching & Filtering
Quickly narrow down your assets using the powerful filtering tools at the top of the asset list.

- **Text Search**: Type in the search box to filter assets by name in real-time.
- **Type Filter**: Use the dropdown to show only assets of a specific `ScriptableObject` type.
- **View Toggles**: Instantly switch between showing **All** assets, only your **Favorites (⭐)**, or **Recently Modified** assets.

#### 🖱️ Working with Assets
Interacting with assets is fast and intuitive. Here is a quick reference guide:

| Action | Result |
| :--- | :--- |
| **Single Click** | Selects an asset and shows its details in the right panel. |
| **Double Click** | Pings the asset in the Project window, instantly locating it. |
| **Right Click** | Opens a context menu for powerful batch operations. |
| **Drag & Drop** | Drag one or more assets directly into Inspector fields or scenes. |
| **Star Icon (⭐)** | Toggles the favorite status for an asset. |
| **`Ctrl` + `Click`** | Select or deselect multiple assets for batch operations. |

### Advanced Analysis Tools

#### 🔗 Dependency & Reference Analysis
Select any `ScriptableObject` to see a complete breakdown of its relationships in the right-hand panel. This is essential for safe refactoring and understanding your project's architecture.

- **Uses (Dependencies)**: Lists all assets that the selected object depends on.
- **Used By (Referencers)**: Shows all other assets that reference the selected object.
- **Scene References**: Lists the specific `GameObjects` in your scenes that use this asset.

####  Scene Scanning
To find references within a specific scene, simply click the **magnifying glass icon (🔍)** next to a scene in the "Scene References" list. The tool will scan the scene and display the exact `GameObjects` using your asset.

<br>

### ⚙️ Configuration
Access the settings by clicking the **gear icon (⚙️)** in the top-right corner of the window.

Here you can configure paths to be excluded from the scan, which is useful for ignoring third-party assets or plugin folders.

**Default Excluded Paths:**
```csharp
Assets/Plugins/
Packages/
```

<br>

## 🤝 Contributing & Supporting

This project is open-source under the **MIT License**, and any form of contribution is welcome and greatly appreciated!

If `ScriptableObject Manager` helps you streamline your workflow, the best way to show support is by **giving it a star ⭐️ on GitHub!** Stars increase the project's visibility and are a great motivation.

Here are other ways you can get involved:

* **💡 Share Ideas & Report Bugs:** Have a great idea for a new feature or found a bug? [Open an issue](https://github.com/Alaxxxx/ScriptableManager/issues) to share the details.
* **🔌 Contribute Code:** Feel free to fork the repository and submit a pull request for bug fixes or new features.
* **🗣️ Spread the Word:** If you know other developers drowning in ScriptableObjects, let them know this tool exists!

Every contribution is incredibly valuable. Thank you for your support!
