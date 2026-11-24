namespace Notifier.Core.Domain;

public enum Topic
{
    TopStories,
    World,
    Business,
    ScienceEnvironment,
    Technology,
}

/// <summary>Headline and FrontPage ignore topics.</summary>
public enum Tier
{
    Headline,
    FrontPage,
    All,
}

public enum DeliveryKind
{
    Real,
    Test,
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    Failed,
}
