using System;
using System.Linq.Expressions;
using System.Reflection;
using BenchmarkDotNet;

namespace Benchmarks
{
    public class ReflectionVsExpressionCompetition: CompetitionBase
    {
        private const int IterationCount = 200000;

        private struct SomeStruct
        {
            private int field;
        }

        [BenchmarkMethod]
        public Action SetFieldViaReflection()
        {
            FieldInfo field = typeof (SomeStruct).GetField("field", BindingFlags.NonPublic | BindingFlags.Instance);

            object someClass = new SomeStruct();
            Func<object, object, object> setMemberDelegate = (container, value) => {
                                                                 field.SetValue(container, value);
                                                                 return container;
                                                             };

            return () => {
                       for (int i = 0; i < IterationCount; i++)
                           someClass = setMemberDelegate(someClass, i);
                   };
        }

        [BenchmarkMethod]
        public Action SetFieldViaExpression()
        {
            FieldInfo field = typeof (SomeStruct).GetField("field", BindingFlags.NonPublic | BindingFlags.Instance);

            ParameterExpression xValue = Expression.Parameter(typeof (object));
            ParameterExpression xContainer = Expression.Parameter(typeof (object));
            ParameterExpression xTypedContainer = Expression.Parameter(typeof (SomeStruct));
            Expression<Func<object, object, object>> xSetField = Expression
                .Lambda<Func<object, object, object>>(
                    Expression.Block(new[] { xTypedContainer },
                        Expression.Assign(
                            xTypedContainer,
                            Expression.Convert(xContainer, typeof (SomeStruct))),
                        Expression.Assign(
                            Expression.Field(xTypedContainer, field),
                            Expression.Convert(xValue, typeof (int))),
                        Expression.Convert(xTypedContainer, typeof (object))),
                    xContainer, xValue);

            object someClass = new SomeStruct();
            Func<object, object, object> setFieldDelegate = xSetField.Compile();

            return () => {
                       for (int i = 0; i < IterationCount; i++)
                           someClass = setFieldDelegate(someClass, i);
                   };
        }
    }
}