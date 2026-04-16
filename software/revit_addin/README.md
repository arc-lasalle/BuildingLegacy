# Building Legacy – Revit Add-in

## Overview

This folder contains the source code of a prototype Revit Add-in developed within the *Building Legacy* project.

The Add-in enables the **semi-automated extraction and structuring of data from BIM models** to support the characterization of reusable building components.

It is designed to bridge BIM data with structured datasets used in the Building Legacy platform.

---

## Purpose

The Add-in supports the following objectives:

* Reduce manual data entry effort
* Improve consistency in component documentation
* Enable structured data extraction aligned with predefined templates
* Facilitate interoperability between BIM models and external data environments

---

## Core Functionality

The Add-in implements a guided workflow that allows users to:

1. Select a component or system type
2. Load a predefined Excel template
3. Automatically detect and extract relevant parameters from BIM elements
4. Review extracted data
5. Export results to structured Excel datasets
6. (Optional) Export IFC data with associated properties

---

## Supported Component Types

The current version supports a predefined set of component categories, including:

* Fire doors / automatic doors
* Metal structural elements (e.g., steel columns)
* Windows
* Tarpaulins
* HVAC systems (e.g., heat pumps)
* Roller shutters / blinds

The list may evolve in future versions.

---

## Data Mapping Approach

The Add-in uses a **name-based mapping strategy**:

* Parameters are matched using the names defined in the Excel template (Column A)
* Extracted values are written into Column B
* Units and formats (Columns C and D) are preserved from the template

This approach ensures flexibility and avoids dependency on fixed cell references.

---

## Technical Implementation

* Language: C#
* Framework: .NET (Windows)
* UI: WPF (Windows Presentation Foundation)
* Excel handling: EPPlus
* BIM platform: Autodesk Revit API (2026)

---

## Known Limitations

* Thermal properties (e.g., conductivity, density) depend on material asset availability
* U-value (heat transfer coefficient) may not be accessible in all models
* Some parameters require inference or manual completion
* Detection of components may vary depending on model structure and naming conventions
* Interface currently available only in Spanish

---

## Repository Structure

```
revit_addin/
│
├── src/                # Source code
├── README.md           # This file
└── instructions.md     # Installation and usage guide
```

---

## Intended Use

This Add-in is provided as a **research prototype**:

* Designed for experimentation and validation
* Not optimized for production environments
* Intended to support the reproducibility of the associated research work

---

## Future Work

Potential improvements include:

* Expansion of supported component types
* Improved parameter detection strategies
* Integration with IFC-based workflows (e.g., IDS)
* Enhanced multilingual support
* More robust handling of missing or incomplete BIM data

---
