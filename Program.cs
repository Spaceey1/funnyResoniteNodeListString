using Elements.Core;
using FrooxEngine;
using System.Reflection;
using Elements.Data;
using ProtoFluxBindings;
class Program
{
	static void Main()
	{
		var generator = new Generator();
		Console.WriteLine("0.Protoflux Nodes\n1.Components\n-1.Exit");
		string userInput = Console.ReadLine();
		switch (userInput)
		{
			case "0":
				generator.GenerateFunny("./funnystring.txt");
				Console.WriteLine("Done!");
				break;
			case "1":
				generator.GenerateFunny("./componentfunnystring.txt", "FrooxEngine.", null, "ProtoFlux"); //null root category means all categories
				Console.WriteLine("Done!");
				break;
				//    case "2": // DO NOT UNCOMMENT this doesn't work as it should, idk why yet but im too lazy to fix it so its just gonna stay like this for now
				//        generator.GenerateFunny("./componentfunnystring.txt", "FrooxEngine.", null, "ProtoFlux"); //null root category means all categories
				//        generator.GenerateFunny("./funnystring.txt");
				//        Console.WriteLine("Done!");
				//	break;
			case "-1":
				Environment.Exit(0);
				break;
			default:
				Console.WriteLine("Invalid input, exiting...");
				Environment.Exit(0);
				break;
		}
	}
}

internal class Generator
{
	private readonly TypeManager typeManager = new(null);
	private readonly List<Type> types = new();
	private string funnyString = "|"; 
	private string prefix = "FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes";

	public Generator()
	{
		InitializeAssembliesAndTypes();
		GlobalTypeRegistry.Initialize("dummy input", false);
		WorkerInitializer.Initialize(types, true);
		Console.WriteLine("Types loaded successfully.");
	}

	public void GenerateFunny
		(
		 string outputFile,
		 string prefix = "FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes",
		 string rootCategory = "ProtoFlux/Runtimes/Execution/Nodes",
		 string ignoreCategory = ""
		)
		{
			this.prefix = prefix;

			var categoryNode = WorkerInitializer.ComponentLibrary.GetSubcategory(rootCategory);
			var categories = GetCategories(categoryNode, ignoreCategory).OrderBy(t => t.type.Name.Length);

			foreach (var (type, isGeneric) in categories)
			{
				if (!isGeneric)
				{
					AddToFunnyString(type);
				}
				else
				{
					ProcessGenericType(type);
				}
				funnyString += "|";
			}

			System.IO.File.WriteAllText(outputFile, funnyString);
		}

	private void InitializeAssembliesAndTypes()
	{
		var assemblies = new List<Assembly>
		{
			Assembly.Load("FrooxEngine"),
			Assembly.Load("QRCoder"),
			Assembly.Load("ProtoFlux.Core"),
			Assembly.Load("ProtoFlux.Nodes.Core"),
			Assembly.Load("ProtoFluxBindings"),
			Assembly.Load("Awwdio"),
		};

		foreach (var assembly in assemblies)
		{
			types.AddRange(assembly.GetTypes());
			GlobalTypeRegistry.RegisterAssembly(assembly, DataModelAssemblyType.Core);
		}
	}

	private void InitializeAssemblies(TypeManager typeManager, IEnumerable<AssemblyTypeRegistry> assemblies)
	{
		var initializeMethod = typeManager.GetType().GetMethod("InitializeAssemblies", BindingFlags.NonPublic | BindingFlags.Instance);

		if (initializeMethod == null)
			throw new InvalidOperationException("The method 'InitializeAssemblies' was not found.");

		SetSystemCompatibilityHash("123");
		initializeMethod.Invoke(typeManager, [assemblies]);
	}

	private void SetSystemCompatibilityHash(string newValue)
	{
		var propertyInfo = typeof(FrooxEngine.GlobalTypeRegistry)
			.GetProperty("SystemCompatibilityHash", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);

		if (propertyInfo != null)
		{
			propertyInfo.SetValue(null, newValue); // 'null' because it's a static property
		}
		else
		{
			throw new InvalidOperationException("Property 'SystemCompatibilityHash' not found.");
		}

	}

	private IEnumerable<(Type type, bool isGeneric)> GetCategories(CategoryNode<Type> categoryNode, string ignoreCategory = "")
	{
		var foundTypes = new List<(Type type, bool isGeneric)>();
		if(categoryNode.Name == ignoreCategory)
		{
			return foundTypes;
		}
		else if (categoryNode == null)
		{
			foundTypes.AddRange(types.Select(type => (type, type.IsGenericTypeDefinition)));
		}
		else
		{
			foreach (var subcategory in categoryNode.Subcategories)
			{
				foundTypes.AddRange(GetCategories(subcategory, ignoreCategory));
			}
		}
		foundTypes.AddRange(categoryNode.Elements.Select(element => (element, element.IsGenericTypeDefinition)));
		return foundTypes;
	}

	private void ProcessGenericType(Type type)
	{
		if (CheckType(type, typeof(Slot)))
		{
			AddToFunnyString(type.MakeGenericType(typeof(Slot)));
		}
		else if (CheckType(type, typeof(float)))
		{
			AddToFunnyString(type.MakeGenericType(typeof(float)));
		}
		else
		{
			var commonGenericTypes = WorkerInitializer.GetCommonGenericTypes(type).ToList();
			foreach (var commonType in commonGenericTypes)
			{
				if (typeManager.IsSupported(commonType))
				{
					AddToFunnyString(commonType);
					break;
				}
			}
		}
	}

	private bool CheckType(Type genericType, Type type)
	{
		try
		{
			var newType = genericType.MakeGenericType(type);
			return true;
		}
		catch
		{
			return false;
		}
	}

	private void AddToFunnyString(Type type)
	{
		funnyString += (type.Namespace + "." + type.GetNiceName()).Replace(prefix, "");
	}
}
