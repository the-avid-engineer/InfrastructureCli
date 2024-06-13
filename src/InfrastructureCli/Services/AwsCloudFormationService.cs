using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;

namespace InfrastructureCli.Services;

public class AwsCloudFormationService : ICloudProvisioningService
{
    private readonly IConsole _console;
    private readonly AwsCloudFormationTemplateOptions _templateOptions;

    internal AwsCloudFormationService(IConsole console, IReadOnlyDictionary<string, JsonElement> templateOptions)
    {
        _console = console;
        _templateOptions = DeserializeTemplateOptions(templateOptions);
    }
    
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

    private static class StackStatuses
    {
        public static readonly StackStatus SuccessfulCreate = StackStatus.CREATE_COMPLETE;

        public static readonly StackStatus[] WaitToEndCreate =
        {
            StackStatus.CREATE_IN_PROGRESS,
        };
        public static readonly StackStatus[] WaitToBeginUpdate =
        {
            StackStatus.CREATE_IN_PROGRESS,
            StackStatus.REVIEW_IN_PROGRESS,
            StackStatus.ROLLBACK_IN_PROGRESS,
            StackStatus.UPDATE_COMPLETE_CLEANUP_IN_PROGRESS,
            StackStatus.UPDATE_IN_PROGRESS,
            StackStatus.UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS,
            StackStatus.UPDATE_ROLLBACK_IN_PROGRESS,
        };

        public static readonly StackStatus SuccessfulUpdate = StackStatus.UPDATE_COMPLETE;
    
        public static readonly StackStatus[] WaitToEndUpdate =
        {
            StackStatus.UPDATE_IN_PROGRESS,
            StackStatus.UPDATE_COMPLETE_CLEANUP_IN_PROGRESS,
        };
    }
    
    private static class ChangeSetStatuses
    {
        public static readonly ChangeSetStatus SuccessfulCreate = ChangeSetStatus.CREATE_COMPLETE;

        public static readonly ChangeSetStatus[] WaitToEndCreate =
        {
            ChangeSetStatus.CREATE_IN_PROGRESS
        };
    }
    
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

    private static AwsCloudFormationTemplateOptions DeserializeTemplateOptions(IReadOnlyDictionary<string, JsonElement> templateOptions)
    {
        return JsonService.Convert<IReadOnlyDictionary<string, JsonElement>, AwsCloudFormationTemplateOptions>(templateOptions);
    }
    
