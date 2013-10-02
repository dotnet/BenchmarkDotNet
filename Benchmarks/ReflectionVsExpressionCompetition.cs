using System;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ReflectionVsExpressionCompetition : BenchmarkCompetition
    {
        private const int IterationCount = 200000;

        private FieldInfo field;
        private object someObject;
        private Func<object, object, object> setFieldDelegate;

        private struct SomeStruct
        {
            private int field;
        }

        protected override void Prepare()
        {
            field = typeof(SomeStruct).GetField("field", BindingFlags.NonPublic | BindingFlags.Instance);
            someObject = new SomeStruct();
        }

        [BenchmarkMethod]
        public void SetFieldViaReflection()
        {
            for (int i = 0; i < IterationCount; i++)
                field.SetValue(someObject, i);
        }

        [BenchmarkMethodInitialize]
        public void SetFieldViaExpressionInitialize()
        {
            ParameterExpression xValue = Expression.Parameter(typeof(object));
            ParameterExpression xContainer = Expression.Parameter(typeof(object));
            ParameterExpression xTypedContainer = Expression.Parameter(typeof(SomeStruct));
            Expression<Func<object, object, object>> xSetField = Expression
                .Lambda<Func<object, object, object>>(
                    Expression.Block(new[] { xTypedContainer },
                        Expression.Assign(
                            xTypedContainer,
                            Expression.Convert(xContainer, typeof(SomeStruct))),
                        Expression.Assign(
                            Expression.Field(xTypedContainer, field),
                            Expression.Convert(xValue, typeof(int))),
                        Expression.Convert(xTypedContainer, typeof(object))),
                    xContainer, xValue);
            setFieldDelegate = xSetField.Compile();
        }

        [BenchmarkMethod]
        public void SetFieldViaExpression()
        {
            for (int i = 0; i < IterationCount; i++)
                someObject = setFieldDelegate(someObject, i);
        }
    }
}