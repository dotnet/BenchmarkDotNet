using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace BenchmarkDotNet.Engines
{
    public static class ConsumerExtensions
    {
        /// <summary>
        /// executes and consumes given <see cref="IEnumerable"/>
        /// <remarks>By using non-generic <see cref="IEnumerable"/> you pay for boxing. Use generic <see cref="IEnumerable{T}"/> if you can.</remarks>
        /// </summary>
        /// <param name="enumerable">non-generic <see cref="IEnumerable"/></param>
        /// <param name="consumer">instance of <see cref="Consumer"/>. Create it on your own once, store it in the field and just pass here</param>
        [PublicAPI]
        public static void Consume(this IEnumerable enumerable, Consumer consumer)
        {
            foreach (object item in enumerable)
            {
                consumer.Consume(item);
            }
        }

        /// <summary>
        /// executes and consumes given <see cref="IQueryable"/>
        /// <remarks>By using non-generic <see cref="IQueryable"/> you pay for boxing. Use generic <see cref="IQueryable{T}"/> if you can.</remarks>
        /// </summary>
        /// <param name="queryable">non-generic <see cref="IQueryable"/></param>
        /// <param name="consumer">instance of <see cref="Consumer"/>. Create it on your own once, store it in the field and just pass here</param>
        [PublicAPI]
        public static void Consume(this IQueryable queryable, Consumer consumer)
        {
            foreach (object item in queryable)
            {
                consumer.Consume(item);
            }
        }

        /// <summary>
        /// executes and consumes given <see cref="IEnumerable{T}"/>
        /// </summary>
        /// <param name="enumerable">generic <see cref="IEnumerable{T}"/></param>
        /// <param name="consumer">instance of <see cref="Consumer"/>. Create it on your own once, store it in the field and just pass here</param>
        [PublicAPI]
        public static void Consume<T>(this IEnumerable<T> enumerable, Consumer consumer)
        {
            foreach (T item in enumerable)
            {
                consumer.Consume<T>(in item);
            }
        }

        /// <summary>
        /// executes and consumes given <see cref="IQueryable{T}"/>
        /// </summary>
        /// <param name="queryable">generic <see cref="IQueryable{T}"/></param>
        /// <param name="consumer">instance of <see cref="Consumer"/>. Create it on your own once, store it in the field and just pass here</param>
        [PublicAPI]
        public static void Consume<T>(this IQueryable<T> queryable, Consumer consumer)
        {
            foreach (T item in queryable)
            {
                consumer.Consume<T>(in item);
            }
        }
    }
}