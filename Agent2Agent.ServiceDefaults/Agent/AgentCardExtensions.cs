using A2A;


public static class AgentCardExtensions
{
    public static AgentRegistryMessage ToRegistryMessage(this AgentCard card, AgentRegistryAction action)
    {
        AgentNotification? notification = null;
        if (card.Capabilities.PushNotifications)
        {
            var notificationExtension = card.Capabilities.Extensions?.FirstOrDefault(ext => ext.Description == "Notification");
            if (notificationExtension != null)
            {
                notification = new AgentNotification
                {
                    Uri = notificationExtension.Uri ?? string.Empty,
                    Id = card.Name,
                };
            }
        }

        return new AgentRegistryMessage(
            action,
            new AgentDetails
            {
                Name = card.Name,
                Description = card.Description,
                Uri = card.Url,
                Version = card.Version,
                Skills = card.Skills.Select(skill => new AgentSkillDetail
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                    Prompts = skill.Examples ?? new List<string>()
                }).ToList()
            },
            notification
        );
    }
}
