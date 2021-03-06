using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;

namespace InfrastructureCli.Services;

public static class AwsCloudFormationService
{
    private class AwsCloudFormationException : Exception
    {
        internal AwsCloudFormationException(string message) : base(message)
        {
        }
    }
    
    public static class OnCreateWithoutChangeSet
    {
        internal static bool EnableTerminationProtection = default;
        internal static OnFailure? OnFailure = OnFailure.ROLLBACK;
        internal static bool DisableRollback = false;

        public static void ShouldNotEnableTerminationProtection() => EnableTerminationProtection = false;
        public static void ShouldEnableTerminationProtection() => EnableTerminationProtection = true;

        public static void ShouldRollbackOnFailure()
        {
            OnFailure = OnFailure.ROLLBACK;
            DisableRollback = false;
        }

        public static void ShouldDeleteOnFailure()
        {
            OnFailure = OnFailure.DELETE;
            DisableRollback = false;
        }

        public static void ShouldDoNothingOnFailure()
        {
            OnFailure = OnFailure.DO_NOTHING;
            DisableRollback = false;
        }

        public static void ShouldEnableRollback()
        {
            OnFailure = OnFailure.ROLLBACK;
            DisableRollback = false;
        }

        public static void ShouldDisableRollback()
        {
            OnFailure = null;
            DisableRollback = true;
        }
    }

    public static class OnUpdateWithoutChangeSet
    {
        internal static bool DisableRollback = false;

        public static void ShouldEnableRollback()
        {
            DisableRollback = false;
        }

        public static void ShouldDisableRollback()
        {
            DisableRollback = true;
        }
    }

    private const string SuccessfulCreateStatus = "CREATE_COMPLETE";

    private static readonly string[] WaitToEndCreateStatuses =
    {
        "CREATE_IN_PROGRESS"
    };

    private static readonly string[] WaitToBeginUpdateStatuses =
    {
        "CREATE_IN_PROGRESS",
        "REVIEW_IN_PROGRESS",
        "ROLLBACK_IN_PROGRESS",
        "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS",
        "UPDATE_IN_PROGRESS",
        "UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS",
        "UPDATE_ROLLBACK_IN_PROGRESS"
    };

    private const string SuccessfulUpdateStatus = "UPDATE_COMPLETE";
    
    private static readonly string[] WaitToEndUpdateStatuses =
    {
        "UPDATE_IN_PROGRESS",
        "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS"
    };    
    
    private static readonly IAmazonCloudFormation Client = new AmazonCloudFormationClient();

    private static Parameter GetParameter(string parameterName, string parameterValue)
    {
        return new Parameter
        {
            ParameterKey = parameterName,
            ParameterValue = parameterValue
        };
    }

    private static Parameter GetParameter(KeyValuePair<string, string> parameter)
    {
        return GetParameter(parameter.Key, parameter.Value);
    }

    private static Parameter ReuseParameter(Parameter parameter)
    {
        return new Parameter
        {
            ParameterKey = parameter.ParameterKey,
            UsePreviousValue = true
        };
    }

    public static AwsCloudFormationTemplateOptions DeserializeTemplateOptions(IReadOnlyDictionary<string, JsonElement> templateOptions)
    {
        return JsonService.Convert<IReadOnlyDictionary<string, JsonElement>, AwsCloudFormationTemplateOptions>(templateOptions);
    }

    public static string GetStackName(AwsCloudFormationTemplateOptions templateOptions)
    {
        return templateOptions.StackName ?? throw new ArgumentException("Missing Required StackName Template Option.", nameof(templateOptions));
    }

    private static List<string> GetCapabilities(AwsCloudFormationTemplateOptions templateOptions)
    {
        return templateOptions.Capabilities?.ToList() ?? new List<string>();
    }

    private static bool GetUseChangeSet(AwsCloudFormationTemplateOptions templateOptions)
    {
        return templateOptions.UseChangeSet ?? false;
    }
    
    private static async Task<Dictionary<string, string>> GetExportedOutputs(List<string> neededExports)
    {
        var exports = new Dictionary<string, string>();

        string? nextToken = null;

        do
        {
            var listExportsResponse = await Client.ListExportsAsync(new ListExportsRequest
            {
                NextToken = nextToken
            });

            foreach (var export in listExportsResponse.Exports)
            {
                if (!neededExports.Contains(export.Name))
                {
                    continue;
                }

                exports.Add(export.Name, export.Value);

                neededExports.Remove(export.Name);
            }

            if (neededExports.Count == 0)
            {
                break;
            }

            nextToken = listExportsResponse.NextToken;
        } while (nextToken != null);

        return exports;
    }

