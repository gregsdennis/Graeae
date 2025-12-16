using Json.Pointer;

namespace Graeae.Models;

internal static class PointerExtensions
{
    public static string[] ToArray(this JsonPointer pointer)
    {
        var count = pointer.SegmentCount;
        var array = new string[count];
        for (int i = 0; i < count; i++)
        {
            array[i] = pointer[i].ToString();
        }

        return array;
    }
}