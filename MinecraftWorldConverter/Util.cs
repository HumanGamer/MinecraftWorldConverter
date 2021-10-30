using System.Collections.Generic;
using System.Diagnostics;
using fNbt;

namespace MinecraftWorldConverter
{
    public static class Util
    {
        public static Dictionary<string, object> GetPropertyMap(this object obj)
        {
            var properties = obj.GetType().GetProperties();
            Dictionary<string, object> propertyList = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                string name = property.Name;
                object value = property.GetValue(obj);
                propertyList[name] = value;
            }

            return propertyList;
        }
        
        public static void AddList<T>(this NbtCompound self, string name, IEnumerable<T> values)
        {
            var list = new NbtList(name);
            foreach (T s in values)
            {
                list.Add(s switch
                {
                    byte val => new NbtByte(val),
                    short val => new NbtShort(val),
                    int val => new NbtInt(val),
                    long val => new NbtLong(val),
                    _ => throw new MCWorldException("AddList: Unsupported type: " + typeof(T))
                });
            }
                    
            self.Add(list);
        }
    }
}