    private static async Task<List<Parameter>> GetImportParameters(IConsole console, AwsCloudFormationTemplateOptions templateOptions)
    {
        var neededExports = new List<string>();

        if (templateOptions.ImportParameters is not null)
        {
            foreach (var (_, exportName) in templateOptions.ImportParameters)
            {
                neededExports.Add(exportName);
            }
        }

        if (templateOptions.ImportParameterLists is not null)
        {
            foreach (var (_, exportNames) in templateOptions.ImportParameterLists)
            {
                neededExports.AddRange(exportNames);
            }
        }

        var importedParameters = new List<Parameter>();

        if (neededExports.Count == 0)
        {
            return importedParameters;
        }

        var exportedOutputs = await GetExportedOutputs(neededExports);

        if (templateOptions.ImportParameters is not null)
        {
            foreach (var (parameterName, exportName) in templateOptions.ImportParameters)
            {
                var exportValue = exportedOutputs.GetValueOrDefault(exportName, "")!;

                importedParameters.Add(GetParameter(parameterName, exportValue));
            }
        }

        if (templateOptions.ImportParameterLists is not null)
        {
            foreach (var (parameterName, exportNames) in templateOptions.ImportParameterLists)
            {
                var exportValues = exportNames
                    .Select(exportName => exportedOutputs.GetValueOrDefault(exportName, "")!);

                importedParameters.Add(GetParameter(parameterName, string.Join(",", exportValues)));
            }
        }

        return importedParameters;
    }

    private static List<Tag> GetTags(AwsCloudFormationTemplateOptions templateOptions)
    {
        return templateOptions
            .Tags?
            .Select(tag => new Tag
            {
                Key = tag.Key,
                Value = tag.Value
            })
            .ToList() ?? new List<Tag>();
    }

    private static async Task<List<Parameter>> GetParameters(IConsole console, AwsCloudFormationTemplateOptions templateOptions, Dictionary<string, string> cliParameters)
    {
        var importParameters = await GetImportParameters(console, templateOptions);

        return cliParameters.Select(GetParameter).Concat(importParameters).ToList();
    }

    private static async Task<List<Parameter>> GetParameters(IConsole console, AwsCloudFormationTemplateOptions templateOptions, Dictionary<string, string> parameters, Stack stack)
    {
        var parameterDictionary = (await GetParameters(console, templateOptions, parameters))
            .ToDictionary(parameter => parameter.ParameterKey);
            
        foreach (var parameter in stack.Parameters.Where(parameter => parameterDictionary.ContainsKey(parameter.ParameterKey) == false))
        {
            parameterDictionary.Add(parameter.ParameterKey, ReuseParameter(parameter));
        }
            
        return parameterDictionary.Values.ToList();
    }

    private static string GetTemplateBody(JsonElement template)
    {
        return JsonService.Serialize(template);
    }

    private static async Task<Stack?> GetStack(AwsCloudFormationTemplateOptions templateOptions)
    {
        var stackName = GetStackName(templateOptions);
        
        try
        {
            var request = new DescribeStacksRequest
            {
                StackName = stackName
            };

            var response = await Client.DescribeStacksAsync(request);

            return response.Stacks.FirstOrDefault();
        }
        catch (Exception exception) when (exception.Message == $"Stack with id {stackName} does not exist")
        {
            return null;
        }
    }

    private static void LogParameters(IConsole console, List<Parameter> parameters)
    {
        console.Out.WriteLine("Parameters:");
                
        foreach (var parameter in parameters)
        {
            console.Out.WriteLine(parameter.UsePreviousValue
                ? $"{parameter.ParameterKey} = (Use Previous Value)"
                : $"{parameter.ParameterKey} = {parameter.ParameterValue}");
        }

        console.Out.WriteLine();
    }

    private static void LogTemplateBody(IConsole console, string templateBody)
    {      
        console.Out.WriteLine("Template Body:");
                
        console.Out.WriteLine(templateBody);

        console.Out.WriteLine();
    }

    private static string GenerateChangeSetName()
    {
        var dateTime = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        return $"on--{dateTime:yyyy-M-d}--at--{dateTime:h-mm-ss-tt}--{guid}";
    }
    
    private static async Task<bool> WaitForStatusChange(bool waitNext, IConsole console, AwsCloudFormationTemplateOptions templateOptions, string? successStatus, params string[] loopStatuses)
    {
        var currentStatus = loopStatuses[0];
            
        while (loopStatuses.Contains(currentStatus))
        {
            console.WriteLine("Wait for 30 seconds");
                
            if (waitNext)
            {
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            else
            {
                waitNext = true;
            }
                
            var stack = await GetStack(templateOptions);

            if (stack == null)
            {
                throw new AwsCloudFormationException("Stack does not exist.");
            }

            currentStatus = stack.StackStatus;
                
            console.WriteLine($"Status is {currentStatus}");
        }

        return successStatus == currentStatus;
    }


    private static async Task<bool> CreateStack(IConsole console, DeployOptions options, AwsCloudFormationTemplateOptions templateOptions)
    {
        var request = new CreateStackRequest
        {
            StackName = GetStackName(templateOptions),
            Capabilities = GetCapabilities(templateOptions),
            Tags = GetTags(templateOptions),
            EnableTerminationProtection = OnCreateWithoutChangeSet.EnableTerminationProtection,
            TemplateBody = GetTemplateBody(options.Template),
            Parameters = await GetParameters(console, templateOptions, options.Parameters)
        };

        if (OnCreateWithoutChangeSet.DisableRollback)
        {
            request.DisableRollback = true;
        }
        else
        {
            request.OnFailure = OnCreateWithoutChangeSet.OnFailure;
        }

        LogParameters(console, request.Parameters);
        LogTemplateBody(console, request.TemplateBody);

        var response = await Client.CreateStackAsync(request);
            
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return false;
        }
            
        return await WaitForStatusChange(true, console, templateOptions, SuccessfulCreateStatus, WaitToEndCreateStatuses);
    }

