//css_ref ..\..\WixSharp.dll;
//css_ref System.Core.dll;
using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Windows.Forms;
using WixSharp;
using WixSharp.CommonTasks;
using WixSharp.Forms;

// Truly a throw away project for dev testing

public class CustomActions
{
    [CustomAction]
    public static ActionResult MyAction(Session session)
    {
        MessageBox.Show("Hello World! (CLR: v" + Environment.Version + ")", "Embedded Managed CA (" + (Is64BitProcess ? "x64" : "x86") + ")");
        session.Log("Begin MyAction Hello World");

        return ActionResult.Success;
    }

    [CustomAction]
    public static ActionResult CheckIfAdmin(Session session)
    {
        if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
        {
            MessageBox.Show(session.GetMainWindow(), "You must start the msi file as admin");

            var startInfo = new ProcessStartInfo();
            startInfo.UseShellExecute = true;
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.FileName = "msiexec.exe";
            startInfo.Arguments = "/i \"" + session.Property("OriginalDatabase") + "\"";
            startInfo.Verb = "runas";

            Process.Start(startInfo);

            return ActionResult.Failure;
        }
        else
        {
            return ActionResult.Success;
        }
    }

    public static bool Is64BitProcess
    {
        get { return IntPtr.Size == 8; }
    }
}

static class Script
{
    static void prepare_dirs(string root)
    {
        for (int i = 0; i < 40; i++)
        {
            var dir = root.PathJoin(i.ToString());
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(dir.PathJoin($"file_{i}.txt"), i.ToString());
        }
    }

