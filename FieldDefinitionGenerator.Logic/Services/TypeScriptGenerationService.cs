using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using FieldDefinitionGenerator.Logic.Services.Abstractions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;

namespace FieldDefinitionGenerator.Logic.Services
{
    public class TypeScriptGenerationService : GenerationService
    {
        // TODO move empty 
        public TypeScriptGenerationService(IOrganizationService service) : base(service)
        {
        }

        public override string Generate(EntityMetadata entityMetadata)
        {
            var resultBuilder = new StringBuilder();
            var attrList = entityMetadata.Attributes.OrderBy(a => a.LogicalName).ToList();

            #region Fields

            // TODO to separate method

            resultBuilder.AppendLine(
                $"{AddTabs(0)}//** Collection of Attribute structures for {entityMetadata.DisplayName.UserLocalizedLabel.Label} */");
            resultBuilder.AppendLine(
                $"{AddTabs(0)}//** Created on {DateTime.Now} */");
            resultBuilder.AppendLine();
            resultBuilder.AppendLine($"{AddTabs(0)}export const enum {entityMetadata.SchemaName}Definition {{");

            resultBuilder.AppendLine($"{AddTabs(1)}LogicalName = \"{entityMetadata.LogicalName}\",");
            resultBuilder.AppendLine($"{AddTabs(1)}LogicalSetName = \"{entityMetadata.EntitySetName}\",");


            foreach (var attr in attrList)
            {
                switch (attr.AttributeType)
                {
                    case AttributeTypeCode.Lookup:
                    case AttributeTypeCode.Customer:
                        {
                            resultBuilder.AppendLine(
                                $"{AddTabs(1)}{attr.LogicalName} = \"{attr.LogicalName}\", // Type: {attr.AttributeType}");
                            resultBuilder.AppendLine(
                                $"{AddTabs(1)}{attr.LogicalName}Api = \"_{attr.LogicalName}_value\", // Type: {attr.AttributeType}");
                        }
                        break;
                    default:
                        {
                            resultBuilder.AppendLine(
                                $"{AddTabs(1)}{attr.LogicalName} = \"{attr.LogicalName}\", // Type: {attr.AttributeType}");
                        }
                        break;
                }
            }

            resultBuilder.AppendLine($"{AddTabs(0)}}}");
            resultBuilder.AppendLine();

            #endregion

            #region Options

            resultBuilder.AppendLine($"{AddTabs(0)}export module {entityMetadata.SchemaName}OptionsDefinition {{");

            foreach (var attr in attrList.Where(a =>
                a.AttributeType == AttributeTypeCode.Picklist ||
                a.AttributeType == AttributeTypeCode.Status ||
                a.AttributeType == AttributeTypeCode.State))
            {
                resultBuilder.AppendLine();

                // From documentation ???
                //if (attribute.AttributeTypeName.Value != "MultiSelectPicklistType") continue;

                var retrieveAttributeResponse = Service.Execute(new RetrieveAttributeRequest
                {
                    LogicalName = attr.LogicalName,
                    EntityLogicalName = entityMetadata.LogicalName,
                    RetrieveAsIfPublished = true
                }) as RetrieveAttributeResponse;

                if (retrieveAttributeResponse == null)
                {
                    continue;
                }

                var optionList = (retrieveAttributeResponse.AttributeMetadata as EnumAttributeMetadata)?.OptionSet
                    .Options.ToArray();
                var enumName = attr.SchemaName;

                if (optionList != null && optionList.Length > 0)
                {
                    resultBuilder.AppendLine($"{AddTabs(1)}export enum {enumName} {{");

                    foreach (var option in optionList)
                    {
                        var label = option.Label.UserLocalizedLabel.Label.ToString(CultureInfo.InvariantCulture);

                        label = label.Replace("(", "_");
                        label = label.Replace(")", "_");
                        label = label.Replace("&", "_");
                        label = label.Replace("'", "_");
                        label = label.Replace("-", "_");
                        label = label.Replace(",", "_");

                        label = label.Replace(" ", string.Empty);
                        var regEx = new Regex("[^a-zA-Z0-9_]");
                        label = regEx.Replace(label, string.Empty);
                        var regExNumberOnly = new Regex("^[0-9]");
                        if (regExNumberOnly.IsMatch(label))
                        {
                            label = $"_{label}";
                        }

                        resultBuilder.AppendLine($"{AddTabs(2)}{label} = {option.Value},");
                    }

                    // TODO Add label mapping 
                }

                resultBuilder.AppendLine($"{AddTabs(1)}}}");
            }

            resultBuilder.Append($"{AddTabs(0)}}}");

            #endregion

            return resultBuilder.ToString();
        }
    }
}
