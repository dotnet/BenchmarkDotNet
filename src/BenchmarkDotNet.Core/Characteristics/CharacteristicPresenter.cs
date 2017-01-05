using System;
using BenchmarkDotNet.Core.Helpers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class CharacteristicPresenter
    {
        public static readonly CharacteristicPresenter DefaultPresenter = new DefaultCharacteristicPresenter();
        public static readonly CharacteristicPresenter FolderPresenter = new FolderCharacteristicPresenter();
        public static readonly CharacteristicPresenter SummaryPresenter = new DefaultCharacteristicPresenter();
        public static readonly CharacteristicPresenter SourceCodePresenter = new SourceCodeCharacteristicPresenter();

        public abstract string ToPresentation(CharacteristicObject obj, Characteristic characteristic);

        private class DefaultCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(CharacteristicObject obj, Characteristic characteristic)
            {
                if (!obj.HasValue(characteristic))
                    return "Default";

                var value = characteristic[obj];
                return (value as IFormattable)?.ToString(null, HostEnvironmentInfo.MainCultureInfo)
                    ?? value?.ToString() 
                    ?? "";
            }
        }

        private class SourceCodeCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(CharacteristicObject obj, Characteristic characteristic)
            {
                // TODO: DO NOT hardcode Characteristic suffix
                var id = characteristic.Id;
                var type = characteristic.DeclaringType.FullName;
                var value = SourceCodeHelper.ToSourceCode(characteristic[obj]);
                return $"{type}.{id}Characteristic[job] = {value}";
            }
        }

        private class FolderCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(CharacteristicObject obj, Characteristic characteristic)
            {
                return obj.HasValue(characteristic)
                    ? FolderNameHelper.ToFolderName(characteristic[obj])
                    : "Default";
            }
        }
    }
}