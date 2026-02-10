# LinkRouter

[![GitHub release](https://img.shields.io/github/v/release/AskerFED/link-router)](https://github.com/AskerFED/link-router/releases)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![Windows](https://img.shields.io/badge/platform-Windows%2010%2F11-blue)](https://github.com/AskerFED/link-router)

A Windows desktop application that intelligently routes URLs to the correct browser and profile. When you click any link from Teams, Outlook, Slack, or any other app, LinkRouter intercepts it and opens it in the right browser based on your configured rules.

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [How It Works](#how-it-works)
- [Usage Guide](#usage-guide)
  - [Individual Rules](#individual-rules)
  - [URL Groups](#url-groups)
  - [Multi-Profile Rules](#multi-profile-rules)
  - [Moving Patterns](#moving-patterns)
- [Configuration](#configuration)
- [Command-Line Options](#command-line-options)
- [Troubleshooting](#troubleshooting)
- [Browser Support](#browser-support)
- [Contributing](#contributing)
- [License](#license)

---

## Features

- **Intelligent URL Routing** - Route URLs to specific browsers/profiles based on domain patterns
- **Multi-Browser Support** - Works with Chrome, Edge, Firefox, Brave, Opera, and Opera GX
- **Profile Management** - Detects and uses all your browser profiles (personal, work, dev)
- **Multi-Profile Rules** - Single rules can offer multiple browser/profile choices via picker
- **URL Groups** - Group related URLs (e.g., all Microsoft 365 domains) with shared settings
- **Built-in Groups** - Pre-configured groups for Microsoft 365 and Google Suite (disabled by default)
- **Move Patterns** - Easily move URL patterns between groups and individual rules
- **Pattern Validation** - Real-time validation with conflict detection and warnings
- **Default Browser Fallback** - Set a fallback browser when no rules match
- **Toast Notifications** - Quick rule creation from notification when no rule matches
- **Clipboard Monitoring** - Monitor clipboard for URLs and automatically route them
- **System Tray Integration** - Minimize to system tray with quick access menu
- **Single Instance** - Prevents multiple instances from running simultaneously
- **Windows Integration** - Registers as a Windows browser handler for http/https protocols
- **Import/Export** - Full backup and restore functionality
- **Atomic Saves** - Safe file operations with automatic rolling backups

---

## Installation

### Requirements

- Windows 10 (1607+) or Windows 11
- 64-bit (x64) architecture

### Option 1: Installer (Recommended)

1. Download `LinkRouterSetup-1.0.0.exe` from Releases
2. Run the installer and follow the prompts
3. Application installs to `C:\Program Files\LinkRouter\`

### Option 2: Build from Source

```bash
git clone https://github.com/AskerFED/link-router.git
cd LinkRouter
dotnet publish BrowserSelector.csproj -c Release -r win-x64 --self-contained
```

Output: `bin\Release\net8.0-windows\win-x64\publish\LinkRouter.exe`

### Option 3: Portable

Copy `LinkRouter.exe` anywhere and run it. Data is stored in `%APPDATA%\LinkRouter\`.

---

## Quick Start

1. **Launch LinkRouter** - Settings window opens on first run
2. **Register as Browser** - Click "Open Windows Settings" in the Settings tab
3. **Set as Default** - In Windows Settings, set LinkRouter as default for HTTP/HTTPS
4. **Configure Default Browser** - Select your fallback browser on the Home tab
5. **Create Rules** - Add rules for domains you want to route to specific browsers

**Quick Rule Creation**: When you click a URL with no matching rule, it opens in your default browser and shows a toast notification. Click the notification to create a rule for that domain.

---

## How It Works

```
URL clicked (Teams, Outlook, Slack, etc.)
        |
        v
LinkRouter intercepts as default browser
        |
        v
+------------------------------------------+
| Priority 1: URL Groups                    |
| (pattern collections like Microsoft 365)  |
+------------------+-----------------------+
                   | No match
                   v
+------------------------------------------+
| Priority 2: Individual URL Rules          |
| (single-pattern rules with browser/profile)|
+------------------+-----------------------+
                   | No match
                   v
+------------------------------------------+
| Priority 3: Default Browser               |
| Opens in fallback browser with toast      |
| notification offering to create a rule    |
+------------------------------------------+
```

### Pattern Matching

| Pattern | Matches |
|---------|---------|
| `github.com` | `github.com`, `www.github.com`, `gist.github.com` |
| `docs.google.com` | Only `docs.google.com` |
| `sharepoint.com` | All SharePoint sites: `*.sharepoint.com` |

**Note**: Wildcards (`*`) are not supported. Use domain patterns for subdomain matching.

---

## Usage Guide

### Individual Rules

Individual rules route specific URL patterns to a browser/profile.

**Creating a Rule:**
1. Go to **Settings** > **Manage Rules**
2. Click **+ Add** > **Add Rule**
3. Enter URL pattern (e.g., `github.com`)
4. Select browser and profile
5. Click **Save**

**Rule Actions:**
| Action | Description |
|--------|-------------|
| Edit | Modify pattern or profiles |
| Delete | Remove the rule (with confirmation) |
| Move to Group | Convert to a URL group pattern |
| Enable/Disable | Toggle without deleting |

---

### URL Groups

URL Groups are collections of URL patterns that share the same browser/profile configuration.

**Built-in Groups (disabled by default):**
- **Microsoft 365** - outlook.office.com, teams.microsoft.com, sharepoint.com, etc.
- **Google Suite** - mail.google.com, drive.google.com, docs.google.com, etc.

**Creating a Custom Group:**
1. Go to **Settings** > **Manage Rules** > **URL Groups** tab
2. Click **+ Add** > **Add URL Group**
3. Enter group name and description
4. Add URL patterns
5. Select browser/profile (or multiple for picker)
6. Click **Save**

**Group Pattern Actions:**
| Action | Description |
|--------|-------------|
| Move to Rule | Convert pattern to individual rule |
| Remove | Delete pattern from group (with confirmation) |
| Restore | Restore deleted built-in patterns |

---

### Multi-Profile Rules

Rules and groups can have multiple browser/profile options.

| Profile Count | Behavior |
|---------------|----------|
| 1 profile | Opens automatically |
| 2+ profiles | Shows profile picker window |

When the picker appears:
- Click a profile to open the URL
- Selection is remembered for future auto-selection (24-hour window)

---

### Moving Patterns

You can move URL patterns between individual rules and groups:

**Move Group Pattern to Individual Rule:**
1. Edit a URL Group
2. Click the arrow icon next to a pattern
3. Configure the individual rule settings
4. Save - pattern is removed from group and added as rule

**Move Individual Rule to Group:**
1. In the Rules list, click the move icon on a rule
2. Select the target group
3. Optionally delete the original rule

This allows flexible reorganization of your URL routing configuration.

---

## Configuration

### Settings

| Setting | Description |
|---------|-------------|
| Rules Processing | Master toggle to enable/disable all rule processing |
| Show Notifications | Toast notification when no rule matches, with option to create a rule |
| Use Last Active Browser | Remember recently used browser/profile |
| Clipboard Monitoring | Watch clipboard for URLs and route them through rules |
| Minimize to Tray | Keep app running in system tray when window is closed |
| Start with Windows | Launch LinkRouter automatically on Windows startup |

### Data Storage

All data is stored in `%APPDATA%\LinkRouter\`:

```
%APPDATA%\LinkRouter\
+-- settings.json       # Application preferences
+-- rules.json          # Individual URL rules
+-- urlgroups.json      # URL groups
+-- backups/            # Automatic rolling backups
```

**Backup & Restore:**
- **Export**: Settings > Backup & Restore > Export
- **Import**: Settings > Backup & Restore > Import (creates pre-import backup)

---

## Command-Line Options

| Argument | Description |
|----------|-------------|
| `<url>` | Process URL through routing rules |
| `--manage` | Open Settings window |
| `--register` | Register as browser and open Settings |
| `--unregister` | Unregister from Windows |
| `--startup` | Silent launch (for Windows startup) |

**Examples:**
```bash
LinkRouter.exe "https://github.com/user/repo"
LinkRouter.exe --manage
LinkRouter.exe --register
```

---

## Troubleshooting

### LinkRouter doesn't appear in Default Apps
1. Run `LinkRouter.exe --register`
2. Restart Windows Explorer or sign out/in

### URLs not being intercepted
1. Verify LinkRouter is set as default browser in Windows Settings
2. Check that Rules Processing is enabled (Settings page)
3. Verify the rule pattern matches the URL

### Browser profiles not detected
1. Close the browser completely
2. Restart LinkRouter

### Rules not matching
1. Check pattern format (lowercase, no wildcards)
2. Verify rule is enabled
3. Check priority order (rules > groups > default)

### Logs
Logs are written to: `D:\BrowserSelector\log.txt`

---

## Browser Support

| Browser | Profile Support |
|---------|-----------------|
| Google Chrome | Full |
| Microsoft Edge | Full |
| Mozilla Firefox | Full |
| Brave Browser | Full |
| Opera | Full |
| Opera GX | Full |

Profiles are detected from standard browser data locations and include:
- Profile name
- Account email (if signed in)
- Profile path for launch arguments

---

## Contributing

1. Fork the repository
2. Create a feature branch: `git checkout -b feature/my-feature`
3. Make your changes and test thoroughly
4. Commit: `git commit -m "Add my feature"`
5. Push: `git push origin feature/my-feature`
6. Open a Pull Request

---

## License

MIT License - See [LICENSE](LICENSE) file for details.

---

**LinkRouter** - Smart URL routing for Windows
