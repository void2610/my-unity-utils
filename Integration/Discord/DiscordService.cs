#if DISCORD_ENABLE && (!UNITY_WEBGL || UNITY_EDITOR)
using System;
using Discord.Sdk;

namespace Void2610.UnityTemplate.Discord
{
    public class DiscordService : IDisposable
    {
        private readonly Client _client;
        private readonly DiscordRichPresenceService _richPresenceService;

        public DiscordService(ulong clientId, string gameName, string url, string defaultDetails)
        {
            _client = new Client();
            _client.SetApplicationId(clientId);
            _richPresenceService = new DiscordRichPresenceService(_client, gameName, url);
            _richPresenceService.SetDetails(defaultDetails);
        }

        public void SetStageProgress(int currentStage, int maxStage)
        {
            _richPresenceService.SetPartySize("stage_progress", currentStage, maxStage);
        }

        public void SetCurrentStage(string activityState)
        {
            _richPresenceService.SetState(activityState);
        }

        public void SetDetails(string details)
        {
            _richPresenceService.SetDetails(details);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
#else
using System;

namespace Void2610.UnityTemplate.Discord
{
    public class DiscordService : IDisposable
    {
        public DiscordService(ulong clientId, string gameName, string url, string defaultDetails) { }
        public void SetStageProgress(int currentStage, int maxStage) { }
        public void SetCurrentStage(string activityState) { }
        public void SetDetails(string details) { }
        public void Dispose() { }
    }
}
#endif
