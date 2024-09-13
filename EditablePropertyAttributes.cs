using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginInterfaces
{
	public class EditablePropertyAttribute : Attribute
	{
		private readonly string name;
		public string Name => name;

		public EditablePropertyAttribute(string name)
		{
			this.name = name;
		}
	}
}
