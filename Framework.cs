using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fumino_Winslayer {

    internal class Framework {
        public static string ConfigLocation = "";
        public static string[] Binaries = Array.Empty<string>();

        public static SocketMessage? GMessage;
        public static DiscordSocketClient? GClient;

        public struct XBotConfig(string botName = "Elizabeth", string botVersion = "1.0.0", string botID = "0", string botToken = "0", bool redactToken = false,
                 string[]? allowedAdmins = null, int verbosity = 0, string prefix = "!") {
            public string BotName = botName;
            public string BotVersion = botVersion;
            public string BotID = botID;
            public string BotToken = botToken;
            public bool RedactToken = redactToken;
            public string[] AllowedAdmins = allowedAdmins = Array.Empty<string>();
            public int Verbosity = verbosity;
            public string Prefix = prefix;
        };

        public static XBotConfig botConfig;

        public static void DebugWrite(Action Function, string Message) {
            if (Function == null) {
                Console.WriteLine(Message); return;
            } else {
                Console.WriteLine("[" + Function.Method.Name + "]: " + Message); return;
            }
        }

        public static bool CheckIfAuthorized(SocketMessage Message) {
            bool Allowed = false;
            foreach (string Admins in botConfig.AllowedAdmins) {
                if (Admins == Message.Author.Id.ToString()) {
                    Allowed = true;
                }
            }
            return Allowed;
        }

        public static void PopulateBinaries() {
            string BinariesPath = Environment.CurrentDirectory + "\\Binaries\\";
            string[] Files = Directory.GetFiles(BinariesPath, "*.exe");
            Binaries = Files;
        }

        static (string, int) ExecuteCommand(string command, string arguments) {
            using (Process process = new Process()) {
                // Set the start information for the process
                // Workaround, call PS first

                string XArguments = arguments;

                process.StartInfo.FileName = "powershell";
                process.StartInfo.Arguments = XArguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                // Start the process
                process.Start();

                // Read the output (if needed)
                string output = process.StandardOutput.ReadToEnd();
                string outputError = process.StandardError.ReadToEnd();
                DebugWrite(null, "[ExecuteCommand]: Execute: " + output);

                // Wait for the process to exit
                process.WaitForExit();

                // Optionally check the exit code
                int exitCode = process.ExitCode;
                if (exitCode != 0 && output.Length == 0) {
                    output = outputError;
                }
                Console.WriteLine($"Exit Code: {exitCode}");
                return (output, exitCode);
            }
        }

        static (string, int) ExecuteCommandBinary(string command, string arguments) {
            using (Process process = new Process()) {
                // Set the start information for the process
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;

                // Start the process
                process.Start();

                // Read the output (if needed)
                string output = process.StandardOutput.ReadToEnd();
                string outputError = process.StandardError.ReadToEnd();
                DebugWrite(null, "[ExecuteCommandBinary]: Execute: " + output);

                // Wait for the process to exit
                process.WaitForExit();

                // Optionally check the exit code
                int exitCode = process.ExitCode;
                if (exitCode != 0 && output.Length == 0) {
                    output = outputError;
                }
                Console.WriteLine($"Exit Code: {exitCode}");
                return (output, exitCode);
            }
        }

        public static async Task Execute() {
            if (GMessage != null && GClient != null) {
                await GMessage.Channel.TriggerTypingAsync();
                if (!CheckIfAuthorized(GMessage)) {
                    await GMessage.Channel.SendMessageAsync("Incorrect permissions " + GMessage.Author.Mention + ", your account has been flagged for review.");
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
                    if (LoopCount == 1) {

                    }
                    if (LoopCount == 2) {
                        Arguments = Arguments + Argument + " ";
                    }
                    if (LoopCount > 2) {
                        Arguments = Arguments + Argument + " ";
                    }
                    LoopCount++;
                }

                // New feature: Check if we have the binary and use the binary directly.
                bool HasNativeBinary = false;
                string BinaryPath = "";
                foreach (string Binary in Binaries) {
                    if (Binary.EndsWith(ExecuteArray[1] + ".exe")) {
                        BinaryPath = Binary;
                        HasNativeBinary = true; break;
                    }
                }

                if (HasNativeBinary) {
                    // Use the binary instead of a powershell call.
                    await GMessage.Channel.SendMessageAsync("Executing found binary [" + BinaryPath + "] with arguments [" + Arguments + "].");
                    (string, int) ECBOutput = ExecuteCommandBinary(BinaryPath, Arguments);
                    if (ECBOutput.Item1.Length > 1950) {
                        File.WriteAllText(Environment.CurrentDirectory + "DiscordUpload.txt", ECBOutput.Item1);
                        await GMessage.Channel.SendFileAsync(Environment.CurrentDirectory + "DiscordUpload.txt");
                        File.Delete(Environment.CurrentDirectory + "DiscordUpload.txt");
                        return;
                    } else {
                        await GMessage.Channel.SendMessageAsync("Output: ```" + ECBOutput.Item1 + "```\n" + "Exit code: " + ECBOutput.Item2);
                        return;
                    }
                } else {
                    await GMessage.Channel.SendMessageAsync("Executing [" + ExecuteArray[1] + "] with arguments [" + Arguments + "].");
                    (string, int) Output = ExecuteCommand(ExecuteArray[1], Arguments);
                    if (Output.Item1.Length > 1950) {
                        File.WriteAllText(Environment.CurrentDirectory + "DiscordUpload.txt", Output.Item1);
                        await GMessage.Channel.SendFileAsync(Environment.CurrentDirectory + "DiscordUpload.txt");
                        File.Delete(Environment.CurrentDirectory + "DiscordUpload.txt");
                        return;
                    } else {
                        await GMessage.Channel.SendMessageAsync("Output: ```" + Output.Item1 + "```\n" + "Exit code: " + Output.Item2);
                        return;
                    }
                }
            } else {
                DebugWrite("[Execute] Something went HORRIBLY wrong, but I recovered. Aborting current command.");
                return; 
            }
        }

        public static async Task ExecuteInline(SocketMessage StaticMessage, DiscordSocketClient StaticClient, string Command, string Arguments) {
            if (StaticMessage != null && StaticClient != null) {
                DebugWrite("[ExecuteInline]: " + "Command=" + Command + ", Argument=" + Arguments);
                await StaticMessage.Channel.TriggerTypingAsync();

                await StaticMessage.Channel.SendMessageAsync("Executing scan for " + StaticMessage.Author.Mention);
                (string, int) ECBOutput = ExecuteCommandBinary(Command, Arguments);
                
                if (ECBOutput.Item1.Length > 1950) {
                    File.WriteAllText(Environment.CurrentDirectory + "DiscordUpload.txt", ECBOutput.Item1);
                    await StaticMessage.Channel.SendFileAsync(Environment.CurrentDirectory + "DiscordUpload.txt");
                    File.Delete(Environment.CurrentDirectory + "DiscordUpload.txt");
                    return;
                } else {
                    await StaticMessage.Channel.SendMessageAsync("Output: ```" + ECBOutput.Item1 + "```\n" + "Exit code: " + ECBOutput.Item2);
                    return;
                }
            }
        }

        public static void DebugWrite(string Message) {
            Console.WriteLine(Message); return;
        }

        private static void DebugWriteMethod(Func<string, string, (string, int)> Function, string Message) {
            if (Function.Target == null) {
                Console.WriteLine(Message); return;
            } else {
                Console.WriteLine("[" + Function.Method.Name + "]: " + Message); return;
            }
        }

        public static void LoadWinslayerConfig() {
            if (File.Exists(Environment.CurrentDirectory + "\\Winslayer.ini")) {
                ConfigLocation = Environment.CurrentDirectory + "\\Winslayer.ini";

                botConfig = new XBotConfig();
                // Parse the config
                string[] ConfigLoad = File.ReadAllLines(ConfigLocation);
                if (ConfigLoad[0] == "# WINSLAYER CONFIG VERSION 1.0.0") {
                    foreach (string Line in ConfigLoad) {
                        if (Line.StartsWith("Name=")) {
                            botConfig.BotName = Line.Split("=")[1];
                        }
                        if (Line.StartsWith("Version=")) {
                            botConfig.BotVersion = Line.Split("=")[1];
                        }
                        if (Line.StartsWith("ID=")) {
                            botConfig.BotID = Line.Split("=")[1];
                        }
                        if (Line.StartsWith("Token=")) {
                            botConfig.BotToken = Line.Split("=")[1];
                        }
                        if (Line.StartsWith("AllowedAdmins=")) {
                            string Post = Line.Split("=")[1];
                            if (botConfig.AllowedAdmins == null) {
                                botConfig.AllowedAdmins = new string[] { };
                            }
                            List<string> AdminList = new List<string>();
                            int LoopCount = 0;
                            foreach (string X in Post.Split(",")) {
                                AdminList.Insert(LoopCount, X);
                            }
                            botConfig.AllowedAdmins = AdminList.ToArray();
                        }
                        if (Line.StartsWith("Prefix=")) {
                            botConfig.Prefix = Line.Split("=")[1];
                        }
                        if (Line.StartsWith("Verbosity=")) {
                            botConfig.Verbosity = Convert.ToInt32(Line.Split("=")[1]);
                        }
                        if (Line.StartsWith("HAS_RedactToken=")) {
                            string Res = Line.Split("=")[1];
                            if (Res == "true" || Res == "0") {
                                botConfig.RedactToken = true;
                            } else {
                                botConfig.RedactToken = false;
                            }
                        }


                    }
                } else {
                    DebugWrite(LoadWinslayerConfig, "The config located at " + ConfigLocation + "isn't a real Winslayer config. Aborting load.");
                    Environment.Exit(-1);
                }
                
                DebugWrite(LoadWinslayerConfig, "Loaded config from " + Environment.CurrentDirectory + "\\Winslayer.ini");
                // Print the contents.
                DebugWrite(LoadWinslayerConfig, "--- Loaded config values ---");
                DebugWrite(LoadWinslayerConfig, "Name: " + botConfig.BotName);
                DebugWrite(LoadWinslayerConfig, "Version: " + botConfig.BotVersion);
                DebugWrite(LoadWinslayerConfig, "ID: " + botConfig.BotID);
                if (botConfig.RedactToken) {
                    DebugWrite(LoadWinslayerConfig, "Token: [Redacted due to RedactToken=true]");
                } else {
                    DebugWrite(LoadWinslayerConfig, "Token: " + botConfig.BotToken);
                }
                if (botConfig.AllowedAdmins == null) {
                    botConfig.AllowedAdmins = new string[] { "348314304375685120" };
                }
                foreach (string Admin in botConfig.AllowedAdmins) {
                    DebugWrite(LoadWinslayerConfig, "Allowed administrators: " + Admin);
                }
                
                DebugWrite(LoadWinslayerConfig, "Prefix: " + botConfig.Prefix);
                switch (botConfig.Verbosity) {
                    case 0: DebugWrite(LoadWinslayerConfig, "Verbosity: Error only"); break;
                    case 1: DebugWrite(LoadWinslayerConfig, "Verbosity: Warnings"); break;
                    case 2: DebugWrite(LoadWinslayerConfig, "Verbosity: Information"); break;
                    case 3: DebugWrite(LoadWinslayerConfig, "Verbosity: Everything"); break;
                    default: DebugWrite(LoadWinslayerConfig, "Verbosity: Error only"); break;
                }

                DebugWrite(LoadWinslayerConfig, "--- Loaded config values ---");

            } else {
                DebugWrite(LoadWinslayerConfig, "Config not found in " + Environment.CurrentDirectory);
                DebugWrite(LoadWinslayerConfig, "Error, cannot continue without config file.");
                Environment.Exit(-1);
            }
        }
    }
}
