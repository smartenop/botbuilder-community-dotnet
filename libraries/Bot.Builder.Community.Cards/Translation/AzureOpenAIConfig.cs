using System;
using System.Net.Http;
using Azure.AI.OpenAI;

namespace Bot.Builder.Community.Cards.Translation
{
    public class AzureOpenAIConfig
    {
        public AzureOpenAIConfig(string endpoint, string key, string deployementID, string promptAR, string promptEN, string promptSingleAR, string promptSingleEN, string targetLocale = null)
        {
            Endpoint = endpoint;
            SubscriptionKey = key;
            DeployementID = deployementID;
            PromptAR = promptAR;
            PromptEN = promptEN;
            TargetLocale = targetLocale;
            PromptSingleEN = promptSingleEN;
            PromptSingelAR = promptSingleAR;
        }

        public string Endpoint { get; set; }
        public string SubscriptionKey { get; set; }
        public string DeployementID { get; set; }
        public string PromptAR { get; set; }
        public string PromptEN { get; set; }
        public string PromptSingelAR { get; set; }
        public string PromptSingleEN { get; set; }
        public string TargetLocale { get; set; }
    }
}
