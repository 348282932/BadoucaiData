using System.ServiceProcess;

namespace Badoucai.EmailService
{
    internal partial class Service : ServiceBase
    {
        public Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            EmailService.Start();
        }

        protected override void OnStop()
        {
            EmailService.Stop();
        }
    }
}
