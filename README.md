# RUKNBIM - Select by Revit ID

**RUKNBIM - Select by Revit ID** is a powerful, free add-in for Autodesk® Navisworks® that allows users to quickly locate and select model elements using their original Revit Element IDs.

---

## Key Features

* **Quick Element Selection:** Locate any Revit element inside Navisworks in seconds.
* **Bulk Selection:** Select multiple elements at once by entering comma-separated IDs (e.g., `102435, 102436, 102440`).
* **Validation & UI Feedback:** Built-in verification warns you if a Revit model is not loaded or if an invalid ID is entered.
* **Ribbon Integration:** Adds a dedicated, easy-to-access **RUKNBIM** tab to the Autodesk Navisworks ribbon.

---

## How It Works

Autodesk Navisworks stores original Revit Element IDs within the internal property attributes. This add-in programmatically queries the property `LcRevitId` (found in the `LcOaNat64AttributeValue` category) across all elements in the active document and matches them against user input to select them.

---

## Getting Started

1. **Open your model:** Load a Revit model (NWC/NWD) in Autodesk Navisworks.
2. **Launch the Add-in:** Navigate to the **RUKNBIM** tab on the Navisworks Ribbon and click the **RUKNBIM - Select by ID** button.
3. **Enter Revit IDs:** Paste a single Revit Element ID or multiple comma-separated IDs into the text box.
4. **Select:** Click **Select**. The corresponding elements will immediately be highlighted and selected in your Navisworks workspace.

---

## Supported Versions

RUKNBIM - Select by Revit ID is compiled and verified to work on the following Autodesk Navisworks versions (both Simulate and Manage):
* Navisworks **2022**
* Navisworks **2023**
* Navisworks **2024**
* Navisworks **2025**
* Navisworks **2026**

---

## Project Structure & Architecture

For developers looking to inspect or build the project:

* **[RUKN.SelectByRevitId.sln](file:///d:/API%20Khalaf/Rukn.Bim.Api/TEST/RuknSelectByRevitId/RUKN.SelectByRevitId.sln):** The Visual Studio solution file compiling the plugins.
* **[RUKN.Search.Common/](file:///d:/API%20Khalaf/Rukn.Bim.Api/TEST/RuknSelectByRevitId/RUKN.Search.Common):** Contains shared resources, ribbon initialization (`PluginRibbon.cs`, `PluginRibbon.xaml` localization), and the `PackageContents.xml` configuration for the Autodesk installer format.
* **[RUKN.Search.Plugin/](file:///d:/API%20Khalaf/Rukn.Bim.Api/TEST/RuknSelectByRevitId/RUKN.Search.Plugin):** Houses the main execution entry points (`SelectByIdPlugin.cs`), GUI dialog window code/styles (`SelectByIdWindow.xaml`, `FeedbackWindow.xaml`), and search/selection logic (`Tools.cs`).
* **[RUKN.Search.2024/](file:///d:/API%20Khalaf/Rukn.Bim.Api/TEST/RuknSelectByRevitId/RUKN.Search.2024):** Visual Studio target project template for building against Navisworks 2024 SDK.

---

## Installation

To install the add-in:
1. Download the compiled release `RUKN.Search.bundle` folder.
2. Copy the `.bundle` folder into your Autodesk plugins folder:
   `%appdata%\Autodesk\ApplicationPlugins\`

---

## Contributing

If you would like to contribute, report issues, or suggest new features:
* Submit a [Pull Request](https://github.com/RuknDevelopment/RUKN.Search/pulls).
* Open an [Issue / Feature Request](https://github.com/RuknDevelopment/RUKN.Search/issues).

---

## About Us

We are an international team of AEC professionals, product designers, and software developers working together to transform construction requirements into accurate and partnership-driven technological solutions.

<p align="center" width="100%">
    <a href="https://www.Rukn.com/">
        <img src="https://s3.amazonaws.com/everse.assets/GithubReadme/Rukn_logo_no+slogan.jpg" alt="Rukn Logo" align="center">
    </a>
</p>

