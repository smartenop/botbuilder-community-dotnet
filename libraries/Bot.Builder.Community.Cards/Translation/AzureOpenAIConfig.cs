using System;
using System.Net.Http;
using Azure.AI.OpenAI;

namespace Bot.Builder.Community.Cards.Translation
{
    public class AzureOpenAIConfig
    {
        public AzureOpenAIConfig(string endpoint, string key, string deployementID)
        {
            AzureEndpoint = endpoint;
            SubscriptionKey = key;
            DeployementID = deployementID;
        }

        public string AzureEndpoint { get; set; }
        public string SubscriptionKey { get; set; }
        public string DeployementID { get; set; }

        public string PromptAR { get; set; }
        public string PromptEN { get; set; }
        public Uri Endpoint { get; set; }
        public Azure.AzureKeyCredential AzureKeyCredential { get; set; }
        public OpenAIClient Client { get; set; }
    }
}
