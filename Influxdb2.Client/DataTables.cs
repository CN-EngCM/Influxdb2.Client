﻿using Influxdb2.Client.Datas;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Influxdb2.Client
{
    /// <summary>
    /// 表示查询结果
    /// </summary>
    public class DataTables : IEnumerable<IDataTable>
    {
        /// <summary>
        /// 所有表格
        /// </summary>
        private readonly IList<IDataTable> tables;

        /// <summary>
        /// 查询结果
        /// </summary>
        /// <param name="tables">表格</param>
        public DataTables(IList<IDataTable> tables)
        {
            this.tables = tables;
        }

        /// <summary>
        /// 以_time列为rowKey，转换为强类型
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public IList<TModel> ToModels<TModel>() where TModel : new()
        {
            return this.ToModels<TModel>(Columns.Time);
        }

        /// <summary>
        /// 转换为强类型
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="rowKey">rowKey值相同的行，将整合到一个model实例</param>
        /// <exception cref="ArgumentException"></exception>
        /// <returns></returns>
        public IList<TModel> ToModels<TModel>(Columns rowKey) where TModel : new()
        {
            if (rowKey.IsEmpty)
            {
                throw new ArgumentException("必须指定至少一个column");
            }

            var result = new List<TModel>();
            var descriptor = ModelDescriptor.Get(typeof(TModel));
            var rowGroups = this
                .SelectMany(item => item.Rows)
                .GroupBy(item => item[rowKey], ColumnValuesEqualityComparer.Instance);

            foreach (var group in rowGroups)
            {
                var model = new TModel();
                var firstRow = group.First(); // 同一组的非field属性，共用分组的第一条记录的值              
                var fieldValueMap = default(Dictionary<string, string?>?);

                foreach (var property in descriptor.PropertyDescriptors)
                {
                    if (firstRow.TryGetValue(property.Name, out var value))
                    {
                        property.SetValue(model, value!);
                    }
                    else if (property.IsFieldColumn == true)
                    {
                        if (fieldValueMap == null)
                        {
                            fieldValueMap = GetFiledValueMap(group);
                        }

                        if (fieldValueMap.TryGetValue(property.Name, out value))
                        {
                            property.SetValue(model, value!);
                        }
                    }
                }

                result.Add(model);
            }

            return result;
        }

        /// <summary>
        /// 获取_field与_value的映射关系
        /// </summary>
        /// <param name="rows"></param>
        /// <returns></returns>
        private static Dictionary<string, string?> GetFiledValueMap(IEnumerable<IDataRow> rows)
        {
            const string FieldName = "_field";
            const string Valuename = "_value";

            var fieldValues = new Dictionary<string, string?>();
            foreach (var row in rows)
            {
                if (row.TryGetValue(FieldName, out var _field))
                {
                    if (_field != null && row.TryGetValue(Valuename, out var _value))
                    {
                        fieldValues.TryAdd(_field, _value);
                    }
                }
            }
            return fieldValues;
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        public IEnumerator<IDataTable> GetEnumerator()
        {
            return this.tables.GetEnumerator();
        }

        /// <summary>
        /// 获取迭代器
        /// </summary>
        /// <returns></returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.tables.GetEnumerator();
        }

        /// <summary>
        /// ColumnValue集合相等比较
        /// </summary>
        private class ColumnValuesEqualityComparer : IEqualityComparer<ColumnValue[]>
        {
            /// <summary>
            /// 获取实例
            /// </summary>
            public static IEqualityComparer<ColumnValue[]> Instance { get; } = new ColumnValuesEqualityComparer();

            /// <summary>
            /// 是否相等
            /// </summary>
            /// <param name="x"></param>
            /// <param name="y"></param>
            /// <returns></returns>
            public bool Equals(ColumnValue[] x, ColumnValue[] y)
            {
                // 比较哈希即可
                return x.Length == y.Length;
            }

            /// <summary>
            /// 获取哈希
            /// </summary>
            /// <param name="obj"></param>
            /// <returns></returns>
            public int GetHashCode(ColumnValue[] obj)
            {
                var hashCode = 0;
                foreach (var item in obj)
                {
                    hashCode ^= item.GetHashCode();
                }
                return hashCode;
            }
        }
    }
}
