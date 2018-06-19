using System;
using System.Collections;
using System.Text;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Helpers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Portability;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class CharacteristicPresenter
    {
        public static readonly CharacteristicPresenter DefaultPresenter = new DefaultCharacteristicPresenter();
        public static readonly CharacteristicPresenter FolderPresenter = new FolderCharacteristicPresenter();
        public static readonly CharacteristicPresenter SummaryPresenter = new DefaultCharacteristicPresenter();
        public static readonly CharacteristicPresenter SourceCodePresenter = new SourceCodeCharacteristicPresenter();

        public abstract string ToPresentation(CharacteristicObject obj, Characteristic characteristic);
        
        public abstract string ToPresentation(object characteristicValue, Characteristic characteristic);

        private class DefaultCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(CharacteristicObject obj, Characteristic characteristic) 
                => obj.HasValue(characteristic) 
                    ? ToPresentation(characteristic[obj], characteristic) 
                    : "Default";

            public override string ToPresentation(object value, Characteristic characteristic)
            {
                if (!(value is string) && value is IEnumerable collection)
                    return ToPresentation(collection);

                if (characteristic == EnvironmentMode.AffinityCharacteristic && value is IntPtr intPtr)
                    return ToPresentation(intPtr);

                return ToPresentation(value);
            }

            // string.Join(separator, nonGenericCollection) is translated to string.Join(separator, params object[]) with single object!! (collection)
            // and ends up with exact the same output as collection.ToString() (typeName[])
            // so I needed to implement this on my own
            private static string ToPresentation(IEnumerable collection)
            {
                var buffer = new StringBuilder();
                bool first = true;
                foreach (var item in collection)
                {
                    if (!first)
                        buffer.Append(',');
                    else
                        first = false;

                    buffer.Append(ToPresentation(item));
                }

                if (buffer.Length == 0)
                    return "Empty";

                return buffer.ToString();
            }

            private static string ToPresentation(object value) 
                => (value as IFormattable)?.ToString(null, HostEnvironmentInfo.MainCultureInfo)
                      ?? value?.ToString()
                      ?? "";

            private static string ToPresentation(IntPtr processorAffinity)
                => (RuntimeInformation.GetCurrentPlatform() == Platform.X64
                        ? Convert.ToString(processorAffinity.ToInt64(), 2)
                        : Convert.ToString(processorAffinity.ToInt32(), 2))
                   .PadLeft(Environment.ProcessorCount, '0');
        }

        private class SourceCodeCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(CharacteristicObject obj, Characteristic characteristic)
                => ToPresentation(characteristic[obj], characteristic);

            public override string ToPresentation(object characteristicValue, Characteristic characteristic)
            {
                // TODO: DO NOT hardcode Characteristic suffix
                var id = characteristic.Id;
                var type = characteristic.DeclaringType.FullName;
                var value = SourceCodeHelper.ToSourceCode(characteristicValue);
                return $"{type}.{id}Characteristic[job] = {value}";
            }
        }

        private class FolderCharacteristicPresenter : CharacteristicPresenter
        {
            public override string ToPresentation(CharacteristicObject obj, Characteristic characteristic) 
                => obj.HasValue(characteristic)
                    ? ToPresentation(characteristic[obj], characteristic)
                    : "Default";

            public override string ToPresentation(object characteristicValue, Characteristic characteristic)
                => FolderNameHelper.ToFolderName(characteristicValue);
        }
    }
}