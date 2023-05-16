using System;

namespace TheExclusiveBotJson
{
    public class SaveFile
    {
        public int TotalPoints { get; set; }
        public List<User> UserList { get; set; } = new List<User>();

    }
    public class User
    {
        public string UserName { get; set; }
        public int UserPoints { get; set; }
        public int UserPosition { get; set; }
    }
}