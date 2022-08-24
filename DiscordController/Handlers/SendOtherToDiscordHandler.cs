using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AllianceDiscordController;
using AlliancesPlugin.Alliances;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace DiscordController.Handlers
{
    public static class SendOtherToDiscordHandler
    {
        public static async void SendToDiscord(string JsonMessage)
        {
            var message = JsonConvert.DeserializeObject<AllianceSendToDiscord>(JsonMessage);

            if (Program.UsedTokens.TryGetValue(message.BotToken, out var inUse))
            {
                await SendMessageToDiscord(message, inUse);
            }
            else
            {
                DiscordClient bot;
                DiscordConfiguration config = new DiscordConfiguration
                {
                    Token = message.BotToken,
                    TokenType = TokenType.Bot,
                };
                bot = new DiscordClient(config);
                await bot.ConnectAsync();
                bot.MessageCreated += AllianceChatHandler.Discord_AllianceMessage;
                Program.UsedTokens.Add(message.BotToken, inUse);
                await SendMessageToDiscord(message, bot);
            }
        }

        public static async Task SendMessageToDiscord(AllianceSendToDiscord Message, DiscordClient Discord)
        {
            DiscordChannel Channel;
            if (Program.StoredChannels.TryGetValue(Message.ChannelId, out var channel))
            {
                Channel = channel;
            }
            else
            {
                Channel = await Discord.GetChannelAsync(Message.ChannelId);
                Program.StoredChannels.Add(Message.ChannelId, Channel);
            }
            if (Message.DoEmbed)
            {

                var embed = new DiscordEmbedBuilder
                {
                    Title = Message.SenderPrefix,
                    Description = Message.MessageText,
                    Color = new DiscordColor(Message.EmbedR, Message.EmbedG, Message.EmbedB)


                };
                Channel.SendMessageAsync(embed);
            }
            else
            {
                var bot = Discord.SendMessageAsync(Channel, $"{Message.SenderPrefix} {Message.MessageText.Replace("/n", "\n")}").Result.Author.Id;
            }
        }
    }
}
