using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LocalizationTool
{
    public static class Utils
    {
        public static byte[] SerializeHeader<T>(this T header) where T : struct
        {
            int position = 0;
            int structSize = Marshal.SizeOf(typeof(T));

            byte[] rawData = new byte[structSize];

            IntPtr buffer = Marshal.AllocHGlobal(structSize);

            Marshal.StructureToPtr(header, buffer, false);
            Marshal.Copy(buffer, rawData, position, structSize);

            Marshal.FreeHGlobal(buffer);

            return rawData;
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> dic, IEnumerable<KeyValuePair<TKey, TValue>> dicToAdd)
        {
            foreach (var item in dicToAdd)
            {
                dic[item.Key] = item.Value;
            }    
        }

        static public IEnumerable<T> Repeat<T>(Func<T> function, int iteration)
        {
            return Enumerable.Repeat(function, iteration).Select(x => x.Invoke());
        }
    }
}

