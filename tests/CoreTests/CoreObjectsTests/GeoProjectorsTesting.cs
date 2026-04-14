using System;
using LiveChartsCore.Geo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreTests.CoreObjectsTests;

[TestClass]
public class GeoProjectorsTesting
{
    [TestMethod]
    public void MercatorProjectorProjectsOriginToCenter()
    {
        var projector = new MercatorProjector(360, 180, 0, 0);
        // lon=0, lat=0 should map to center of map
        var result = projector.ToMap(new double[] { 0, 0 });

        Assert.IsTrue(Math.Abs(result[0] - 180) < 0.1f);
        Assert.IsTrue(Math.Abs(result[1] - 90) < 0.1f);
    }

    [TestMethod]
    public void MercatorProjectorProjectsTopLeft()
    {
        var projector = new MercatorProjector(360, 180, 0, 0);
        // lon=-180, lat=0 should be at left edge
        var result = projector.ToMap(new double[] { -180, 0 });

        Assert.IsTrue(Math.Abs(result[0] - 0) < 0.1f);
    }

    [TestMethod]
    public void MercatorProjectorProjectsTopRight()
    {
        var projector = new MercatorProjector(360, 180, 0, 0);
        // lon=180, lat=0 should be at right edge
        var result = projector.ToMap(new double[] { 180, 0 });

        Assert.IsTrue(Math.Abs(result[0] - 360) < 0.1f);
    }

    [TestMethod]
    public void MercatorProjectorRespectsOffset()
    {
        var projector = new MercatorProjector(360, 180, 50, 25);
        var result = projector.ToMap(new double[] { -180, 0 });

        Assert.IsTrue(Math.Abs(result[0] - 50) < 0.1f);
    }

    [TestMethod]
    public void MercatorProjectorProperties()
    {
        var projector = new MercatorProjector(800, 600, 10, 20);
        Assert.IsTrue(Math.Abs(projector.MapWidth - 800) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.MapHeight - 600) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.XOffset - 10) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.YOffset - 20) < 0.001f);
    }

    [TestMethod]
    public void MercatorProjectorPreferredRatio()
    {
        var ratio = MercatorProjector.PreferredRatio;
        Assert.IsTrue(ratio.Length == 2);
        Assert.IsTrue(Math.Abs(ratio[0] - 1) < 0.001f);
        Assert.IsTrue(Math.Abs(ratio[1] - 1) < 0.001f);
    }

    [TestMethod]
    public void MercatorProjectorIsAlwaysVisible()
    {
        var projector = new MercatorProjector(360, 180, 0, 0);
        Assert.IsTrue(projector.IsVisible(0, 0));
        Assert.IsTrue(projector.IsVisible(180, 90));
        Assert.IsTrue(projector.IsVisible(-180, -90));
    }

    [TestMethod]
    public void OrthographicProjectorProjectsCenterToScreenCenter()
    {
        var projector = new OrthographicProjector(500, 500, 0, 0, 0, 0);
        var result = projector.ToMap(new double[] { 0, 0 });

        Assert.IsTrue(Math.Abs(result[0] - 250) < 0.1f);
        Assert.IsTrue(Math.Abs(result[1] - 250) < 0.1f);
    }

    [TestMethod]
    public void OrthographicProjectorCenterPointIsVisible()
    {
        var projector = new OrthographicProjector(500, 500, 0, 0, 0, 0);
        Assert.IsTrue(projector.IsVisible(0, 0));
    }

    [TestMethod]
    public void OrthographicProjectorOppositePointIsNotVisible()
    {
        var projector = new OrthographicProjector(500, 500, 0, 0, 0, 0);
        Assert.IsTrue(!projector.IsVisible(180, 0));
    }

    [TestMethod]
    public void OrthographicProjectorPointJustBeyondHorizonIsNotVisible()
    {
        var projector = new OrthographicProjector(500, 500, 0, 0, 0, 0);
        Assert.IsTrue(!projector.IsVisible(91, 0));
    }

    [TestMethod]
    public void OrthographicProjectorPointJustBeforeHorizonIsVisible()
    {
        var projector = new OrthographicProjector(500, 500, 0, 0, 0, 0);
        Assert.IsTrue(projector.IsVisible(89, 0));
    }

    [TestMethod]
    public void OrthographicProjectorProperties()
    {
        var projector = new OrthographicProjector(800, 600, 10, 20, 45, 30);
        Assert.IsTrue(Math.Abs(projector.MapWidth - 800) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.MapHeight - 600) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.XOffset - 10) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.YOffset - 20) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.CenterLongitude - 45) < 0.001);
        Assert.IsTrue(Math.Abs(projector.CenterLatitude - 30) < 0.001);
        Assert.IsTrue(projector.Radius > 0);
    }

    [TestMethod]
    public void OrthographicProjectorPreferredRatio()
    {
        var ratio = OrthographicProjector.PreferredRatio;
        Assert.IsTrue(ratio.Length == 2);
        Assert.IsTrue(Math.Abs(ratio[0] - 1) < 0.001f);
        Assert.IsTrue(Math.Abs(ratio[1] - 1) < 0.001f);
    }

    [TestMethod]
    public void OrthographicProjectorRadiusIsHalfMinDimension()
    {
        var projector = new OrthographicProjector(800, 600, 0, 0, 0, 0);
        Assert.IsTrue(Math.Abs(projector.Radius - 300) < 0.001f);
    }

    [TestMethod]
    public void OrthographicProjectorScreenCenter()
    {
        var projector = new OrthographicProjector(800, 600, 50, 30, 0, 0);
        Assert.IsTrue(Math.Abs(projector.ScreenCenterX - 450) < 0.001f);
        Assert.IsTrue(Math.Abs(projector.ScreenCenterY - 330) < 0.001f);
    }

    [TestMethod]
    public void OrthographicProjectorVisibilityWithDifferentCenter()
    {
        // Globe facing New York (lon=-74, lat=40.7)
        var projector = new OrthographicProjector(500, 500, 0, 0, -74, 40.7);

        // Nearby should be visible
        Assert.IsTrue(projector.IsVisible(-74, 40.7));  // center
        Assert.IsTrue(projector.IsVisible(-80, 35));     // nearby

        // Tokyo (far side) should not be visible
        Assert.IsTrue(!projector.IsVisible(139.7, 35.7));
    }

    [TestMethod]
    public void OrthographicProjectorSymmetry()
    {
        var projector = new OrthographicProjector(500, 500, 0, 0, 0, 0);

        // Points at equal positive and negative longitude should mirror
        var left = projector.ToMap(new double[] { -45, 0 });
        var right = projector.ToMap(new double[] { 45, 0 });

        // Both should be equal distance from center
        var centerX = 250f;
        Assert.IsTrue(Math.Abs(Math.Abs(left[0] - centerX) - Math.Abs(right[0] - centerX)) < 0.1f);

        // Y coordinates should be equal
        Assert.IsTrue(Math.Abs(left[1] - right[1]) < 0.1f);
    }
}
