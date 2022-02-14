﻿using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;

namespace InfrastructureCli.Services
{
    internal static class AwsCloudFormationService
    {
        private class AwsCloudFormationException : Exception
        {
            public AwsCloudFormationException(string message) : base(message)
            {
            }
        }
        
        private static readonly IAmazonCloudFormation Client = new AmazonCloudFormationClient();

        private static Parameter GetParameter(KeyValuePair<string, string> parameter)
        {
            return new()
            {
                ParameterKey = parameter.Key,
                ParameterValue = parameter.Value,
            };
        }

        private static Parameter ReuseParameter(Parameter parameter)
        {
            return new()
            {
                ParameterKey = parameter.ParameterKey,
                UsePreviousValue = true,
            };
        }

        private static Tag GetTag(KeyValuePair<string, string> tag)
        {
            return new()
            {
                Key = tag.Key,
                Value = tag.Value,
            };
        }

        private static string GetStackName(Configuration configuration)
        {
            return configuration.Label;
        }

        private static List<Parameter> GetParameters(Dictionary<string, string> parameters)
        {
            return parameters.Select(GetParameter).ToList();
        }

        private static List<string> GetCapabilities(Dictionary<string, JsonElement> templateOptions)
        {
            return templateOptions.TryGetValue("Capabilities", out var capabilitiesElement)
                ? JsonService.Convert<JsonElement, List<string>>(capabilitiesElement)
                : new List<string>();
        }
        
        private static List<Parameter> GetParameters(Dictionary<string, string> parameters, Stack stack)
        {
            var parameterDictionary = GetParameters(parameters)
                .ToDictionary(parameter => parameter.ParameterKey);
            
            foreach (var parameter in stack.Parameters)
            {
                if (parameterDictionary.ContainsKey(parameter.ParameterKey) == false)
                {
                    parameterDictionary.Add(parameter.ParameterKey, ReuseParameter(parameter));
                }
            }
            
            return parameterDictionary.Values.ToList();
        }

        private static List<Tag> GetTags(Configuration configuration)
        {
            return configuration.Tags.Select(GetTag).ToList();
        }

        private static string GetTemplateBody(JsonElement template)
        {
            return JsonService.Serialize(template);
        }

        private static async Task<Stack?> GetStack(Configuration configuration)
        {
            var stackName = GetStackName(configuration);
            
            try
            {
                var request = new DescribeStacksRequest
                {
                    StackName = stackName,
                };

                var response = await Client.DescribeStacksAsync(request);

                return response.Stacks.FirstOrDefault();
            }
            catch (Exception exception) when (exception.Message == $"Stack with id {stackName} does not exist")
            {
                return null;
            }
        }

        private static async Task<bool> WaitForStatusChange(IConsole console, Configuration configuration, string successStatus, params string[] loopStatuses)
        {
            var currentStatus = loopStatuses[0];
            
            while (loopStatuses.Contains(currentStatus))
            {
                console.WriteLine("Wait for 30 seconds");
                
                await Task.Delay(TimeSpan.FromSeconds(30));
                
                var stack = await GetStack(configuration);

                if (stack == null)
                {
                    throw new AwsCloudFormationException("Stack does not exist.");
                }

                currentStatus = stack.StackStatus;
                
                console.WriteLine($"Status is {currentStatus}");
            }

            return successStatus == currentStatus;
        }

        private static async Task<bool> CreateStack(IConsole console, DeployOptions options)
        {
            var request = new CreateStackRequest
            {
                StackName = GetStackName(options.Configuration),
                Tags = GetTags(options.Configuration),
                TemplateBody = GetTemplateBody(options.Template),
                Parameters = GetParameters(options.Parameters),
                Capabilities = GetCapabilities(options.TemplateOptions),
            };

            var response = await Client.CreateStackAsync(request);
            
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            
            return await WaitForStatusChange(console, options.Configuration, "CREATE_COMPLETE", "CREATE_IN_PROGRESS");
        }

        private static async Task<bool> UpdateStack(IConsole console, Stack stack, DeployOptions options)
        {
            try
            {
                var request = new UpdateStackRequest
                {
                    StackName = GetStackName(options.Configuration),
                    Tags = GetTags(options.Configuration),
                    TemplateBody = GetTemplateBody(options.Template),
                    Parameters = options.UsePreviousParameters
                        ? GetParameters(options.Parameters, stack)
                        : GetParameters(options.Parameters),
                    Capabilities = GetCapabilities(options.TemplateOptions),
                };
                
                console.Out.WriteLine("Parameters:");
                
                foreach (var parameter in request.Parameters)
                {
                    if (parameter.UsePreviousValue)
                    {
                        console.Out.WriteLine($"{parameter.ParameterKey} = (Use Previous Value)");
                    }
                    else
                    {
                        console.Out.WriteLine($"{parameter.ParameterKey} = {parameter.ParameterValue}");
                    }
                }

                console.Out.WriteLine();
                
                console.Out.WriteLine("Template Body:");
                
                console.Out.WriteLine(request.TemplateBody);

                var response = await Client.UpdateStackAsync(request);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                return await WaitForStatusChange(console, options.Configuration, "UPDATE_COMPLETE", "UPDATE_IN_PROGRESS", "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS");
            }
            catch (AmazonCloudFormationException exception) when (exception.Message == "No updates are to be performed.")
            {
                console.Out.WriteLine("No updates are to be performed.");
                
                return true;
            }
        }

        public static async Task<string?> Get(GetOptions options)
        {
            var stack = await GetStack(options.Configuration);

            return stack?
                .Outputs
                .SingleOrDefault(output => output.ExportName == options.PropertyName)?
                .OutputValue;
        }
        
        public static async Task<bool> Deploy(IConsole console, DeployOptions options)
        {
            var stack = await GetStack(options.Configuration);

            if (stack != null)
            {
                return await UpdateStack(console, stack, options);
            }

            return await CreateStack(console, options);
        }
    }
}
