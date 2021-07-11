using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Unity;
using Zero.Foundation.Aspect;
using Zero.Foundation.System;

namespace Zero.Foundation.Plugins.Implementations
{
   public class WebPluginLoader : ChokeableClass, IWebPluginLoader, IDisposable
   {
      #region Constructor

      public WebPluginLoader(IFoundation iFoundation, IWebHostEnvironment webHostEnvironment, IHandleExceptionProvider iHandleExceptionProvider)
          : base(iFoundation)
      {
         this.WebHostEnvironment = webHostEnvironment;
         this.IHandleExceptionProvider = iHandleExceptionProvider;
      }
      ~WebPluginLoader()
      {
         Dispose(false);
      }

      #endregion

      #region Statics

      private static Dictionary<Type, IWebPlugin> _LoadedPlugins = new Dictionary<Type, IWebPlugin>();
      public static List<PluginConfig> _LoadedPluginConfigs { get; set; }

      protected static List<IWebPlugin> _LATEST_REGISTERED_PLUGINS = null;
      protected static readonly object _InitializeSyncRoot = new object();

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

      public virtual List<IWebPlugin> RegisterWebPlugins()
      {
         return base.ExecuteFunction("RegisterWebPlugins", delegate ()
         {
            List<IWebPlugin> allPlugins = this.AcquireAndLoadPermittedPlugins();
            List<IWebPlugin> loadedPlugins = new List<IWebPlugin>();

            foreach (IWebPlugin item in allPlugins)
            {
               try
               {
                  base.Logger.Write(string.Format("Starting plugin '{0}-{1}'", item.DisplayName, item.DisplayVersion), Category.Trace);
                  item.Register();
                  this.RaiseOnWebPluginRegistered(loadedPlugins, item);
                  loadedPlugins.Add(item);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error While Loading '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Error);
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
            this.RaiseOnAfterWebPluginsRegistered(loadedPlugins);
            _LATEST_REGISTERED_PLUGINS = loadedPlugins;
            return loadedPlugins;
         });
      }

      public virtual List<IWebPlugin> AcquireAndLoadPermittedPlugins()
      {
         return base.ExecuteFunction<List<IWebPlugin>>("AcquireAndLoadPermittedPlugins", delegate ()
         {
            List<IWebPlugin> plugins = new List<IWebPlugin>();

            List<PluginConfig> pluginConfigs = this.AcquirePluginConfigs();
            foreach (PluginConfig item in pluginConfigs)
            {
               try
               {
                  this.OnBeforePluginAcquireItem(item);

                  if (!this.PerformPluginPermitted(item))
                  {
                     base.Logger.Write(string.Format("Item not permitted, Skipping: '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                     continue;
                  }
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
                        plugins.Add(instance);
                        base.Logger.Write(string.Format("Added existing instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                        alreadyLoaded = true;
                     }
                  }
                  base.Logger.Write(string.Format("Attempting to load existing foundation instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);

                  if (instance == null)
                  {
                     instance = base.IFoundation.GetPluginManager().FoundationPlugins.FirstOrDefault(p => p.GetType() == item.PluginType) as IWebPlugin;
                     if (instance != null)
                     {
                        plugins.Add(instance);
                        _LoadedPlugins[item.PluginType] = instance;
                        base.Logger.Write(string.Format("Added existing foundation instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);
                        alreadyLoaded = true;
                     }
                  }

                  base.Logger.Write(string.Format("Attempting to create instance of '{0}-{1}'", item.SystemName, item.Version), Category.Trace);

                  // resolve or create
                  if (instance == null)
                  {
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
                     IDictionary<string, string> pluginConfigOptions = PerformLoadPluginConfigOptions(item);

                     if (!alreadyLoaded)
                     {
                        if (instance.WebInitialize(base.IFoundation, pluginConfigOptions))
                        {
                           plugins.Add(instance);
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
                        this.OnAfterPluginAcquireItem(item, instance, pluginConfigOptions, !alreadyLoaded);
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
            plugins.Sort(delegate (IWebPlugin l, IWebPlugin r) { return l.DesiredRegistrationPriority.CompareTo(r.DesiredRegistrationPriority); });
            return plugins;
         });
      }

      #endregion

      #region Protected Methods

      
      
      protected virtual List<PluginConfig> AcquirePluginConfigs()
      {
         return base.ExecuteFunction("AcquirePluginConfigs", delegate ()
         {
            if (_LoadedPluginConfigs == null)
            {
               List<PluginConfig> pluginConfigs = new List<PluginConfig>();

               try
               {
                  DirectoryInfo pluginFolder = new DirectoryInfo(Path.Combine(this.WebHostEnvironment.ContentRootPath, FoundationAssumptions.WEB_PLUGIN_FOLDER_PATH));
                  DirectoryInfo shadowCopyFolder = new DirectoryInfo(Path.Combine(this.WebHostEnvironment.ContentRootPath, FoundationAssumptions.WEB_PLUGIN_FOLDER_PATH, FoundationAssumptions.WEB_PLUGIN_SHADOWCOPY_PATH));

                  Directory.CreateDirectory(pluginFolder.FullName);
                  Directory.CreateDirectory(shadowCopyFolder.FullName);

                  //clear out shadow copied plugins
                  FileInfo[] oldFiles = shadowCopyFolder.GetFiles("*", SearchOption.AllDirectories);
                  foreach (FileInfo file in oldFiles)
                  {
                     this.IFoundation.LogTrace("Deleting " + file.Name);
                     try
                     {
                        File.Delete(file.FullName);
                     }
                     catch (Exception exc)
                     {
                        this.IFoundation.LogTrace("Error pre-deleting file " + file.Name + ". Exception: " + exc);
                     }
                  }

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
                            .Where(x => !oldFiles.Select(q => q.FullName).Contains(x.FullName))
                            .Where(x => IsPackagePluginFolder(x.Directory))
                            .ToList();

                        //other plugin description info
                        FileInfo mainPluginFile = pluginDependencyFiles.Where(x => x.Name.Equals(config.AssemblyFileName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                        if (mainPluginFile == null)
                        {
                           throw new Exception(string.Format("Unable to find plugin assembly: '{0}'", config.AssemblyFileName));
                        }
                        config.SourceAssemblyFile = mainPluginFile;

                        AssemblyLoadContext context = new AssemblyLoadContext("pluginContext", false);
                        context.Resolving += (AssemblyLoadContext context, AssemblyName assemblyName) =>
                        {
                           string expectedPath = Path.Combine(shadowCopyFolder.FullName, assemblyName.Name + ".dll");
                           return context.LoadFromAssemblyPath(expectedPath);
                        };

                        //shadow copy files
                        config.ReferencedAssembly = this.PrepareAndLoadAssembly(mainPluginFile, shadowCopyFolder);

                        //load all other assemblies now
                        foreach (FileInfo plugin in pluginDependencyFiles.Where(x => !x.Name.Equals(mainPluginFile.Name, StringComparison.OrdinalIgnoreCase)))
                        {
                           this.PrepareAndLoadAssembly(plugin, shadowCopyFolder);
                        }

                        //init plugin type 
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



      protected virtual void RaiseOnWebPluginUnRegistered(List<IWebPlugin> webPlugins, IWebPlugin iWebPlugin)
      {
         base.ExecuteMethod("RaiseOnWebPluginUnRegistered", delegate ()
         {
            foreach (IWebPlugin item in webPlugins)
            {
               try
               {
                  item.OnWebPluginUnRegistered(iWebPlugin);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnWebPluginUnRegistered for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }
      protected virtual void RaiseOnAfterWebPluginsUnRegistered(List<IWebPlugin> webPlugins)
      {
         base.ExecuteMethod("RaiseOnAfterWebPluginsUnRegistered", delegate ()
         {
            foreach (IWebPlugin item in webPlugins)
            {
               try
               {
                  item.OnAfterWebPluginsUnRegistered(webPlugins.ToArray());
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnAfterWebPluginsUnRegistered for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }

      protected virtual void RaiseOnWebPluginRegistered(List<IWebPlugin> webPlugins, IWebPlugin iWebPlugin)
      {
         base.ExecuteMethod("RaiseOnWebPluginRegistered", delegate ()
         {
            foreach (IWebPlugin item in webPlugins)
            {
               try
               {
                  item.OnWebPluginRegistered(iWebPlugin);
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnWebPluginRegistered for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
               }
            }
         });
      }
      protected virtual void RaiseOnAfterWebPluginsRegistered(List<IWebPlugin> webPlugins)
      {
         base.ExecuteMethod("RaiseOnAfterWebPluginsRegistered", delegate ()
         {
            foreach (IWebPlugin item in webPlugins)
            {
               try
               {
                  item.OnAfterWebPluginsRegistered(webPlugins.ToArray());
               }
               catch (Exception ex)
               {
                  base.Logger.Write(string.Format("Error raising OnAfterWebPluginsRegistered for '{0}-{1}': {2}", item.DisplayName, item.DisplayVersion, ex.Message), Category.Warning);
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

      protected virtual bool PerformPluginPermitted(PluginConfig pluginConfig)
      {
         // designed for override
         return true;
      }
      protected virtual IDictionary<string, string> PerformLoadPluginConfigOptions(PluginConfig pluginConfig)
      {
         // designed for override
         return null;
      }

      protected virtual void OnBeforePluginAcquireItem(PluginConfig pluginConfig)
      {
         // designed for override
      }
      protected virtual void OnAfterPluginAcquireItem(PluginConfig pluginConfig, IWebPlugin webPlugin, IDictionary<string, string> pluginConfigOptions, bool initialLoad)
      {
         // designed for override
      }

      /// <summary>
      /// Not Aspect Wrapped
      /// </summary>
      private Assembly PrepareAndLoadAssembly(FileInfo file, DirectoryInfo shadowCopyFolder)
      {
         if (file.Directory.Parent == null)
         {
            throw new InvalidOperationException("The plugin directory for the " + file.Name + " file exists in a folder outside of the allowed folder heirarchy");
         }

         this.IFoundation.LogTrace(file.FullName + " to " + shadowCopyFolder.FullName);
         FileInfo shadowCopiedPlugin = this.CopyFileToFolder(file, shadowCopyFolder);

         try
         {
            //we can now register the plugin definition
            Assembly shadowCopiedAssembly = Assembly.LoadFile(shadowCopiedPlugin.FullName);
            return shadowCopiedAssembly;
         }
         catch (Exception ex)
         {
            this.IFoundation.LogTrace(string.Format("Error loading {0} as a .net assembly: {1}", file.Name, ex.Message));
            return null;
         }
      }

      /// <summary>
      /// Not Aspect Wrapped
      /// </summary>
      private FileInfo CopyFileToFolder(FileInfo pluginFileInfo, DirectoryInfo shadowCopyPluginFolder)
      {
         FileInfo shadowCopiedPlug = new FileInfo(Path.Combine(shadowCopyPluginFolder.FullName, pluginFileInfo.Name));
         try
         {
            File.Copy(pluginFileInfo.FullName, shadowCopiedPlug.FullName, true);
         }
         catch (UnauthorizedAccessException)
         {
            this.IFoundation.LogTrace(shadowCopiedPlug.FullName + " has denied access, attempting to rename");
            //this occurs when the files are locked,
            //for some reason devenv locks plugin files some times and for another crazy reason you are allowed to rename them
            //which releases the lock, so that it what we are doing here, once it's renamed, we can re-shadow copy
            try
            {
               string oldFile = shadowCopiedPlug.FullName + Guid.NewGuid().ToString("N") + ".old";
               File.Move(shadowCopiedPlug.FullName, oldFile);
            }
            catch (IOException)
            {
               this.IFoundation.LogTrace(shadowCopiedPlug.FullName + " rename failed, cannot initialize plugin");
               throw;
            }
         }
         catch (IOException)
         {
            this.IFoundation.LogTrace(shadowCopiedPlug.FullName + " is locked, attempting to rename");
            //this occurs when the files are locked,
            //for some reason devenv locks plugin files some times and for another crazy reason you are allowed to rename them
            //which releases the lock, so that it what we are doing here, once it's renamed, we can re-shadow copy
            try
            {
               string oldFile = shadowCopiedPlug.FullName + Guid.NewGuid().ToString("N") + ".old";
               File.Move(shadowCopiedPlug.FullName, oldFile);
            }
            catch (IOException)
            {
               this.IFoundation.LogTrace(shadowCopiedPlug.FullName + " rename failed, cannot initialize plugin");
               throw;
            }
            //ok, we've made it this far, now retry the shadow copy
            File.Copy(pluginFileInfo.FullName, shadowCopiedPlug.FullName, true);
         }
         return shadowCopiedPlug;
      }


      #endregion

      #region IDisposable Members

      private void Dispose(bool disposing)
      {
         if (disposing)
         {
            // nothing yet
         }
      }
      public void Dispose()
      {
         Dispose(true);
         GC.SuppressFinalize(this);
      }

      #endregion
   }
}
