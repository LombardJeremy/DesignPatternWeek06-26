using System;

[Flags]
public enum ServiceFlags
{
    Single,
    Multiple,
}

public class ServiceAttribute : Attribute
{
    public ServiceFlags Flags { get; private set; }

    public ServiceAttribute()
    {
        Flags = ServiceFlags.Single;
    }

    public ServiceAttribute(ServiceFlags flags)
    {
        Flags = flags;
    }
}
