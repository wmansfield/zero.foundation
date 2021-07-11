using Unity;

namespace Zero.Foundation.System
{
   public interface IDynamicallySelfRegister
   {
      void SelfRegister(IUnityContainer container);
   }
}