    private static string GetTemplateBody(JsonElement template)
    {
        return JsonService.Serialize(template);
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

    private static bool GetChangeSetNameOption(DeployOptions deployOptions, [NotNullWhen(true)] out string? changeSetName)
    {
        return deployOptions.Options.TryGetValue("ChangeSetName", out changeSetName);
    }
    
    private static string GetChangeSetName(DeployOptions deployOptions)
    {
        if (GetChangeSetNameOption(deployOptions, out var changeSetName))
        {
            return changeSetName;
        }
        
        var dateTime = DateTime.UtcNow;
        var guid = Guid.NewGuid();

        return $"on--{dateTime:yyyy-M-d}--at--{dateTime:h-mm-ss-tt}--{guid}";
    }


    
    private string GetStackName()
    {
        return _templateOptions.StackName ?? throw new ArgumentException("Missing Required StackName Template Option.", nameof(_templateOptions.StackName));
    }

    private List<string> GetCapabilities()
    {
        return _templateOptions.Capabilities?.ToList() ?? new List<string>();
    }

    private bool GetUseChangeSet(DeployOptions deployOptions)
    {
        if (GetChangeSetNameOption(deployOptions, out _))
        {
            return true;
        }
        
        return _templateOptions.UseChangeSet ?? false;
    }
    
    private async Task<List<Parameter>> GetImportParameters()
    {
        var neededExports = new List<string>();

        if (_templateOptions.ImportParameters is not null)
        {
            foreach (var (_, exportName) in _templateOptions.ImportParameters)
            {
                neededExports.Add(exportName);
            }
        }

        if (_templateOptions.ImportParameterLists is not null)
        {
            foreach (var (_, exportNames) in _templateOptions.ImportParameterLists)
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

        if (_templateOptions.ImportParameters is not null)
        {
            foreach (var (parameterName, exportName) in _templateOptions.ImportParameters)
            {
                var exportValue = exportedOutputs.GetValueOrDefault(exportName, "")!;

                importedParameters.Add(GetParameter(parameterName, exportValue));
            }
        }

        if (_templateOptions.ImportParameterLists is not null)
        {
            foreach (var (parameterName, exportNames) in _templateOptions.ImportParameterLists)
            {
                var exportValues = exportNames
                    .Select(exportName => exportedOutputs.GetValueOrDefault(exportName, "")!);

                importedParameters.Add(GetParameter(parameterName, string.Join(",", exportValues)));
            }
        }

        return importedParameters;
    }

    private List<Tag> GetTags()
    {
        return _templateOptions
            .Tags?
            .Select(tag => new Tag
            {
                Key = tag.Key,
                Value = tag.Value
            })
            .ToList() ?? new List<Tag>();
    }

    private async Task<List<Parameter>> GetParameters(Dictionary<string, string> cliParameters)
    {
        var importParameters = await GetImportParameters();

        return cliParameters.Select(GetParameter).Concat(importParameters).ToList();
    }

    private async Task<List<Parameter>> GetParameters(Dictionary<string, string> parameters, Stack stack)
    {
        var parameterDictionary = (await GetParameters(parameters))
            .ToDictionary(parameter => parameter.ParameterKey);
            
        foreach (var parameter in stack.Parameters.Where(parameter => parameterDictionary.ContainsKey(parameter.ParameterKey) == false))
        {
            parameterDictionary.Add(parameter.ParameterKey, ReuseParameter(parameter));
        }
            
        return parameterDictionary.Values.ToList();
    }
    
    private async Task<Stack?> GetStack()
    {
        var stackName = GetStackName();
        
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

    private async Task<ChangeSetStatus?> GetChangeSetStatus(string changeSetName)
    {
        var stackName = GetStackName();
        
        try
        {
            var request = new DescribeChangeSetRequest
            {
                StackName = stackName,
                ChangeSetName = changeSetName
            };

            var response = await Client.DescribeChangeSetAsync(request);

            return response.Status;
        }
        catch
        {
            return null;
        }
    }

    private void LogParameters(List<Parameter> parameters)
    {
        _console.Out.WriteLine("Parameters:");
                
        foreach (var parameter in parameters)
        {
            _console.Out.WriteLine(parameter.UsePreviousValue
                ? $"{parameter.ParameterKey} = (Use Previous Value)"
                : $"{parameter.ParameterKey} = {parameter.ParameterValue}");
        }

        _console.Out.WriteLine();
    }

    private void LogTemplateBody(string templateBody)
    {      
        _console.Out.WriteLine("Template Body:");
                
        _console.Out.WriteLine(templateBody);

        _console.Out.WriteLine();
    }

    private async Task<bool> WaitForStackStatusChange(bool waitNext, StackStatus? successStatus, params StackStatus[] loopStatuses)
    {
        var currentStatus = loopStatuses[0];
            
        while (loopStatuses.Contains(currentStatus))
        { 
            if (waitNext)
            {
                _console.WriteLine("Wait for 30 seconds");
                
                await Task.Delay(TimeSpan.FromSeconds(30));
            }
            else
            {
                waitNext = true;
            }
                
            var stack = await GetStack();

            if (stack == null)
            {
                throw new AwsCloudFormationException("Stack does not exist.");
            }

            currentStatus = stack.StackStatus;
                
            _console.WriteLine($"Stack status is {currentStatus}");
        }

        return successStatus == currentStatus;
    }
    
    private async Task<bool> WaitForChangeSetStatusChange(string changeSetName, ChangeSetStatus successStatus,
        params ChangeSetStatus[] loopStatuses)
    {
        var currentStatus = loopStatuses[0];

        while (loopStatuses.Contains(currentStatus))
        {
            _console.WriteLine("Wait for 15 seconds");

            await Task.Delay(TimeSpan.FromSeconds(15));

            var changeSetStatus = await GetChangeSetStatus(changeSetName);

            if (changeSetStatus == null)
            {
                throw new AwsCloudFormationException("ChangeSet does not exist.");
            }

            currentStatus = changeSetStatus.Value;
        
            _console.WriteLine($"ChangeSet status is {currentStatus.Value}");
        }

        return successStatus == currentStatus;
    }

    private async Task<bool> ChangeSetExists(string changeSetName)
    {
        var changeSetStatus = await GetChangeSetStatus(changeSetName);

        return changeSetStatus != null;
    }

    private async Task<bool> CreateStack(DeployOptions deployOptions)
    {
        var request = new CreateStackRequest
        {
            StackName = GetStackName(),
            Capabilities = GetCapabilities(),
            Tags = GetTags(),
            EnableTerminationProtection = OnCreateWithoutChangeSet.EnableTerminationProtection,
            TemplateBody = GetTemplateBody(deployOptions.Template),
            Parameters = await GetParameters(deployOptions.Parameters)
        };

        if (OnCreateWithoutChangeSet.DisableRollback)
        {
            request.DisableRollback = true;
        }
        else
        {
            request.OnFailure = OnCreateWithoutChangeSet.OnFailure;
        }

        LogParameters(request.Parameters);
        LogTemplateBody(request.TemplateBody);

        var response = await Client.CreateStackAsync(request);
            
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return false;
        }
            
        return await WaitForStackStatusChange(true, StackStatuses.SuccessfulCreate, StackStatuses.WaitToEndCreate);
    }

    private async Task<bool> CreateStackWithChangeSet(DeployOptions deployOptions)
    {
        var changeSetName = GetChangeSetName(deployOptions);

        if (await ChangeSetExists(changeSetName))
        {
            _console.Out.WriteLine("ChangeSet already exists.");
            return false;
        }

        ChangeSetType changeSetType;
        List<ResourceToImport>? resourcesToImport;
        List<Tag>? tags;
        
        if (deployOptions.Options.TryGetValue("ResourcesToImport", out var resourcesToImportJson))
        {
            changeSetType = ChangeSetType.IMPORT;
            resourcesToImport = JsonSerializer.Deserialize<List<ResourceToImport>>(resourcesToImportJson);
            tags = null;
        }
        else
        {
            changeSetType = ChangeSetType.CREATE;
            resourcesToImport = null;
            tags = GetTags();
        }
        
        var request = new CreateChangeSetRequest
        {
            ChangeSetType = changeSetType,
            ResourcesToImport = resourcesToImport,
            ChangeSetName = changeSetName,
            StackName = GetStackName(),
            Capabilities = GetCapabilities(),
            Tags = tags,
            TemplateBody = GetTemplateBody(deployOptions.Template),
            Parameters = await GetParameters(deployOptions.Parameters)
        };
            
        LogParameters(request.Parameters);
        LogTemplateBody(request.TemplateBody);

        var response = await Client.CreateChangeSetAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return false;
        }

        return await WaitForChangeSetStatusChange(changeSetName, ChangeSetStatuses.SuccessfulCreate,
            ChangeSetStatuses.WaitToEndCreate);
    }
    
    private async Task<bool> UpdateStack(Stack stack, DeployOptions options)
    {
        try
        {
            await WaitForStackStatusChange(false,null, StackStatuses.WaitToBeginUpdate);
            
            var request = new UpdateStackRequest
            {
                StackName = GetStackName(),
                Capabilities = GetCapabilities(),
                Tags = GetTags(),
                TemplateBody = GetTemplateBody(options.Template),
                DisableRollback = OnUpdateWithoutChangeSet.DisableRollback,
                Parameters = await (options.UsePreviousParameters
                    ? GetParameters(options.Parameters, stack)
                    : GetParameters(options.Parameters))
            };
            
            LogParameters(request.Parameters);
            LogTemplateBody(request.TemplateBody);

            var response = await Client.UpdateStackAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            return await WaitForStackStatusChange(true, StackStatuses.SuccessfulUpdate, StackStatuses.WaitToEndUpdate);
        }
        catch (AmazonCloudFormationException exception) when (exception.Message == "No updates are to be performed.")
        {
            _console.Out.WriteLine("No updates are to be performed.");
                
            return true;
        }
    }

    private async Task<bool> UpdateStackWithChangeSet(Stack stack, DeployOptions deployOptions)
    {
        var changeSetName = GetChangeSetName(deployOptions);

        if (await ChangeSetExists(changeSetName))
        {
            _console.Out.WriteLine("ChangeSet already exists.");
            return false;
        }
        
        await WaitForStackStatusChange(false, null, StackStatuses.WaitToBeginUpdate);

        var request = new CreateChangeSetRequest
        {
            ChangeSetType = ChangeSetType.UPDATE,
            ChangeSetName = changeSetName,
            StackName = GetStackName(),
            Capabilities = GetCapabilities(),
            Tags = GetTags(),
            TemplateBody = GetTemplateBody(deployOptions.Template),
            Parameters = await (deployOptions.UsePreviousParameters
                ? GetParameters(deployOptions.Parameters, stack)
                : GetParameters(deployOptions.Parameters))
        };
        
        LogParameters(request.Parameters);
        LogTemplateBody(request.TemplateBody);

        var response = await Client.CreateChangeSetAsync(request);

        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return false;
        }
        
        return await WaitForChangeSetStatusChange(changeSetName, ChangeSetStatuses.SuccessfulCreate,
            ChangeSetStatuses.WaitToEndCreate);
    }

