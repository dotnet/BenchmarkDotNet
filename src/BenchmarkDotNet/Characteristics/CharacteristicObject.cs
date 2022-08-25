using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;

using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace BenchmarkDotNet.Characteristics
{
    // TODO: better naming.
    [DynamicallyAccessedMembers(CharacteristicMemberTypes)]
    public abstract class CharacteristicObject
    {
        #region IdCharacteristic

        internal const DynamicallyAccessedMemberTypes CharacteristicMemberTypes =
            DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties
            | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields;

        protected static string ResolveId(CharacteristicObject obj, string actual)
        {
            if (!string.IsNullOrEmpty(actual) && actual != IdCharacteristic.FallbackValue)
                return actual;

            string result = CharacteristicSetPresenter.Display.ToPresentation(obj);

            if (result.Length == 0)
                result = IdCharacteristic.FallbackValue;

            return result;
        }

        public static readonly Characteristic<string> IdCharacteristic = Characteristic.Create<CharacteristicObject, string>(nameof(Id), ResolveId, "Default", true);
        #endregion

        #region Fields & ctor

        private Dictionary<Characteristic, object> sharedValues;
        private bool frozen;

        protected CharacteristicObject()
        {
            Owner = null;
            sharedValues = new Dictionary<Characteristic, object>();
        }

        protected CharacteristicObject(string id) : this()
        {
            if (!string.IsNullOrEmpty(id))
            {
                IdCharacteristic[this] = id;
            }
        }
        #endregion

        #region Assertions

        private void AssertNotFrozen()
        {
            if (Frozen)
            {
                throw new InvalidOperationException($"The current object {this} is frozen. Create a copy to modify.");
            }
        }

        private void AssertIsRoot()
        {
            if (Owner != null)
            {
                throw new InvalidOperationException(
                    "The current operation allowed for root nodes only, " +
                    $"but the value {this} is attached to another node, {Owner}.");
            }
        }

        private void AssertIsNonFrozenRoot()
        {
            AssertNotFrozen();
            AssertIsRoot();
        }

        private static void AssertIsAssignable(Characteristic characteristic, object value)
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

        private CharacteristicObject Owner { get; set; }

        protected CharacteristicObject OwnerOrSelf => Owner ?? this;

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
           IsPropertyBag
                ? sharedValues.Keys.Where(c => !c.IgnoreOnApply).OrderBy(c => c.Id)
                : this.GetAllCharacteristics().Where(c => !c.IgnoreOnApply);

        private IEnumerable<Characteristic> GetCharacteristicsToApply(CharacteristicObject other)
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
            if (sharedValues.TryGetValue(characteristic, out var result))
                return !ReferenceEquals(result, Characteristic.EmptyValue);

            return false;
        }

        internal T GetValue<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] T>(Characteristic<T> characteristic)
        {
            return (T)GetValue((Characteristic)characteristic);
        }

        internal object GetValue(Characteristic characteristic)
        {
            if (!sharedValues.TryGetValue(characteristic, out var result))
                result = Characteristic.EmptyValue;

            return ResolveCore(characteristic, result);
        }

        private object ResolveCore(Characteristic characteristic, object result)
        {
            return characteristic.ResolveValueCore(this, result);
        }
        #endregion

        #region Resolve
        public T ResolveValue<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] T>(Characteristic<T> characteristic, IResolver resolver)
        {
            return resolver.Resolve(this, characteristic);
        }

        public T ResolveValue<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] T>(Characteristic<T> characteristic, IResolver resolver, T defaultValue)
        {
            return resolver.Resolve(this, characteristic, defaultValue);
        }

        public object ResolveValue(Characteristic characteristic, IResolver resolver)
        {
            return resolver.Resolve(this, characteristic);
        }

        public object ResolveValue(Characteristic characteristic, IResolver resolver, object defaultValue)
        {
            return resolver.Resolve(this, characteristic, defaultValue);
        }

        public T ResolveValue<[DynamicallyAccessedMembers(CharacteristicObject.CharacteristicMemberTypes)] T>(Characteristic<T> characteristic, T defaultValue)
        {
            return HasValue(characteristic) ? GetValue(characteristic) : (T)characteristic.ResolveValueCore(this, defaultValue);
        }

        [PublicAPI]
        public object ResolveValue(Characteristic characteristic, object defaultValue)
        {
            return HasValue(characteristic) ? GetValue(characteristic) : characteristic.ResolveValueCore(this, defaultValue);
        }

        public T? ResolveValueAsNullable<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] T>(Characteristic<T> characteristic) where T : struct
        {
            return HasValue(characteristic) ? GetValue(characteristic) : (T?)null;
        }
        #endregion

        #region Set value
        internal void SetValue<[DynamicallyAccessedMembers(CharacteristicMemberTypes)] T>(Characteristic<T> characteristic, T value)
        {
            SetValue((Characteristic)characteristic, value);
        }

        internal void SetValue(Characteristic characteristic, object value)
        {
            AssertNotFrozen();

            if (characteristic.HasChildCharacteristics)
            {
                AssertIsAssignable(characteristic, value);

                var oldObjectValue = (CharacteristicObject)GetValue(characteristic);
                var newObjectValue = (CharacteristicObject)ResolveCore(characteristic, value);

                if (!ReferenceEquals(oldObjectValue, newObjectValue))
                {
                    oldObjectValue?.DetachFromOwner(characteristic);
                    newObjectValue?.AttachToOwner(OwnerOrSelf, characteristic);
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

                    var characteristicObject = (CharacteristicObject)ResolveCore(characteristic, value);
                    characteristicObject.SetOwnerCore(OwnerOrSelf);

                    sharedValues[characteristic] = characteristicObject;
                }
                else
                {
                    sharedValues[characteristic] = value;
                }
            }
        }

        private void SetOwnerCore(CharacteristicObject newOwner)
        {
            if (newOwner == null)
                throw new ArgumentNullException(nameof(newOwner));

            AssertNotFrozen();
            newOwner.AssertIsNonFrozenRoot();

            Owner = newOwner;
            sharedValues = newOwner.sharedValues;
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

            Owner = null;
            sharedValues = new Dictionary<Characteristic, object>();
            frozen = false;

            oldValues.Remove(thisCharacteristic);
            foreach (var characteristic in GetCharacteristicsToApply())
            {
                if (oldValues.TryGetValue(characteristic, out var value))
                {
                    oldValues.Remove(characteristic);
                    SetValueCore(characteristic, value);
                }
            }
        }

        private void AttachToOwner(CharacteristicObject newOwner, Characteristic thisCharacteristic)
        {
            if (newOwner == null)
                throw new ArgumentNullException(nameof(newOwner));
            if (IsPropertyBag)
                throw new InvalidOperationException(
                    $"The property bag {this} cannot be used as characteristic's value.");

            AssertIsNonFrozenRoot();
            newOwner.AssertIsNonFrozenRoot();

            var oldValues = sharedValues;

            newOwner.SetValueOnAttach(thisCharacteristic, this);
            foreach (var pair in oldValues)
            {
                newOwner.SetValueOnAttach(pair.Key, pair.Value);
            }
        }

        private void SetValueOnAttach(Characteristic characteristic, object value)
        {
            AssertIsAssignable(characteristic, value);

            if (characteristic.HasChildCharacteristics)
            {
                // DONTTOUCH: workaround on case there were no parent characteristic.
                var characteristicObject = (CharacteristicObject)GetValue(characteristic);
                characteristicObject?.DetachFromOwner(characteristic);
            }

            SetValueCore(characteristic, value);
        }
        #endregion
        #endregion

        #region Apply

        [PublicAPI]
        public void Apply(CharacteristicObject other) => ApplyCore(other);

        protected CharacteristicObject ApplyCore(CharacteristicObject other) =>
            ApplyCore(
                other,
                GetCharacteristicsToApply(other));

        private CharacteristicObject ApplyCore(
            [CanBeNull] CharacteristicObject other,
            [NotNull] IEnumerable<Characteristic> characteristicsToApply)
        {
            AssertNotFrozen();

            if (other == null)
                return this;

            foreach (var characteristic in characteristicsToApply)
            {
                if (!other.sharedValues.TryGetValue(characteristic, out var value))
                    continue;

                if (characteristic.HasChildCharacteristics)
                {
                    if (!HasValue(characteristic))
                    {
                        var characteristicObject = (CharacteristicObject)ResolveCore(characteristic, value);
                        if (characteristicObject != null)
                        {
                            value = Activator.CreateInstance(characteristicObject.GetType());
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

        [PublicAPI]
        public void Freeze() => FreezeCore();

        protected CharacteristicObject FreezeCore()
        {
            AssertIsRoot();

            if (!frozen)
                frozen = true;

            return this;
        }

        [PublicAPI]
        public CharacteristicObject UnfreezeCopy() => UnfreezeCopyCore();

        protected CharacteristicObject UnfreezeCopyCore()
        {
            AssertIsRoot();

            var newRoot = (CharacteristicObject)Activator.CreateInstance(GetType());
            newRoot.ApplyCore(this);

            return newRoot;
        }
        #endregion

        public string Id => IdCharacteristic[this];

        public override string ToString() => Id;
    }
}