    private static async Task<bool> CreateStackWithChangeSet(IConsole console, DeployOptions options, AwsCloudFormationTemplateOptions templateOptions)
    {
        var request = new CreateChangeSetRequest
        {
            ChangeSetType = ChangeSetType.CREATE,
            ChangeSetName = GenerateChangeSetName(),
            StackName = GetStackName(templateOptions),
            Capabilities = GetCapabilities(templateOptions),
            Tags = GetTags(templateOptions),
            TemplateBody = GetTemplateBody(options.Template),
            Parameters = await GetParameters(console, templateOptions, options.Parameters)
        };
            
        LogParameters(console, request.Parameters);
        LogTemplateBody(console, request.TemplateBody);

        var response = await Client.CreateChangeSetAsync(request);

        return response.HttpStatusCode == HttpStatusCode.OK;
    }

    private static async Task<bool> UpdateStack(IConsole console, Stack stack, DeployOptions options, AwsCloudFormationTemplateOptions templateOptions)
    {
        try
        {
            await WaitForStatusChange(false, console, templateOptions, null, WaitToBeginUpdateStatuses);
            
            var request = new UpdateStackRequest
            {
                StackName = GetStackName(templateOptions),
                Capabilities = GetCapabilities(templateOptions),
                Tags = GetTags(templateOptions),
                TemplateBody = GetTemplateBody(options.Template),
                DisableRollback = OnUpdateWithoutChangeSet.DisableRollback,
                Parameters = await (options.UsePreviousParameters
                    ? GetParameters(console, templateOptions, options.Parameters, stack)
                    : GetParameters(console, templateOptions, options.Parameters))
            };
            
            LogParameters(console, request.Parameters);
            LogTemplateBody(console, request.TemplateBody);

            var response = await Client.UpdateStackAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            return await WaitForStatusChange(true, console, templateOptions, SuccessfulUpdateStatus, WaitToEndUpdateStatuses);
        }
        catch (AmazonCloudFormationException exception) when (exception.Message == "No updates are to be performed.")
        {
            console.Out.WriteLine("No updates are to be performed.");
                
            return true;
        }
    }

    private static async Task<bool> UpdateStackWithChangeSet(IConsole console, Stack stack, DeployOptions options, AwsCloudFormationTemplateOptions templateOptions)
    {
        try
        {
            var request = new CreateChangeSetRequest
            {
                ChangeSetType = ChangeSetType.UPDATE,
                ChangeSetName = GenerateChangeSetName(),
                StackName = GetStackName(templateOptions),
                Capabilities = GetCapabilities(templateOptions),
                Tags = GetTags(templateOptions),
                TemplateBody = GetTemplateBody(options.Template),
                Parameters = await (options.UsePreviousParameters
                    ? GetParameters(console, templateOptions, options.Parameters, stack)
                    : GetParameters(console, templateOptions, options.Parameters))
            };
            
            LogParameters(console, request.Parameters);
            LogTemplateBody(console, request.TemplateBody);

            var response = await Client.CreateChangeSetAsync(request);

            return response.HttpStatusCode != HttpStatusCode.OK;
        }
        catch (AmazonCloudFormationException exception) when (exception.Message == "No updates are to be performed.")
        {
            console.Out.WriteLine("No updates are to be performed.");
                
            return true;
        }
    }

    internal static async Task<string?> Get(GetOptions getOptions)
    {
        var templateOptions = DeserializeTemplateOptions(getOptions.TemplateOptions);

        var stack = await GetStack(templateOptions);

        return stack?
            .Outputs
            .SingleOrDefault(output => output.OutputKey == getOptions.PropertyName)?
            .OutputValue;
    }
        
    internal static async Task<bool> Deploy(IConsole console, DeployOptions deployOptions)
    {
        var templateOptions = DeserializeTemplateOptions(deployOptions.TemplateOptions);

        var stack = await GetStack(templateOptions);

        if (GetUseChangeSet(templateOptions))
        {
            if (stack != null)
            {
                return await UpdateStackWithChangeSet(console, stack, deployOptions, templateOptions);
            }
            
            return await CreateStackWithChangeSet(console, deployOptions, templateOptions);
        }
        
        if (stack != null)
        {
            return await UpdateStack(console, stack, deployOptions, templateOptions);
        }

        return await CreateStack(console, deployOptions, templateOptions);
    }
}