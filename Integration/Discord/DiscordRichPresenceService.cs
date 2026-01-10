#if DISCORD_ENABLE && (!UNITY_WEBGL || UNITY_EDITOR)
using UnityEngine;
using Discord.Sdk;

namespace Void2610.UnityTemplate.Discord
{
    public class DiscordRichPresenceService
    {
        private readonly Client _client;
        private readonly Activity _activity;

        public DiscordRichPresenceService(Client client, string name, string url)
        {
            _client = client;
            _activity = new Activity();
            _activity.SetName(name);
            _activity.SetType(ActivityTypes.Playing);
            _activity.SetDetailsUrl(url);

            UpdateRichPresence();
        }

        public void SetDetails(string details)
        {
            _activity.SetDetails(details);
            UpdateRichPresence();
        }

        public void SetState(string state)
        {
            _activity.SetState(state);
            UpdateRichPresence();
        }

        public void SetUrl(string url)
        {
            _activity.SetDetailsUrl(url);
            UpdateRichPresence();
        }

        public void SetPartySize(string id, int size, int max)
        {
            var party = new ActivityParty();
            party.SetId(id);
            party.SetCurrentSize(size);
            party.SetMaxSize(max);
            _activity.SetParty(party);
            UpdateRichPresence();
        }

        private void UpdateRichPresence()
        {
            _client.UpdateRichPresence(_activity, result =>
            {
                if (!result.Successful())
                    Debug.LogError($"Failed to update rich presence - {result.Error()}");
            });
        }
    }
}
#endif
