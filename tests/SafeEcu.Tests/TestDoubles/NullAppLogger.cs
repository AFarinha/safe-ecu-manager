using SafeEcu.Application.Common;

namespace SafeEcu.Tests.TestDoubles;

public sealed class NullAppLogger : IAppLogger
{
    public void Information(string message)
    {
    }

    public void Warning(string message)
    {
    }

    public void Error(string message, Exception? exception = null)
    {
    }
}
