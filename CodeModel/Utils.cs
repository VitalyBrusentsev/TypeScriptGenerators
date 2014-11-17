using System.Collections.Generic;
using System.Linq;

namespace CodeModel
{
    public static class Utils
    {
        public static string GetTSName(Type type)
        {
            string name = type.TSElementName;
            if (type.IsEnum)
            {
                name = GetAreaLocalName(name);
            }
            else if (!type.IsPrimitive && type.IsProjectDefined)
            {
                name = GetModelName(name);
            }

            name += string.Concat(Enumerable.Repeat("[]", type.ArrayRank));

            return name;
        }

        public static string GetTSParamDefinition(Parameter p)
        {
            return string.Format("{0}{1}: {2}", p.FromBody ? "/*[FromBody]*/ " : "", p.Name, GetTSName(p.Type));
        }

        public static string GetTSFieldValueDefinition(Parameter p)
        {
            return string.Format("{0}: {0}", p.Name);
        }

        public static string GetHttpVerb(string name)
        {
            var method = "GET";
            if (name.ToLower().StartsWith("get"))
                method = "GET";
            else if (name.ToLower().StartsWith("post"))
                method = "POST";
            else if (name.ToLower().StartsWith("delete"))
                method = "DELETE";
            else if (name.ToLower().StartsWith("put"))
                method = "PUT";
            else if (name.ToLower().StartsWith("patch"))
                method = "PATCH";
            return method;
        }

        public static IEnumerable<T> TakeAllButLast<T>(IEnumerable<T> source)
        {
            var it = source.GetEnumerator();
            bool hasRemainingItems = false;
            bool isFirst = true;
            T item = default(T);

            do
            {
                hasRemainingItems = it.MoveNext();
                if (hasRemainingItems)
                {
                    if (!isFirst) yield return item;
                    item = it.Current;
                    isFirst = false;
                }
            } while (hasRemainingItems);
        }

        public static string GetAreaLocalName(string name)
        {
            var snippet = ".Areas.";
            var index = name.IndexOf(snippet);
            return index == -1 ? name : name.Substring(index + snippet.Length);
        }

        public static IEnumerable<string> GetAreaName(string name)
        {
            var snippet = ".Areas.";
            var index = name.IndexOf(snippet);
            var last = (index == -1) ? name : name.Substring(index + snippet.Length);
            return TakeAllButLast(last.Split('.'));
        }

        public static string GetAreaNamespace(string name)
        {
            return string.Join(".", GetAreaName(name));
        }

        public static string GetProxyName(string name)
        {
            var className = name.Split('.').Last();
            return "I" + GetRouteName(className) + "Service";
        }

        public static string GetServiceName(string name)
        {
            var className = name.Split('.').Last();
            return GetRouteName(className) + "Service";
        }

        public static string GetRouteName(string name)
        {
            if (name.EndsWith("Controller"))
                name = name.Substring(0, name.Length - "Controller".Length);
            return name;
        }

        public static string GetModelName(string fullName)
        {
            var parts = GetAreaLocalName(fullName).Split('.');
            return string.Join(".", TakeAllButLast(parts)) + ".I" + parts.Last();
        }
    }
}