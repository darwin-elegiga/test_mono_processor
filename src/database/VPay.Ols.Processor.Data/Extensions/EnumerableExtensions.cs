using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace VPay.Ols.Processor.Data.Extensions;

public static class EnumerableExtensions
{
    public static DataTable ToDataTable<T>(this IEnumerable<T> iList)
    {
        var dataTable = new DataTable();
        var propertyDescriptorCollection = TypeDescriptor.GetProperties(typeof(T));

        for (var i = 0; i < propertyDescriptorCollection.Count; i++)
        {
            PropertyDescriptor propertyDescriptor = propertyDescriptorCollection[i];
            Type type = propertyDescriptor.PropertyType;

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = Nullable.GetUnderlyingType(type);
            }

            dataTable.Columns.Add(propertyDescriptor.Name, type);
        }

        var values = new object[propertyDescriptorCollection.Count];
        foreach (var iListItem in iList)
        {
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = propertyDescriptorCollection[i].GetValue(iListItem);
            }
            dataTable.Rows.Add(values);
        }

        return dataTable;
    }
}
