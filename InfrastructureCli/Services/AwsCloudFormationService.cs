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

        private static List<Parameter> GetParameters(Configuration configuration)
        {
            return configuration.Parameters.Select(GetParameter).ToList();
        }

        private static List<Tag> GetTags(Configuration configuration)
        {
            return configuration.Tags.Select(GetTag).ToList();
        }

        private static string GetTemplateBody(JsonElement template)
        {
            return JsonService.Serialize(template);
        }

        private static async Task<bool> StackExists(Configuration configuration)
        {
            var stackName = GetStackName(configuration);

            try
            {
                var request = new GetTemplateSummaryRequest
                {
                    StackName = stackName
                };

                var response = await Client.GetTemplateSummaryAsync(request);

                return response.HttpStatusCode == HttpStatusCode.OK;
            }
            catch (AmazonCloudFormationException exception) when (exception.Message == $"Stack with id {stackName} does not exist")
            {
                return false;
            }
        }

        private static async Task<bool> CreateStack(Configuration configuration, JsonElement template)
        {
            var request = new CreateStackRequest
            {
                StackName = GetStackName(configuration),
                Parameters = GetParameters(configuration),
                Tags = GetTags(configuration),
                TemplateBody = GetTemplateBody(template),
            };

            var response = await Client.CreateStackAsync(request);

            return response.HttpStatusCode == HttpStatusCode.Created;
        }

        private static async Task<bool> UpdateStack(Configuration configuration, JsonElement template)
        {
            try
            {
                var request = new UpdateStackRequest
                {
                    StackName = GetStackName(configuration),
                    Parameters = GetParameters(configuration),
                    Tags = GetTags(configuration),
                    TemplateBody = GetTemplateBody(template),
                };

                var response = await Client.UpdateStackAsync(request);

                return response.HttpStatusCode == HttpStatusCode.Accepted;
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

            var stackExists = await StackExists(configuration);

            if (stackExists)
            {
                return await UpdateStack(configuration, template);
            }

            return await CreateStack(configuration, template);
        }
    }
}
