using LiveChartsGeneratedCode;

namespace SnapshotTests;

public static class Extensions
{
    extension(SourceGenSKChart chart)
    {
        public void PointerAt(double x, double y)
        {
            chart.CoreChart._isPointerIn = true;
            chart.CoreChart._isToolTipOpen = true;
            chart.CoreChart._pointerPosition = new(x, y);
        }

        public void Snapshot(string name)
        {
            if (!Directory.Exists("Snapshots")) _ = Directory.CreateDirectory("Snapshots");
            var path = Path.Combine("Snapshots", $"{name}.png");
            chart.SaveImage(path);
        }
    }
}
