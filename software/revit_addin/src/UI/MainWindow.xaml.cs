using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BLComponentTemplate.Services.Export;
using BLComponentTemplate.Services.Revit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace BLComponentTemplate
{
    public partial class MainWindow : Window
    {
        private readonly Document _document;

        public ExportSessionContext SessionContext { get; private set; }

        private PreviewControl _previewControl;
        private View3D _previewView;

        private bool _assemblyColumnAdded = false;

        public MainWindow(Document document)
        {
            InitializeComponent();
            _document = document;

            LoadScales();
            LoadComponentTypesForSelectedScale();
            UpdateAssemblySelectionColumn();
        }

        private void LoadScales()
        {
            ElementScaleComboBox.ItemsSource = new List<string>
            {
                "Componente",
                "Sistema"
            };

            ElementScaleComboBox.SelectedIndex = 0;
        }

        private void LoadComponentTypesForSelectedScale()
        {
            string selectedScale = ElementScaleComboBox.SelectedItem as string;

            List<string> items;

            if (string.Equals(selectedScale, "Sistema", StringComparison.OrdinalIgnoreCase))
            {
                items = new List<string>
                {
                    "Estructuras metálicas",
                    "Sistemas de climatización"
                };
                    }
                    else
                    {
                        items = new List<string>
                {
                    "Puertas cortafuegos",
                    "Pilares de acero",
                    "Vidrios",
                    "Lonas",
                    "Bombas de calor",
                    "Persianas enrollables",
                    "Ventanas"
                };
            }

            ComponentTypeComboBox.ItemsSource = items;
            ComponentTypeComboBox.SelectedIndex = items.Count > 0 ? 0 : -1;
        }

        private void SearchMatchesButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedType = ComponentTypeComboBox.SelectedItem as string;

            if (string.IsNullOrWhiteSpace(selectedType))
            {
                MessageBox.Show(
                    "Debes seleccionar un tipo de componente.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool preciseMode = PreciseModeCheckBox.IsChecked == true;

            List<ComponentMatch> matches = RevitElementFinder.FindMatches(_document, selectedType, preciseMode);

            MatchesDataGrid.ItemsSource = matches;

            ClearPreview();

            string modeText = preciseMode ? "precisa" : "general";

            if (matches.Count == 0)
            {
                StatusTextBlock.Text = $"No se han encontrado coincidencias con búsqueda {modeText} para: {selectedType}";
                NextButton.IsEnabled = false;
            }
            else
            {
                StatusTextBlock.Text = $"Se han encontrado {matches.Count} tipos coincidentes con búsqueda {modeText} para: {selectedType}";
                NextButton.IsEnabled = true;
            }
        }

        private void MatchesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComponentMatch selectedMatch = MatchesDataGrid.SelectedItem as ComponentMatch;

            if (selectedMatch == null)
            {
                ClearPreview();
                return;
            }

            try
            {
                ShowPreview(selectedMatch);
            }
            catch (Exception ex)
            {
                ClearPreview();
                StatusTextBlock.Text = $"No se pudo generar la previsualización: {ex.Message}";
            }
        }

        private void ShowPreview(ComponentMatch selectedMatch)
        {
            ClearPreview();

            List<ElementId> previewIds = GetPreviewElementIds(selectedMatch);

            if (previewIds == null || previewIds.Count == 0)
            {
                PreviewPlaceholderText.Text = "No se encontró ninguna instancia representativa para visualizar.";
                PreviewPlaceholderText.Visibility = System.Windows.Visibility.Visible;
                return;
            }

            using (Transaction tx = new Transaction(_document, "BL - Preview View"))
            {
                tx.Start();

                _previewView = CreateTemporary3DView(_document);
                PrepareViewForPreview(_document, _previewView, previewIds);

                BoundingBoxXYZ bb = BuildCombinedBoundingBox(_document, previewIds);
                if (bb != null)
                {
                    _previewView.IsSectionBoxActive = true;
                    _previewView.SetSectionBox(ExpandBoundingBox(bb, 0.25));
                }

                tx.Commit();
            }

            _previewControl = new PreviewControl(_document, _previewView.Id);
            PreviewHost.Content = _previewControl;
            PreviewPlaceholderText.Visibility = System.Windows.Visibility.Collapsed;
        }

        private List<ElementId> GetPreviewElementIds(ComponentMatch selectedMatch)
        {
            var result = new List<ElementId>();

            if (selectedMatch?.InstanceElementIds == null || selectedMatch.InstanceElementIds.Count == 0)
                return result;

            Element mainElement = _document.GetElement(new ElementId(selectedMatch.InstanceElementIds.First()));
            if (mainElement == null)
                return result;

            result.Add(mainElement.Id);

            // Caso especial: Vidrios -> intentar incluir el host
            string selectedComponentType = ComponentTypeComboBox.SelectedItem as string;
            bool isGlass = string.Equals(selectedComponentType, "Vidrios", StringComparison.OrdinalIgnoreCase);

            if (isGlass && mainElement is FamilyInstance fi && fi.Host != null)
            {
                result.Add(fi.Host.Id);
            }

            return result.Distinct().ToList();
        }

        private static BoundingBoxXYZ ExpandBoundingBox(BoundingBoxXYZ bbox, double marginFactor)
        {
            XYZ min = bbox.Min;
            XYZ max = bbox.Max;

            double dx = (max.X - min.X) * marginFactor;
            double dy = (max.Y - min.Y) * marginFactor;
            double dz = (max.Z - min.Z) * marginFactor;

            dx = Math.Max(dx, 0.5);
            dy = Math.Max(dy, 0.5);
            dz = Math.Max(dz, 0.5);

            return new BoundingBoxXYZ
            {
                Min = new XYZ(min.X - dx, min.Y - dy, min.Z - dz),
                Max = new XYZ(max.X + dx, max.Y + dy, max.Z + dz)
            };
        }

        private void ClearPreview()
        {
            PreviewHost.Content = null;

            if (_previewControl != null)
            {
                _previewControl.Dispose();
                _previewControl = null;
            }

            if (_previewView != null && _previewView.IsValidObject)
            {
                try
                {
                    using (Transaction tx = new Transaction(_document, "BL - Delete Preview View"))
                    {
                        tx.Start();
                        _document.Delete(_previewView.Id);
                        tx.Commit();
                    }
                }
                catch
                {
                    // No interrumpimos la UI por fallo de limpieza.
                }
            }

            _previewView = null;
            PreviewPlaceholderText.Text = "Selecciona un componente para visualizarlo";
            PreviewPlaceholderText.Visibility = System.Windows.Visibility.Visible;
        }

        private static View3D CreateTemporary3DView(Document doc)
        {
            ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .First(v => v.ViewFamily == ViewFamily.ThreeDimensional);

            View3D view = View3D.CreateIsometric(doc, viewFamilyType.Id);
            view.Name = $"BL_Preview_{Guid.NewGuid():N}".Substring(0, 20);

            return view;
        }

        private static void PrepareViewForPreview(Document doc, View3D view, List<ElementId> visibleIds)
        {
            List<ElementId> allViewElements = new FilteredElementCollector(doc, view.Id)
                .WhereElementIsNotElementType()
                .ToElementIds()
                .ToList();

            HashSet<long> keepIds = visibleIds
                .Select(id => id.Value)
                .ToHashSet();

            List<ElementId> toHide = allViewElements
                .Where(id => !keepIds.Contains(id.Value))
                .Where(id =>
                {
                    Element e = doc.GetElement(id);
                    if (e == null)
                        return false;

                    if (e.IsHidden(view))
                        return false;

                    return e.CanBeHidden(view);
                })
                .ToList();

            if (toHide.Count > 0)
            {
                view.HideElements(toHide);
            }
        }

        private static BoundingBoxXYZ BuildCombinedBoundingBox(Document doc, List<ElementId> ids)
        {
            if (ids == null || ids.Count == 0)
                return null;

            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;
            bool any = false;

            foreach (ElementId id in ids)
            {
                Element e = doc.GetElement(id);
                if (e == null)
                    continue;

                BoundingBoxXYZ bb = e.get_BoundingBox(null);
                if (bb == null)
                    continue;

                any = true;

                minX = Math.Min(minX, bb.Min.X);
                minY = Math.Min(minY, bb.Min.Y);
                minZ = Math.Min(minZ, bb.Min.Z);

                maxX = Math.Max(maxX, bb.Max.X);
                maxY = Math.Max(maxY, bb.Max.Y);
                maxZ = Math.Max(maxZ, bb.Max.Z);
            }

            if (!any)
                return null;

            return new BoundingBoxXYZ
            {
                Min = new XYZ(minX, minY, minZ),
                Max = new XYZ(maxX, maxY, maxZ)
            };
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedScale = ElementScaleComboBox.SelectedItem as string;
            string selectedComponentType = ComponentTypeComboBox.SelectedItem as string;
            ComponentMatch selectedMatch = MatchesDataGrid.SelectedItem as ComponentMatch;
            bool preciseMode = PreciseModeCheckBox.IsChecked == true;

            if (string.IsNullOrWhiteSpace(selectedScale))
            {
                MessageBox.Show(
                    "Debes seleccionar una escala.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedComponentType))
            {
                MessageBox.Show(
                    "Debes seleccionar un tipo.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            bool isSystem = string.Equals(selectedScale, "Sistema", StringComparison.OrdinalIgnoreCase);

            if (isSystem)
            {
                var allMatches = MatchesDataGrid.ItemsSource as List<ComponentMatch>;
                var selectedMatches = allMatches?
                    .Where(m => m.IsIncludedInAssembly)
                    .ToList() ?? new List<ComponentMatch>();

                if (selectedMatches.Count == 0)
                {
                    MessageBox.Show(
                        "Debes seleccionar al menos un subcomponente para formar el sistema.",
                        "BL Component Template",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                ComponentMatch referenceMatch = selectedMatches.First();

                SessionContext = new ExportSessionContext
                {
                    Document = _document,
                    ElementScale = selectedScale,
                    ComponentType = selectedComponentType,
                    PreciseMode = preciseMode,
                    SelectedMatch = referenceMatch,
                    SelectedMatches = selectedMatches,
                    ComponentDimensionsUnit = "mm",
                    MaterialAreaOrVolumeUnit = "m3",
                    ExportableParameters = RevitExportDataBuilder.Build(
                        _document,
                        selectedScale,
                        selectedComponentType,
                        referenceMatch,
                        selectedMatches,
                        "mm",
                        "m3")
                };
            }
            else
            {
                if (selectedMatch == null)
                {
                    MessageBox.Show(
                        "Debes seleccionar un componente antes de continuar.",
                        "BL Component Template",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                SessionContext = new ExportSessionContext
                {
                    Document = _document,
                    ElementScale = selectedScale,
                    ComponentType = selectedComponentType,
                    PreciseMode = preciseMode,
                    SelectedMatch = selectedMatch,
                    SelectedMatches = new List<ComponentMatch> { selectedMatch },
                    ComponentDimensionsUnit = "mm",
                    MaterialAreaOrVolumeUnit = "m3",
                    ExportableParameters = RevitExportDataBuilder.Build(
                        _document,
                        selectedScale,
                        selectedComponentType,
                        selectedMatch,
                        new List<ComponentMatch> { selectedMatch },
                        "mm",
                        "m3")
                };
            }

            DialogResult = true;
            Close();
        }

        private void ComponentTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateAssemblySelectionColumn();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            ClearPreview();
            base.OnClosed(e);
        }

        private void UpdateAssemblySelectionColumn()
        {
            bool isSystem = IsSystemScaleSelected();

            if (isSystem && !_assemblyColumnAdded)
            {
                var checkColumn = new DataGridCheckBoxColumn
                {
                    Header = "Incluir",
                    Binding = new System.Windows.Data.Binding("IsIncludedInAssembly"),
                    Width = 80
                };

                MatchesDataGrid.Columns.Insert(0, checkColumn);
                _assemblyColumnAdded = true;
            }
            else if (!isSystem && _assemblyColumnAdded)
            {
                MatchesDataGrid.Columns.RemoveAt(0);
                _assemblyColumnAdded = false;
            }
        }

        private bool IsSystemScaleSelected()
        {
            return string.Equals(
                ElementScaleComboBox.SelectedItem as string,
                "Sistema",
                StringComparison.OrdinalIgnoreCase);
        }

        private void CompositeAssemblyModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            UpdateAssemblySelectionColumn();
        }

        private void ElementScaleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadComponentTypesForSelectedScale();
            UpdateAssemblySelectionColumn();

            MatchesDataGrid.ItemsSource = null;
            ClearPreview();
            StatusTextBlock.Text = "Aún no se ha ejecutado ninguna búsqueda.";
            NextButton.IsEnabled = false;
        }
    }
}