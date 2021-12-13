using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using FieldDefinitionGenerator.Logic.Constants;
using FieldDefinitionGenerator.Logic.Services;
using FieldDefinitionGenerator.Logic.Services.Abstractions;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using XrmToolBox.Extensibility;

namespace FieldDefinitionGenerator
{
    public partial class MyPluginControl : PluginControlBase
    {
        private Settings mySettings;
        private string entityName;

        public MyPluginControl()
        {
            InitializeComponent();
        }

        private void MyPluginControl_Load(object sender, EventArgs e)
        {
            ShowInfoNotification("This is a notification that can lead to XrmToolBox repository", new Uri("https://github.com/MscrmTools/XrmToolBox"));

            // Loads or creates the settings for the plugin
            if (!SettingsManager.Instance.TryLoad(GetType(), out mySettings))
            {
                mySettings = new Settings();

                LogWarning("Settings not found => a new settings file has been created!");
            }
            else
            {
                LogInfo("Settings found and loaded");
            }

            // Custom onLoad

            ExecuteMethod(GetEntities);

            fileTypeComboBox.Items.AddRange(new object[]
                {
                    //FileTypes.CS,
                    FileTypes.TS
                }
            );

            // Set disabled 
            fileTypeComboBox.SelectedItem = FileTypes.TS;
            fileTypeComboBox.Enabled = false;
        }

        private void tsbClose_Click(object sender, EventArgs e)
        {
            CloseTool();
        }

        private void GetEntities()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Getting entities",
                Work = (worker, args) =>
                {
                    args.Result = Service.RetrieveMultiple(new QueryExpression("entity")
                    {
                        ColumnSet = new ColumnSet("logicalname")
                    });
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (args.Result is EntityCollection result)
                    {
                        entitiesComboBox.Items.AddRange(result
                            .Entities
                            .Select(e => e.GetAttributeValue<string>("logicalname"))
                            .OrderBy(e => e)
                            .ToArray());
                    }
                }
            });
        }

        /// <summary>
        /// This event occurs when the plugin is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyPluginControl_OnCloseTool(object sender, EventArgs e)
        {
            // Before leaving, save the settings
            SettingsManager.Instance.Save(GetType(), mySettings);
        }

        /// <summary>
        /// This event occurs when the connection has been updated in XrmToolBox
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService, ConnectionDetail detail, string actionName, object parameter)
        {
            base.UpdateConnection(newService, detail, actionName, parameter);

            if (mySettings != null && detail != null)
            {
                mySettings.LastUsedOrganizationWebappUrl = detail.WebApplicationUrl;
                LogInfo("Connection has changed to: {0}", detail.WebApplicationUrl);
            }
        }

        private void Generate_Click(object sender, EventArgs e)
        {
            entityName = entitiesComboBox.Text;

            ExecuteMethod(GenerateFile);
        }

        private void GenerateFile()
        {
            WorkAsync(new WorkAsyncInfo
            {
                Message = "Creating definition...",
                Work = (worker, args) =>
                {
                    var request = new RetrieveEntityRequest
                    {
                        EntityFilters = EntityFilters.Attributes,
                        LogicalName = entityName
                    };

                    args.Result = Service.Execute(request);
                },
                PostWorkCallBack = (args) =>
                {
                    if (args.Error != null)
                    {
                        MessageBox.Show(args.Error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                    if (args.Result is RetrieveEntityResponse result)
                    {
                        var generationService = (IGenerationService)new TypeScriptGenerationService(Service);

                        codeTextBox.Text = generationService.Generate(result.EntityMetadata);
                    }
                }
            });
        }

        private void entitiesComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            // entityName = entitiesComboBox.SelectedItem.ToString();
        }
    }
}