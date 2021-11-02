using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Unity;
using Zero.Foundation.Aspect;
using Zero.Foundation.System;

namespace Zero.Foundation.Plugins.Implementations
{
    public class WebPluginLoader : ChokeableClass, IWebPluginLoader
    {
        #region Constructor

        public WebPluginLoader(IFoundation iFoundation, IServiceCollection serviceCollection, IWebHostEnvironment webHostEnvironment, IHandleExceptionProvider iHandleExceptionProvider)
            : base(iFoundation)
        {
            this.ServiceCollection = serviceCollection;
            this.WebHostEnvironment = webHostEnvironment;
            this.IHandleExceptionProvider = iHandleExceptionProvider;
        }

        #endregion

        #region Statics

        private static Dictionary<Type, IWebPlugin> _LoadedPlugins = new Dictionary<Type, IWebPlugin>();
        public static List<PluginConfig> _LoadedPluginConfigs { get; set; }

        protected static List<IWebPlugin> _LATEST_REGISTERED_PLUGINS = null;

        #endregion

        #region Properties

        protected IWebHostEnvironment WebHostEnvironment { get; set; }
        protected IServiceCollection ServiceCollection { get; set; }


        #endregion

        #region Public Methods

        public virtual List<IWebPlugin> GetRegisteredPlugins()
        {
            return base.ExecuteFunction<List<IWebPlugin>>("GetRegisteredPlugins", delegate ()
            {
                List<IWebPlugin> result = new List<IWebPlugin>();
                if (_LATEST_REGISTERED_PLUGINS != null)
                {
                    result.AddRange(_LATEST_REGISTERED_PLUGINS);
                }
                return result;
            });
        }

        public virtual List<IWebPlugin> InitializePlugins()
        {
            return base.ExecuteFunction("InitializePlugins", delegate ()
            {
                List<IWebPlugin> allPlugins = _LoadedPlugins.Values.ToList();

                allPlugins.Sort(delegate (IWebPlugin l, IWebPlugin r) { return l.DesiredInitializionPriority.CompareTo(r.DesiredInitializionPriority); });
                List<IWebPlugin> initializedPlugins = new List<IWebPlugin>();

                foreach (IWebPlugin item in allPlugins)
                {
                    try
                    {
                        base.Logger.Write(string.Format("Starting plugin '{0}-{1}'", item.DisplayName, item.DisplayVersion), Category.Trace);
                        item.Initialize();
                        this.RaiseOnWebPluginInitialized(initializedPlugins, item);
                        initializedPlugins.Add(item);
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Error While initializing '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Error);
                        bool rethrow = false;
                        Exception newExeption = null;
                        if (this.IHandleExceptionProvider != null)
                        {
                            IHandleException handler = this.IHandleExceptionProvider.CreateHandler();
                            if (handler != null)
                            {
                                handler.HandleException(ex, out rethrow, out newExeption);
                                // not throwing.. just enabling logging
                            }
                        }
                    }
                }

                this.RaiseOnAllWebPluginsInitialized(initializedPlugins);
                _LATEST_REGISTERED_PLUGINS = initializedPlugins;
                return initializedPlugins;
            });
        }

