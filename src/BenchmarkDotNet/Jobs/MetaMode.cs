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
        /// nor the code generated for the child process.
        /// it is not ignored on apply, even though categories are additive rather than overwritten: UnfreezeCopy is
        /// built on Apply, so an ignored characteristic would be dropped by every WithXxx call. The places that have
        /// to add rather than overwrite merge the categories explicitly, see <see cref="Configs.ImmutableConfigBuilder"/>
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

            // Assigning null removes the characteristic (see CharacteristicObject.SetValueCore), which is what an
            // empty set of categories has to do: a job that was given no categories must stay indistinguishable from
            // one that was never given any, otherwise `WithCategories(selection)` on an empty selection would mark
            // the job as changed.
            set => CategoriesCharacteristic[this] = Unique(value) is { Count: > 0 } unique ? unique : null!;
        }

        /// <summary>
        /// Adds the specified <paramref name="categories"/> to <see cref="Categories"/>.
        /// The categories that are already present are not duplicated (the comparison is case insensitive).
        /// </summary>
        public void AddCategories(IEnumerable<string> categories)
        {
            ArgumentNullException.ThrowIfNull(categories);

            Categories = [.. Categories, .. categories];
        }

        /// <summary>
        /// checks whether the job belongs to given category (the comparison is case insensitive)
        /// </summary>
        public bool HasCategory(string category) => Categories.Contains(category, StringComparer.OrdinalIgnoreCase);

        // Every category assigned to a job goes through here, which is why the arguments are validated here rather
        // than in the WithCategory/WithCategories extensions: the property and AddCategories are public too, and
        // guarding only the extensions would leave `job.Meta.Categories = null` reported as a null `source` thrown
        // out of Distinct, and would let a null category be stored and only fail much later.
        private static IReadOnlyList<string> Unique(IEnumerable<string> categories)
        {
            ArgumentNullException.ThrowIfNull(categories);

            var all = categories.ToArray();

            // A null category can never be selected and it survives the merging done when jobs are deduplicated,
            // so it would surface far away from the call that introduced it.
            if (all.Any(category => category is null))
                throw new ArgumentException("A job category must not be null.", nameof(categories));

            // the categories are used to select jobs, so we don't want the users to end up with the same category
            // twice just because they have used a different casing
            return [.. all.Distinct(StringComparer.OrdinalIgnoreCase)];
        }
    }
}
