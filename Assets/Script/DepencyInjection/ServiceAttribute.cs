using System;

[Flags]
public enum ServiceFlags
{
    None = 0,
}

public class ServiceAttribute : Attribute
{

    public ServiceFlags Flags { get; private set; }
    
    public ServiceAttribute()
    {
        Flags = ServiceFlags.None;
    }
    
    public ServiceAttribute(ServiceFlags flags)
    {
        Flags = flags;
    }
}
