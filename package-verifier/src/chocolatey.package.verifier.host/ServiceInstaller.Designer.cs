namespace chocolatey.package.verifier.host
{
    using verifier.infrastructure.configuration;

    partial class ServiceInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.serviceProcessInstaller1 = new System.ServiceProcess.ServiceProcessInstaller();
            this.serviceInstaller1 = new System.ServiceProcess.ServiceInstaller();
            // 
            // serviceProcessInstaller1
            // 
            this.serviceProcessInstaller1.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.serviceProcessInstaller1.Password = null;
            this.serviceProcessInstaller1.Username = null;
            // 
            // serviceInstaller1
            // 
            this.serviceInstaller1.Description = "Ensures Chocolatey packages pass testing. Uses Vagrant and GitHub gists to test a" +
    "nd share results.";
            this.serviceInstaller1.DisplayName = "Chocolatey Package Verifier";
            this.serviceInstaller1.ServiceName = "choco-pkg-verifier";
            var config = Config.get_configuration_settings();
            if (!string.IsNullOrWhiteSpace(config.InstanceName))
            {
                this.serviceInstaller1.DisplayName = "Chocolatey Package Verifier ({0})".format_with(config.InstanceName);
                this.serviceInstaller1.ServiceName = "choco-pkg-verifier-{0}".format_with(config.InstanceName.to_lower());
            }

            this.serviceInstaller1.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ServiceInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.serviceProcessInstaller1,
            this.serviceInstaller1});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller serviceProcessInstaller1;
        private System.ServiceProcess.ServiceInstaller serviceInstaller1;
    }
}