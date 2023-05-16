using System.Runtime.Serialization;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;

namespace TheExclusiveBotJson
{
    class Program
    {
        // All console write line are for debug none are necessary
        static void Main(string[] args)
        {
            // Start a new client with the info for which channel and which account to use with the authentication key.
            IrcClient client = new IrcClient("irc.twitch.tv", 6667, "theexclusivebot", "oauth:wxdnxkymdfgkn8jmereoxa16jb8xmt", "theexclusivefurry");

            // Start a new pinger which makes sure that the bot is still connected to twitch
            var pinger = new Pinger(client);
            pinger.Start();

            // Checks if the required save file exist and if it only exist in the wrong format use the function to make the Json file with the information in the txt file
            if (!File.Exists("Count.Json") && File.Exists("Count.txt"))
            {
                txtToJsonSaveFile();
            }
            else if (!File.Exists("Count.txt"))
            {
                ///-----------------------This might cause an error test 
                File.Create("Count.Json");
            }
            // Makes a new SaveFile object and reads the saved information from the save file.
            SaveFile? saveFile = new SaveFile();
            saveFile = JsonSerializer.Deserialize<SaveFile>(File.ReadAllText("Count.Json"));

            // Makes the variable options so the JsonSerializer saves in a specific way.
            var options = new JsonSerializerOptions { WriteIndented = true };

            int cuteCounterCurrent = 0;

            // While loop that the code runs in.
            while (true)
            {
                // Sort the User list so that it order by most points first
                saveFile.UserList.Sort((thing1, thing2) => thing1.UserPoints.CompareTo(thing2.UserPoints));
                saveFile.UserList.Reverse();

                // Makes so the userPosition is the correct number.
                int tempCounter = 0;
                foreach (var item in saveFile.UserList)
                {
                    tempCounter++;
                    //Console.WriteLine($"{item.userName} {item.userPoints} {item.userPosition}");
                    item.UserPosition = tempCounter;
                }
                // Create the current chatter/user and setting the current username to blank.
                string userName = " ";
                User chatter = new User();

                // Get and write the message to console
                Console.WriteLine("Reading message");
                var message = client.ReadMessage();
                Console.WriteLine($"Message: {message}");

                // Separate the message end set the username to the currents chatters username.
                string[] words = message.Split(" ");
                userName = (words[0].Split("!")[0]).TrimStart(':');

                // Check if the user exist in the json file and get there info. 
                bool jsonUserExist = false;
                var foundUserName = saveFile.UserList.FirstOrDefault(item => item.UserName == userName);
                if (foundUserName != null)
                {
                    chatter = foundUserName;
                    Console.WriteLine("json yes");
                    jsonUserExist = true;
                }
                // Search the message for the word cute and adding points to current count, total count and the users count.
                if (WordSearch(message, ref cuteCounterCurrent, ref userName, chatter, ref jsonUserExist, saveFile) == true)
                {
                    // If it found the word cute it send a message in chat to say how many cutes said this stream and it updates the list with the new numbers.
                    string cuteCounterCurrentString = Convert.ToString(cuteCounterCurrent);
                    client.SendChatMessage("Cutes written this stream: " + cuteCounterCurrentString);
                    Console.WriteLine($"JSON: {chatter.UserName} {chatter.UserPoints} {chatter.UserPosition}");
                    // If the user did not exist earlier it adds the user to the list. 
                    if (jsonUserExist == false)
                    {
                        saveFile.UserList.Add(chatter);
                    }
                    // Updates the save file.
                    string JsonText = JsonSerializer.Serialize(saveFile, options);
                    Console.WriteLine(JsonText);
                    File.WriteAllText("Count.json", JsonText);

                }
                Console.WriteLine(saveFile.TotalPoints);

                // Check if the message starts with a ! then check if its a viable command
                string? maybeCommand;
                if (checkCommand(message, out maybeCommand) == true)
                {
                    if (maybeCommand != null)
                    {
                        // If the command is helpcute then explain what the bot does and say the command to se other commands.
                        if (maybeCommand.ToLower() == "helpcute")
                        {
                            client.SendChatMessage("@" + userName + " This bot counts the number of cutes written in this chat. It count both this stream and over all stream since the bot got activated. Type !cuteCommands to see see possible commands");
                        }
                        // If the command is mycutes the it will tell the user there cute count and what position on the scoreboard they are and if they haven't any cutes saved it says that. 
                        if (maybeCommand.ToLower() == "mycutes")
                        {
                            if (jsonUserExist == true)
                            {
                                client.SendChatMessage("@" + chatter.UserName + " Your cute count is " + chatter.UserPoints + " and you are number " + chatter.UserPosition + " on the scoreboard.");
                            }
                            if (jsonUserExist == false)
                            {
                                client.SendChatMessage("@" + userName + " you have not said cute in this chat how weird. You should start.");
                            }
                        }
                        // If the command is cutecommands then tell the user all the possible commands and what they do.
                        if (maybeCommand.ToLower() == "cutecommands")
                        {
                            client.SendChatMessage("@" + userName + "  Type !helpCute to see what this bot does. !MyCutes to see the number of cutes you have written in total and your position on the scoreboard. !CuteTop5 to see the top 5 people with the most cutes written");
                        }
                        // Tell the user the current top 5 on the cute scoreboard
                        if (maybeCommand.ToLower() == "cutetop5")
                        {
                            client.SendChatMessage($"@{userName} The current top 5 for the cute count is. In first place{saveFile.UserList[0].UserName} with {saveFile.UserList[0].UserPoints} cutes. In second place {saveFile.UserList[1].UserName} with {saveFile.UserList[1].UserPoints} cutes. In third place {saveFile.UserList[2].UserName} with {saveFile.UserList[2].UserPoints} cutes. In fourth place {saveFile.UserList[3].UserName} with {saveFile.UserList[3].UserPoints} cutes. In fifth place {saveFile.UserList[4].UserName} with {saveFile.UserList[4].UserPoints} cutes.");
                        }

                    }
                }
            }
        }
        /// <summary>
        /// Checks a message if it contains one of the search words and if it does adds to there points and to the total and if they don't have any earlier points then it adds them to the list.  And returns False or True if it contains or not,
        /// </summary>
        /// <param name="message">The message from the Twitch Chat</param>
        /// <param name="checkWordCountTotal"></param>
        /// <param name="checkWordCountCurrent"></param>
        /// <param name="userName">UserName of the person from the Twitch chat</param>
        /// <param name="userPoints">The currents points of the user from the message</param>
        /// <param name="userPosition">The current user position on the save list</param>
        /// <param name="countFileList">The save file List</param>
        /// <returns></returns>
        static bool WordSearch(string message, ref int checkWordCountCurrent, ref string userName, User chatter, ref bool jsonUserExist, SaveFile saveFile)
        {

            bool cuteYes = false;
            // Split the message in to separate words
            string[] words = message.Split(" ");

            // Loop to check each word if the match the check word
            foreach (var word in words)
            {
                // Use regex to check the word for cute or cutie
                Match m = Regex.Match(word.ToLower(), @"^(.*?(cute|cutie|qt))+.*?");

                // Array with the commands so they don't count on the cute meter.
                string[] commands = { ":!mycutes", ":!helpcute", ":!cutecommands", ":!cutetop5" };
                // Checks if the match was true and if it was not from a command
                if (m.Success && commands.Contains(word.ToLower()) == false)
                {
                    // if they don't exist make the username the current message username.
                    if (jsonUserExist == false)
                    {
                        chatter.UserName = userName;
                        Console.WriteLine("json no");
                    }

                    Console.WriteLine(word);
                    // Add the amount of cutes found in the word to all relevant lists
                    checkWordCountCurrent += m.Groups[1].Captures.Count;
                    chatter.UserPoints += m.Groups[1].Captures.Count;
                    cuteYes = true;
                    saveFile.TotalPoints += m.Groups[1].Captures.Count;
                }
            }
            // Returns either true or false depending if the search word was found. 
            if (cuteYes == true)
            {
                return (true);
            }
            else
            {
                return (false);
            }
        }
        /// <summary>
        /// Checks if the first character is ! of the message and returns False or True also out puts the string that might be the command.
        /// </summary>
        /// <param name="message">The message from the Twitch Chat</param>
        /// <param name="maybeCommand"></param>
        /// <returns></returns>
        static bool checkCommand(string message, out string? maybeCommand)
        {
            // Split the message at : and check if it became more than 2 segments otherwise stop the function
            string[] splitMessage = message.Split(":");
            if (splitMessage.Length > 2)
            {
                // Take the first character of the message that the user wrote and output true if it was ! also makes maybeCommand the the message without !. 
                char firstChar = splitMessage[2][0];
                Console.WriteLine(firstChar);
                if (firstChar == '!')
                {
                    Console.WriteLine("GEG");
                    maybeCommand = splitMessage[2].TrimStart('!');
                    return true;
                }
            }
            // Outputs false and null if the message did not start with !
            maybeCommand = null;
            return false;
        }

