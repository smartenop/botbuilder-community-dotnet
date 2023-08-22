using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Azure.AI.OpenAI;

namespace Bot.Builder.Community.Cards.Translation
{
    public class AdaptiveCardTranslatorGPTOld
    {
        private const string AzureOpenAIEndpoint = "AzureOpenAIEndpoint";
        private const string AzureOpenAIKey = "AzureOpenAIKey";
        private const string AzureOpenAIDeployementID = "AzureOpenAIDeployementID";


        public AdaptiveCardTranslatorGPTOld(IConfiguration configuration)
        {
            AzureOpenAIConfig = new AzureOpenAIConfig(AzureOpenAIEndpoint, AzureOpenAIKey, AzureOpenAIDeployementID);

            AzureOpenAIConfig.Endpoint = new Uri(AzureOpenAIConfig.AzureEndpoint);
            AzureOpenAIConfig.AzureKeyCredential = new Azure.AzureKeyCredential(AzureOpenAIConfig.SubscriptionKey);
            AzureOpenAIConfig.Client = new OpenAIClient(AzureOpenAIConfig.Endpoint, AzureOpenAIConfig.AzureKeyCredential);

            AzureOpenAIConfig.PromptAR = "Translate the following text to Arabic:\n";
            AzureOpenAIConfig.PromptEN = "Translate the following text to English:\n";
        }

        // TODO: Move AdaptiveCardTranslator.DefaultSettings to AdaptiveCardTranslatorSettings.Default
        public static AdaptiveCardTranslatorSettings DefaultSettings => new AdaptiveCardTranslatorSettings
        {
            PropertiesToTranslate = new[]
            {
                AdaptiveProperties.Value,
                AdaptiveProperties.Text,
                AdaptiveProperties.AltText,
                AdaptiveProperties.FallbackText,
                AdaptiveProperties.DisplayText,
                AdaptiveProperties.Title,
                AdaptiveProperties.Placeholder,
                AdaptiveProperties.Data,
            },
        };

        public AdaptiveCardTranslatorSettings Settings { get; set; } = DefaultSettings;

        public AzureOpenAIConfig AzureOpenAIConfig { get; set; }


        public static async Task<List<string>> TranslateTextsAsync(
           List<string> inputs,
           AzureOpenAIConfig config,
           AdaptiveCardTranslatorSettings settings = null,
           CancellationToken cancellationToken = default)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            //var requestBody = JsonConvert.SerializeObject(inputs.Select(input => new { Text = input }));

            var completionOptions = new CompletionsOptions
            {
                Prompts = { config.PromptAR },
                MaxTokens = 128,
                Temperature = 0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                NucleusSamplingFactor = 1 // Top P
            };

            Completions response = await config.Client.GetCompletionsAsync(config.DeployementID, completionOptions);

            var result = response.Choices.Select(translatorResponse => translatorResponse?.Text).ToList();

            return result;
        }

        public static async Task<List<string>> TranslateAsync<T>(
            T card,
            AzureOpenAIConfig config,
            AdaptiveCardTranslatorSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var completionOptions = new CompletionsOptions
            {
                Prompts = { config.PromptAR },
                MaxTokens = 128,
                Temperature = 0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                NucleusSamplingFactor = 1 // Top P
            };

            Completions response = await config.Client.GetCompletionsAsync(config.DeployementID, completionOptions);

            var result = response.Choices.Select(translatorResponse => translatorResponse?.Text).ToList();

            return result;
        }



        private static List<JToken> GetTokensToTranslate(
            JObject cardJObject,
            AdaptiveCardTranslatorSettings settings)
        {
            var tokens = new List<JToken>();

            // Find potential strings to translate
            foreach (var token in cardJObject.Descendants().Where(token => token.Type == JTokenType.String))
            {
                var parent = token.Parent;

                if (parent != null)
                {
                    var shouldTranslate = false;
                    var container = parent.Parent;

                    switch (parent.Type)
                    {
                        // If the string is the value of a property...
                        case JTokenType.Property:

                            var propertyName = (parent as JProperty).Name;

                            // container is assumed to be a JObject because it's the parent of a JProperty in this case
                            if (settings.PropertiesToTranslate?.Contains(propertyName) == true
                                && (propertyName != AdaptiveProperties.Value || IsValueTranslatable(container as JObject)))
                            {
                                shouldTranslate = true;
                            }

                            break;

                        // If the string is in an array...
                        case JTokenType.Array:

                            if (IsArrayElementTranslatable(container))
                            {
                                shouldTranslate = true;
                            }

                            break;
                    }

                    if (shouldTranslate)
                    {
                        tokens.Add(token);
                    }
                }
            }

            return tokens;
        }

        private static bool IsArrayElementTranslatable(JContainer arrayContainer)
            => (arrayContainer as JProperty)?.Name == AdaptiveProperties.Inlines;

        private static bool IsValueTranslatable(JObject valueContainer)
        {
            if (valueContainer is null)
            {
                return false;
            }

            var elementType = valueContainer[AdaptiveProperties.Type];
            var parent = valueContainer.Parent;
            var grandparent = parent?.Parent;

            // value should be translated in facts, imBack (for MS Teams), and Input.Text,
            // and ignored in Input.Date and Input.Time and Input.Toggle and Input.ChoiceSet and Input.Choice
            return (elementType?.Type == JTokenType.String
                    && elementType.IsOneOf(AdaptiveInputTypes.Text, ActionTypes.ImBack))
                || (elementType == null
                    && (grandparent as JProperty)?.Name == AdaptiveProperties.Facts
                    && parent.Type == JTokenType.Array);
        }
    }
}
