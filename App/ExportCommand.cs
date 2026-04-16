using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using BLComponentTemplate.Models;
using BLComponentTemplate.Services.Export;
using BLComponentTemplate.Services.IFC;
using Microsoft.Win32;
using BLComponentTemplate.Services.Rfa;
using BLComponentTemplate.Services.Rvt;

namespace BLComponentTemplate.App
{
    [Transaction(TransactionMode.Manual)]
    public class ExportCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;

            if (uiDoc == null)
            {
                TaskDialog.Show(
                    "BL Component Template",
                    "No hay ningún documento de Revit abierto.");
                return Result.Cancelled;
            }

            Document doc = uiDoc.Document;
            ExportSessionContext sessionContext = null;

            while (true)
            {
                MainWindow mainWindow = new MainWindow(doc);
                bool? mainResult = mainWindow.ShowDialog();

                if (mainResult != true || mainWindow.SessionContext == null)
                    return Result.Cancelled;

                sessionContext = mainWindow.SessionContext;

                ReviewWindow reviewWindow = new ReviewWindow(sessionContext);
                bool? reviewResult = reviewWindow.ShowDialog();

                if (reviewWindow.GoBackRequested)
                {
                    continue;
                }

                if (reviewResult == true)
                {
                    if (reviewWindow.SelectedAction == ReviewAction.ExportRfa)
                    {
                        bool isSystemAssembly =
                            string.Equals(sessionContext.ElementScale, "Sistema", StringComparison.OrdinalIgnoreCase);

                        if (isSystemAssembly)
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog
                            {
                                Title = "Guardar proyecto Revit",
                                Filter = "Revit Project (*.rvt)|*.rvt",
                                DefaultExt = ".rvt",
                                AddExtension = true,
                                FileName = BuildDefaultRvtFileName(sessionContext)
                            };

                            bool? saveResult = saveFileDialog.ShowDialog();

                            if (saveResult != true)
                                return Result.Cancelled;

                            try
                            {
                                RvtAssemblyExportService.Export(doc, sessionContext, saveFileDialog.FileName);

                                TaskDialog.Show(
                                    "BL Component Template",
                                    $"Exportación RVT completada correctamente.\n\nFichero generado:\n{saveFileDialog.FileName}");

                                return Result.Succeeded;
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show(
                                    "BL Component Template",
                                    $"Se ha producido un error durante la exportación RVT:\n\n{ex.Message}");

                                return Result.Failed;
                            }
                        }
                        else
                        {
                            SaveFileDialog saveFileDialog = new SaveFileDialog
                            {
                                Title = "Guardar familia Revit",
                                Filter = "Revit Family (*.rfa)|*.rfa",
                                DefaultExt = ".rfa",
                                AddExtension = true,
                                FileName = BuildDefaultRfaFileName(sessionContext)
                            };

                            bool? saveResult = saveFileDialog.ShowDialog();

                            if (saveResult != true)
                                return Result.Cancelled;

                            try
                            {
                                RfaExportService.Export(doc, sessionContext.SelectedMatch, saveFileDialog.FileName);

                                TaskDialog.Show(
                                    "BL Component Template",
                                    $"Exportación RFA completada correctamente.\n\nFichero generado:\n{saveFileDialog.FileName}");

                                return Result.Succeeded;
                            }
                            catch (Exception ex)
                            {
                                TaskDialog.Show(
                                    "BL Component Template",
                                    $"Se ha producido un error durante la exportación RFA:\n\n{ex.Message}");

                                return Result.Failed;
                            }
                        }
                    }

                    if (reviewWindow.SelectedAction == ReviewAction.ExportIfc)
                    {
                        SaveFileDialog saveFileDialog = new SaveFileDialog
                        {
                            Title = "Guardar exportación IFC",
                            Filter = "IFC Files (*.ifc)|*.ifc",
                            DefaultExt = ".ifc",
                            AddExtension = true,
                            FileName = BuildDefaultIfcFileName(sessionContext)
                        };

                        bool? saveResult = saveFileDialog.ShowDialog();

                        if (saveResult != true)
                            return Result.Cancelled;

                        IfcSharedParameterService.EnsureParameters(doc);
                        IfcParameterValueWriter.Write(doc, sessionContext);

                        try
                        {
                            IfcExportService.Export(doc, saveFileDialog.FileName, sessionContext);

                            TaskDialog.Show(
                                "BL Component Template",
                                $"Exportación IFC completada correctamente.\n\nFichero generado:\n{saveFileDialog.FileName}");

                            return Result.Succeeded;
                        }
                        catch (Exception ex)
                        {
                            TaskDialog.Show(
                                "BL Component Template",
                                $"Se ha producido un error durante la exportación IFC:\n\n{ex.Message}");

                            return Result.Failed;
                        }
                    }
                }

                return Result.Cancelled;
            }
        }

        private string BuildDefaultRvtFileName(ExportSessionContext context)
        {
            string component = SanitizeFileName(context?.ComponentType ?? "Componente");
            return $"{component}_assembly.rvt";
        }

        private string BuildDefaultIfcFileName(ExportSessionContext context)
        {
            string component = SanitizeFileName(context?.ComponentType ?? "Componente");
            string family = SanitizeFileName(context?.SelectedMatch?.Family ?? "SinFamilia");
            string type = SanitizeFileName(context?.SelectedMatch?.TypeName ?? "SinTipo");

            return $"{component}_{family}_{type}.ifc";
        }

        private string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Undefined";

            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                value = value.Replace(c, '_');
            }

            return value.Replace(" ", "_");
        }

        private string BuildDefaultRfaFileName(ExportSessionContext context)
        {
            string family = SanitizeFileName(context?.SelectedMatch?.Family ?? "Familia");
            string type = SanitizeFileName(context?.SelectedMatch?.TypeName ?? "Tipo");

            return $"{family}_{type}.rfa";
        }
    }
}