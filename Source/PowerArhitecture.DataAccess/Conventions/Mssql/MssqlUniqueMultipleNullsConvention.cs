﻿using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using PowerArhitecture.DataAccess.Specifications;
using PowerArhitecture.DataAccess.Settings;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;

namespace PowerArhitecture.DataAccess.Conventions.Mssql
{
    /// <summary>
    /// Create a unique constraint that allows multiple NULL values. Applies to Mssql 2008 and above
    /// This only apply to nullable unique columns
    /// http://stackoverflow.com/questions/767657/how-do-i-create-unique-constraint-that-also-allows-nulls-in-sql-server
    /// </summary>
    public class MssqlUniqueMultipleNullsConvention : ISchemaConvention
    {
        private readonly ConventionsSettings _settings;
        private static readonly IInternalLogger Logger = LoggerProvider.LoggerFor(typeof(SchemaExport));
        private readonly HashSet<string> _validDialects = new HashSet<string>
            {
                typeof (MsSql2012Dialect).FullName,
                typeof (MsSql2008Dialect).FullName
            };

        public MssqlUniqueMultipleNullsConvention(ConventionsSettings settings)
        {
            _settings = settings;
        }

        public bool CanApply(Dialect dialect)
        {
            return _settings.UniqueWithMultipleNulls && _validDialects.Contains(dialect.GetType().FullName);
        }

        public void ApplyBeforeExecutingQuery(Configuration config, IDbConnection connection, IDbCommand dbCommand)
        {
            var tableMatch = Regex.Match(dbCommand.CommandText, @"create\s+table\s+([\[\]\w_]+)");
            if(!tableMatch.Success) return;
            var tableName = tableMatch.Groups[1].Value.TrimStart('[').TrimEnd(']');
            var matches = Regex.Matches(dbCommand.CommandText,
                                        @"(([\[\]\w_]+)\s+([\w\(\)]+)\s+(not null|null) unique)|(unique\s+\(([^\)]+))\)");
            if (matches.Count == 0) return;
            var script = new StringBuilder();
            script.AppendLine();
            foreach (var match in matches.Cast<Match>().Where(match => match.Success))
            {
                string uniqueKeySql;
                if (string.IsNullOrEmpty(match.Groups[2].Value)) //named unique key
                {
                    var columns = match.Groups[6].Value.Split(',').Select(o => o.Trim()).ToList();
                    uniqueKeySql = string.Format("CONSTRAINT {0} UNIQUE ({1})",
                                                 GetUniqueKeyName(tableName, columns), string.Join(", ", columns));
                    dbCommand.CommandText = dbCommand.CommandText.Replace(match.Groups[0].Value, uniqueKeySql);
                    
                }
                else
                {
                    var column = match.Groups[2].Value;
                    uniqueKeySql = match.Groups[0].Value.Replace("unique", "");
                    dbCommand.CommandText = dbCommand.CommandText.Replace(match.Groups[0].Value, uniqueKeySql);
                    
                    if (match.Groups[4].Value == "null") //create filtered unique index
                    {
                        script.AppendFormat("CREATE UNIQUE NONCLUSTERED INDEX {0} ON {1}({2}) WHERE {2} IS NOT NULL",
                                            GetUniqueKeyName(tableName, column), tableName, column);
                        script.AppendLine();
                    }
                    else
                    {
                        dbCommand.CommandText = dbCommand.CommandText.Remove(dbCommand.CommandText.LastIndexOf(')'), 1);
                        dbCommand.CommandText += string.Format(",CONSTRAINT {0} UNIQUE ({1}))",
                                                               GetUniqueKeyName(tableName, column), column);
                    }
                    
                }
            }
            dbCommand.CommandText += script.ToString();
        }

        private static string GetUniqueKeyName(string tableName, string columnName)
        {
            return GetUniqueKeyName(tableName, new List<string> {columnName});
        }

        private static string GetUniqueKeyName(string tableName, IEnumerable<string> columnNames)
        {
            return string.Format("UQ_{0}_{1}", tableName, string.Join("_", columnNames.Select(o => o.TrimEnd(']').TrimStart('['))));
        }

        public void ApplyAfterExecutingQuery(Configuration config, IDbConnection connection, IDbCommand dbCommand)
        {
        }
    }
}
