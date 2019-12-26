namespace BenchmarkDotNet.Helpers
{
    public class UnitPresentation
    {
        public static readonly UnitPresentation Default = new UnitPresentation(true, 0);
        public static readonly UnitPresentation Invisible = new UnitPresentation(false, 0);

        public bool IsVisible { get; private set; }
        public int MinUnitWidth { get; private set; }

        public UnitPresentation(bool isVisible, int minUnitWidth)
        {
            IsVisible = isVisible;
            MinUnitWidth = minUnitWidth;
        }
        
        public static UnitPresentation FromVisibility(bool isVisible) => new UnitPresentation(isVisible, 0);

        public static UnitPresentation FromWidth(int unitWidth) => new UnitPresentation(true, unitWidth);
    }
}