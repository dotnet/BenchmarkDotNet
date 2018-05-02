namespace BenchmarkDotNet.Characteristics
{
    public sealed class CharacteristicSet : CharacteristicObject<CharacteristicSet>
    {
        public CharacteristicSet()
        {
            Apply();
        }

        public CharacteristicSet(CharacteristicObject other)
        {
            Apply(other);
        }

        public CharacteristicSet(params CharacteristicObject[] others)
        {
            Apply(others);
        }

        protected override bool IsPropertyBag => true;
    }
}