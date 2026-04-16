using System;
using System.Reflection;
using Autodesk.Revit.UI;

namespace BLComponentTemplate.App
{
    public class RevitApp : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "Building Legacy";
            string panelName = "Excel Export";
            string buttonName = "Exportador";
            string buttonText = "Exportar\nExcel";

            try
            {
                // Crear pestaña (si ya existe, fallará; por eso lo capturamos)
                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch
                {
                    // La pestaña ya existe
                }

                RibbonPanel panel = GetOrCreatePanel(application, tabName, panelName);

                string assemblyPath = Assembly.GetExecutingAssembly().Location;
                string commandClassName = "BLComponentTemplate.App.ExportCommand";

                PushButtonData buttonData = new PushButtonData(
                    buttonName,
                    buttonText,
                    assemblyPath,
                    commandClassName
                );

                PushButton button = panel.AddItem(buttonData) as PushButton;

                if (button != null)
                {
                    button.ToolTip = "Abre la aplicación de exportación a Excel.";
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error al iniciar add-in", ex.Message);
                return Result.Failed;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private RibbonPanel GetOrCreatePanel(
            UIControlledApplication application,
            string tabName,
            string panelName)
        {
            foreach (RibbonPanel panel in application.GetRibbonPanels(tabName))
            {
                if (panel.Name == panelName)
                    return panel;
            }

            return application.CreateRibbonPanel(tabName, panelName);
        }
    }
}