using Elements.Core;
using FrooxEngine;
using System.Reflection;

class Program
{
    static void Main()
    {
        var generator = new Generator();
        generator.GenerateFunny();
        Console.WriteLine("Done!");
    }
}

internal class Generator
{
    private readonly TypeManager typeManager = new(null);
    private readonly string prefix = "[ProtoFluxBindings]FrooxEngine.ProtoFlux.Runtimes.Execution.Nodes";
    private readonly List<Type> types = new();
    private string funnyString = "|";

    public Generator()
    {
        InitializeAssembliesAndTypes();
        GlobalTypeRegistry.Initialize("dummy input", false);
        WorkerInitializer.Initialize(types, true);
        Console.WriteLine("Types loaded successfully.");
    }

    public void GenerateFunny()
    {
        try
        {
            var categoryNode = WorkerInitializer.ComponentLibrary.GetSubcategory("ProtoFlux/Runtimes/Execution/Nodes");
            var categories = GetCategories(categoryNode).OrderBy(t => t.type.Name.Length);

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

            System.IO.File.WriteAllText("./funnystring.txt", funnyString);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void InitializeAssembliesAndTypes()
    {
        var assemblies = new List<Assembly>
        {
            Assembly.Load("FrooxEngine"),
            Assembly.Load("ProtoFluxBindings"),
            Assembly.Load("ProtoFlux.Nodes.Core"),
        };

        foreach (var assembly in assemblies)
        {
            types.AddRange(assembly.GetTypes());
        }

        var assemblyTypeRegistries = assemblies.Select(a => new AssemblyTypeRegistry(a, DataModelAssemblyType.Core)).ToList();
        InitializeAssemblies(typeManager, assemblyTypeRegistries);
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

    private IEnumerable<(Type type, bool isGeneric)> GetCategories(CategoryNode<Type> categoryNode)
    {
        var foundTypes = new List<(Type type, bool isGeneric)>();

        if (categoryNode == null)
        {
            foundTypes.AddRange(types.Select(type => (type, type.IsGenericTypeDefinition)));
        }
        else
        {
            foreach (var subcategory in categoryNode.Subcategories)
            {
                foundTypes.AddRange(GetCategories(subcategory));
            }

            foundTypes.AddRange(categoryNode.Elements.Select(element => (element, element.IsGenericTypeDefinition)));
        }

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
            return typeManager.IsSupported(newType) && newType.IsValidGenericType(true);
        }
        catch
        {
            return false;
        }
    }

    private void AddToFunnyString(Type type)
    {
        var newtype = typeManager.EncodeType(type).Replace(prefix, "");
        funnyString += newtype;
    }
}
