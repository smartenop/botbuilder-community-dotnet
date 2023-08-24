using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Bot.Schema;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Bot.Builder.Community.Cards.Translation
{
    public class AdaptiveCardTranslatorGPT
    {

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

        private static string Trim(string text)
        {
            return text.Trim(new Char[] { ' ', '*', '\\', '"', '-' });
        }

        public AzureOpenAIConfig AzureOpenAIConfig { get; set; }
        public static async Task<T> TranslateAsync<T>(
            T card,
            AzureOpenAIConfig config,
            AdaptiveCardTranslatorSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (string.IsNullOrWhiteSpace(config.SubscriptionKey))
            {
                throw new ArgumentNullException(nameof(config.SubscriptionKey));
            }

            if (string.IsNullOrWhiteSpace(config.TargetLocale))
            {
                throw new ArgumentNullException(nameof(config.TargetLocale));
            }

            return await TranslateAsync(
                card,
                async (inputs, innerCancellationToken) =>
                {
                    if (inputs.Count() == 1)
                        return await TranslateWithGPT4(inputs.First(), config);

                    return await TranslateWithGPT4(inputs.ToList(), config);
                },
                settings,
                cancellationToken).ConfigureAwait(false);
        }

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

            if (string.IsNullOrWhiteSpace(config.SubscriptionKey))
            {
                throw new ArgumentNullException(nameof(config.SubscriptionKey));
            }

            if (string.IsNullOrWhiteSpace(config.TargetLocale))
            {
                throw new ArgumentNullException(nameof(config.TargetLocale));
            }

            if (inputs.Count() == 1)
                return await TranslateWithGPT4(inputs.First(), config);

            return await TranslateWithGPT4(inputs, config);
        }

        private static async Task<List<string>> TranslateWithGPT4(
          string input,
          AzureOpenAIConfig config
      )
        {
            List<TranslationSamples> samples = new List<TranslationSamples>();
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Temperature = 0,
                MaxTokens = 2024,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                NucleusSamplingFactor = 1
            };
            if (config.TargetLocale == "ar")
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, config.PromptSingelAR ?? config.PromptAR));
            }
            else
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, config.PromptSingleEN ?? config.PromptEN));
            }

            Console.WriteLine("User Prompt" + input);

            OpenAIClient client = new OpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.SubscriptionKey));

            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, input));

            Azure.Response<ChatCompletions> completionsResponse = await client.GetChatCompletionsAsync(config.DeployementID, chatCompletionsOptions);
            string completion = completionsResponse.Value.Choices[0].Message.Content;

            Console.WriteLine("ChatGPT4 response : " + completion);

            char[] cArray = { '"' };

            return completion.Split('#').Select(x => x.Trim('"').Replace("\\n-", Environment.NewLine)).ToList().Select(x => Trim(x)).ToList();
        }

        private static async Task<List<string>> TranslateWithGPT4(
           List<string> inputs,
           AzureOpenAIConfig config
       )
        {
            var prompt = JsonConvert.SerializeObject(inputs);
            List<TranslationSamples> samples = new List<TranslationSamples>();
            var chatCompletionsOptions = new ChatCompletionsOptions()
            {
                Temperature = 0,
                MaxTokens = 2024,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                NucleusSamplingFactor = 1
            };

            if (config.TargetLocale == "ar")
            {
                samples.Add(new TranslationSamples
                {
                    AsistantText = "مرحبا",
                    userText = "Hello"
                });
                samples.Add(new TranslationSamples
                {
                    AsistantText = "يبدو أنك ترغب في الإبلاغ عن الأعراض التالية \\: n\\-الشعور بالمرض أو الغثيان\\n",
                    userText = "It sounds like you would like to report the following symptoms:\n- Feeling sick or queasy\n"
                });
                samples.Add(new TranslationSamples
                {
                    AsistantText = "عندى صداع.",
                    userText = "I have a headache."
                });
                samples.Add(new TranslationSamples
                {
                    AsistantText = "يكمل",
                    userText = "continue"
                });

                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, config.PromptAR));
            }
            else
            {
                samples.Add(new TranslationSamples
                {
                    userText = "مرحبا",
                    AsistantText = "Hello"
                });
                samples.Add(new TranslationSamples
                {
                    userText = "يبدو أنك ترغب في الإبلاغ عن الأعراض التالية \\: n\\-الشعور بالمرض أو الغثيان\\n",
                    AsistantText = "It sounds like you would like to report the following symptoms:\n- Feeling sick or queasy\n"
                });
                samples.Add(new TranslationSamples
                {
                    userText = "عندى صداع.",
                    AsistantText = "I have a headache."
                });
                samples.Add(new TranslationSamples
                {
                    userText = "يكمل",
                    AsistantText = "continue"
                });

                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, config.PromptEN));
            }

            Console.WriteLine("User Prompt : " + prompt);

            OpenAIClient client = new OpenAIClient(new Uri(config.Endpoint), new AzureKeyCredential(config.SubscriptionKey));

            foreach (var message in samples)
            {
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, message.userText));
                chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, message.AsistantText));
            }

            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));


            Azure.Response<ChatCompletions> completionsResponse = await client.GetChatCompletionsAsync(config.DeployementID, chatCompletionsOptions);
            string completion = Trim(completionsResponse.Value.Choices[0].Message.Content);

            Console.WriteLine("ChatGPT4 response" + completion);

            char[] cArray = { '"' };
            try
            {
                string[] tranlations = JsonConvert.DeserializeObject<string[]>(completion);

                return tranlations.Select(x => x.Trim('"').Replace("\\n-", Environment.NewLine)).ToList().Select(x => Trim(x)).ToList();
            }
            catch (Exception ex)
            {

            }

            return completion.Split('#').Select(x => x.Trim('"').Replace("\\n-", Environment.NewLine)).ToList().Select(x => Trim(x)).ToList();

        }

        private static async Task<List<string>> TranslateWithGPT(
            List<string> inputs,
            AzureOpenAIConfig config
        )
        {
            var prompt = @"
            {instructionEN}
            User:
            [
                ""مرحبا"" ,""عندى صداع"" , ""يبدو أنك ترغب في الإبلاغ عن الأعراض التالية:  \n  شعور المرض أو الغثيان \n""
]


            Assistant:
                ""Hello # I have a headache # It sounds like you would like to report the following symptoms:\n- Feeling sick or queasy\n""

            User: {userInput}

            Assistant:

            ";

            prompt = prompt.Replace("{instructionEN}", config.PromptEN);

            if (config.TargetLocale == "ar")
            {
                prompt = @"
            {instructionAR}
            User:
            [
                ""Hello"", ""I have a headache"",""It sounds like you would like to report the following symptoms:\n- Feeling sick or queasy\n""
            ]
            Assistant:
                ""مرحبا # عندى صداع # يبدو أنك ترغب في الإبلاغ عن الأعراض التالية:  \n  شعور المرض أو الغثيان \n""
            User: {userInput}
            Assistant:

            ";

                prompt = prompt.Replace("{instructionAR}", config.PromptAR);

            }


            var json = JsonConvert.SerializeObject(inputs);
            prompt = prompt.Replace("{userInput}", json);

            Console.WriteLine("User Prompt" + prompt);


            var completionOptions = new CompletionsOptions
            {
                Prompts = { prompt },
                MaxTokens = 2048,
                Temperature = 0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                NucleusSamplingFactor = 1 // Top P
            };

            var openAIClient = new OpenAIClient(new Uri(config.Endpoint), new Azure.AzureKeyCredential(config.SubscriptionKey));
            Completions response = await openAIClient.GetCompletionsAsync(config.DeployementID, completionOptions);

            Console.WriteLine("ChatGPT response" + response.Choices.First().Text);

            char[] cArray = { '"' };

            return response.Choices.First().Text.Split('#').Select(x => x.Trim('"').Replace("\\n-", Environment.NewLine)).ToList().Select(x => Trim(x)).ToList();
        }

        private static async Task<List<string>> TranslateWithGPT(
            string input,
            AzureOpenAIConfig config
        )
        {
            var prompt = @"
            {instructionEN}
           
            User: {userInput}

            Assistant:

            ";

            prompt = prompt.Replace("{instructionEN}", config.PromptSingleEN ?? config.PromptEN);

            if (config.TargetLocale == "ar")
            {
                prompt = @"
            {instructionAR}
            
            User: {userInput}

            Assistant:

            ";

                prompt = prompt.Replace("{instructionAR}", config.PromptSingelAR ?? config.PromptAR);

            }

            prompt = prompt.Replace("{userInput}", input);

            Console.WriteLine("User Prompt" + prompt);


            var completionOptions = new CompletionsOptions
            {
                Prompts = { prompt },
                MaxTokens = 2048,
                Temperature = 0f,
                FrequencyPenalty = 0.0f,
                PresencePenalty = 0.0f,
                NucleusSamplingFactor = 1 // Top P
            };

            var openAIClient = new OpenAIClient(new Uri(config.Endpoint), new Azure.AzureKeyCredential(config.SubscriptionKey));
            Completions response = await openAIClient.GetCompletionsAsync(config.DeployementID, completionOptions);

            Console.WriteLine("ChatGPT response" + response.Choices.First().Text);

            char[] cArray = { '"' };

            return response.Choices.First().Text.Split('#').Select(x => x.Replace("\\n-", Environment.NewLine)).ToList().Select(x => Trim(x)).ToList();
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
            }
            else
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

    public class TranslationSamples
    {
        public string userText { get; set; }
        public string AsistantText { get; set; }
    }
}
