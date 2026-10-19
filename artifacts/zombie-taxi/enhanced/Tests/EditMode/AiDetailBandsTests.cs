using NUnit.Framework;
using ZombieTaxi.Core;

public class AiDetailBandsTests
{
    private static readonly AiDetailBands Bands = new(nearDistance: 30f, midDistance: 90f);

    private static float Sqr(float value) => value * value;

    [Test]
    public void ClassifiesByDistanceBand()
    {
        Assert.AreEqual(AiDetailLevel.Near, Bands.Classify(Sqr(10f)));
        Assert.AreEqual(AiDetailLevel.Mid, Bands.Classify(Sqr(60f)));
        Assert.AreEqual(AiDetailLevel.Far, Bands.Classify(Sqr(200f)));
    }

    [Test]
    public void BandEdgesBelongToTheCloserBand()
    {
        Assert.AreEqual(AiDetailLevel.Near, Bands.Classify(Sqr(30f)));
        Assert.AreEqual(AiDetailLevel.Mid, Bands.Classify(Sqr(90f)));
    }

    [Test]
    public void ZeroDistanceIsNear()
    {
        Assert.AreEqual(AiDetailLevel.Near, Bands.Classify(0f));
    }

    [Test]
    public void InvertedThresholdsAreCorrected()
    {
        // Mid closer than Near would otherwise make the Mid band unreachable.
        AiDetailBands inverted = new(nearDistance: 90f, midDistance: 30f);

        Assert.GreaterOrEqual(inverted.MidSqrDistance, inverted.NearSqrDistance);
        Assert.AreEqual(AiDetailLevel.Near, inverted.Classify(Sqr(50f)));
        Assert.AreEqual(AiDetailLevel.Far, inverted.Classify(Sqr(120f)));
    }

    [Test]
    public void RepathIntervalGrowsWithDistance()
    {
        const float baseInterval = 0.25f;

        float near = AiDetailBands.RepathInterval(AiDetailLevel.Near, baseInterval);
        float mid = AiDetailBands.RepathInterval(AiDetailLevel.Mid, baseInterval);
        float far = AiDetailBands.RepathInterval(AiDetailLevel.Far, baseInterval);

        Assert.Greater(mid, near);
        Assert.Greater(far, mid);
    }

    [Test]
    public void NearAgentsHaveNoExtraRepathCooldown()
    {
        // A chasing agent must repath whenever it is serviced, otherwise it
        // steers at a stale position and trails a moving target.
        Assert.AreEqual(0f, AiDetailBands.RepathInterval(AiDetailLevel.Near, 0.25f));
        Assert.AreEqual(0f, AiDetailBands.RepathInterval(AiDetailLevel.Near, 10f));
    }

    [Test]
    public void ThresholdsAreStoredSquared()
    {
        Assert.AreEqual(900f, Bands.NearSqrDistance, 0.001f);
        Assert.AreEqual(8100f, Bands.MidSqrDistance, 0.001f);
    }
}
