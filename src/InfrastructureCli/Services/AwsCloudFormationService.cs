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
        internal static string OnFailure = "ROLLBACK";

        public static void ShouldNotEnableTerminationProtection() => EnableTerminationProtection = false;
        public static void ShouldEnableTerminationProtection() => EnableTerminationProtection = true;

        public static void ShouldRollbackOnFailure() => OnFailure = "ROLLBACK";
        public static void ShouldDeleteOnFailure() => OnFailure = "DELETE";
        public static void ShouldDoNothingOnFailure() => OnFailure = "DO_NOTHING";
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

    private static Parameter GetParameter(KeyValuePair<string, string> parameter)
    {
        return new Parameter
        {
            ParameterKey = parameter.Key,
            ParameterValue = parameter.Value
        };
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

    private static List<Parameter> GetParameters(Dictionary<string, string> parameters)
    {
        return parameters.Select(GetParameter).ToList();
    }

    private static List<string> GetCapabilities(AwsCloudFormationTemplateOptions templateOptions)
    {
        return templateOptions.Capabilities?.ToList() ?? new List<string>();
    }

    private static bool GetUseChangeSet(AwsCloudFormationTemplateOptions templateOptions)
    {
        return templateOptions.UseChangeSet ?? false;
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
        
    private static List<Parameter> GetParameters(Dictionary<string, string> parameters, Stack stack)
    {
        var parameterDictionary = GetParameters(parameters)
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
    
    private static async Task<bool> WaitForStatusChange(IConsole console, AwsCloudFormationTemplateOptions templateOptions, string? successStatus, params string[] loopStatuses)
    {
        var currentStatus = loopStatuses[0];
            
        while (loopStatuses.Contains(currentStatus))
        {
            console.WriteLine("Wait for 30 seconds");
                
            await Task.Delay(TimeSpan.FromSeconds(30));
                
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
            OnFailure = OnCreateWithoutChangeSet.OnFailure,
            TemplateBody = GetTemplateBody(options.Template),
            Parameters = GetParameters(options.Parameters)
        };
            
        LogParameters(console, request.Parameters);
        LogTemplateBody(console, request.TemplateBody);

        var response = await Client.CreateStackAsync(request);
            
        if (response.HttpStatusCode != HttpStatusCode.OK)
        {
            return false;
        }
            
        return await WaitForStatusChange(console, templateOptions, SuccessfulCreateStatus, WaitToEndCreateStatuses);
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
            Parameters = GetParameters(options.Parameters)
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
            await WaitForStatusChange(console, templateOptions, null, WaitToBeginUpdateStatuses);
            
            var request = new UpdateStackRequest
            {
                StackName = GetStackName(templateOptions),
                Capabilities = GetCapabilities(templateOptions),
                Tags = GetTags(templateOptions),
                TemplateBody = GetTemplateBody(options.Template),
                Parameters = options.UsePreviousParameters
                    ? GetParameters(options.Parameters, stack)
                    : GetParameters(options.Parameters)
            };
            
            LogParameters(console, request.Parameters);
            LogTemplateBody(console, request.TemplateBody);

            var response = await Client.UpdateStackAsync(request);

            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return false;
            }

            return await WaitForStatusChange(console, templateOptions, SuccessfulUpdateStatus, WaitToEndUpdateStatuses);
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
                Parameters = options.UsePreviousParameters
                    ? GetParameters(options.Parameters, stack)
                    : GetParameters(options.Parameters)
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
        var templateOptions = DeserializeTemplateOptions(getOptions.Configuration.TemplateOptions);

        var stack = await GetStack(templateOptions);

        return stack?
            .Outputs
            .SingleOrDefault(output => output.OutputKey == getOptions.PropertyName)?
            .OutputValue;
    }
        
    internal static async Task<bool> Deploy(IConsole console, DeployOptions deployOptions)
    {
        var templateOptions = DeserializeTemplateOptions(deployOptions.Configuration.TemplateOptions);

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