using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Lynicon.Utility
{
    public static class EnumX
    {
        public static string DisplayName(this Enum en)
        {
            FieldInfo fi = en.GetType().GetField(en.ToString(), BindingFlags.Static | BindingFlags.Public);
            var attr = fi.GetCustomAttribute<DisplayAttribute>();
            if (attr != null)
                return attr.Name;
            else
                return en.ToString();
        }

        public static T Parse<T>(string val)
        {
            return (T)Enum.Parse(typeof(T), val);
        }

        public static string EnumDisplayName<T>(string val) where T : struct
        {
            T enumVal = Parse<T>(val);
            return (enumVal as Enum).DisplayName();
        }

        public static List<SelectListItem> EnumSelectList(Type enumType)
        {
            return EnumSelectList(enumType, null);
        }
        public static List<SelectListItem> EnumSelectList(Type enumType, object current)
        {
            bool isNullable = false;
            if (enumType.IsGenericType() && enumType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                enumType = Nullable.GetUnderlyingType(enumType);
                isNullable = true;
            }

            var values = Enum.GetValues(enumType).Cast<Enum>()
                .Select(v => new SelectListItem
                {
                    Selected = v.Equals(current),
                    Text = v.DisplayName(),
                    Value = v.ToString()
                }).ToList();

            if (isNullable)
            {
                values.Insert(0, new SelectListItem { Selected = (current == null), Text = "", Value = null });
            }

            return values;
        }
    }
}