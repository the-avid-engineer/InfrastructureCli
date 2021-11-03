using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using InfrastructureCli.Models;

namespace InfrastructureCli.Services
{
    internal static class AwsCloudFormationService
    {
        private static readonly IAmazonCloudFormation Client = new AmazonCloudFormationClient();

        private static Parameter GetParameter(KeyValuePair<string, string> parameter)
        {
            return new()
            {
                ParameterKey = parameter.Key,
                ParameterValue = parameter.Value,
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

        private static List<Parameter> ReuseParameters(Stack stack)
        {
            return stack.Parameters.Select(parameter => new Parameter
            {
                ParameterKey = parameter.ParameterKey,
                UsePreviousValue = true,
            }).ToList();
        }

        private static List<Parameter> GetParameters(Dictionary<string, string> parameters)
        {
            return parameters.Select(GetParameter).ToList();
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

        private static async Task<bool> WaitForStatusChange(Configuration configuration, string loopStatus, params string[] successStatuses)
        {
            var currentStatus = loopStatus;
            
            while (currentStatus == loopStatus)
            {
                Console.WriteLine("Wait for 30 seconds");
                
                await Task.Delay(TimeSpan.FromSeconds(30));
                
                var stack = await GetStack(configuration);

                if (stack == null)
                {
                    throw new Exception("Stack does not exist.");
                }

                currentStatus = stack.StackStatus;
                
                Console.WriteLine($"Status is {currentStatus}");
            }

            return successStatuses.Contains(currentStatus);
        }

        private static async Task<bool> CreateStack(Configuration configuration, JsonElement template)
        {
            var request = new CreateStackRequest
            {
                StackName = GetStackName(configuration),
                Tags = GetTags(configuration),
                TemplateBody = GetTemplateBody(template),
            };

            var response = await Client.CreateStackAsync(request);
            
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                return false;
            }
            
            return await WaitForStatusChange(configuration, "CREATE_IN_PROGRESS", "CREATE_COMPLETE");
        }

        private static async Task<bool> UpdateStack(Stack stack, Configuration configuration, JsonElement template)
        {
            try
            {
                var request = new UpdateStackRequest
                {
                    StackName = GetStackName(configuration),
                    Tags = GetTags(configuration),
                    TemplateBody = GetTemplateBody(template),
                    Parameters = ReuseParameters(stack),
                };

                var response = await Client.UpdateStackAsync(request);

                if (response.HttpStatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                return await WaitForStatusChange(configuration, "UPDATE_IN_PROGRESS", "UPDATE_COMPLETE_CLEANUP_IN_PROGRESS", "UPDATE_COMPLETE");
            }
            catch (AmazonCloudFormationException exception) when (exception.Message == "No updates are to be performed.")
            {
                return false;
            }
        }

        public static async Task<bool> Deploy(Configuration configuration, JsonElement template)
        {
            var expectedAccountId = configuration.Metas.GetValueOrDefault("AccountId");
            var currentAccountId = await AwsSecurityTokenService.GetAccountId();

            if (currentAccountId != expectedAccountId)
            {
                throw new Exception($"This configuration is for account {expectedAccountId} but this command is running for account {currentAccountId}.");
            }

            var expectedRegion = configuration.Metas.GetValueOrDefault("Region");
            var currentRegion = FallbackRegionFactory.GetRegionEndpoint().SystemName;

            if (currentRegion != expectedRegion)
            {
                throw new Exception($"This configuration is for region {expectedRegion} but this command is running for region {currentRegion}.");
            }

            var stack = await GetStack(configuration);

            if (stack != null)
            {
                return await UpdateStack(stack, configuration, template);
            }

            return await CreateStack(configuration, template);
        }
    }
}
