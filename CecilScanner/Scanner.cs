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
        private List<Controller> _controllers = new List<Controller>();
        private Dictionary<string, Model> _models = new Dictionary<string, Model>();
        private Dictionary<string, Enum> enums = new Dictionary<string, Enum>();

        public Type GetType(TypeReference codeType)
        {
            var name = codeType.Name;
            var primitiveType = GetPrimitiveType(codeType);
            if (primitiveType != null)
            {
                return primitiveType;
            }

            if (_allEnums.Contains(codeType.GetSafeFullName()))
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

            if (_allClasses.Contains(codeType.GetSafeFullName()))
            {
                return new Type { FullName = codeType.GetSafeFullName(), IsProjectDefined = true, TSElementName = codeType.GetSafeFullName() };
            }
            return new Type { FullName = codeType.GetSafeFullName(), TSElementName = "any" };
        }

        public Type GetArrayType(TypeReference type)
        {
            int rank = 0;
            var elementType = GetArrayElementType(type);
            while (elementType != null)
            {
                rank++;
                type = elementType;
                elementType = GetArrayElementType(type);
            }
            if (rank == 0)
            {
                return null;
            }
            var primitiveType = GetPrimitiveType(type);
            if (primitiveType != null)
            {
                primitiveType.ArrayRank = rank;
                return primitiveType;
            }
            Include(type);
            var isEnum = _allEnums.Contains(type.GetSafeFullName());
            return new Type
            {
                FullName = type.GetSafeFullName(),
                ArrayRank = rank,
                IsProjectDefined = true,
                IsEnum = isEnum,
                TSElementName = type.GetSafeFullName()
            };
        }

        public Type GetPrimitiveType(TypeReference type)
        {
            var typeName = type.Name;
            switch (ExtractNullableType(type).GetSafeFullName())
            {
                case "System.Void":
                    return new Type { FullName = typeName, IsPrimitive = true, TSElementName = "void" };

                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.Decimal":
                case "System.Single":
                case "System.Double":
                    return new Type { FullName = typeName, IsPrimitive = true, TSElementName = "number" };

                case "System.Boolean":
                    return new Type { FullName = typeName, IsPrimitive = true, TSElementName = "boolean" };

                case "System.String":
                    return new Type { FullName = typeName, IsPrimitive = true, TSElementName = "string" };

                case "System.DateTime":
                    return new Type { FullName = typeName, IsPrimitive = true, TSElementName = "string" };

                case "System.Object":
                    return new Type { FullName = typeName, IsPrimitive = true, TSElementName = "any" };
            }
            return null;
        }

        TypeReference ExtractNullableType(TypeReference type)
        {
            if (type.Name == "Nullable`1")
            {
                var genericType = type as GenericInstanceType;
                return genericType.GenericArguments.First();
            }
            return type;
        }

        public TypeReference GetArrayElementType(TypeReference type)
        {
            // Check if array
            if (type.IsArray)
            {
                return type.GetElementType();
            }

            // Check if IEnumerable<T>, IList<T>, etc.
            var genericType = type as GenericInstanceType;
            if (genericType == null)
            {
                return null;
            }

            if (type.Name == "IEnumerable`1" || type.Resolve().Interfaces.Any(i => i.Name == "IEnumerable`1"))
            {
                return genericType.GenericArguments.First();
            }
            return null;
        }


        private static bool IsWebAPI(TypeDefinition type)
        {
            var baseType = type.BaseType;
            if (baseType == null)
            {
                return false;
            }
            if (baseType.Scope.Name == type.Scope.Name)
            {
                return IsWebAPI(baseType.Resolve());
            }

            // Do not reference web DLLs, just perform a string match
            return (baseType.Name == "ApiController" || baseType.Name == "Controller");
        }

        public Api ScanApi(IEnumerable<string> assemblyPaths)
        {
            var resolver = new Resolver();
            var readerParameters = new ReaderParameters { AssemblyResolver = resolver };
            var assemblies = assemblyPaths.Select(path => AssemblyDefinition.ReadAssembly(path, readerParameters)).ToList();
            assemblies.ForEach(resolver.Register);

            var allTypes = assemblies.SelectMany(asm => asm.MainModule.GetTypes()).ToList();

            var apiClasses = allTypes.Where(t => t.IsPublic && !t.IsAbstract && t.IsClass && IsWebAPI(t));

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
                _controllers.Add(controller);
            }

            return new Api
            {
                Controllers = _controllers,
                Models = _models.Values,
                Enums = enums.Values
            };
        }

        private void Include(TypeReference t)
        {
            if (t.IsPrimitive)
            {
                return;
            }

            var fullName = t.GetSafeFullName();

            if (_allEnums.Contains(fullName))
            {
                if (enums.ContainsKey(fullName))
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
                enums[fullName] = theEnum;
            }
            else if (_allClasses.Contains(fullName) && !_models.ContainsKey(fullName))
            {
                // recursively process model types to extract all referenced models and their dependencies
                var modelDef = new Model
                {
                    Type = GetType(t),
                };
                var propList = new List<Member>();
                _models[fullName] = modelDef;
                var theClass = t.Resolve();
                var members = theClass.Properties;
                foreach (var property in members)
                {
                    var propType = GetType(property.PropertyType);
                    if (!_models.ContainsKey(propType.FullName))
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

        void ValidateMethod(Method method)
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
