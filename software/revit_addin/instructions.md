# Installation and Usage Instructions

## 1. System Requirements

Before installing the Add-in, ensure that the following requirements are met:

* Autodesk Revit 2026
* Windows operating system
* .NET compatible environment
* Visual Studio (for compilation, if using source code)

---

## 2. Installation

### Step 1 – Compile the Add-in

1. Open the solution file in Visual Studio
2. Build the project in **Debug** or **Release** mode
3. Locate the generated `.dll` file in:

```
/bin/Debug/netX.X-windows/
```

---

### Step 2 – Install the Add-in in Revit

1. Copy the `.dll` file to a local folder (e.g., `C:\RevitAddins\BuildingLegacy\`)

2. Create or edit a `.addin` file with the following structure:

```xml
<RevitAddIns>
  <AddIn Type="Application">
    <Name>Building Legacy Component Template</Name>
    <Assembly>C:\RevitAddins\BuildingLegacy\BLComponentTemplate.dll</Assembly>
    <AddInId>YOUR-GUID-HERE</AddInId>
    <FullClassName>BLComponentTemplate.RevitApp</FullClassName>
    <VendorId>BL</VendorId>
    <VendorDescription>Building Legacy Project</VendorDescription>
  </AddIn>
</RevitAddIns>
```

3. Place the `.addin` file in:

```
C:\ProgramData\Autodesk\Revit\Addins\2026\
```

---

### Step 3 – Launch Revit

* Open Autodesk Revit 2026
* Navigate to the **Add-ins** tab
* Locate the Building Legacy Add-in

---

## 3. Workflow

### Step 1 – Select Component Type

* Choose the type of component or system from the dropdown menus
* Depending on the configuration, separate selections may exist for:

  * Components
  * Systems

---

### Step 2 – Load Excel Template

* Import a template from:

```
data/templates/
```

* The template is loaded into memory and used as the target structure

---

### Step 3 – Select BIM Elements

* Select one or more elements from the Revit model
* Selection may depend on:

  * Category
  * Naming conventions
  * Matching strategy (precise vs general)

---

### Step 4 – Automatic Parameter Extraction

The Add-in attempts to extract:

* Product name
* Manufacturer / brand
* Model reference
* Dimensions
* Materials
* Surface / volume
* Number of instances
* Thermal properties (if available)

---

### Step 5 – Review Data

* Extracted data is displayed for validation
* Users may:

  * Edit values
  * Add missing information
  * Confirm export selection

---

### Step 6 – Export Results

* Export structured data to Excel

* Data is written using:

  * Column A → parameter names (fixed)
  * Column B → extracted values

* Optional: export IFC file with associated properties

---

## 4. Notes on Data Handling

* Parameter matching is based on **names, not positions**
* Units are defined in the template and should not be modified
* Multi-value fields use semicolon-separated values

---

## 5. Known Issues

* Some material properties may return null values
* Thermal data depends on Revit analytical model configuration
* Incomplete BIM models may lead to missing parameters
* Detection of certain components may require manual adjustment

---

## 6. Troubleshooting

| Issue               | Possible Cause        | Solution                  |
| ------------------- | --------------------- | ------------------------- |
| Add-in not visible  | Incorrect .addin path | Check installation folder |
| Missing parameters  | BIM model lacks data  | Complete manually         |
| Null thermal values | No thermal assets     | Verify material settings  |
| Export fails        | Excel file locked     | Close file before export  |

---

## 7. Recommendations

* Use consistent naming conventions in BIM models
* Validate templates before use
* Combine automated extraction with manual verification
* Test with sample files provided in `/examples/`

---

## 8. Disclaimer

This Add-in is a research prototype and may not cover all edge cases.
Results should be validated before use in real-world applications.
