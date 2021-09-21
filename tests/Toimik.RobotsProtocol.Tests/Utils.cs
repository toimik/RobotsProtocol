namespace Toimik.RobotsProtocol.Tests
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    public class Utils
    {
        [ExcludeFromCodeCoverage]
        private Utils()
        {
        }

        public static T GetOnlyItem<T>(IEnumerable<T> items)
        {
            var enumerator = items.GetEnumerator();
            return GetOnlyItem(enumerator);
        }

        public static T GetOnlyItem<T>(IEnumerator<T> enumerator)
        {
            enumerator.MoveNext();
            var item = enumerator.Current;
            return item;
        }
    }
}