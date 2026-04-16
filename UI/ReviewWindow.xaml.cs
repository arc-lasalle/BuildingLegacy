using System;
using System.IO;
using System.Linq;
using System.Windows;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Export;
using Microsoft.Win32;
using System.Collections.Generic;
using BLComponentTemplate.Models;


namespace BLComponentTemplate
{
    public partial class ReviewWindow : Window
    {
        public ExportSessionContext SessionContext { get; }

        public bool GoBackRequested { get; private set; }

        public ReviewAction SelectedAction { get; private set; } = ReviewAction.None;

        public ReviewWindow(ExportSessionContext sessionContext)
        {
            InitializeComponent();
            SessionContext = sessionContext;

            LoadSummary();
            LoadExportableParameters();
            UpdateTemplateStatus();
            UpdateRfaOrRvtButtonLabel();
        }       
   
        private void LoadSummary()
        {
            if (SessionContext == null)
                return;

            ComponentTypeTextBlock.Text = SessionContext.ComponentType ?? "";
            SearchModeTextBlock.Text = SessionContext.PreciseMode ? "Precisa" : "General";

            if (SessionContext.SelectedMatch != null)
            {
                CategoryTextBlock.Text = SessionContext.SelectedMatch.Category ?? "";
                FamilyTextBlock.Text = SessionContext.SelectedMatch.Family ?? "";
                TypeNameTextBlock.Text = SessionContext.SelectedMatch.TypeName ?? "";
                TypeIdTextBlock.Text = SessionContext.SelectedMatch.ElementId ?? "";
            }
        }

        private void LoadExportableParameters()
        {
            ExportableParametersDataGrid.ItemsSource = SessionContext?.ExportableParameters;
        }

        private void UpdateTemplateStatus()
        {
            if (SessionContext == null || string.IsNullOrWhiteSpace(SessionContext.ImportedTemplatePath))
            {
                TemplateStatusTextBlock.Text = "No se ha importado todavía ninguna plantilla Excel.";
            }
            else
            {
                string fileName = Path.GetFileName(SessionContext.ImportedTemplatePath);
                TemplateStatusTextBlock.Text = $"Plantilla seleccionada: {fileName}";
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            GoBackRequested = true;
            SelectedAction = ReviewAction.Back;
            DialogResult = false;
            Close();
        }

        private void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = "Seleccionar plantilla Excel",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx|Excel Macro-Enabled Workbook (*.xlsm)|*.xlsm|Todos los archivos (*.*)|*.*",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = false
            };

            if (!string.IsNullOrWhiteSpace(SessionContext?.ImportedTemplatePath))
            {
                try
                {
                    string currentDirectory = Path.GetDirectoryName(SessionContext.ImportedTemplatePath);
                    if (!string.IsNullOrWhiteSpace(currentDirectory) && Directory.Exists(currentDirectory))
                    {
                        openFileDialog.InitialDirectory = currentDirectory;
                    }
                }
                catch
                {
                }
            }

            bool? result = openFileDialog.ShowDialog();

            if (result != true)
                return;

            if (SessionContext == null)
            {
                MessageBox.Show(
                    "No hay una sesión de exportación activa.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SessionContext.ImportedTemplatePath = openFileDialog.FileName;
            UpdateTemplateStatus();

            MessageBox.Show(
                "Plantilla importada correctamente.",
                "BL Component Template",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExportDataButton_Click(object sender, RoutedEventArgs e)
        {
            if (SessionContext == null)
            {
                MessageBox.Show(
                    "No hay una sesión de exportación activa.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(SessionContext.ImportedTemplatePath))
            {
                MessageBox.Show(
                    "Debes importar una plantilla Excel antes de exportar los datos.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            int selectedCount = SessionContext.ExportableParameters?.Count(p => p.IsSelectedForExport) ?? 0;

            if (selectedCount == 0)
            {
                MessageBox.Show(
                    "Debes marcar al menos un dato para exportar.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Title = "Guardar Excel exportado",
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                DefaultExt = ".xlsx",
                AddExtension = true,
                FileName = BuildDefaultExportFileName()
            };

            bool? result = saveFileDialog.ShowDialog();

            if (result != true)
                return;

            try
            {
                ExcelExportService.Export(SessionContext, saveFileDialog.FileName);

                MessageBox.Show(
                    $"Exportación completada correctamente.\n\nFichero generado:\n{saveFileDialog.FileName}",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Se ha producido un error al exportar:\n\n{ex.Message}",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            MessageBox.Show(
                $"Exportación completada correctamente.\n\nFichero generado:\n{saveFileDialog.FileName}",
                "BL Component Template",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            SelectedAction = ReviewAction.ExportExcel;
            DialogResult = true;
            Close();

        }

        private void ExportRfaButton_Click(object sender, RoutedEventArgs e)
        {
            if (SessionContext == null)
            {
                MessageBox.Show(
                    "No hay un componente seleccionado para exportar.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SelectedAction = ReviewAction.ExportRfa;
            DialogResult = true;
            Close();
        }

        private void ExportIfcButton_Click(object sender, RoutedEventArgs e)
        {
            if (SessionContext == null)
            {
                MessageBox.Show(
                    "No hay una sesión de exportación activa.",
                    "BL Component Template",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            SelectedAction = ReviewAction.ExportIfc;
            DialogResult = true;
            Close();
        }

        private string BuildDefaultExportFileName()
        {
            string component = SanitizeFileName(SessionContext?.ComponentType ?? "Componente");
            string family = SanitizeFileName(SessionContext?.SelectedMatch?.Family ?? "SinFamilia");
            string type = SanitizeFileName(SessionContext?.SelectedMatch?.TypeName ?? "SinTipo");

            return $"{component}_{family}_{type}.xlsx";
        }

        private string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Undefined";

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(" ", "_");
        }        

        private void UpdateRfaOrRvtButtonLabel()
        {
            bool isSystemAssembly =
                string.Equals(SessionContext?.ElementScale, "Sistema", StringComparison.OrdinalIgnoreCase);

            if (isSystemAssembly)
            {
                ExportRfaButton.Content = "Exportar proyecto (RVT)";
                ExportRfaButton.IsEnabled = true;
            }
            else
            {
                ExportRfaButton.Content = "Exportar familia (RFA)";
                ExportRfaButton.IsEnabled = true;
            }
        }
    }
}