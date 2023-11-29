using Discord;
using Discord.WebSocket;
using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Security;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using static Fumino_Winslayer.Framework;
using System.ServiceProcess;

namespace Fumino_Winslayer {
    internal class Commands {

        public static string Prefix = botConfig.Prefix;

        public static bool HasMention(SocketMessage Message, DiscordSocketClient Client) {
            return Message.MentionedUsers.Any(m => m.Id == Client.CurrentUser.Id);
        }
        public static bool HasTrigger(SocketMessage Message, DiscordSocketClient Client, string Trigger) {
            return Message.Content == Prefix + Trigger || Message.Content.Contains(Trigger);
        }

        public static async Task Help() {
            if (GMessage != null && GClient != null) {
                await GMessage.Channel.SendMessageAsync(
                    "------- Fumino Winslayer -------\n" +
                    "[P] denotes the command can be called with a prefix.\n" +
                    "[@] denotes the command can be called with a mention.\n" +
                    "[M] denotes a moderator or above level command.\n" +
                    "[***!***] denotes a protected command. Unless you're an admin, don't use these.\n\n" +
                    "[@P] Help: Displays this information.\n" +
                    "[@P] Ping: Displays a box to click, and the bot responds with your username.\n" +
                    "[@P***!***] Quit: Immediately exits the service.\n" +
                    "[@P***!***] Execute [Command] [Arguments, space delimited]: Checks if the executable plugin is available, " +
                        "and if so, calls it with supplied arguments denoted by spaces, " +
                        "otherise attempts to execute it in powershell.\n" +
                    "[@P***!***] UpdateNickname: Simply sets Winslayer's nickname to the supplied config value.\n" + 
                    "[@PM Kick [Mentioned user] [Reason]: Kicks the mentioned user.\n" +
                    "[@PM Ban [Mentioned user] [Reason]: Bans the mentioned user.\n" +
                    "[@P] Slap [Mentioned user]: Slaps the mentioned user.");
                return;
            } else {
                DebugWrite("[Help] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task Ping() {
            if (GMessage != null && GClient != null) {
                await GMessage.Channel.TriggerTypingAsync();
                // Create a new ComponentBuilder, in which dropdowns & buttons can be created.
                var cb = new ComponentBuilder()
                    .WithButton("Click me!", "PingID", ButtonStyle.Primary);

                // Send a message with content 'pong', including a button.
                // This button needs to be build by calling .Build() before being passed into the call.
                await GMessage.Channel.SendMessageAsync("Pong!", components: cb.Build());
                return;
            } else {
                DebugWrite("[Ping] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task Debug() {
            if (GMessage != null && GClient != null) {
                await GMessage.Channel.TriggerTypingAsync();
                Version osVersion = Environment.OSVersion.Version;
                string osVersionString = $"{osVersion.Major}.{osVersion.Minor}.{osVersion.Build}";
                Assembly assembly = Assembly.GetExecutingAssembly();
                Version version = assembly.GetName().Version;
                string versionString = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
                await GMessage.Channel.SendMessageAsync(
                    "Host: " + osVersionString + "\n"
                    + "User: " + Environment.UserName + "\n"
                    + "Process path: " + Environment.ProcessPath + "\n"
                    + "Version: " + versionString);
                return;
            } else {
                DebugWrite("[Debug] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task Quit() {
            if (GMessage != null && GClient != null) {
                await GMessage.Channel.TriggerTypingAsync();
                if (CheckIfAuthorized(GMessage)) {
                    await GMessage.Channel.SendMessageAsync("Stopping service and exiting immediately.");

                    // CA1416 - This only runs on Windows anyways, why throw that error?
                    #pragma warning disable CA1416
                    ServiceController WinslayerService = new ServiceController("Winslayer");
                    WinslayerService.Stop(); // This kills us right now
                    #pragma warning restore CA1416

                    Environment.Exit(0);
                    return;
                } else {
                    await GMessage.Channel.SendMessageAsync("Incorrect permissions " + GMessage.Author.Mention + ", your account has been flagged for review.");
                    return;
                }
            } else {
                DebugWrite("[Quit] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task UpdateNickname() {
            if (GMessage != null && GClient != null) {
                if (CheckIfAuthorized(GMessage)) {
                    await GMessage.Channel.TriggerTypingAsync();
                    if (GClient.CurrentUser.Username == botConfig.BotName) {
                        await GMessage.Channel.SendMessageAsync("No reason to update, the name hasn't changed.");
                        return;
                    } else {
                        foreach (SocketGuild guild in GClient.Guilds) {
                            var PrimaryServer = GClient.GetGuild(guild.Id);
                            var Me = PrimaryServer.GetUser(GClient.CurrentUser.Id);
                            await Me.ModifyAsync(x => { x.Nickname = botConfig.BotName; });
                        }
                        await GMessage.Channel.SendMessageAsync("Updated my nickname to " + botConfig.BotName);
                        return;
                    }
                } else {
                    await GMessage.Channel.SendMessageAsync("This is a protected command, but is non-harmful, so your account has not been flagged.");
                }
            } else {
                DebugWrite("[UpdateNickname] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task BotKick(Discord.IGuildUser Target, string Reason) {
            if (GMessage != null && GClient != null) {
                if (CheckIfAuthorized(GMessage)) {
                    var TargetName = Target.DisplayName;
                    await Target.KickAsync(Reason);
                    await GMessage.Channel.SendMessageAsync(TargetName + " has been kicked for the following reason: " + Reason);
                } else {
                    await GMessage.Channel.SendMessageAsync("Yeah you aren't an admin.");
                }
            } else {
                DebugWrite("[BotKick] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task BotSlap(Discord.IGuildUser Target) {
            if (GMessage != null && GClient != null) {
                var TargetName = Target.DisplayName;
                await GMessage.Channel.SendMessageAsync(TargetName + " has been slapped by me.");
            } else {
                DebugWrite("[BotKick] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return;
            }
        }

        public static async Task Scan() {
            if (GClient != null && GMessage != null) {
                DiscordSocketClient TempClient = GClient;
                SocketMessage TempMessage = GMessage;
                if (File.Exists("C:\\Program Files (x86)\\Nmap\\nmap.exe")) {
                    DebugWrite("Found the nmap binary, can continue.");
                } else {
                    DebugWrite("Failed to find the nmap binary. Downloading.");
                    return;
                }

                System.String[] Words = GMessage.Content.Split(" ");
                if (Words.Length == 1) {
                    await GMessage.Channel.SendMessageAsync("Nothing to execute. Aborting.");
                    return;
                }

                string pattern = @"(""[^""]+""|\S+)";
                Regex regex = new Regex(pattern);
                MatchCollection matches = regex.Matches(GMessage.Content);
                Console.WriteLine("Command split result: ");
                String[] ToExecute = { };
                List<string> list = new List<string>(ToExecute);
                foreach (Match match in matches) {
                    list.Add(match.Value);
                    Console.WriteLine($"[{match.Value}]");
                }
                string[] ExecuteArray = list.ToArray();
                Console.WriteLine("String array length: " + ExecuteArray.Length);
                string Arguments = "";
                int LoopCount = 0;
                foreach (string Argument in ExecuteArray) {
                    Console.WriteLine("Looping with: " + Argument);
                    if (LoopCount == 0) {

                    }
                    if (LoopCount == 1 && Argument != "scan") {
                        // this should be the target.
                        Arguments = Arguments + Argument;
                    }
                    if (LoopCount == 2) {
                        Arguments = Arguments + Argument;
                    }
                    LoopCount++;
                }
                _ = ExecuteInline(TempMessage, TempClient, "nmap.exe", Arguments + " -sV -Pn");
            } else {
                DebugWrite("[Scan]: Yeah shit went wrong during the scan.");
                return;
            } 
        }

        public static string GetDriveInfo() {
            DriveInfo[] allDrives = DriveInfo.GetDrives();
            // Create a string to store the output
            string output = "";
            // Loop through each drive and get its capacity
            foreach (DriveInfo drive in allDrives) {
                if (drive.IsReady) {
                    // Convert bytes to terabytes using a double constant for division
                    double totalCapacityTB = (double)drive.TotalSize / (1024.0 * 1024 * 1024 * 1024);
                    // Append the drive information to the output string
                    output += $"{drive.Name}\\: {totalCapacityTB:F3} TB\n";
                }
            }
            return output;
        }

        public static async Task MessageHandler(SocketMessage Message, DiscordSocketClient Client) {
            // Do nothing if it's just the bot talking, or other bots.
            if (Message.Author.Id == Client.CurrentUser.Id || Message.Author.IsBot) {
                return;
            }

            GMessage = Message;
            GClient = Client;

            // First, break the entire thing down
            string MessageContentLowered = GMessage.Content.ToLower();

            bool HasBeenMentioned = false;
            Discord.IUser MentionedUser = null;

            // Check if we are running in prefix mode or mention mode
            // Two person mode. Bot and other user.
            if (Message.MentionedUsers.Count > 0) {
                foreach (Discord.IUser Users in Message.MentionedUsers) {
                    if (Users.Id == Client.CurrentUser.Id) {
                        HasBeenMentioned = true;
                    } else {
                        MentionedUser = Users;
                    }
                }
            }

            if (MessageContentLowered.StartsWith(Prefix + "help")) {
                await Help(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("help")) {
                await Help(); return;
            }

            if (MessageContentLowered.StartsWith(Prefix + "ping")) {
                await Ping(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("ping")) {
                await Ping(); return;
            }

            if (MessageContentLowered.StartsWith(Prefix + "debug")) {
                await Debug(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("debug")) {
                await Debug(); return;
            }

            if (MessageContentLowered.StartsWith(Prefix + "quit")) {
                await Quit(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("quit")) {
                await Quit(); return;
            }

            // No mention support for this one. Something broke and it refuses to execute correctly.
            if (MessageContentLowered.StartsWith(Prefix + "execute")) {
                _ = Execute(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("execute")) {
                _ = Execute(); return;
            }

            if (MessageContentLowered.StartsWith(Prefix + "updatenickname")) {
                await UpdateNickname(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("updatenickname")) {
                await Quit(); return;
            }

            if (MessageContentLowered.StartsWith(Prefix + "kick")) {
                if (MentionedUser != null) {
                    IGuildUser UserX = (IGuildUser)MentionedUser;
                    await BotKick(UserX, Message.Content); return;
                } else {
                    await GMessage.Channel.SendMessageAsync("No user mentioned. Aborting kick process.");
                }
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("kick")) {
                if (MentionedUser != null) {
                    IGuildUser UserX = (IGuildUser)MentionedUser;
                    await BotKick(UserX, Message.Content); return;
                } else {
                    await GMessage.Channel.SendMessageAsync("No user mentioned. Aborting kick process.");
                }
            }

            if (MessageContentLowered.StartsWith(Prefix + "slap")) {
                if (MentionedUser != null) {
                    IGuildUser UserX = (IGuildUser)MentionedUser;
                    await BotSlap(UserX); return;
                } else {
                    await GMessage.Channel.SendMessageAsync("Can't slap myself you know.");
                }
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("slap")) {
                if (MentionedUser != null) {
                    IGuildUser UserX = (IGuildUser)MentionedUser;
                    await BotSlap(UserX); return;
                } else {
                    await GMessage.Channel.SendMessageAsync("Can't slap myself you know.");
                }
            }

            if (MessageContentLowered.StartsWith(Prefix + "scan")) {
                await Scan(); return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("scan")) {
                await Scan(); return;
            }

            if (MessageContentLowered.StartsWith(Prefix + "drives")) {
                await GMessage.Channel.SendMessageAsync("Current drive information: \n" + GetDriveInfo());
                return;
            }
            if (HasBeenMentioned && MessageContentLowered.Contains("drives")) {
                await GMessage.Channel.SendMessageAsync("Current drive information: \n" + GetDriveInfo());
                return;
            }
        }
    }
}
