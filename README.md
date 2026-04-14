# Building Legacy – Data and Software Repository

## Overview

This repository provides the datasets, templates, and software prototype developed within the *Building Legacy* project to support the characterization, documentation, and reuse of building components.

It accompanies a scientific article currently under submission, and is intended to ensure **transparency, reproducibility, and reuse** of the proposed framework.

The repository includes:

* A parameter mapping between BAMB and Building Legacy
* Component data sheets derived from a real case study
* Reusable data entry templates
* A prototype Revit Add-on for semi-automated data extraction

---

## Repository Structure

```
BuildingLegacy/
│
├── data/
│   ├── parameter_mapping/
│   ├── component_sheets/
│   └── templates/
│
├── software/
│   └── revit_addin/
│
├── docs/
│
└── examples/
```

### Description

* **data/** → Contains all datasets used and produced in the study
* **software/** → Revit Add-on prototype source code
* **docs/** → Supporting methodological documentation
* **examples/** → Sample inputs and outputs for reproducibility

---

## Data Description

### 1. Parameter Mapping

Location:

```
data/parameter_mapping/
```

This dataset contains the mapping and adaptation of parameters from the BAMB (Buildings As Material Banks) framework to the Building Legacy context.

The adaptation addresses the transition from:

* **Product lifecycle perspective (BAMB)**
  to
* **In-situ reuse and existing building context (Building Legacy)**

The mapping reflects:

* Parameter selection criteria based on **Availability (A1–A5)**
* Parameter relevance based on **Eligibility (E1–E5)**
* Resulting classification into **S1–S4 categories**

These criteria are described in detail in the associated scientific article.

---

### 2. Component Data Sheets

Location:

```
data/component_sheets/
```

This folder contains the completed data sheets for six component categories derived from the case study:

* Fire doors
* Metal structures pillars
* Glasses
* Tarpaulins
* Heat pumps
* Steel roll-up shutters

These datasets were generated from the analysis of a pilot building (Abaceria Central Market) and represent the **minimum dataset required to describe reusable components**.

---

### 3. Data Entry Templates

Location:

```
data/templates/
```

These templates are designed to support standardized data collection for reusable building components.

Each template follows a fixed structure:

| Column | Description        |
| ------ | ------------------ |
| A      | Parameter name     |
| B      | Parameter value    |
| C      | Data type / format |
| D      | Unit               |

Key characteristics:

* Parameter names are fixed and used for mapping
* Units are predefined and should not be modified
* Multi-value entries are encoded using semicolon-separated values (e.g., `16; 0.04`)

These templates are compatible with the Revit Add-on provided in this repository.

---

## Software – Revit Add-on

Location:

```
software/revit_addin/
```

This repository includes a prototype Revit Add-on developed for **semi-automated extraction of component data** from BIM models.

### Main Features

* Selection of predefined component types
* Import of Excel templates
* Automatic population of selected parameters from BIM elements
* Export of completed datasets to Excel
* Optional export to IFC (when applicable)

### Requirements

* Autodesk Revit 2026
* .NET compatible environment

### Installation

1. Compile the project in Visual Studio
2. Copy the generated `.dll` file to the appropriate Revit Add-ins folder
3. Place the `.addin` file with the correct path reference

### Limitations

* Some parameters (e.g., thermal properties) depend on Revit’s internal data availability
* U-value and thermal assets may not be accessible in all models
* The Add-on currently supports a predefined set of component types
* Interface language: Spanish

---

## Reproducibility

To reproduce the workflow presented in the article:

1. Select a component template from `data/templates/`
2. Load the template into the Revit Add-on
3. Select BIM elements corresponding to the component
4. Execute the automated parameter extraction
5. Export the populated dataset to Excel
6. (Optional) Export IFC data or Export RFA component family

Example files are provided in:

```
examples/
```

---

## Documentation

Additional documentation is available in:

```
docs/
```

Including:

* Methodological framework
* Data model description
* Supporting figures and diagrams

---

## License

This repository includes both software and datasets:

* Software: MIT License
* Data: Creative Commons Attribution 4.0 International (CC BY 4.0)

---

## Citation

If you use this repository, please cite:

```
Author(s). Building Legacy Repository. 2026.
```

A formal citation will be provided upon publication of the associated article.

---

## Acknowledgements

The research reported in this article has been conducted in the Building Legacy project, which has received funding from the Ministerio de Industria, Turismo: 2024 Call for Proposals under the Recovery, Transformation and Resilience Plan, under grant agreement no AEI-010500-2024-19. The authors would like to thank the Recursos Urbans (URRU) for its assistance in reviewing the reusable component data sheets elaborated in this research. The authors also want to thank Sonia Monclús for her intensive work in coordinating, managing and completing this research project.

---

## Contact

For questions or collaborations, please contact:

* Gonçal Costa (goncal.costa@salle.url.edu), ARC-HER group
* La Salle Campus Barcelona – Universidad Ramon Llull

---

## Disclaimer

This repository provides a research prototype and datasets for academic purposes.
The tools and data are not intended for direct commercial deployment without further validation.

