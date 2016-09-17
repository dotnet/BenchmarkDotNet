using System.Linq;

namespace BenchmarkDotNet.Characteristics
{
    public abstract class CharacteristicSetPresenter
    {
        public static readonly CharacteristicSetPresenter Default = new DefaultPresenter();
        public static readonly CharacteristicSetPresenter Display = new DisplayPresenter();
        public static readonly CharacteristicSetPresenter Folder = new FolderPresenter();
        public static readonly CharacteristicSetPresenter SourceCode = new SourceCodePresenter();

        public abstract string ToPresentation(CharacteristicSet set);

        private class DefaultPresenter : CharacteristicSetPresenter
        {
            private const string Separator = "&";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.DefaultPresenter;

            public override string ToPresentation(CharacteristicSet set)
            {
                var values = set.GetValues().Where(c => !c.IsDefault).Select(c => c.Id + "=" + CharacteristicPresenter.ToPresentation(c));
                return string.Join(Separator, values);
            }
        }

        private class FolderPresenter : CharacteristicSetPresenter
        {
            private const string Separator = "_";
            private const string EqualsSeparator = "-";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.FolderPresenter;

            public override string ToPresentation(CharacteristicSet set)
            {
                var values = set.
                    GetValues().
                    Where(c => !c.IsDefault).
                    Select(c => c.GetDisplayId() + EqualsSeparator + CharacteristicPresenter.ToPresentation(c));
                return string.Join(Separator, values);
            }
        }

        private class DisplayPresenter : CharacteristicSetPresenter
        {
            private const string Separator = ", ";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.DefaultPresenter;

            public override string ToPresentation(CharacteristicSet set)
            {
                var values = set.GetValues().Where(c => !c.IsDefault).Select(c => c.GetDisplayId() + "=" + CharacteristicPresenter.ToPresentation(c));
                return string.Join(Separator, values);
            }
        }

        private class SourceCodePresenter : CharacteristicSetPresenter
        {
            private const string Separator = ", ";
            private static readonly CharacteristicPresenter CharacteristicPresenter = CharacteristicPresenter.SourceCodePresenter;

            public override string ToPresentation(CharacteristicSet set)
            {
                var values = set.GetValues().Where(c => !c.IsDefault).Select(c => CharacteristicPresenter.ToPresentation(c));
                return "new CharacteristicSet(" + string.Join(Separator, values) + ")";
            }
        }
    }
}