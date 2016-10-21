using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BenchmarkDotNet.Characteristics
{
    // TODO: better naming.
    public abstract class JobMode
    {
        #region IdCharacteristic
        private const string IdSeparator = "&";

        protected static string ResolveId(JobMode obj, string actual)
        {
            if (!string.IsNullOrEmpty(actual) && actual != IdCharacteristic.FallbackValue)
                return actual;

            var result = CharacteristicSetPresenter.Display.ToPresentation(obj);

            if (result.Length == 0)
                result = IdCharacteristic.FallbackValue;

            return result;
        }

        public static readonly Characteristic<string> IdCharacteristic = Characteristic.Create(
            (JobMode j) => j.Id,
            ResolveId, "Default", true);
        #endregion

        #region Fields & ctor
        private JobMode owner;
        private Dictionary<Characteristic, object> sharedValues;
        private bool frozen;

        protected JobMode()
        {
            owner = null;
            sharedValues = new Dictionary<Characteristic, object>();
        }

        protected JobMode(string id) : this()
        {
            if (!string.IsNullOrEmpty(id))
            {
                IdCharacteristic[this] = id;
            }
        }
        #endregion

        #region Assertions
        protected void AssertNotFrozen()
        {
            if (Frozen)
            {
                throw new InvalidOperationException($"The current object {this} is frozen. Create a copy to modify.");
            }
        }

        protected void AssertIsRoot()
        {
            if (Owner != null)
            {
                throw new InvalidOperationException(
                    "The current operation allowed for root nodes only, " +
                    $"but the value {this} is attached to another node, {Owner}.");
            }
        }

        protected void AssertIsNonFrozenRoot()
        {
            AssertNotFrozen();
            AssertIsRoot();
        }

        protected void AssertIsAssignable(Characteristic characteristic, object value)
        {
            if (ReferenceEquals(value, Characteristic.EmptyValue) || ReferenceEquals(value, null))
            {
                if (characteristic.HasChildCharacteristics)
                    throw new ArgumentNullException(characteristic.Id);

                return;
            }

            if (!characteristic.CharacteristicType.GetTypeInfo().IsInstanceOfType(value))
                throw new ArgumentException(
                    $"The value {value} is not assignable to {characteristic} property.",
                    characteristic.Id);
        }
        #endregion

        #region Properties
        protected JobMode Owner => owner;

        protected JobMode OwnerOrSelf => owner ?? this;

        public bool Frozen => Owner?.Frozen ?? frozen;

        protected virtual bool IsPropertyBag => false;

        public bool HasChanges => GetCharacteristicsWithValues().Any(c => c.IsPresentableCharacteristic());
        #endregion

        #region  GetCharacteristics helpers
        public IEnumerable<Characteristic> GetCharacteristicsWithValues() =>
            IsPropertyBag
                ? sharedValues.Keys.OrderBy(c => c.Id)
                : this.GetAllCharacteristics().Where(HasValue);

        private IEnumerable<Characteristic> GetCharacteristicsToApply() =>
             this.GetAllCharacteristics().Where(c => !c.IgnoreOnApply);

        private IEnumerable<Characteristic> GetCharacteristicsToApply(JobMode other)
        {
            var result = other.GetCharacteristicsToApply();
            if (GetType() != other.GetType() && !IsPropertyBag)
                result = result.Intersect(this.GetAllCharacteristics());

            return result;
        }
        #endregion

        #region Get or set value
        #region Get value
        public bool HasValue(Characteristic characteristic)
        {
            object result;
            if (sharedValues.TryGetValue(characteristic, out result))
                return !ReferenceEquals(result, Characteristic.EmptyValue);

            return false;
        }

        internal T GetValue<T>(Characteristic<T> characteristic)
        {
            return (T)GetValue((Characteristic)characteristic);
        }

        internal object GetValue(Characteristic characteristic)
        {
            object result;
            if (!sharedValues.TryGetValue(characteristic, out result))
                result = Characteristic.EmptyValue;

            return ResolveCore(characteristic, result);
        }

        private object ResolveCore(Characteristic characteristic, object result)
        {
            return characteristic.ResolveValueCore(this, result);
        }
        #endregion

        #region Resolve
        public T ResolveValue<T>(Characteristic<T> characteristic, IResolver resolver)
        {
            return resolver.Resolve(this, characteristic);
        }

        public object ResolveValue(Characteristic characteristic, IResolver resolver)
        {
            return resolver.Resolve(this, characteristic);
        }

        public T ResolveValue<T>(Characteristic<T> characteristic, T defaultValue)
        {
            return HasValue(characteristic) ? GetValue(characteristic) : (T)characteristic.ResolveValueCore(this, defaultValue);
        }

        public object ResolveValue(Characteristic characteristic, object defaultValue)
        {
            return HasValue(characteristic) ? GetValue(characteristic) : characteristic.ResolveValueCore(this, defaultValue);
        }

        public T? ResolveValueAsNullable<T>(Characteristic<T> characteristic) where T : struct
        {
            return HasValue(characteristic) ? GetValue(characteristic) : (T?)null;
        }
        #endregion

        #region Set value
        internal void SetValue<T>(Characteristic<T> characteristic, T value)
        {
            SetValue((Characteristic)characteristic, value);
        }

        internal void SetValue(Characteristic characteristic, object value)
        {
            AssertNotFrozen();

            if (characteristic.HasChildCharacteristics)
            {
                AssertIsAssignable(characteristic, value);

                var oldJobValue = (JobMode)GetValue(characteristic);
                var newJobValue = (JobMode)ResolveCore(characteristic, value);

                if (!ReferenceEquals(oldJobValue, newJobValue))
                {
                    oldJobValue?.DetachFromOwner(characteristic);
                    newJobValue?.AttachToOwner(OwnerOrSelf, characteristic);
                }
            }
            else
            {
                SetValueCore(characteristic, value);
            }
        }

        private void SetValueCore(Characteristic characteristic, object value)
        {
            AssertIsAssignable(characteristic, value);

            if (ReferenceEquals(value, Characteristic.EmptyValue) || ReferenceEquals(value, null))
            {
                sharedValues.Remove(characteristic);
            }
            else
            {
                if (characteristic.HasChildCharacteristics)
                {
                    if (sharedValues.ContainsKey(characteristic))
                        throw new ArgumentException(
                            $"The current node {this} has value for {characteristic} already.",
                            nameof(characteristic));

                    var jobMode = (JobMode)ResolveCore(characteristic, value);
                    jobMode.SetOwnerCore(OwnerOrSelf);

                    sharedValues[characteristic] = jobMode;
                }
                else
                {
                    sharedValues[characteristic] = value;
                }
            }
        }

        private void SetOwnerCore(JobMode newOwnerJob)
        {
            if (newOwnerJob == null)
                throw new ArgumentNullException(nameof(newOwnerJob));

            AssertNotFrozen();
            newOwnerJob.AssertIsNonFrozenRoot();

            owner = newOwnerJob;
            sharedValues = newOwnerJob.sharedValues;
            frozen = false;
        }

        private void DetachFromOwner(Characteristic thisCharacteristic)
        {
            AssertNotFrozen();
            if (IsPropertyBag)
                throw new InvalidOperationException("The property bag has no owner.");

            if (Owner == null)
                return;

            var oldValues = sharedValues;

            owner = null;
            sharedValues = new Dictionary<Characteristic, object>();
            frozen = false;

            oldValues.Remove(thisCharacteristic);
            foreach (var characteristic in GetCharacteristicsToApply())
            {
                object value;
                if (oldValues.TryGetValue(characteristic, out value))
                {
                    oldValues.Remove(characteristic);
                    SetValueCore(characteristic, value);
                }
            }
        }

        private void AttachToOwner(JobMode newOwnerJob, Characteristic thisCharacteristic)
        {
            if (newOwnerJob == null)
                throw new ArgumentNullException(nameof(newOwnerJob));
            if (IsPropertyBag)
                throw new InvalidOperationException(
                    $"The property bag {this} cannot be used as characteristic's value.");

            AssertIsNonFrozenRoot();
            newOwnerJob.AssertIsNonFrozenRoot();

            var oldValues = sharedValues;

            newOwnerJob.SetValueOnAttach(thisCharacteristic, this);
            foreach (var pair in oldValues)
            {
                newOwnerJob.SetValueOnAttach(pair.Key, pair.Value);
            }
        }

        private void SetValueOnAttach(Characteristic characteristic, object value)
        {
            AssertIsAssignable(characteristic, value);

            if (characteristic.HasChildCharacteristics)
            {
                // DONTTOUCH: workaround on case there were no parent characteristic.
                var jobMode = (JobMode)GetValue(characteristic);
                jobMode?.DetachFromOwner(characteristic);
            }

            SetValueCore(characteristic, value);
        }
        #endregion
        #endregion

        #region Apply
        public void Apply(JobMode other) => ApplyCore(other);

        protected JobMode ApplyCore(JobMode other) =>
            ApplyCore(
                other,
                GetCharacteristicsToApply(other));

        private JobMode ApplyCore(
            JobMode other,
            IEnumerable<Characteristic> characteristicsToApply)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));
            AssertNotFrozen();

            foreach (var characteristic in characteristicsToApply)
            {
                object value;
                if (!other.sharedValues.TryGetValue(characteristic, out value))
                    continue;

                if (characteristic.HasChildCharacteristics)
                {
                    if (!HasValue(characteristic))
                    {
                        var jobMode = (JobMode)ResolveCore(characteristic, value);
                        if (jobMode != null)
                        {
                            value = Activator.CreateInstance(jobMode.GetType());
                        }

                        SetValueCore(characteristic, value);
                    }
                }
                else
                {
                    SetValueCore(characteristic, value);
                }
            }

            return this;
        }
        #endregion

        #region Freeze / unfreeze
        public void Freeze() => FreezeCore();

        protected JobMode FreezeCore()
        {
            AssertIsRoot();

            if (!frozen)
                frozen = true;

            return this;
        }

        public JobMode UnfreezeCopy() => UnfreezeCopyCore();

        protected JobMode UnfreezeCopyCore()
        {
            AssertIsRoot();

            var newRoot = (JobMode)Activator.CreateInstance(GetType());
            newRoot.ApplyCore(this);

            return newRoot;
        }
        #endregion

        public string Id => IdCharacteristic[this];

        public override string ToString() => Id;
    }
}