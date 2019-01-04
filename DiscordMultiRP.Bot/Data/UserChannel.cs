﻿namespace DiscordMultiRP.Bot.Data
{
    public class UserChannel
    {
        public int Id { get; set; }

        public User User { get; set; }

        public Channel Channel { get; set; }

        public Proxy LastProxy { get; set; }
    }
}