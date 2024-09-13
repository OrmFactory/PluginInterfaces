using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginInterfaces.Structure;

public class Project
{
	public string Name;
	public string Comment;

	public List<Schema> Schemas = new();

	public HashSet<string> Tags = new();
	public List<Parameter> Parameters = new();
}

public class Schema
{
	public string Name;
	public string Comment;
	public List<Table> Tables = new();

	public HashSet<string> Tags = new();
	public List<Parameter> Parameters = new();
}

public class Table
{
	public string TableName;
	public string Comment;
	public string ClassName;
	public string RepositoryName;
	public Schema Schema;
	public List<Column> Columns = new();
	public List<ForeignKey> ForeignKeys = new();
	public List<StaticField> StaticFields = new();

	public HashSet<string> Tags = new();
	public List<Parameter> Parameters = new();
}

public class ForeignKey
{
	public string Name;
	public string Comment;
	public string FieldName;
	public bool IsVirtual;
	public bool IsReverseKey;
	public Column FromColumn;
	public Column ToColumn;

	public HashSet<string> Tags = new();
	public List<Parameter> Parameters = new();

	public ForeignKey GetReverse()
	{
		var key = new ForeignKey
		{
			Name = Name,
			Comment = Comment,
			FieldName = FieldName,
			IsVirtual = IsVirtual,
			ToColumn = FromColumn,
			FromColumn = ToColumn,
			IsReverseKey = true,
			Parameters = new List<Parameter>(Parameters),
			Tags = new HashSet<string>(Tags)
		};
		return key;
	}
}

public class Column
{
	public string ColumnName;
	public string Comment;
	public string Default;
	public string FieldName;
	public bool Nullable;
	public bool PrimaryKey;
	public bool AutoIncrement;
	public string DatabaseType;
	public Table Table;

	public HashSet<string> Tags = new();
	public List<Parameter> Parameters = new();

	public string RelativeName(Table table)
	{
		if (table == Table) return ColumnName;
		if (table.Schema == Table.Schema)
			return Table.TableName + "." + ColumnName;
		return Table.Schema.Name + "." + Table.TableName + "." + ColumnName;
	}
}
public class StaticField
{
	public string Id;
	public string Name;
	public string Comment;
}

public class Parameter
{
	public string Name;
	public string Value;
}