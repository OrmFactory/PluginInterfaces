using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PluginInterfaces.Structure;

namespace PluginInterfaces;

public interface IGenerator
{
	string Name { get; }
	string Description { get; }
	void Generate(Project project, GenerationOptions options);
}