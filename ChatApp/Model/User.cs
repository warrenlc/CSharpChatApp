using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace ChatApp.Model
{
    internal static class User
    {
        public static string? Username { get; set; }

        public static string? ChattingWith { get; set; }

        public static List<Dictionary<string, List<Message>>>? Conversations { get; set; } = new List<Dictionary<string, List<Message>>>();


        public static void AddMessage(Message message)
        {
            string lookupKey = "";
            if (message.Sender == Username)
            {
                lookupKey = message.Receiver;
            }
            else
            {
                lookupKey = message.Sender;
            }
            var conversation = Conversations?.Find(c => c.ContainsKey(lookupKey));
            if (conversation == null)
            {
                conversation = new Dictionary<string, List<Message>>
                {
                    { lookupKey, new List<Message> { message } }
                };
                Conversations?.Add(conversation);
            }
            else
            {
                conversation[lookupKey].Add(message);
            }
        }


        public static List<Message> FindConversationsFromSearchTerm(string searchTerm)
        {
            if (Conversations == null)
            {
                return new List<Message>();
            }
            List<Message> conversations = Conversations
                .SelectMany(dict => dict.Where(kv => kv.Key.Contains(searchTerm)).Select(kv => kv.Value))
                .FirstOrDefault(new List<Message> { });
            return new List<Message>(conversations);
        }


        public static void SaveMessages()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Username}_conversations.json");
            string jsonConversations = JsonConvert.SerializeObject(Conversations, Formatting.Indented);
            File.WriteAllText(filePath, jsonConversations);
        }


        public static void FetchSavedMessages()
        {
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), $"{Username}_conversations.json");
            try
            {
                string jsonserialized = File.ReadAllText(filePath);
                Conversations = JsonConvert.DeserializeObject<List<Dictionary<string, List<Message>>>?>(jsonserialized);
            }
            catch (FileNotFoundException f)
            {
                Debug.WriteLine($"File not found {f}");
            }
            catch (DirectoryNotFoundException d)
            {
                Debug.WriteLine($"Directory not found {d}");
            }
        }
    }
}
