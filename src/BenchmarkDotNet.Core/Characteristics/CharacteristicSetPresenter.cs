using System.Collections.Generic;
using System.Linq;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class CharacteristicSetPresenter
    {
        public static readonly CharacteristicSetPresenter Default = new DefaultPresenter();
        public static readonly CharacteristicSetPresenter Display = new DisplayPresenter();
        public static readonly CharacteristicSetPresenter Folder = new FolderPresenter();
        public static readonly CharacteristicSetPresenter SourceCode = new SourceCodePresenter();

        public abstract string ToPresentation(CharacteristicObject obj);

        protected virtual IEnumerable<Characteristic> GetPresentableCharacteristics(CharacteristicObject obj, bool includeIgnoreOnApply = false) =>
            obj
                .GetCharacteristicsWithValues()
                .Where(c => c.IsPresentableCharacteristic(includeIgnoreOnApply));

        private class DefaultPresenter : CharacteristicSetPresenter
        {
            private const string Separator = "&";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.DefaultPresenter;

            public override string ToPresentation(CharacteristicObject obj)
            {
                var values = GetPresentableCharacteristics(obj)
                    .Select(c => c.FullId + "=" + CharacteristicPresenter.ToPresentation(obj, c));
                return string.Join(Separator, values);
            }
        }

        private class FolderPresenter : CharacteristicSetPresenter
        {
            private const string Separator = "_";
            private const string EqualsSeparator = "-";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.FolderPresenter;

            public override string ToPresentation(CharacteristicObject obj)
            {
                var values = GetPresentableCharacteristics(obj)
                    .Select(c => c.Id + EqualsSeparator + CharacteristicPresenter.ToPresentation(obj, c));
                return string.Join(Separator, values);
            }
        }

        private class DisplayPresenter : CharacteristicSetPresenter
        {
            private const string Separator = ", ";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.DefaultPresenter;

            public override string ToPresentation(CharacteristicObject obj)
            {
                var values = GetPresentableCharacteristics(obj)
                    .Select(c => c.Id + "=" + CharacteristicPresenter.ToPresentation(obj, c));
                return string.Join(Separator, values);
            }
        }

        private class SourceCodePresenter : CharacteristicSetPresenter
        {
            private const string Separator = "; ";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.SourceCodePresenter;

            public override string ToPresentation(CharacteristicObject obj)
            {
                var values = GetPresentableCharacteristics(obj, includeIgnoreOnApply: true)
                    .Select(c => CharacteristicPresenter.ToPresentation(obj, c));
                return string.Join(Separator, values);
            }
        }
    }
}