    async Task<string?> ICloudProvisioningService.GetProperty(GetOptions getOptions)
    {
        var stack = await GetStack();

        if (getOptions.PropertyName == "::StackName")
        {
            return stack?.StackName;
        }

        return stack?
            .Outputs
            .SingleOrDefault(output => output.OutputKey == getOptions.PropertyName)?
            .OutputValue;
    }
        
    async Task<bool> ICloudProvisioningService.IsDeployed()
    {
        var stack = await GetStack();

        return stack != null;
    }

    async Task<bool> ICloudProvisioningService.IsResourceDeployed(string resourceId)
    {
        var stack = await GetStack();
        
        if (stack == null)
        {
            return false;
        }
        
        try
        {
            var request = new DescribeStackResourceRequest
            {
                StackName = stack.StackName,
                LogicalResourceId = resourceId,
            };

            var response = await Client.DescribeStackResourceAsync(request);

            return response.HttpStatusCode == HttpStatusCode.OK;
        }
        catch (Exception exception)
            when (exception.Message == $"Resource {resourceId} does not exist for stack {stack.StackName}")
        {
            return false;
        }
    }
    
    async Task<bool> ICloudProvisioningService.Deploy(DeployOptions deployOptions)
    {
        var stack = await GetStack();

        if (GetUseChangeSet(deployOptions))
        {
            if (stack != null)
            {
                return await UpdateStackWithChangeSet(stack, deployOptions);
            }
            
            return await CreateStackWithChangeSet(deployOptions);
        }
        
        if (stack != null)
        {
            return await UpdateStack(stack, deployOptions);
        }

        return await CreateStack(deployOptions);
    }
}