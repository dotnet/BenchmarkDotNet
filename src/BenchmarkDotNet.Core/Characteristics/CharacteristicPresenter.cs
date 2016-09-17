using System.Linq;
using BenchmarkDotNet.Core.Helpers;
using SimpleJson.Reflection;
using BenchmarkDotNet.Extensions;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class CharacteristicPresenter
    {
        public static readonly CharacteristicPresenter DefaultPresenter = new DefaultCharacteristicPresenter();
        public static readonly CharacteristicPresenter FolderPresenter = new FolderCharacteristicPresenter();
        public static readonly CharacteristicPresenter SummaryPresenter = new DefaultCharacteristicPresenter();
        public static readonly CharacteristicPresenter SourceCodePresenter = new SourceCodeCharacteristicPresenter();

        public abstract string ToPresentation(ICharacteristic characteristic);

        private class DefaultCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(ICharacteristic characteristic)
            {
                return characteristic.IsDefault ? "Default" : characteristic.ObjectValue.ToString();
            }
        }

        private class SourceCodeCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(ICharacteristic characteristic)
            {
                var valueType = ReflectionUtils.GetGenericTypeArguments(characteristic.GetType()).First().GetCorrectTypeName();
                var id = characteristic.Id;
                var value = SourceCodeHelper.ToSourceCode(characteristic.ObjectValue);
                return $"Characteristic<{valueType}>.Create(\"{id}\").Mutate({value})";
            }
        }

        private class FolderCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(ICharacteristic characteristic)
            {
                return characteristic.IsDefault ? "Default" : FolderNameHelper.ToFolderName(characteristic.ObjectValue);
            }
        }
    }
}