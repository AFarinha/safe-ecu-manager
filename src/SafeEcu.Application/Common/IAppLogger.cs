namespace SafeEcu.Application.Common;

public interface IAppLogger
{
    void Information(string message);

    void Warning(string message);

    void Error(string message, Exception? exception = null);
}
