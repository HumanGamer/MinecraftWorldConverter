using System.Collections.Generic;

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
    }
}