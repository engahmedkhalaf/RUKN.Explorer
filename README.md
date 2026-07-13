# RUKN Explorer - Viewpoint by Level

**RUKN Explorer - Viewpoint by Level** is a powerful Autodesk® Navisworks® add-in that allows users to automatically create clean, isolated viewpoints for each Revit Level with a slice clipping plane placed at the level's Z elevation.

---

<img width="1713" height="817" alt="image" src="https://github.com/user-attachments/assets/ae1e3cf9-053f-4398-a966-c6794cb58ccb" />

---

## Key Features

* **Automatic Viewpoint Generation:** Automatically generate structured viewpoints for all Revit levels in the model.
* **Top & Bottom Section Cuts (Slices):** Define custom Top and Bottom offsets to isolate specific floor levels.
* **Unit Conversion:** Support for multiple working units (mm, cm, m, ft) with automatic scaling.
* **Grid and Selection Tree Optimization:** High-performance caching of level elevations from Revit properties or active Grid Systems to prevent Navisworks from freezing on large models.
* **Modeless UI & Ribbon Integration:** Adds a dedicated button under the **RUKNBIM** tab on the Navisworks Ribbon with modeless keyboard interaction.

---

## How It Works

The add-in queries Revit levels and active Grid Systems inside the active document. It caches level names and elevations once to optimize performance. For each level, it sets up two parallel clipping planes (Top and Bottom Z boundaries based on user offsets) to create a slice cut, then saves the generated viewpoint.

---

## Getting Started

1. **Open your model:** Load a Revit-exported model (NWC/NWD) containing levels or grids in Autodesk Navisworks.
2. **Launch the Add-in:** Navigate to the **RUKNBIM** ribbon tab and click the **RUKN Explorer** button.
3. **Configure Settings:** Choose your preferred units, check/uncheck offsets, and input offset values.
4. **Generate Viewpoints:** Select the target model levels and click **Generate** to automatically create viewpoints with section cuts.

---

## Supported Versions

RUKN Explorer - Viewpoint by Level is compiled and verified to work on the following Autodesk Navisworks versions (both Simulate and Manage):
* Navisworks **2022**
* Navisworks **2023**
* Navisworks **2024**
* Navisworks **2025**
* Navisworks **2026**

---

## Project Structure & Architecture

For developers looking to inspect or build the project:

* **[RUKN.Explorer.sln](file:///d:/API%20Khalaf/Rukn.Bim.Api/WIP/NAVIS/RUKN%20Explorer/RUKN.Explorer.sln):** The Visual Studio solution file compiling the plugins.
* **[RUKN.InsightPro.Common/](file:///d:/API%20Khalaf/Rukn.Bim.Api/WIP/NAVIS/RUKN%20Explorer/RUKN.InsightPro.Common):** Contains shared resources, ribbon initialization (`PluginRibbon.cs`, `PluginRibbon.xaml` localization), and the `PackageContents.xml` configuration for the Autodesk installer format.
* **[RUKN.InsightPro.Plugin/](file:///d:/API%20Khalaf/Rukn.Bim.Api/WIP/NAVIS/RUKN%20Explorer/RUKN.InsightPro.Plugin):** Houses the main execution entry points, GUI dialog window code/styles (`ModelProcessingWindow.xaml`, `ModelProcessingWindow.xaml.cs`), and search/selection logic.
* **[RUKN.InsightPro.2024/](file:///d:/API%20Khalaf/Rukn.Bim.Api/WIP/NAVIS/RUKN%20Explorer/RUKN.InsightPro.2024):** Visual Studio target project template for building against Navisworks 2024 SDK.

---

## Technical Developer Notes

### 1. Navisworks Ribbon Configuration
To ensure your custom plugin ribbon buttons appear correctly in Autodesk Navisworks:
* You need to supplement the `CommandHandlerPlugin` with a `.xaml` definition file (for newer versions) or the `.name` file localized structure.
* For professional deployment, place your custom plugin folder in the following user directory:
  `%AppData%\Autodesk Navisworks Manage [Version]\Plugins\YourPluginName\`
* To ensure the ribbon registers and loads, you must include a `plugin.xml` or `PackageContents.xml` in your plugin folder structure to register the add-in with Navisworks.

### 2. Deep-Diving into Quantification (COM API)
While the standard .NET API (the `State` object) is excellent for reading object property values:
* The **Quantification Workbook** itself is largely exposed through the older COM API.
* To manipulate, query, or extract data from the Quantification items (which contain the takeoff formulas and mapped resources), you must interact with the **`InwOpQuantification`** interface.

---

## Installation

To install the add-in:
1. Download the compiled release `RUKN.Explorer.bundle` folder.
2. Copy the `.bundle` folder into your Autodesk plugins folder:
   `%appdata%\Autodesk\ApplicationPlugins\`

---

## Contributing

If you would like to contribute, report issues, or suggest new features:
* Submit a [Pull Request](https://github.com/engahmedkhalaf/RUKN.Explorer/pulls).
* Open an [Issue / Feature Request](https://github.com/engahmedkhalaf/RUKN.Explorer/issues).

---

## Feedback & Reviews

If you appreciate the work put into this free add-in, please consider giving a review on the App Store description page!

---

## About Us

We are an international team of AEC professionals, product designers, and software developers working together to transform construction requirements into accurate and partnership-driven technological solutions.

<p align="center" width="100%">
    <a href="https://www.ruknbim.com/">
        <img src="https://s3.amazonaws.com/everse.assets/GithubReadme/Rukn_logo_no+slogan.jpg" alt="Rukn Logo" align="center">
    </a>
</p>
