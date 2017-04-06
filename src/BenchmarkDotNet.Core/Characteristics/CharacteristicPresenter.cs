﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
                var collection = value as IList;
                if (collection != null)
                    return ToPresentation(collection);

                return ToPresentation(value);
            }

            // string.Join(separator, nonGenericCollection) is translated to string.Join(separator, params object[]) with single object!! (collection)
            // and ends up with exact the same output as collection.ToString() (typeName[])
            // so I needed to implement this on my own
            private static string ToPresentation(IList collection)
            {
                if (collection.Count == 0)
                    return "Empty";

                var buffer = new StringBuilder(collection.Count * 10);
                for (int i = 0; i < collection.Count - 1; i++)
                {
                    buffer.Append(ToPresentation(collection[i]));
                    buffer.Append(',');
                }
                buffer.Append(ToPresentation(collection[collection.Count - 1]));

                return buffer.ToString();
            }

            private static string ToPresentation(object value)
            {
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