        public virtual void LoadPlugins()
        {
            base.ExecuteMethod("LoadPlugins", delegate ()
            {
                List<PluginConfig> pluginConfigs = this.AcquireAndLoadPluginAssemblies();

                // Prepare Any Embedded assets
                List<IFileProvider> fileProviders = new List<IFileProvider>();
                fileProviders.Add(this.WebHostEnvironment.ContentRootFileProvider);
                
                foreach (PluginConfig item in pluginConfigs)
                {
                    try
                    {
                        fileProviders.Add(new ManifestEmbeddedFileProvider(item.ReferencedAssembly));
                    }
                    catch (Exception exc)
                    {
                        base.Logger.Write(string.Format("Unable to load manifest file provider for '{0}-{1}' Assembly: {2}. Error: {3}", item.SystemName, item.Version, item.ReferencedAssembly.FullName, exc.Message), Category.Trace);
                    }
                    foreach(Assembly assembly in item.SubordinateAssemblies)
                    {
                        try
                        {
                            fileProviders.Add(new ManifestEmbeddedFileProvider(assembly));
                        }
                        catch (Exception exc)
                        {
                            base.Logger.Write(string.Format("Unable to load manifest file provider for '{0}-{1}' Assembly: {2}. Error: {3}", item.SystemName, item.Version, assembly.FullName, exc.Message), Category.Trace);
                        }
                    }
                }
                IFileProvider compositeProvider = new CompositeFileProvider(fileProviders.ToArray());
                this.ServiceCollection.AddSingleton<IFileProvider>(compositeProvider);


                // Run WebPlugin LifeCycle
                foreach (PluginConfig item in pluginConfigs)
                {
                    try
                    {
                        this.OnBeforePluginAcquireItem(item);

                        if (item.PluginType == null)
                        {
                            base.Logger.Write(string.Format("No Plugin type found for plugin, no Instance will be createdf '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                            continue;
                        }
                        base.Logger.Write(string.Format("Attempting to load existing instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);

                        IWebPlugin instance = null;
                        bool alreadyLoaded = false;
                        if (_LoadedPlugins.ContainsKey(item.PluginType))
                        {
                            instance = _LoadedPlugins[item.PluginType];
                            if (instance != null)
                            {
                                base.Logger.Write(string.Format("'{0}-{1}' is already loaded", item.SystemName, item.Version), Category.Trace);
                                alreadyLoaded = true;
                            }
                        }
                        base.Logger.Write(string.Format("Attempting to load instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);

                        if (instance == null)
                        {
                            instance = base.IFoundation.GetPluginManager().FoundationPlugins.FirstOrDefault(p => p.GetType() == item.PluginType) as IWebPlugin;
                            if (instance != null)
                            {
                                _LoadedPlugins[item.PluginType] = instance;
                                base.Logger.Write(string.Format("Added existing instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                                alreadyLoaded = true;
                            }
                        }


                        // resolve or create
                        if (instance == null)
                        {
                            base.Logger.Write(string.Format("Attempting to create instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);

                            bool resolveSuccess = false;
                            if (!resolveSuccess)
                            {
                                try
                                {
                                    instance = base.IFoundation.Container.Resolve(item.PluginType) as IWebPlugin;
                                    resolveSuccess = true;
                                }
                                catch { } // ah well
                            }
                            if (!resolveSuccess)
                            {
                                try
                                {
                                    // is this any good ??
                                    instance = Activator.CreateInstance(item.PluginType) as IWebPlugin;
                                }
                                catch { } // ah well
                            }
                        }
                        if (instance != null)
                        {
                            if (!alreadyLoaded)
                            {
                                if (instance.Construct(base.IFoundation))
                                {
                                    _LoadedPlugins[item.PluginType] = instance;

                                    base.Logger.Write(string.Format("Added instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                                }
                                else
                                {
                                    instance = null;
                                    base.Logger.Write(string.Format("The following plugin did not initialize: '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                                }
                            }
                            if (instance != null)
                            {
                                this.OnAfterPluginAcquireItem(item, instance, !alreadyLoaded);
                            }
                        }
                        else
                        {
                            base.Logger.Write(string.Format("Unable to create instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                        }
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Unable to load plugin for '{0}-{1}': {2}", item.SystemName, item.Version, ex.Message), Category.Error);
                    }
                }
                
            });
        }

        #endregion

        #region Protected Methods
        protected virtual List<PluginConfig> AcquireAndLoadPluginAssemblies()
        {
            return base.ExecuteFunction("AcquireAndLoadPluginAssemblies", delegate ()
            {
                if (_LoadedPluginConfigs == null)
                {
                    List<PluginConfig> pluginConfigs = new List<PluginConfig>();

                    try
                    {
                        DirectoryInfo pluginFolder = new DirectoryInfo(Path.Combine(this.WebHostEnvironment.ContentRootPath, FoundationAssumptions.WEB_PLUGIN_FOLDER_PATH));

                        Directory.CreateDirectory(pluginFolder.FullName);

                        foreach (FileInfo pluginConfigFile in pluginFolder.GetFiles(FoundationAssumptions.WEB_PLUGIN_CONFIG_NAME, SearchOption.AllDirectories))
                        {
                            try
                            {
                                //parse file
                                PluginConfig config = ParsePluginConfigFile(pluginConfigFile.FullName);

                                //some validation
                                if (string.IsNullOrWhiteSpace(config.SystemName))
                                {
                                    throw new Exception(string.Format("A plugin has no system name. Try assigning the plugin a unique name and recompiling.", config.SystemName));
                                }
                                if (pluginConfigs != null && pluginConfigs.Any(x => x.SystemName.Equals(config.SystemName, StringComparison.OrdinalIgnoreCase)))
                                {
                                    throw new Exception(string.Format("A plugin with '{0}' system name is already defined", config.SystemName));
                                }

                                //get list of all DLLs in plugins (not in bin!)
                                List<FileInfo> pluginDependencyFiles = pluginConfigFile.Directory.GetFiles("*.dll", SearchOption.AllDirectories)
                                    //just make sure we're not registering shadow copied plugins
                                    .Where(x => IsPackagePluginFolder(x.Directory))
                                    .ToList();

                                //other plugin description info
                                FileInfo mainPluginFile = pluginDependencyFiles.Where(x => x.Name.Equals(config.AssemblyFileName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                                if (mainPluginFile == null)
                                {
                                    throw new Exception(string.Format("Unable to find plugin assembly: '{0}'", config.AssemblyFileName));
                                }
                                config.SourceAssemblyFile = mainPluginFile;


                                AssemblyLoadContext context = new AssemblyLoadContext(config.SystemName, false);
                                // load self
                                config.ReferencedAssembly = this.PrepareAndLoadAssembly(context, config, mainPluginFile);

                                // load all other assemblies now
                                foreach (FileInfo plugin in pluginDependencyFiles.Where(x => !x.Name.Equals(mainPluginFile.Name, StringComparison.OrdinalIgnoreCase)))
                                {
                                    Assembly assembly = this.PrepareAndLoadAssembly(context, config, plugin);
                                    if(assembly != null)
                                    {
                                        config.SubordinateAssemblies.Add(assembly);
                                    }
                                }

                                // resolve IWebPlugin type 
                                Type[] referencedTypes = null;
                                try
                                {
                                    referencedTypes = config.ReferencedAssembly.GetTypes();
                                }
                                catch (ReflectionTypeLoadException rex)
                                {
                                    this.IFoundation.LogTrace("WebPluginLoader:Error:: " + rex.Message);
                                    foreach (var item in rex.LoaderExceptions)
                                    {
                                        this.IFoundation.LogTrace("WebPluginLoader:Error:: " + item.Message);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    this.IFoundation.LogError(ex);
                                }
                                if (referencedTypes != null)
                                {
                                    foreach (Type t in referencedTypes)
                                    {
                                        if (config.PluginType == null)
                                        {
                                            if (typeof(IWebPlugin).IsAssignableFrom(t))
                                            {
                                                if (!t.IsInterface && t.IsClass && !t.IsAbstract)
                                                {
                                                    config.PluginType = t;
                                                }
                                            }
                                        }

                                        if (config.PluginType != null)
                                        {
                                            break;
                                        }
                                    }
                                }

                                pluginConfigs.Add(config);
                            }
                            catch (Exception ex)
                            {
                                Exception fail = new Exception("Could not initialize plugin folder: " + ex.Message, ex);
                                this.IFoundation.LogTrace(fail.Message);
                                throw fail;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Exception fail = new Exception("Could not initialize plugin folder", ex);
                        this.IFoundation.LogTrace(fail.Message);
                        throw fail;
                    }

                    _LoadedPluginConfigs = pluginConfigs;
                }
                return _LoadedPluginConfigs;
            });
        }

        protected virtual void RaiseOnWebPluginInitialized(List<IWebPlugin> webPlugins, IWebPlugin iWebPlugin)
        {
            base.ExecuteMethod("RaiseOnWebPluginInitialized", delegate ()
            {
                foreach (IWebPlugin item in webPlugins)
                {
                    try
                    {
                        item.OnWebPluginInitialized(iWebPlugin);
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Error raising RaiseOnWebPluginInitialized for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
                    }
                }
            });
        }
        protected virtual void RaiseOnAllWebPluginsInitialized(List<IWebPlugin> webPlugins)
        {
            base.ExecuteMethod("RaiseOnAllWebPluginsInitialized", delegate ()
            {
                foreach (IWebPlugin item in webPlugins)
                {
                    try
                    {
                        item.OnAllWebPluginsInitialized(webPlugins.ToArray());
                    }
                    catch (Exception ex)
                    {
                        base.Logger.Write(string.Format("Error raising RaiseOnAllWebPluginsInitialized for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
                    }
                }
            });
        }

        protected virtual PluginConfig ParsePluginConfigFile(string filePath)
        {
            string fileContents = File.ReadAllText(filePath);

            PluginConfig config = new PluginConfig();
            if (!string.IsNullOrEmpty(fileContents))
            {
                string[] settings = fileContents.Replace("\r", "").Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string setting in settings)
                {
                    int separatorIndex = setting.IndexOf(':');
                    if (separatorIndex == -1)
                    {
                        continue;
                    }
                    string key = setting.Substring(0, separatorIndex).Trim();
                    string value = setting.Substring(separatorIndex + 1).Trim();

                    switch (key)
                    {
                        case "Feature":
                            config.Feature = value;
                            break;
                        case "FriendlyName":
                            config.FriendlyName = value;
                            break;
                        case "SystemName":
                            config.SystemName = value;
                            break;
                        case "Version":
                            config.Version = value;
                            break;
                        case "Author":
                            config.Author = value;
                            break;
                        case "Description":
                            config.Description = value;
                            break;
                        case "AssemblyFileName":
                            config.AssemblyFileName = value;
                            break;
                        default:
                            break;
                    }
                }
            }
            return config;
        }

        private static bool IsPackagePluginFolder(DirectoryInfo folder)
        {
            if (folder == null)
            {
                return false;
            }
            if (folder.Parent == null)
            {
                return false;
            }
            if (!folder.Parent.Name.Equals(FoundationAssumptions.WEB_PLUGIN_FOLDER_PATH, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        protected virtual void OnBeforePluginAcquireItem(PluginConfig pluginConfig)
        {
            // designed for override
        }
        protected virtual void OnAfterPluginAcquireItem(PluginConfig pluginConfig, IWebPlugin webPlugin, bool initialLoad)
        {
            // designed for override
        }

        /// <summary>
        /// Not Aspect Wrapped
        /// </summary>
        private Assembly PrepareAndLoadAssembly(AssemblyLoadContext context, PluginConfig config, FileInfo file)
        {
            if (file.Directory.Parent == null)
            {
                throw new InvalidOperationException("The plugin directory for the " + file.Name + " file exists in a folder outside of the allowed folder heirarchy");
            }

            try
            {
                ApplicationPartManager applicationPartManager = this.ServiceCollection.GetLastImplementation<ApplicationPartManager>();
                context.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
                {
                    string expectedPath = Path.Combine(file.DirectoryName, assemblyName.Name + ".dll");
                    return context.LoadFromAssemblyPath(expectedPath);
                };
                Assembly shadowCopiedAssembly = context.LoadFromAssemblyPath(file.FullName);
                if (file.FullName == Path.GetFileNameWithoutExtension(config.AssemblyFileName) + "Views.dll")
                {
                    applicationPartManager.ApplicationParts.Add(new CompiledRazorAssemblyPart(shadowCopiedAssembly));
                }
                else
                {
                    ApplicationPartFactory partFactory = ApplicationPartFactory.GetApplicationPartFactory(shadowCopiedAssembly);
                    foreach (ApplicationPart applicationPart in partFactory.GetApplicationParts(shadowCopiedAssembly))
                    {
                        applicationPartManager.ApplicationParts.Add(applicationPart);
                    }
                }
                return shadowCopiedAssembly;
            }
            catch (Exception ex)
            {
                this.IFoundation.LogTrace(string.Format("Error loading {0} as a .net assembly: {1}", file.Name, ex.Message));
                return null;
            }
        }

        #endregion

        
    }
}