        /// <summary>
        /// Function that converts a txt save file to Json file.
        /// </summary>
        static void txtToJsonSaveFile()
        {
            // Read the information on the txt file to a list and make a temporary save file object. 
            List<string> tempList = File.ReadAllLines("Count.txt").ToList();
            SaveFile tempJsonSaveFile = new SaveFile();
            // Makes the variable options so the JsonSerializer saves in a specific way.
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Read the total points of the list in to the temp object.
            tempJsonSaveFile.TotalPoints = int.Parse(tempList[0]);
            // Loop trough every item in the list and add them the temp object. 
            int i = 0;
            foreach (var item in tempList)
            {
                // Skip the first item because its the total points and not a user. 
                if (item != tempList[0])
                {
                    i++;
                    // Parse the information from the list in to the temp user then add it to the temp savefile object. 
                    User tempUser = new User();
                    tempUser.UserPoints = int.Parse(item.Split(" ")[0]);
                    tempUser.UserName = item.Split(" ")[1];
                    tempUser.UserPosition = i;
                    Console.WriteLine($"{tempUser.UserName}, {tempUser.UserPoints}, {tempUser.UserPosition}");

                    tempJsonSaveFile.UserList.Add(tempUser);

                }
            }
            // Create the json file and write the information to it.
            File.WriteAllText("Count.json", JsonSerializer.Serialize(tempJsonSaveFile, options));
        }
    }
}