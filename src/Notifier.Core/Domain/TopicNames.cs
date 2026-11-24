namespace Notifier.Core.Domain;

public static class TopicNames
{
    public static string Display(Topic topic) => topic switch
    {
        Topic.TopStories => "Top stories",
        Topic.World => "World",
        Topic.Business => "Business",
        Topic.ScienceEnvironment => "Science & Environment",
        Topic.Technology => "Technology",
        _ => topic.ToString(),
    };
}
