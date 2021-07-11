using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Zero.Foundation.System.Implementations
{
   public class AssemblyClassFinder : IFindClassTypes
   {
      #region Constructors

      public AssemblyClassFinder(ILogger iLogger)
      {
         _iLogger = iLogger;
         this.LoadAppDomainAssemblies = true;
         this.AssemblyNames = new List<string>();
         this.IgnorePattern = "^System|^mscorlib|^Microsoft|^CppCodeProvider|^VJSharpCodeProvider|^WebDev|^MvcContrib|^AjaxControlToolkit";
      }

      #endregion

      #region Private Properties

      private ILogger _iLogger;
      private readonly List<FoundAssembly> _assemblyCache = new List<FoundAssembly>();
      private readonly List<Type> _assemblySearchCache = new List<Type>();

      #endregion

      #region Public Properties

      public virtual AppDomain AppDomain
      {
         get { return AppDomain.CurrentDomain; }
      }

      public bool LoadAppDomainAssemblies { get; set; }
      public IList<string> AssemblyNames { get; set; }
      public string IgnorePattern { get; set; }

      #endregion

      #region Public Methods

      public IEnumerable<Type> FindClassesOfType<T>(bool onlyConcreteClasses)
      {
         return FindClassesOfType(typeof(T), onlyConcreteClasses);
      }
      public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, bool onlyConcreteClasses)
      {
         return FindClassesOfType(assignTypeFrom, GetAssemblies(null), onlyConcreteClasses);
      }
      public IEnumerable<Type> FindClassesOfType<T>(IList<string> AssemblyNames, bool onlyConcreteClasses)
      {
         return FindClassesOfType(typeof(T), AssemblyNames, onlyConcreteClasses);
      }
      public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IList<string> assemblyNamesToInclude, bool onlyConcreteClasses)
      {
         return FindClassesOfType(assignTypeFrom, GetAssemblies(assemblyNamesToInclude), onlyConcreteClasses);
      }
      public IEnumerable<Type> FindClassesOfType<T, TAssemblyAttribute>(bool onlyConcreteClasses) where TAssemblyAttribute : Attribute
      {
         var found = FindAssembliesWithAttribute<TAssemblyAttribute>();
         return FindClassesOfType<T>(found, onlyConcreteClasses);
      }
      public IEnumerable<Type> FindClassesOfType<T>(IEnumerable<Assembly> assemblies, bool onlyConcreteClasses)
      {
         return FindClassesOfType(typeof(T), assemblies, onlyConcreteClasses);
      }

      public IEnumerable<Type> FindClassesOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies, bool onlyConcreteClasses)
      {
         foreach (var a in assemblies)
         {
            Type[] types = null;
            try
            {
               types = a.GetTypes();
            }
            catch
            {
               // ah well, we're looking for valid items.
            }
            if (types != null)
            {
               foreach (var t in types)
               {
                  if (assignTypeFrom.IsAssignableFrom(t))
                  {
                     if (!t.IsInterface)
                     {
                        if (onlyConcreteClasses)
                        {
                           if (t.IsClass && !t.IsAbstract)
                           {
                              yield return t;
                           }
                        }
                        else
                        {
                           yield return t;
                        }
                     }
                  }
               }
            }
         }
      }

      public IEnumerable<Assembly> FindAssembliesWithAttribute<T>()
      {
         return FindAssembliesWithAttribute<T>(GetAssemblies(null));
      }
      public IEnumerable<Assembly> FindAssembliesWithAttribute<T>(DirectoryInfo assemblyPath)
      {
         var assemblies = (from f in Directory.GetFiles(assemblyPath.FullName, "*.dll")
                           select Assembly.LoadFrom(f)
                               into assembly
                           let customAttributes = assembly.GetCustomAttributes(typeof(T), false)
                           where customAttributes.Any()
                           select assembly).ToList();
         return FindAssembliesWithAttribute<T>(assemblies);
      }
      public IEnumerable<Assembly> FindAssembliesWithAttribute<T>(IEnumerable<Assembly> assemblies)
      {
         if (!_assemblySearchCache.Contains(typeof(T)))
         {
            var foundAssemblies = (from assembly in assemblies
                                   let customAttributes = assembly.GetCustomAttributes(typeof(T), false)
                                   where customAttributes.Any()
                                   select assembly).ToList();
            _assemblySearchCache.Add(typeof(T));
            foreach (var a in foundAssemblies)
            {
               _assemblyCache.Add(new FoundAssembly { Assembly = a, PluginAttributeType = typeof(T) });
            }
         }

         return _assemblyCache
             .Where(x => x.PluginAttributeType.Equals(typeof(T)))
             .Select(x => x.Assembly)
             .ToList();
      }

      public virtual IList<Assembly> GetAssemblies(IList<string> assemblyNamesToInclude)
      {
         var addedAssemblyNames = new List<string>();
         var assemblies = new List<Assembly>();

         if (LoadAppDomainAssemblies)
         {
            AddAssembliesInAppDomain(addedAssemblyNames, assemblies);
         }
         AddConfiguredAssemblies(this.AssemblyNames, addedAssemblyNames, assemblies);
         if (assemblyNamesToInclude != null)
         {
            AddConfiguredAssemblies(assemblyNamesToInclude, addedAssemblyNames, assemblies);
         }
         return assemblies;
      }

      public void LoadMatchingAssembliesInAppDomain(string directoryPath)
      {
         List<string> loadedAssemblyNames = new List<string>();
         foreach (Assembly a in GetAssemblies(null))
         {
            loadedAssemblyNames.Add(a.FullName);
         }

         if (!Directory.Exists(directoryPath))
         {
            return;
         }

         foreach (string dllPath in Directory.GetFiles(directoryPath, "*.dll"))
         {
            try
            {
               var an = AssemblyName.GetAssemblyName(dllPath);
               if (Matches(an.FullName) && !loadedAssemblyNames.Contains(an.FullName))
               {
                  _iLogger.Write("AssemblyClassFinder:Loading Assembly: " + an.FullName, Category.Trace);
                  this.AppDomain.Load(an);
               }
            }
            catch (BadImageFormatException ex)
            {
               _iLogger.Write("AssemblyClassFinder:Error Loading Assembly: " + ex.Message, Category.Error);
            }
         }
      }


      #endregion

      #region Private Methods

      private void AddAssembliesInAppDomain(List<string> addedAssemblyNames, List<Assembly> assemblies)
      {
         foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
         {
            if (Matches(assembly.FullName))
            {
               if (!addedAssemblyNames.Contains(assembly.FullName))
               {
                  assemblies.Add(assembly);
                  addedAssemblyNames.Add(assembly.FullName);
               }
            }
         }
      }
      private void AddConfiguredAssemblies(IList<string> configuredAssemblyNames, List<string> addedAssemblyNames, List<Assembly> assemblies)
      {
         foreach (string assemblyName in configuredAssemblyNames)
         {
            Assembly assembly = Assembly.Load(assemblyName);
            if (!addedAssemblyNames.Contains(assembly.FullName))
            {
               assemblies.Add(assembly);
               addedAssemblyNames.Add(assembly.FullName);
            }
         }
      }

      private bool Matches(string assemblyFullName)
      {
         return !Matches(assemblyFullName, IgnorePattern);
      }
      private bool Matches(string assemblyFullName, string pattern)
      {
         return Regex.IsMatch(assemblyFullName, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
      }

      #endregion

      private class FoundAssembly
      {
         internal Assembly Assembly { get; set; }
         internal Type PluginAttributeType { get; set; }
      }
   }
}
