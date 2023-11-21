using Discord;
using Discord.WebSocket;
using Fumino_Winslayer;
using System;
using System.Threading;
using System.Threading.Tasks;
using static Fumino_Winslayer.Commands;
using static Fumino_Winslayer.Framework;

namespace BasicBot {
    class Program {
        private readonly DiscordSocketClient _client;

        static void Main(string[] args)
            => new Program()
                .MainAsync()
                .GetAwaiter()
                .GetResult();

        public Program() {
            var config = new DiscordSocketConfig {
                GatewayIntents = GatewayIntents.AllUnprivileged | 
                GatewayIntents.MessageContent | 
                GatewayIntents.GuildPresences |
                GatewayIntents.GuildMembers
            };

            _client = new DiscordSocketClient(config);

            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;
            _client.InteractionCreated += InteractionCreatedAsync;
        }

        public async Task MainAsync() {
            // Check if a config file is present, with the correct header.
            DebugWrite(null, "[Main]: " + "Bot is initializing.");
            LoadWinslayerConfig();
            DebugWrite(null, "[Main]: " + "Loaded config, populating binaries.");

            // First, get all binaries for !execute
            PopulateBinaries();
            DebugWrite(null, "[Main]: " + "Populated with " + Binaries.Length + " binaries.");

            // Set the nickname

            // Winslayer token
            await _client.LoginAsync(TokenType.Bot, botConfig.BotToken);
            await _client.StartAsync();

            // Block the program until it is closed.
            await Task.Delay(Timeout.Infinite);
        }

        private Task LogAsync(LogMessage log) {
            Console.WriteLine(log.ToString());
            return Task.CompletedTask;
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private Task ReadyAsync() {
            Console.WriteLine($"{_client.CurrentUser} is connected!");

            return Task.CompletedTask;
        }

        // This is not the recommended way to write a bot - consider
        // reading over the Commands Framework sample.
        private async Task MessageReceivedAsync(SocketMessage message) {
            // The bot should never respond to itself.
            await Fumino_Winslayer.Commands.MessageHandler(message, _client);
        }

        // For better functionality & a more developer-friendly approach to handling any kind of interaction, refer to:
        // https://discordnet.dev/guides/int_framework/intro.html
        private async Task InteractionCreatedAsync(SocketInteraction interaction) {
            if (interaction is SocketMessageComponent component) {
                // Interaction breaker
                // PingID
                if (component.Data.CustomId == "PingID") {
                    await interaction.RespondAsync("Button successfully clicked by " + interaction.User.Username);
                } else {
                    Console.WriteLine("An ID has been received that has no handler!");
                }
                // Whatever comes next
            }
        }
    }
}