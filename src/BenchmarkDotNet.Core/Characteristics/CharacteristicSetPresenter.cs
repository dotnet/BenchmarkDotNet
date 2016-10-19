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

        public abstract string ToPresentation(JobMode jobMode);

        protected virtual IEnumerable<Characteristic> GetPresentableCharacteristics(JobMode jobMode, bool withDontClone = false) =>
            jobMode
                .GetCharacteristicsWithValues()
                .Where(c => c.IsPresentableCharacteristic(withDontClone));

        private class DefaultPresenter : CharacteristicSetPresenter
        {
            private const string Separator = "&";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.DefaultPresenter;

            public override string ToPresentation(JobMode jobMode)
            {
                var values = GetPresentableCharacteristics(jobMode)
                    .Select(c => c.FullId + "=" + CharacteristicPresenter.ToPresentation(jobMode, c));
                return string.Join(Separator, values);
            }
        }

        private class FolderPresenter : CharacteristicSetPresenter
        {
            private const string Separator = "_";
            private const string EqualsSeparator = "-";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.FolderPresenter;

            public override string ToPresentation(JobMode jobMode)
            {
                var values = GetPresentableCharacteristics(jobMode)
                    .Select(c => c.Id + EqualsSeparator + CharacteristicPresenter.ToPresentation(jobMode, c));
                return string.Join(Separator, values);
            }
        }

        private class DisplayPresenter : CharacteristicSetPresenter
        {
            private const string Separator = ", ";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.DefaultPresenter;

            public override string ToPresentation(JobMode jobMode)
            {
                var values = GetPresentableCharacteristics(jobMode)
                    .Select(c => c.Id + "=" + CharacteristicPresenter.ToPresentation(jobMode, c));
                return string.Join(Separator, values);
            }
        }

        // TODO: add new T() line to the code.
        // OR: make this work with  CharacteristicSet ONLY!!!
        private class SourceCodePresenter : CharacteristicSetPresenter
        {
            private const string Separator = "; ";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.SourceCodePresenter;

            public override string ToPresentation(JobMode jobMode)
            {
                var values = GetPresentableCharacteristics(jobMode, withDontClone: true)
                    .Select(c => CharacteristicPresenter.ToPresentation(jobMode, c));
                return string.Join(Separator, values);
            }
        }
    }
}