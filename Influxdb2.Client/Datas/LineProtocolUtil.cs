﻿using System;
using System.Data;
using System.Text;

namespace Influxdb2.Client.Datas
{
    /// <summary>
    /// LineProtocol工具
    /// </summary>
    static class LineProtocolUtil
    {
        /// <summary>
        /// 对标签或字段名进行编码
        /// </summary>
        /// <param name="name">名称</param>
        /// <exception cref="NoNullAllowedException"></exception>
        /// <returns></returns>
        public static string Encode(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new NoNullAllowedException($"标签或字段名的值不能为空");
            }

            var span = name.AsSpan();
            if (span.IndexOfAny(",= \r\n") >= 0)
            {
                var buidler = new StringBuilder();
                foreach (var c in span)
                {
                    if (c == '\r' || c == '\n')
                    {
                        continue;
                    }
                    if (c == ',' || c == '=' || c == ' ')
                    {
                        buidler.Append('\\');
                    }
                    buidler.Append(c);
                }
                return buidler.ToString();
            }
            return name;
        }         

        /// <summary>
        /// 对字段内容进行编码
        /// </summary>
        /// <param name="value">字段内容</param> 
        /// <returns></returns>
        public static string? EncodeFieldValue(object? value)
        {
            var stringValue = value?.ToString();
            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }

            var span = stringValue.AsSpan();
            if (span.IndexOfAny("\"\r\n") >= 0)
            {
                var buidler = new StringBuilder();
                foreach (var c in span)
                {
                    if (c == '\r' || c == '\n')
                    {
                        continue;
                    }
                    if (c == '"')
                    {
                        buidler.Append('\\');
                    }
                    buidler.Append(c);
                }
                return buidler.ToString();
            }
            return stringValue;
        }


        /// <summary>
        /// 创建字段值的编码器
        /// </summary>
        /// <param name="fieldType">字段类型</param>
        /// <returns></returns>
        public static Func<object?, string?> CreateFieldValueEncoder(Type fieldType)
        {
            if (fieldType == typeof(sbyte) ||
                fieldType == typeof(byte) ||
                fieldType == typeof(short) ||
                fieldType == typeof(int) ||
                fieldType == typeof(long) ||
                typeof(Enum).IsAssignableFrom(fieldType))
            {
                return value => value == null ? Throw() : $"{value}i";
            }

            if (fieldType == typeof(ushort) ||
                fieldType == typeof(uint) ||
                fieldType == typeof(ulong))
            {
                return value => value == null ? Throw() : $"{value}u";
            }

            if (fieldType == typeof(bool) ||
                fieldType == typeof(decimal) ||
                fieldType == typeof(float) ||
                fieldType == typeof(double))
            {
                return value => value == null ? Throw() : value.ToString();
            }

            return value =>
            {
                var encodeValue = EncodeFieldValue(value);
                return encodeValue == null ? null : @$"""{encodeValue}""";
            };

            static string? Throw()
            {
                throw new NoNullAllowedException("非文本字段的值不能为null");
            }
        }

        /// <summary>
        /// 获取unix纳秒时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long? GetNsTimestamp(DateTime? dateTime)
        {
            if (dateTime == null)
            {
                return null;
            }

            var value = dateTime.Value;
            if (value.Kind == DateTimeKind.Unspecified)
            {
                value = DateTime.SpecifyKind(value, DateTimeKind.Local);
            }

            var dateTimeOffset = new DateTimeOffset(value);
            return GetNsTimestamp(dateTimeOffset);
        }

        /// <summary>
        /// 获取unix纳秒时间戳
        /// </summary>
        /// <param name="dateTimeOffset"></param>
        /// <returns></returns>
        public static long? GetNsTimestamp(DateTimeOffset? dateTimeOffset)
        {
            if (dateTimeOffset == null)
            {
                return null;
            }
            return dateTimeOffset.Value.Subtract(DateTimeOffset.UnixEpoch).Ticks * 100;
        }
    }
}
