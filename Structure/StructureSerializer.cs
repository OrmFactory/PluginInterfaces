using System.ComponentModel.DataAnnotations;
using System.Xml.Linq;

namespace PluginInterfaces.Structure;

public static class StructureSerializer
{
	public static XDocument Serialize(Project project)
	{
		var projectXml = new XElement("Project", 
			new XAttribute("Software", "OrmFactory.com"),
			new XAttribute("Name", project.Name));
		AddComment(projectXml, project.Comment);
		AddTags(projectXml, project.Tags);
		AddParameters(projectXml, project.Parameters);
		projectXml.Add(project.Schemas.Select(SerializeSchema));

		var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), projectXml);
		return doc;
	}

	private static void AddComment(XElement e, string comment)
	{
		if (comment != string.Empty) e.Add(new XAttribute("Comment", comment));
	}

	private static void AddParameters(XElement e, IEnumerable<Parameter> parameters)
	{
		foreach (var parameter in parameters)
		{
			var px = new XElement("Parameter", new XAttribute("Name", parameter.Name), new XAttribute("Value", parameter.Value));
			e.Add(px);
		}
	}

	private static void AddTags(XElement e, IEnumerable<string> tags)
	{
		if (tags.Any()) e.Add(new XAttribute("Tags", string.Join(", ", tags)));
	}

	private static XElement SerializeSchema(Schema schema)
	{
		var schemaXml = new XElement("Schema", new XAttribute("Name", schema.Name));
		AddComment(schemaXml, schema.Comment);
		AddTags(schemaXml, schema.Tags);
		AddParameters(schemaXml, schema.Parameters);
		schemaXml.Add(schema.Tables.Select(SerializeTable));
		return schemaXml;
	}

	private static XElement SerializeTable(Table table)
	{
		var tableXml = new XElement("Table", 
			new XAttribute("Name", table.TableName),
			new XAttribute("ClassName", table.ClassName),
			new XAttribute("RepositoryName", table.RepositoryName));
		AddComment(tableXml, table.Comment);
		AddTags(tableXml, table.Tags);
		AddParameters(tableXml, table.Parameters);
		tableXml.Add(table.Columns.Select(SerializeColumn));
		tableXml.Add(table.ForeignKeys.Select(SerializeForeignKey));

		return tableXml;
	}

	private static XElement SerializeForeignKey(ForeignKey fk)
	{
		var fkXml = new XElement("ForeignKey",
			new XAttribute("Name", fk.Name),
			new XAttribute("FieldName", fk.FieldName),
			new XAttribute("FromColumn", fk.FromColumn.ColumnName),
			new XAttribute("ToColumn", fk.ToColumn.RelativeName(fk.FromColumn.Table)));
		if (fk.IsVirtual) fkXml.Add(new XAttribute("Virtual", fk.IsVirtual));
		if (fk.IsReverseKey) fkXml.Add(new XAttribute("Reverse", fk.IsReverseKey));
		AddComment(fkXml, fk.Comment);
		AddTags(fkXml, fk.Tags);
		AddParameters(fkXml, fk.Parameters);
		return fkXml;
	}

	private static XElement SerializeColumn(Column column)
	{
		var columnXml = new XElement("Column", 
			new XAttribute("Name", column.ColumnName), 
			new XAttribute("DatabaseType", column.DatabaseType),
			new XAttribute("FieldName", column.FieldName));
		if (column.Default != string.Empty) columnXml.Add(new XAttribute("Default", column.Default));
		if (column.PrimaryKey) columnXml.Add(new XAttribute("PrimaryKey", column.PrimaryKey));
		if (column.Nullable) columnXml.Add(new XAttribute("Nullable", column.Nullable));
		if (column.AutoIncrement) columnXml.Add(new XAttribute("AutoIncrement", column.AutoIncrement));
		AddComment(columnXml, column.Comment);
		AddTags(columnXml, column.Tags);
		AddParameters(columnXml, column.Parameters);
		return columnXml;
	}

	public static Project FromXml(XDocument modelXml)
	{
		var project = new Project();
		var projectXml = modelXml.Element("Project");
		var schemas = projectXml.Elements("Schema").ToList();

		var fkDeposit = new Dictionary<Table, IEnumerable<XElement>>();

		foreach (var schemaXml in schemas)
		{
			var schema = new Schema();
			schema.Name = schemaXml.Attribute("Name").Value;
			schema.Comment = ReadComment(schemaXml);
			schema.Tags = new (ReadTags(schemaXml));
			schema.Parameters = ReadParameters(schemaXml);

			var tables = schemaXml.Elements("Table");
			foreach (var tableXml in tables)
			{
				var table = ReadTable(tableXml);
				table.Schema = schema;
				schema.Tables.Add(table);
				var foreignKeys = tableXml.Elements("ForeignKey").ToList();
				if (foreignKeys.Any()) 
					fkDeposit[table] = foreignKeys;
			}

			project.Schemas.Add(schema);
		}

		foreach (var kv in fkDeposit)
		{
			var table = kv.Key;
			var foreignKeysXml = kv.Value;
			foreach (var element in foreignKeysXml)
			{
				var key = ReadForeignKey(element, table, project);
				table.ForeignKeys.Add(key);
			}
		}

		return project;
	}

	private static Table ReadTable(XElement element)
	{
		var table = new Table
		{
			TableName = element.Attribute("Name").Value,
			ClassName = element.Attribute("ClassName").Value,
			RepositoryName = element.Attribute("RepositoryName").Value
		};
		table.Comment = ReadComment(element);
		table.Tags = ReadTags(element);
		table.Parameters = ReadParameters(element);
		
		var columns = element.Elements("Column");
		foreach (var columnXml in columns)
		{
			var column = ReadColumn(columnXml);
			column.Table = table;
			table.Columns.Add(column);
		}
		return table;
	}

	private static ForeignKey ReadForeignKey(XElement element, Table table, Project project)
	{
		var key = new ForeignKey
		{
			Name = element.Attribute("Name").Value,
			FieldName = element.Attribute("FieldName").Value,
		};
		if (element.Attribute("IsVirtual")?.Value == "true") key.IsVirtual = true;
		if (element.Attribute("Reverse")?.Value == "true") key.IsReverseKey = true;
		key.Comment = ReadComment(element);
		key.Tags = ReadTags(element);
		key.Parameters = ReadParameters(element);
		var fromColumnName = element.Attribute("FromColumn").Value;
		key.FromColumn = table.Columns.First(c => c.ColumnName == fromColumnName);
		var toColumnName = element.Attribute("ToColumn").Value;
		key.ToColumn = project.Schemas
			.SelectMany(s => s.Tables)
			.SelectMany(t => t.Columns)
			.First(t => t.RelativeName(table) == toColumnName);
		return key;
	}

	private static Column ReadColumn(XElement element)
	{
		var column = new Column
		{
			ColumnName = element.Attribute("Name").Value,
			DatabaseType = element.Attribute("DatabaseType").Value,
			FieldName = element.Attribute("FieldName").Value,
			Default = element.Attribute("Default")?.Value ?? ""

		};
		if (element.Attribute("PrimaryKey")?.Value == "true") column.PrimaryKey = true;
		if (element.Attribute("Nullable")?.Value == "true") column.Nullable = true;
		if (element.Attribute("AutoIncrement")?.Value == "true") column.AutoIncrement = true;
		column.Comment = ReadComment(element);
		column.Tags = ReadTags(element);
		column.Parameters = ReadParameters(element);
		return column;
	}

	private static List<Parameter> ReadParameters(XElement element)
	{
		var res = new List<Parameter>();
		var parameters = element.Elements("Parameter");
		foreach (var parameter in parameters)
		{
			res.Add(new Parameter
			{
				Name = parameter.Attribute("Name").Value,
				Value = parameter.Attribute("Value").Value
			});
		}
		return res;
	}

	private static HashSet<string> ReadTags(XElement element)
	{
		var res = new HashSet<string>();
		var tags = element.Attribute("Tags")?.Value ?? "";
		var parts = tags.Split(',', StringSplitOptions.RemoveEmptyEntries);
		foreach (var part in parts)
		{
			var str = part.Trim();
			if (str != string.Empty) res.Add(str);
		}
		return res;
	}

	private static string ReadComment(XElement element)
	{
		return element.Attribute("Comment")?.Value ?? "";
	}
}