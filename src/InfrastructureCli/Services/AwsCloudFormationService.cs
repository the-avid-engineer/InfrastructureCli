using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.CommandLine;
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

        private static async Task<Stack?> GetStack(DeployOptions options)
        {
            var stackName = GetStackName(options.Configuration);
            
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

        private static async Task<bool> WaitForStatusChange(IConsole console, DeployOptions options, string successStatus, params string[] loopStatuses)
        {
            var currentStatus = loopStatuses[0];
            
            while (loopStatuses.Contains(currentStatus))
            {
                console.WriteLine("Wait for 30 seconds");
                
                await Task.Delay(TimeSpan.FromSeconds(30));
                
                var stack = await GetStack(options);

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
                Parameters = GetParameters(options.Parameters)
            };

            var response = await Client.CreateStackAsync(request);
            
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            
            return await WaitForStatusChange(console, options, "CREATE_COMPLETE", "CREATE_IN_PROGRESS");
        }

        private static async Task<bool> UpdateStack(IConsole console, Stack stack, DeployOptions options)
        {
            try
            {
                var request = new UpdateStackRequest
                {
                    StackName = GetStackName(options.Configuration),
                    Tags = GetTags(options.Configuration),
                    Parameters = options.UsePreviousParameters
                        ? GetParameters(options.Parameters, stack)
                        : GetParameters(options.Parameters)
                };

                if (options.UsePreviousTemplate)
                {
                    request.UsePreviousTemplate = true;
                }
                else
                {
                    request.TemplateBody = GetTemplateBody(options.Template);
                }

                var response = await Client.UpdateStackAsync(request);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                return await WaitForStatusChange(console, options, "UPDATE_COMPLETE", "UPDATE_IN_PROGRESS", "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS");
            }
            catch (AmazonCloudFormationException exception) when (exception.Message == "No updates are to be performed.")
            {
                return false;
            }
        }

        public static async Task<bool> Deploy(IConsole console, DeployOptions options)
        {
            var expectedAccountId = options.Configuration.Metas.GetValueOrDefault("AccountId");
            var currentAccountId = await AwsSecurityTokenService.GetAccountId();

            if (currentAccountId != expectedAccountId)
            {
                throw new AwsCloudFormationException($"This configuration is for account {expectedAccountId} but this command is running for account {currentAccountId}.");
            }

            var expectedRegion = options.Configuration.Metas.GetValueOrDefault("Region");
            var currentRegion = FallbackRegionFactory.GetRegionEndpoint().SystemName;

            if (currentRegion != expectedRegion)
            {
                throw new AwsCloudFormationException($"This configuration is for region {expectedRegion} but this command is running for region {currentRegion}.");
            }

            var stack = await GetStack(options);

            if (stack != null)
            {
                return await UpdateStack(console, stack, options);
            }

            return await CreateStack(console, options);
        }
    }
}
