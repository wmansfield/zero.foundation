using System;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Unity;

namespace Zero.Foundation.Web
{
    public class FoundationHost
    {
        public FoundationHost()
        {
            this.CancellationSource = new CancellationTokenSource();
            this.Container = new UnityContainer();
            this.Container.RegisterInstance(this);
        }
        public CancellationTokenSource CancellationSource { get; set; }
        public UnityContainer Container { get; set; }
        public IHost Host { get; set; }
        public bool ShouldRestart { get; set; }

        public virtual void RequestRestart()
        {
            this.ShouldRestart = true;
            this.CancellationSource.Cancel();
            this.CancellationSource = new CancellationTokenSource();

        }
        public virtual void RequestStop()
        {
            this.ShouldRestart = false;
            this.CancellationSource.Cancel();
            this.CancellationSource = new CancellationTokenSource();
        }
    }
}
