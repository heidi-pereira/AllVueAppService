using System.Collections.Generic;

namespace MIG.SurveyPlatform.MapGeneration.Serialization
{
    internal static class SerializationInfoExtensions
    {
        public static SerializableEnumerable<T> For<T>(this ISerializationInfo<T> info, IEnumerable<T> enumerable)
        {
            return new SerializableEnumerable<T>(enumerable, info);
        }
    }
}