using BenchmarkDotNet.Characteristics;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Jobs
{
    public class MetaMode : JobMode<MetaMode>
    {
        [PublicAPI] public static readonly Characteristic<bool> BaselineCharacteristic = CreateHiddenCharacteristic<bool>(nameof(Baseline));
        [PublicAPI] public static readonly Characteristic<bool> IsMutatorCharacteristic = CreateIgnoreOnApplyCharacteristic<bool>(nameof(IsMutator));
        [PublicAPI] public static readonly Characteristic<bool> IsDefaultCharacteristic = CreateHiddenCharacteristic<bool>(nameof(IsDefault));

        /// <summary>
        /// the categories of the job, the job equivalent of <see cref="Attributes.BenchmarkCategoryAttribute"/>
        /// <remarks>
        /// the characteristic is hidden on purpose: categories are metadata used to select jobs, they must not affect
        /// the generated job id (<see cref="JobIdGenerator"/>), the folder names, the summary
        /// nor the code generated for the child process
        /// </remarks>
        /// </summary>
        [PublicAPI] public static readonly Characteristic<IReadOnlyList<string>> CategoriesCharacteristic = CreateHiddenCharacteristic<IReadOnlyList<string>>(nameof(Categories));

        public bool Baseline
        {
            get => BaselineCharacteristic[this];
            set => BaselineCharacteristic[this] = value;
        }

        /// <summary>
        /// mutator job should not be added to the config, but instead applied to other jobs in given config
        /// </summary>
        public bool IsMutator
        {
            get => IsMutatorCharacteristic[this];
            set => IsMutatorCharacteristic[this] = value;
        }

        /// <summary>
        /// set to true if you want to specify custom default settings for default job used by console arguments parser
        /// </summary>
        public bool IsDefault
        {
            get => IsDefaultCharacteristic[this];
            set => IsDefaultCharacteristic[this] = value;
        }

        /// <summary>
        /// the categories of the job. Setting it overrides the categories that the job already has,
        /// use <see cref="AddCategories"/> if you want to add to them.
        /// </summary>
        public IReadOnlyList<string> Categories
        {
            get => CategoriesCharacteristic[this] ?? [];
            set => CategoriesCharacteristic[this] = Unique(value);
        }

        /// <summary>
        /// Adds the specified <paramref name="categories"/> to <see cref="Categories"/>.
        /// The categories that are already present are not duplicated (the comparison is case insensitive).
        /// </summary>
        public void AddCategories(IEnumerable<string> categories) => Categories = [.. Categories, .. categories];

        /// <summary>
        /// checks whether the job belongs to given category (the comparison is case insensitive)
        /// </summary>
        public bool HasCategory(string category) => Categories.Contains(category, StringComparer.OrdinalIgnoreCase);

        // the categories are used to select jobs, so we don't want the users to end up with the same category twice
        // just because they have used a different casing
        private static IReadOnlyList<string> Unique(IEnumerable<string> categories)
            => [.. categories.Distinct(StringComparer.OrdinalIgnoreCase)];
    }
}