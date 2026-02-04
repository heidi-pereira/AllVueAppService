namespace OpenEnds.Tests;

public class TestTimeProvider(DateTime now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow()
    {
        return now;
    }
}