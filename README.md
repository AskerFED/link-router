# BrowserSelector

A Windows desktop application that acts as an intelligent browser router. When you click any URL from applications like Teams, Outlook, Word, or emails, BrowserSelector intercepts the link and lets you choose which browser and profile to open it with.

## Features

- **Multi-Browser Support** - Detects and works with Chrome, Edge, Firefox, Brave, Opera, and Opera GX
- **Profile Management** - Access all your browser profiles (personal, work, etc.) and open links directly in the right profile
- **URL Groups** - Group URLs by domain patterns with auto-open behavior (e.g., all Microsoft 365 URLs open in Edge Work profile)
- **Profile Groups** - Define groups of browser profiles to choose from when URLs match specific patterns (e.g., GitHub URLs show a picker with Dev profiles)
- **Individual Rules** - Create specific rules for individual domains
- **Priority-Based Routing** - Intelligent URL matching with clear priority order
- **Default Browser** - Set a fallback browser for when no rules match
- **Windows Integration** - Registers as a default browser handler and can start with Windows
- **Built-in Groups** - Pre-configured groups for Microsoft 365 and Google services (disabled by default)

## How It Works

```
URL clicked (from any app)
        │
        ▼
Windows calls BrowserSelector
        │
        ▼
┌───────────────────────────────────┐
│ Priority 1: URL Groups            │
│ (auto-open with configured        │
│  browser/profile)                 │
└───────────────┬───────────────────┘
                │ No match
                ▼
┌───────────────────────────────────┐
│ Priority 2: Profile Groups        │
│ (show picker with group's         │
│  profiles)                        │
└───────────────┬───────────────────┘
                │ No match
                ▼
┌───────────────────────────────────┐
│ Priority 3: Individual Rules      │
│ (auto-open with rule's            │
│  browser/profile)                 │
└───────────────┬───────────────────┘
                │ No match
                ▼
┌───────────────────────────────────┐
│ Priority 4: Default Browser       │
│ or show picker for selection      │
└───────────────────────────────────┘
```

## Installation

### Prerequisites
- Windows 10/11
- .NET 8.0 Runtime (or SDK for building from source)

### Option 1: Use Pre-built Installer
Run the installer from the `Installer` folder.

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/yourusername/BrowserSelector.git
cd BrowserSelector

# Build the project
dotnet build

# Or publish as single executable
dotnet publish -c Release -r win-x64 --self-contained
```

### Register as Default Browser
1. Launch BrowserSelector (opens Settings window)
2. Go to the "App" tab
3. Click "Register" button
4. Go to Windows Settings > Apps > Default Apps
5. Set BrowserSelector as your default browser for HTTP/HTTPS links

## Usage

### First-Time Setup
1. Launch BrowserSelector (Settings window opens automatically)
2. Go to "App" tab and click "Register"
3. Set your default browser for unmatched URLs
4. Create URL Groups or Profile Groups as needed

### Opening Links
When you click a URL:
1. **URL Group match** - Opens automatically in the group's configured browser/profile
2. **Profile Group match** - Shows a picker with the group's profiles to choose from
3. **Individual Rule match** - Opens automatically in the rule's configured browser/profile
4. **No match** - Uses default browser or shows picker for manual selection

### URL Groups (Auto-Open)
Create URL Groups to automatically open matching URLs with a specific browser/profile:
1. Go to Settings > URL tab
2. Click "+ Add" > "Add URL Group"
3. Enter group name and add URL patterns (e.g., "outlook.office.com", "sharepoint.com")
4. Select browser and profile for auto-open
5. Save

### Profile Groups (Picker)
Create Profile Groups to show a profile picker for matching URLs:
1. Go to Settings > Profiles tab
2. Click "+ Add Profile Group"
3. Enter group name and add URL patterns
4. Add multiple browser/profile combinations
5. Save - When URLs match, a picker shows these profiles

### Individual Rules
Create specific rules for individual URLs:
1. Click a link to open the browser picker
2. Select your preferred browser and profile
3. Check "Add Rule for this URL"
4. Click "Open" - the rule is saved automatically

### Managing Settings
Launch BrowserSelector without arguments or use `--manage` flag to open Settings:
- **URL tab** - Manage individual rules and URL groups
- **Profiles tab** - Manage profile groups
- **App tab** - Default browser, registration status

### Command-Line Arguments

| Argument | Description |
|----------|-------------|
| `<url>` | Open the specified URL (normal usage) |
| `--startup` | Silent launch for Windows startup |
| `--manage` | Open the rules manager window |
| `--register` | Register as default browser |
| `--unregister` | Unregister from Windows |

## Configuration

### Data Location
All configuration is stored in:
```
%APPDATA%\BrowserSelector\
    ├── settings.json        (Default browser settings)
    ├── rules.json           (Individual URL rules)
    ├── urlgroups.json       (URL Groups)
    └── profilegroups.json   (Profile Groups)
