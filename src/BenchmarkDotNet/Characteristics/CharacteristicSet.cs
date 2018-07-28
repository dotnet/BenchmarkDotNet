using JetBrains.Annotations;

namespace BenchmarkDotNet.Characteristics
{
    public sealed class CharacteristicSet : CharacteristicObject<CharacteristicSet>
    {
        [PublicAPI] public CharacteristicSet() => Apply();
        [PublicAPI] public CharacteristicSet(CharacteristicObject other) => Apply(other);
        [PublicAPI] public CharacteristicSet(params CharacteristicObject[] others) => Apply(others);

        protected override bool IsPropertyBag => true;
    }
}