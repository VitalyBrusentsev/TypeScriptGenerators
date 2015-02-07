using CodeModel;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace CecilScanner
{
    public class Scanner
    {
        private ISet<string> _allEnums;
        private ISet<string> _allClasses;
        private List<Controller> _includedControllers = new List<Controller>();
        private Dictionary<string, Model> _includedModels = new Dictionary<string, Model>();
        private Dictionary<string, Enum> _includedEnums = new Dictionary<string, Enum>();
        //private TypeReference _objectType = new TypeReference("System", "Object", null, null, false);

        private Type GetType(TypeReference codeType)
        {
            var primitiveType = GetPrimitiveType(codeType);
            if (primitiveType != null)
            {
                return primitiveType;
            }

            if (IsDeclaredEnum(codeType))
            {
                return new Type
                {
                    FullName = codeType.GetSafeFullName(),
                    IsEnum = true,
                    IsProjectDefined = true,
                    TSElementName = codeType.GetSafeFullName()
                };
            }

            var arrayType = GetArrayType(codeType);
            if (arrayType != null)
            {
                return arrayType;
            }

            var taskType = GetTaskType(codeType);
            if (taskType != null)
            {
                return taskType;
            }

            if (IsDeclaredClass(codeType))
            {
                return new Type
                {
                    FullName = codeType.GetSafeFullName(),
                    IsProjectDefined = true,
                    TSElementName = codeType.GetSafeFullName()
                };
            }
            return Types.Any;
        }

        private Type GetTaskType(TypeReference type)
        {
            var elementType = GetTaskElementType(type);

            if (elementType == null)
            {
                return null;
            }

            var primitiveType = GetType(elementType);
            if (primitiveType == null)
            {
                return null;
            }

            Include(elementType);
            return primitiveType;
        }

        private TypeReference GetTaskElementType(TypeReference type)
        {
            var genericType = type as GenericInstanceType;
            if (genericType == null)
            {
                return null;
            }

            if (type.Name == "Task`1")
            {
                return genericType.GenericArguments.First();
            }

            return null;
        }

        private Type GetArrayType(TypeReference type)
        {
            int dimensions = 0;
            var elementType = GetArrayElementType(type);
            while (elementType != null)
            {
                dimensions++;
                type = elementType;
                elementType = GetArrayElementType(type);
            }
            if (dimensions == 0)
            {
                return null;
            }
            var primitiveType = GetPrimitiveType(type);
            if (primitiveType != null)
            {
                return new Type
                {
                    IsPrimitive = true,
                    IsProjectDefined = false,
                    FullName = primitiveType.FullName,
                    ArrayDimensions = dimensions,
                    TSElementName = primitiveType.TSElementName
                };
            }

            var isClass = IsDeclaredClass(type);
            var isEnum = IsDeclaredEnum(type);
            var isProjectDefined = isClass || isEnum;

            var fullName = type.GetSafeFullName();

            if (isClass)
            {
                Include(type);
            }

            return new Type
            {
                IsPrimitive = !isProjectDefined,
                FullName = isProjectDefined ? fullName : "any",
                ArrayDimensions = dimensions,
                IsProjectDefined = isProjectDefined,
                IsEnum = isEnum,
                TSElementName = isProjectDefined ? fullName : "any"
            };
        }

        private Type GetPrimitiveType(TypeReference type)
        {
            var typeName = type.Name;
            type = ExtractNullableType(type);
            switch (type.GetSafeFullName())
            {
                case "System.Void":
                    return Types.Void;

                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.Decimal":
                case "System.Single":
                case "System.Double":
                    return Types.Number;

                case "System.Boolean":
                    return Types.Boolean;

                case "System.String":
                    return Types.String;

                case "System.DateTime":
                    return Types.String;

                case "System.Object":
                    return Types.Any;
            }
            return null;
        }

        private bool IsDeclaredClass(TypeReference type)
        {
            return _allClasses.Contains(type.GetSafeFullName());
        }

        private bool IsDeclaredEnum(TypeReference type)
        {
            return _allEnums.Contains(type.GetSafeFullName());
        }

        private TypeReference ExtractNullableType(TypeReference type)
        {
            if (type.Name == "Nullable`1")
            {
                var genericType = type as GenericInstanceType;
                return genericType.GenericArguments.First();
            }
            return type;
        }

        private TypeReference GetArrayElementType(TypeReference type)
        {
            // Check if array
            if (type.IsArray)
            {
                return ((ArrayType)type).ElementType;
            }

            // Check if IEnumerable<T>, IList<T>, etc.
            var genericType = type as GenericInstanceType;
            if (genericType == null)
            {
                return null;
            }

            // Dictionaries become 'any'
            if (type.Name == "IDictionary`2" || type.Resolve().Interfaces.Any(i => i.Name == "IDictionary`2"))
            {
                return null;
            }

            if (type.Name == "IEnumerable`1" || type.Resolve().Interfaces.Any(i => i.Name == "IEnumerable`1"))
            {
                return genericType.GenericArguments.First();
            }
            return null;
        }

        public Api ScanApi(IEnumerable<string> assemblyPaths)
        {
            var resolver = new Resolver();
            var readerParameters = new ReaderParameters { AssemblyResolver = resolver };
            var assemblies = assemblyPaths.Select(path => AssemblyDefinition.ReadAssembly(path, readerParameters)).ToList();
            assemblies.ForEach(resolver.Register);

            var allTypes = assemblies.SelectMany(asm => asm.MainModule.GetTypes()).ToList();

            var apiClasses = allTypes.Where(t => t.IsPublic && !t.IsAbstract && t.IsClass && t.IsWebAPI());

            _allEnums = new HashSet<string>(allTypes.Where(t => t.IsPublic && t.IsEnum).Select(t => t.GetSafeFullName()));

            _allClasses = new HashSet<string>(allTypes.Where(t => t.IsClass).Select(t => t.GetSafeFullName()));

            foreach (var classType in apiClasses)
            {
                var typeDef = GetType(classType);
                var areaName = Utils.GetAreaNamespace(typeDef.FullName);
                if (areaName == null) continue;
                var methods = new List<Method>();
                var controller = new Controller
                {
                    Type = typeDef,
                    Methods = methods
                };

                foreach (var member in classType.Methods.Where(m => m.IsPublic && !m.IsConstructor))
                {
                    var resultType = GetType(member.ReturnType);
                    var parameters = new List<Parameter>();
                    var attributes = new List<Attribute>();
                    var method = new Method
                    {
                        OriginalName = member.Name,
                        Name = RenameIfConflicts(methods, member.Name),
                        HttpVerb = Utils.GetHttpVerb(member.Name),
                        Type = resultType,
                        Parameters = parameters,
                        Attributes = attributes
                    };

                    foreach (var p in member.Parameters)
                    {
                        var paramType = GetType(p.ParameterType);
                        var param = new Parameter
                        {
                            Name = p.Name,
                            Type = paramType,
                            FromBody = p.CustomAttributes.Any(a => a.AttributeType.Name == "FromBodyAttribute")
                        };
                        parameters.Add(param);
                    }

                    foreach (var attr in member.CustomAttributes)
                    {
                        //attributes.Add(new AttributeDefinition { Name = attr.Name, Value = attr.Value.ToString() });
                    }
                    ValidateMethod(method);
                    methods.Add(method);

                    // pull the data types along with the generated methods
                    if (method.IsValid)
                    {
                        Include(member.ReturnType);
                        foreach (var param in member.Parameters)
                        {
                            Include(param.ParameterType);
                        }
                    }
                }
                _includedControllers.Add(controller);
            }

            return new Api
            {
                Controllers = _includedControllers,
                Models = _includedModels.Values,
                Enums = _includedEnums.Values
            };
        }

        private void Include(TypeReference t)
        {
            if (t.IsPrimitive)
            {
                return;
            }

            var fullName = t.GetSafeFullName();

            if (IsDeclaredEnum(t))
            {
                if (_includedEnums.ContainsKey(fullName))
                {
                    return;
                }
                var fields = t.Resolve().Fields
                    .Where(f => f.IsLiteral && f.IsStatic && f.HasConstant)
                    .Select(f => new EnumMember { Name = f.Name, Value = f.Constant.ToString() });
                var theEnum = new Enum
                {
                    Type = GetType(t),
                    Members = fields
                };
                _includedEnums[fullName] = theEnum;
            }
            else if (IsDeclaredClass(t) && !_includedModels.ContainsKey(fullName))
            {
                // recursively process model types to extract all referenced models and their dependencies
                var modelDef = new Model
                {
                    Type = GetType(t),
                };
                var propList = new List<Member>();
                _includedModels[fullName] = modelDef;
                var theClass = t.Resolve();
                var members = theClass.GetPropertiesWithInheritance();
                foreach (var property in members)
                {
                    var propType = GetType(property.PropertyType);
                    if (!_includedModels.ContainsKey(propType.FullName))
                    {
                        // Process type if it hasn't been added yet
                        Include(property.PropertyType);
                    }
                    propList.Add(new Member { Type = propType, Name = property.Name });
                }
                modelDef.Properties = propList;
            }
        }

        private string RenameIfConflicts(IEnumerable<Method> methods, string name)
        {
            int suffix = 1;
            var test = name;
            var lookup = new HashSet<string>(methods.Select(m => m.Name));
            while (lookup.Contains(test))
            {
                test = name + (suffix++);
            }
            return test;
        }

        private void ValidateMethod(Method method)
        {
            method.IsValid = false;
            if (method.HttpVerb == "GET" && method.Parameters.Any(p => p.FromBody))
            {
                method.ValidationError = "[FromBody] parameters are not allowed in GET methods";
                return;
            }
            if (method.Parameters.Count(p => p.FromBody) > 1)
            {
                method.ValidationError = "Only one [FromBody] parameter is allowed";
                return;
            }
            method.IsValid = true;
        }
    }
}