```

### URL Groups (urlgroups.json)
```json
[
  {
    "Id": "guid",
    "Name": "Microsoft 365",
    "IsEnabled": true,
    "UrlPatterns": ["outlook.office.com", "sharepoint.com", "teams.microsoft.com"],
    "Behavior": "UseDefault",
    "DefaultBrowserName": "Microsoft Edge",
    "DefaultBrowserPath": "C:\\...\\msedge.exe",
    "DefaultProfileName": "Work"
  }
]
```

### Profile Groups (profilegroups.json)
```json
[
  {
    "Id": "guid",
    "Name": "Development Profiles",
    "IsEnabled": true,
    "UrlPatterns": ["github.com", "gitlab.com"],
    "Members": [
      {
        "BrowserName": "Chrome",
        "ProfileName": "Dev Profile",
        "DisplayOrder": 0
      },
      {
        "BrowserName": "Firefox",
        "ProfileName": "Developer Edition",
        "DisplayOrder": 1
      }
    ]
  }
]
```

### Individual Rules (rules.json)
```json
[
  {
    "Id": "guid",
    "Pattern": "stackoverflow.com",
    "BrowserName": "Chrome",
    "BrowserPath": "C:\\...\\chrome.exe",
    "ProfileName": "Development",
    "ProfileArguments": "--profile-directory=\"Profile 2\"",
    "CreatedDate": "2024-01-15T10:30:00"
  }
]
```

## Project Structure

```
BrowserSelector/
├── App.xaml(.cs)                      # Application entry point
├── MainWindow.xaml(.cs)               # Browser/profile picker UI
├── SettingsWindow.xaml(.cs)           # Main settings UI (3 tabs)
├── EditRuleWindow.xaml(.cs)           # Rule editor dialog
├── AddRuleWindow.xaml(.cs)            # New rule dialog
├── EditUrlGroupWindow.xaml(.cs)       # URL Group editor
├── EditProfileGroupWindow.xaml(.cs)   # Profile Group editor
├── ProfileGroupPickerWindow.xaml(.cs) # Profile picker for matched groups
├── TestAutomationWindow.xaml(.cs)     # Automated testing UI
│
├── BrowserDetector.cs                 # Browser & profile detection
├── RegistryHelper.cs                  # Windows registry operations
├── UrlRule.cs                         # Rule model, manager & matching
├── UrlGroup.cs                        # URL Group model
├── UrlGroupManager.cs                 # URL Group persistence & matching
├── ProfileGroup.cs                    # Profile Group model
├── ProfileGroupManager.cs             # Profile Group persistence & matching
├── SettingsManager.cs                 # App settings management
├── BrowserInfoWithColor.cs            # Browser display model
│
├── Themes/
│   └── Generic.xaml                   # Shared styles and templates
│
├── Controls/                          # Custom WPF controls
│
├── BrowserSelector.csproj             # Project configuration
└── BrowserSelector.sln                # Solution file
```

## Technologies

- **.NET 8.0** - Target framework
- **WPF** - Windows Presentation Foundation for UI
- **System.Text.Json** - JSON serialization
- **Windows Registry API** - Default browser registration
- **DispatcherTimer** - Notification timing

## Browser Profile Detection

BrowserSelector detects profiles by reading browser configuration files:

| Browser | Profile Source |
|---------|---------------|
| Chrome | `%LOCALAPPDATA%\Google\Chrome\User Data\*\Preferences` |
| Edge | `%LOCALAPPDATA%\Microsoft\Edge\User Data\*\Preferences` |
| Firefox | `%APPDATA%\Mozilla\Firefox\profiles.ini` |
| Brave | `%LOCALAPPDATA%\BraveSoftware\Brave-Browser\User Data\*\Preferences` |
| Opera | `%APPDATA%\Opera Software\Opera Stable\Preferences` |

## Recent Changes (Phase 2)

### URL Routing Improvements
- **New Priority System**: URL Groups > Profile Groups > Individual Rules > Default
- **Profile Groups with URL Patterns**: Profile Groups can now have their own URL patterns independent of URL Groups
- **Simplified URL Groups**: URL Groups now only support auto-open behavior (profile picker functionality moved to Profile Groups)

### UI Improvements
- **Unified Settings Window**: All settings consolidated into a single window with 3 tabs (URL, Profiles, App)
- **Numbered Lists**: URL patterns and profile members displayed as numbered lists
- **Move to Group**: Individual rules can be moved to URL Groups or Profile Groups
- **Compact Scrollbars**: Professional styling throughout
- **Enable/Disable Toggles**: Groups can be enabled/disabled without deleting

### Startup Behavior
- **Direct Settings Access**: Opening app without URL shows Settings window directly
- **Registration Flow**: `--register` command shows Settings with App tab after registration

### Test Automation
- **New Tests**: Profile Group URL matching, Priority system verification
- **12 Automated Tests**: Comprehensive test coverage for all features

## License

MIT License - See LICENSE file for details.

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.
