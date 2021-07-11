using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zero.Foundation.System
{
    public interface IFindClassTypes
    {
        IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses);
        IEnumerable<Type> FindClassesOfType<T>(IList<string> assemblyNamesToInclude, bool onlyConcreteClasses);

        IList<Assembly> GetAssemblies(IList<string> assemblyNamesToInclude);
    }
}
