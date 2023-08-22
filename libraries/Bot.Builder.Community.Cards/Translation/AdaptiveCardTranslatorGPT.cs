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
    public class AdaptiveCardTranslatorGPT
    {
        private static string translatorInstructionEN;
        private static string translatorInstructionAR;
        private const string MicrosoftTranslatorKey = "MicrosoftTranslatorKey";
        private const string MicrosoftTranslatorLocale = "MicrosoftTranslatorLocale";
        private const string MicrosoftTranslatorEndpoint = "MicrosoftTranslatorEndpoint";
        private const string MicrosoftTranslatorRegionKey = "MicrosoftTranslatorRegionKey";
        private static string MicrosoftTranslatorRegionValue = "eastus";

        private static readonly Uri DefaultBaseAddress = new Uri("https://api.cognitive.microsofttranslator.com");

        private static readonly Lazy<HttpClient> LazyClient = new Lazy<HttpClient>(() => new HttpClient
        {
            BaseAddress = DefaultBaseAddress,
        });

        public AdaptiveCardTranslatorGPT(IConfiguration configuration)
        {
            MicrosoftTranslatorConfig = new MicrosoftTranslatorConfig(
                configuration[MicrosoftTranslatorKey],
                configuration[MicrosoftTranslatorLocale]);

            MicrosoftTranslatorRegionValue = configuration[MicrosoftTranslatorRegionKey];
            var endpoint = configuration[MicrosoftTranslatorEndpoint];
            translatorInstructionEN = configuration.GetValue<string>("translatorInstructionEN");
            translatorInstructionAR = configuration.GetValue<string>("translatorInstructionAR");

            if (!string.IsNullOrWhiteSpace(endpoint))
            {
                MicrosoftTranslatorConfig.HttpClient = new HttpClient
                {
                    BaseAddress = new Uri(endpoint),
                };
            }
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

        public MicrosoftTranslatorConfig MicrosoftTranslatorConfig { get; set; }

        public static async Task<T> TranslateAsync<T>(
            T card,
            MicrosoftTranslatorConfig config,
            AdaptiveCardTranslatorSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return await TranslateAsync(
                card,
                config.TargetLocale,
                config.SubscriptionKey,
                config.HttpClient,
                settings,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<List<string>> TranslateTextsAsync(
             List<string> inputs,
         MicrosoftTranslatorConfig config,
         AdaptiveCardTranslatorSettings settings = null,
         CancellationToken cancellationToken = default)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            return await TranslateTextsAsync(
                inputs,
                config.TargetLocale,
                config.SubscriptionKey,
                config.HttpClient,
                settings,
                cancellationToken).ConfigureAwait(false);
        }

        private static async Task<List<string>> TranslateWithGPT(
            List<string> inputs,
            string targetLocale
        )
        {
            var endpoint = new Uri("https://demo-moh-opeanai.openai.azure.com/");
            var credentials = new Azure.AzureKeyCredential("3fe866e8b4c14109983e3d9ab1716bd7");
            var deployementID = "Gpt3.5: Demo-MOH-gpt-35-turbo";
            var openAIClient = new OpenAIClient(endpoint, credentials);


            var prompt = @"
            {instructionEN}
            User:
            [
                ""مرحبا"", ""عندى صداع""
            ]


            Assistant: [
                ""Hello"", ""I have a headache""
            ]

            User: {userInput}

            Assistant:

            ";

            prompt = prompt.Replace("{instructionEN}", translatorInstructionEN);

            if (targetLocale == "ar")
            {
                prompt = @"
            {instructionAR}
            User:
            [
                ""Hello"", ""I have a headache""
            ]


            Assistant: [
                ""مرحبا"", ""عندى صداع""
            ]

            User: {userInput}

            Assistant:

            ";

                prompt = prompt.Replace("{instructionAR}", translatorInstructionAR);

            }

            Console.WriteLine("User Prompt" + prompt);

            var json = JsonConvert.SerializeObject(inputs.Select(input => new { input }));
            prompt = prompt.Replace("{userInput}", json);

            var completionOptions = new CompletionsOptions
            {
                Prompts = { prompt },
                MaxTokens = 1024,
                Temperature = 0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                NucleusSamplingFactor = 1 // Top P
            };

            Completions response = await openAIClient.GetCompletionsAsync(deployementID, completionOptions);

            Console.WriteLine("ChatGPT response" + response.Choices.First().Text);

            return response.Choices.First().Text.Split(',').ToList();
        }

        public static async Task<List<string>> TranslateTextsAsync(
           List<string> inputs,
           string targetLocale,
           string subscriptionKey,
           HttpClient httpClient = null,
           AdaptiveCardTranslatorSettings settings = null,
           CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentNullException(nameof(subscriptionKey));
            }

            if (string.IsNullOrWhiteSpace(targetLocale))
            {
                throw new ArgumentNullException(nameof(targetLocale));
            }

            return await TranslateWithGPT(inputs, targetLocale);
        }

        public static async Task<T> TranslateAsync<T>(
            T card,
            string targetLocale,
            string subscriptionKey,
            HttpClient httpClient = null,
            AdaptiveCardTranslatorSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(subscriptionKey))
            {
                throw new ArgumentNullException(nameof(subscriptionKey));
            }

            if (string.IsNullOrWhiteSpace(targetLocale))
            {
                throw new ArgumentNullException(nameof(targetLocale));
            }

            return await TranslateAsync(
                card,
                async (inputs, innerCancellationToken) =>
                {
                    return await TranslateWithGPT(inputs.ToList(), targetLocale);
                },
                settings,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T> TranslateAsync<T>(
            T card,
            TranslateOneDelegate translateOneAsync,
            AdaptiveCardTranslatorSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            if (translateOneAsync is null)
            {
                throw new ArgumentNullException(nameof(translateOneAsync));
            }

            return await TranslateAsync(
                card,
                async (inputs, innerCancellationToken) =>
                {
                    var tasks = inputs.Select(async input => await translateOneAsync(input, innerCancellationToken).ConfigureAwait(false));
                    return await Task.WhenAll(tasks).ConfigureAwait(false);
                },
                settings,
                cancellationToken).ConfigureAwait(false);
        }

        public static async Task<T> TranslateAsync<T>(
            T card,
            TranslateManyDelegate translateManyAsync,
            AdaptiveCardTranslatorSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            if (card == null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (translateManyAsync is null)
            {
                throw new ArgumentNullException(nameof(translateManyAsync));
            }

            JObject cardJObject;

            if (card is JObject jObject)
            {
                // If the card is already a JObject then we want to make sure
                // it gets copied instead of modified in place
                cardJObject = (JObject)jObject.DeepClone();
            } else
            {
                cardJObject = card.ToJObject(true) ?? throw new ArgumentException(
                    "The Adaptive Card is not an appropriate type or is serialized incorrectly.",
                    nameof(card));
            }

            var tokens = GetTokensToTranslate(cardJObject, settings ?? DefaultSettings);

            var translations = await translateManyAsync(
                tokens.Select(Convert.ToString),
                cancellationToken).ConfigureAwait(false);

            if (translations != null)
            {
                foreach (var (token, translation) in tokens.Zip(translations, Tuple.Create))
                {
                    if (!string.IsNullOrWhiteSpace(translation))
                    {
                        token.Replace(translation);
                    }
                }
            }

            return card.FromJObject(cardJObject);
        }

        public async Task<T> TranslateAsync<T>(
            T card,
            CancellationToken cancellationToken = default)
        {
            return await TranslateAsync(
                card,
                MicrosoftTranslatorConfig,
                Settings,
                cancellationToken).ConfigureAwait(false);
        }

        public async Task<T> TranslateAsync<T>(
            T card,
            string targetLocale,
            CancellationToken cancellationToken = default)
        {
            return await TranslateAsync(
                card,
                targetLocale,
                MicrosoftTranslatorConfig.SubscriptionKey,
                MicrosoftTranslatorConfig.HttpClient,
                Settings,
                cancellationToken).ConfigureAwait(false);
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