    static void Issue_386()
    {
        var project =
            new ManagedProject("ElevatedSetup",
                new Dir(@"%ProgramFiles%\My Company\My Product",
                     new File(@"Files\bin\MyApp.exe")));

        project.ManagedUI = ManagedUI.Default;
        project.AddAction(new ManagedAction(CustomActions.CheckIfAdmin,
                                            Return.check,
                                            When.Before,
                                            Step.AppSearch,
                                            Condition.NOT_Installed,
                                            Sequence.InstallUISequence));

        project.UIInitialized += (SetupEventArgs e) =>
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show(e.Session.GetMainWindow(), "You must start the msi file as admin", e.ProductName);
                e.Result = ActionResult.Failure;

                var startInfo = new ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = "msiexec.exe";
                startInfo.Arguments = "/i \"" + e.MsiFile + "\"";
                startInfo.Verb = "runas";

                Process.Start(startInfo);
            }
        };

        // project.PreserveTempFiles = true;
        Compiler.BuildMsi(project);
    }

    static void Issue_374()
    {
        string inDir = @"C:\temp\wixIn\";
        string outDir = @"C:\temp\wixOut\";
        string file = @"C:\temp\wixIn\MyApp.exe";
        file = "setup.cs";

        var project = new Project("TestMsi")
        {
            GUID = Guid.NewGuid(),
            PreserveTempFiles = true,
            OutDir = outDir,
            UI = WUI.WixUI_ProgressOnly,
            Dirs = new[]
            {
                 new Dir(@"temp", new Dir(@"wixIn", new WixSharp.File(file, new FileShortcut("MyShortcut", inDir))))
             }
        };

        Compiler.BuildMsi(project);
    }

    static void Issue_377()
    {
        var project = new Project("someProject",
            new Dir(new Id("someDirId"), "someDirPath",
                new File("someFilePath"
                    ,new FileAssociation("someExt")
                    {
                         Icon = "someFile.ico",
                        Advertise = true
                    }
                    )));

        project.ControlPanelInfo.ProductIcon = "someProduct.ico";

        Compiler.BuildMsi(project);
    }

    static void Issue_440()
    {
        Compiler.WixLocation = @"E:\Projects\WixSharp\Support\Issue_#440\wix_error\packages\WiX.4.0.0.5512-pre\tools";
        Compiler.WixSdkLocation = @"E:\Projects\WixSharp\Support\Issue_#440\wix_error\packages\WiX.4.0.0.5512-pre\tools\sdk";


        var project = new ManagedProject("TestMsi")
        {
            GUID = Guid.NewGuid(),
            PreserveTempFiles = true,
            UI = WUI.WixUI_ProgressOnly,
            Dirs = new[]
            {
                 new Dir(@"temp", new Dir(@"wixIn", new WixSharp.File(@"E:\Projects\WixSharp\Source\src\WixSharp.Samples\Support\testpad\setup.cs")))
            }
        };

        Compiler.BuildMsi(project);
    }

    static void Issue_378()
    {
        AutoElements.DisableAutoUserProfileRegistry = true;
        // Compiler.LightOptions += " -sice:ICE38";

        var project = new Project("My Product",
            // new Dir(@"%ProgramFiles%/My Company/My Product",
            new Dir(@"%LocalAppData%/My Company/My Product",
                new File("setup.cs")));

        // project.DefaultFeature = mainFeature;
        project.PreserveTempFiles = true;
        project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889c");
        project.BuildMsi();
    }

    static void Issue_298()
    {
        var project = new Project("MyProduct",
            new Dir(@"%ProgramFiles%\My Company\My Product",
                new File("setup.cs"),
                new File("setup.cs")
                ))
        {
            Platform = Platform.x64,
            GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b")
        };

        project.AddRegValue(new RegValue(RegistryHive.LocalMachine, @"Software\test", "foo_value", "bar") { Win64 = false });
        project.AddRegValue(new RegValue(RegistryHive.LocalMachine, @"Software\test", "foo_value", "bar") { Win64 = false });

        //         new RegValue(Feature, RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", "WebBrowserContainer", 11000) { Win64 = false },
        // new RegValue(Feature, RegistryHive.LocalMachine, @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_96DPI_PIXEL", "WebBrowserContainer", 1) { Win64 = false },

        // Compiler.LightOptions += " -sice:ICE80";
        project.PreserveTempFiles = true;
        var ttt = project.BuildMsi();
    }

    static void Issue_298b()
    {
        var project =
            new Project("MyProduct",
                new RegValue(RegistryHive.LocalMachine, @"Software\test", "foo_value", "bar") { Win64 = false },
                new RegValue(RegistryHive.LocalMachine, @"Software\test", "foo_value", "bar") { Win64 = true });

        project.PreserveTempFiles = true;
        project.Platform = Platform.x64;
        project.BuildMsi();

        // project.GUID = new Guid("6fe30b47-2577-43ad-9095-1861ba25889b");

        // // Compiler.LightOptions += " -sice:ICE80";
        // project.BuildMsiCmd();
    }

    static public void Main(string[] args)
    {
        // HiTeach_MSI.Program.Main1(); return;
        Issue_377(); return;
        Issue_440(); return;
        Issue_386(); return;
        Issue_378(); return;
        Issue_374(); return;
        Issue_298(); return;
        // Compiler.AutoGeneration.LegacyDefaultIdAlgorithm = true;

        var serverFeature = new Feature("Server");
        // var completeFeature = new Feature("Complete");
        // completeFeature.Add(serverFeature);

        Project project = new Project("TaxPacc",
                // new LaunchCondition("CUSTOM_UI=\"true\" OR REMOVE=\"ALL\"", "Please run setup.exe instead."),
                new Dir(@"%ProgramFiles%\TaxPacc",
                    new File("setup.cs")),

                    new Dir(serverFeature,
                    @"%CommonAppDataFolder%\TaxPacc\Server",
                        new DirPermission("serviceaccountusername", "serviceaccountdomain", GenericPermission.All)
                ));
        project.UI = WUI.WixUI_FeatureTree;
        project.PreserveTempFiles = true;
        project.BuildMsiCmd();
    }

    static public void Main1(string[] args)
    {
        var project = new ManagedProject("IsUninstallTest",
                            new Dir(@"%ProgramFiles%\UninstallTest",
                                new File(@"files\setup.cs")));

        project.AfterInstall += Project_AfterInstall;
        project.PreserveTempFiles = true;
        project.BuildWxs();
    }

    private static void Project_AfterInstall(SetupEventArgs e)
    {
        MessageBox.Show("Is Uninstalling: " + e.IsUninstalling);
        if (e.IsUninstalling)
        {
            // e.IsUninstalling is always false if the uninstall is triggered via executing the msi again
            // and click remove in the maintenance dialog
        }
    }

    static public void Main2(string[] args)
    {
        var project = new ManagedProject("MyProduct",
                            new Dir(@"C:\My Company\My Product",
                                new File("setup.cs")));

        project.ManagedUI = new ManagedUI();
        project.ManagedUI.InstallDialogs.Add(Dialogs.Progress)
                                        .Add(Dialogs.Exit);

        project.ManagedUI.ModifyDialogs.Add(Dialogs.Progress)
                                        .Add(Dialogs.Exit);

        project.UIInitialized += (SetupEventArgs e) =>
            {
                if (e.IsInstalling && !e.IsUpgrading)
                {
                    e.Session["ALLUSERS"] = "2";
                    if (MessageBox.Show("Install for All?", e.ProductName, MessageBoxButtons.YesNo) == DialogResult.Yes)
                        e.Session["MSIINSTALLPERUSER"] = "0";
                    else
                        e.Session["MSIINSTALLPERUSER"] = "1";
                }
            };

        project.BuildMsi();
    }

    static public void Main3(string[] args)
    {
        var application = new Feature("Application") { Name = "Application", Description = "Application" };
        var drivers = new Feature("Drivers") { Name = "Drivers", Description = "Drivers", AttributesDefinition = $"Display = {FeatureDisplay.expand}" };
        var driver1 = new Feature("Driver 1") { Name = "Driver 1", Description = "Driver 1", IsEnabled = false };
        var driver2 = new Feature("Driver 2") { Name = "Driver 2", Description = "Driver 2" };

        var project =
            new ManagedProject("MyProduct",
                new Dir(@"%ProgramFiles%\My Company\My Product",
                    new File(application, @"Files\Bin\MyApp.exe"),
                    new Dir("Drivers",
                        new Dir("Driver1",
                            new File(driver1, @"Files\Docs\Manual.txt")),
                        new Dir("Driver2",
                            new File(driver2, @"Files\Docs\Manual.txt")))));

        // project.Package.AttributesDefinition = "InstallPrivileges=elevated;AdminImage=yes;InstallScope=perMachine";
        // project.UI = WUI.WixUI_InstallDir;

        project.ManagedUI = new ManagedUI();
        project.ManagedUI.InstallDialogs.Add(Dialogs.Welcome)
                                        .Add(Dialogs.Features)
                                        .Add(Dialogs.InstallDir)
                                        .Add(Dialogs.Progress)
                                        .Add(Dialogs.Exit);

        //removing entry dialog
        project.ManagedUI.ModifyDialogs.Add(Dialogs.MaintenanceType)
                                       .Add(Dialogs.Features)
                                       .Add(Dialogs.Progress)
                                       .Add(Dialogs.Exit);

        project.GUID = new Guid("6f330b47-2577-43ad-9095-1861ba25889b");

        drivers.Add(driver1);
        drivers.Add(driver2);

        project.PreserveTempFiles = true;
        project.BuildMsi();
    }
}