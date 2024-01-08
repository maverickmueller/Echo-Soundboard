using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

class Program
{

    static async Task Main(string[] args)
    {
        Console.Write("Enter the player name to check: ");
        string playerNameToCheck = Console.ReadLine();
        Random random = new Random();

        if (string.IsNullOrWhiteSpace(playerNameToCheck))
        {
            Console.WriteLine("Invalid player name. Please provide a valid player name.");
            return;
        }

        Dictionary<string, int> previousGoalStats = new Dictionary<string, int>();
        int previousStunCount = 0;
        string voiceModUrl = "ws://localhost:59129/v1";
        string apiKey = "controlapi-ij9k34062";

        try
        {
            using (ClientWebSocket webSocket = new ClientWebSocket())
            {
                // Connect to Voicemod WebSocket server
                Uri serverUri = new Uri(voiceModUrl);
                await webSocket.ConnectAsync(serverUri, CancellationToken.None);
                Console.WriteLine("Connected to Voicemod");

                // Create a random GUID
                Guid randomGuid = Guid.NewGuid();

                // Create the payload object
                var payloadObj = new
                {
                    id = "ff7d7f15-0cbf-4c44-bc31-b56e0a6c9fa6",
                    action = "registerClient",
                    payload = new
                    {
                        clientKey = apiKey
                    }
                };

                // Convert the payload to JSON string
                string payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(payloadObj);

                // Convert the JSON string to bytes
                byte[] payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

                // Send the payload to the server
                await webSocket.SendAsync(new ArraySegment<byte>(payloadBytes),
                                          WebSocketMessageType.Text,
                                          true,
                                          CancellationToken.None);

                Console.WriteLine("Connection Initialized");

                //GET SOUNDS
                await Task.Delay(3000);
                string soundPath = await SendAsyncAndGetFileName(webSocket, "Pants", false);
                Console.WriteLine("Pants = " + soundPath);
                string bonk1Path = await SendAsyncAndGetFileName(webSocket, "Bonk 1", true);
                Console.WriteLine("bonk1Path = " + bonk1Path);
                string bonk2Path = await SendAsyncAndGetFileName(webSocket, "Bonk 2", true);
                Console.WriteLine("bonk2Path = " + bonk2Path);
                string bonk3Path = await SendAsyncAndGetFileName(webSocket, "Bonk 3", true);
                Console.WriteLine("bonk3Path = " + bonk3Path);
                string[] bonkPaths = new string[] { bonk1Path, bonk2Path, bonk3Path };

                Console.WriteLine("Soundboard sound retrieved successfully!");

                // You can add additional code here to process the response from the server if needed.
                while (true)
                {
                    try
                    {
                        // Query the API
                        string apiUrl = "http://127.0.0.1:6724/stats";
                        string jsonResponse = await QueryApi(apiUrl);

                        // Parse the JSON response
                        dynamic statsData = JsonConvert.DeserializeObject(jsonResponse);

                        // Check if any player on the given player name's team has had their goal statistic increase
                        bool goalIncreased = CheckGoalIncrease(playerNameToCheck, statsData, previousGoalStats);
                        bool stunsIncreased = CheckStunsIncrease(playerNameToCheck, statsData, ref previousStunCount);

                        if (goalIncreased)
                        {
                            Console.WriteLine("GOAL");
                            // Create a random GUID
                            randomGuid = Guid.NewGuid();

                            // Create the payload object
                            var newPayloadObj = new
                            {
                                action = "playMeme",
                                id = randomGuid,
                                payload = new
                                {
                                    FileName = soundPath,
                                    IsKeyDown = true
                                }
                            };

                            // Convert the payload to JSON string
                            payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(newPayloadObj);

                            // Convert the JSON string to bytes
                            payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

                            // Send the payload to the server
                            await webSocket.SendAsync(new ArraySegment<byte>(payloadBytes),
                                                      WebSocketMessageType.Text,
                                                      true,
                                                      CancellationToken.None);

                            Console.WriteLine("Soundboard sound sent successfully!");
                        }
                        if (stunsIncreased)
                        {
                            Console.WriteLine("STUN");
                            // Create a random GUID
                            randomGuid = Guid.NewGuid();

                            string selectedBonk = bonkPaths[random.Next(0, 3)];

                            // Create the payload object
                            var newPayloadObj = new
                            {
                                action = "playMeme",
                                id = randomGuid,
                                payload = new
                                {
                                    FileName = selectedBonk,
                                    IsKeyDown = true
                                }
                            };

                            // Convert the payload to JSON string
                            payloadJson = Newtonsoft.Json.JsonConvert.SerializeObject(newPayloadObj);

                            // Convert the JSON string to bytes
                            payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

                            // Send the payload to the server
                            await webSocket.SendAsync(new ArraySegment<byte>(payloadBytes),
                                                      WebSocketMessageType.Text,
                                                      true,
                                                      CancellationToken.None);

                            Console.WriteLine("Soundboard sound sent successfully!");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error: " + ex.Message);
                    }

                    // Wait for one second before the next query
                    await Task.Delay(200);
                }

                // Close the WebSocket connection
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "WebSocket connection closed", CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

    }

    private static async Task<string> QueryApi(string apiUrl)
    {
        using (var httpClient = new HttpClient())
        {
            return await httpClient.GetStringAsync(apiUrl);
        }
    }

    private static bool CheckStunsIncrease(string playerName, dynamic statsData, ref int prevStuns)
    {
        foreach (var team in statsData.teams)
        {
            foreach (var player in team.players)
            {
                if (player.player_name == playerName)
                {
                    int stunCount = Convert.ToInt32(player.stuns);
                    if (stunCount > prevStuns)
                    {
                        prevStuns = stunCount;
                        return true;
                    }
                    else
                    {
                        prevStuns = stunCount;
                        return false;
                    }
                }
            }
        }

        return false;
    }

    private static bool CheckGoalIncrease(string playerName, dynamic statsData, Dictionary<string,int> prevStats)
    {
        dynamic playerTeam = null;
        foreach (var team in statsData.teams)
        {
            foreach (var player in team.players)
            {
                if (player.player_name == playerName)
                {
                    playerTeam = team;
                    break;
                }
            }

            if (playerTeam != null)
            {
                break;
            }
        }

        if (playerTeam == null)
        {
            return false; // Player not found in any team
        }

        bool goalIncreased = false;

        foreach (var player in playerTeam.players)
        {
            int goals = player.goals;
            int previousGoals;
            var stringName = Convert.ToString(player.player_name);

            if (prevStats.TryGetValue(stringName, out previousGoals))
            {
                if (goals > previousGoals)
                {
                    goalIncreased = true;
                    break;
                }
            }
            else
            {
                prevStats.Add(stringName, Convert.ToInt32(goals));
            }
        }

        // Update previousGoalStats for all players on the player's team

        foreach (var player in playerTeam.players)
        {
            prevStats[Convert.ToString(player.player_name)] = Convert.ToInt32(player.goals);
        }

        return goalIncreased;
    }

    private static async Task<string> SendAsyncAndGetFileName(ClientWebSocket webSocket, string name, bool skipAuth)
    {

        // Receive the response
        var responseBuffer = new ArraySegment<byte>(new byte[1024]);
        WebSocketReceiveResult result;
        var responseBuilder = new StringBuilder();
        var jsonResponse = new JObject();
        if (!skipAuth)
        {
            do
            {
                responseBuilder = new StringBuilder();
                do
                {
                    result = await webSocket.ReceiveAsync(responseBuffer, CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        responseBuilder.Append(Encoding.UTF8.GetString(responseBuffer.Array, 0, result.Count));
                    }
                }
                while (!result.EndOfMessage);

                // Parse the JSON response
                jsonResponse = JObject.Parse(responseBuilder.ToString());
            } while (jsonResponse["action"] == null || jsonResponse["action"].ToString() != "registerClient");
            Console.WriteLine("We're authenticated");
        }

        
        var guid = new Guid();
        // Your request data
        var requestData = new
        {
            action = "getMemes",
            id = guid,
            payload = new { }
        };

        // Convert the request data to JSON
        var jsonRequest = JObject.FromObject(requestData).ToString();

        // Convert the JSON request to bytes
        var requestBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonRequest));
        // Send the request
        webSocket.SendAsync(requestBuffer, WebSocketMessageType.Text, true, CancellationToken.None);

        // Receive the response
        responseBuffer = new ArraySegment<byte>(new byte[1024]);
        responseBuilder = new StringBuilder();
        jsonResponse = new JObject();
        do
        {
            responseBuilder = new StringBuilder();
            do
            {
                result = await webSocket.ReceiveAsync(responseBuffer, CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    responseBuilder.Append(Encoding.UTF8.GetString(responseBuffer.Array, 0, result.Count));
                }
            }
            while (!result.EndOfMessage);

            // Parse the JSON response
            jsonResponse = JObject.Parse(responseBuilder.ToString());
        } while (jsonResponse["actionType"] == null || jsonResponse["actionType"].ToString() != "getMemes");


        // Get the listOfMemes array from the actionObject
        var listOfMemes = jsonResponse["actionObject"]["listOfMemes"];

        // Search for the item with the name "Pants"
        var targetMeme = listOfMemes.FirstOrDefault(meme => meme["Name"].ToString() == name);

        // Check if the target meme was found and extract the FileName value
        if (targetMeme != null)
        {
            var fileNameOfPantsMeme = targetMeme["FileName"].ToString();
            return fileNameOfPantsMeme;
        }
        else
        {
            throw new Exception("Meme with name " + name + " not found.");
        }
    }
}
