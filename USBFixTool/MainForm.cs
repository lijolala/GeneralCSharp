using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Security.Principal;
using System.Drawing;

namespace USBFixTool
{
    public class MainForm : Form
    {
        private Button btnDisableSelectiveSuspend;
        private Button btnResetUSBDrivers;
        private Button btnDisableFastStartup;
        private Button btnRunAll;
        private Button btnEnableUSBDevices;
        private TextBox txtLog;
        private Label lblStatus;

        public MainForm()
        {
            InitializeComponents();
            CheckAdminPrivileges();
        }

        private void InitializeComponents()
        {
            this.Text = "USB Driver Fix Tool";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Status Label
            lblStatus = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(550, 30),
                Text = "Ready. Please run as Administrator for full functionality.",
                ForeColor = Color.DarkBlue
            };

            // Button 1: Disable USB Selective Suspend
            btnDisableSelectiveSuspend = new Button
            {
                Location = new Point(20, 60),
                Size = new Size(250, 40),
                Text = "Disable USB Selective Suspend"
            };
            btnDisableSelectiveSuspend.Click += BtnDisableSelectiveSuspend_Click;

            // Button 2: Reset USB Drivers
            btnResetUSBDrivers = new Button
            {
                Location = new Point(310, 60),
                Size = new Size(250, 40),
                Text = "Reset USB Drivers"
            };
            btnResetUSBDrivers.Click += BtnResetUSBDrivers_Click;

            // Button 3: Disable Fast Startup
            btnDisableFastStartup = new Button
            {
                Location = new Point(20, 110),
                Size = new Size(250, 40),
                Text = "Disable Fast Startup"
            };
            btnDisableFastStartup.Click += BtnDisableFastStartup_Click;

            // Button 4: Run All Fixes
            btnRunAll = new Button
            {
                Location = new Point(310, 110),
                Size = new Size(250, 40),
                Text = "Run All Fixes",
                BackColor = Color.LightGreen
            };
            btnRunAll.Click += BtnRunAll_Click;

            // Button 5: Enable USB Devices
            btnEnableUSBDevices = new Button
            {
                Location = new Point(20, 160),
                Size = new Size(250, 40),
                Text = "Enable All USB Devices"
            };
            btnEnableUSBDevices.Click += BtnEnableUSBDevices_Click;

            // Log TextBox
            txtLog = new TextBox
            {
                Location = new Point(20, 210),
                Size = new Size(540, 210),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9)
            };

            this.Controls.Add(lblStatus);
            this.Controls.Add(btnDisableSelectiveSuspend);
            this.Controls.Add(btnResetUSBDrivers);
            this.Controls.Add(btnDisableFastStartup);
            this.Controls.Add(btnRunAll);
            this.Controls.Add(btnEnableUSBDevices);
            this.Controls.Add(txtLog);
        }

        private void CheckAdminPrivileges()
        {
            bool isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
                .IsInRole(WindowsBuiltInRole.Administrator);

            if (!isAdmin)
            {
                lblStatus.Text = "⚠ WARNING: Not running as Administrator. Some features may not work.";
                lblStatus.ForeColor = Color.Red;
                LogMessage("Application started WITHOUT administrator privileges.");
            }
            else
            {
                lblStatus.Text = "✓ Running as Administrator. All features available.";
                lblStatus.ForeColor = Color.Green;
                LogMessage("Application started with administrator privileges.");
            }
        }

        private void LogMessage(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
        }

        private void BtnDisableSelectiveSuspend_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Disabling USB Selective Suspend for all power plans...");

                // Get all power plan GUIDs
                var powerPlanGuids = GetAllPowerPlanGuids();
                
                if (powerPlanGuids.Count == 0)
                {
                    LogMessage("ERROR: Could not retrieve any power plans.");
                    return;
                }

                foreach (var planGuid in powerPlanGuids)
                {
                    LogMessage($"Disabling for power plan: {planGuid}");

                    // Disable for AC (plugged in)
                    ExecutePowerCfg($"/setacvalueindex {planGuid} 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
                    
                    // Disable for DC (battery)
                    ExecutePowerCfg($"/setdcvalueindex {planGuid} 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
                }

                // Apply the active plan
                string? activePlanGuid = GetActivePowerPlanGuid();
                if (!string.IsNullOrEmpty(activePlanGuid))
                {
                    ExecutePowerCfg($"/setactive {activePlanGuid}");
                }

                LogMessage("✓ USB Selective Suspend disabled for all power plans successfully.");
                MessageBox.Show("USB Selective Suspend has been disabled for all power plans.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnResetUSBDrivers_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Resetting USB drivers...");

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");

                int count = 0;
                foreach (ManagementObject device in searcher.Get())
                {
                    try
                    {
                        string deviceId = device["DeviceID"]?.ToString() ?? "";
                        string name = device["Name"]?.ToString() ?? "Unknown Device";
                        
                        LogMessage($"Disabling: {name}");
                        device.InvokeMethod("Disable", null);
                        
                        System.Threading.Thread.Sleep(500);
                        
                        LogMessage($"Enabling: {name}");
                        device.InvokeMethod("Enable", null);
                        
                        count++;
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to reset device: {ex.Message}");
                    }
                }

                LogMessage($"✓ Reset {count} USB devices successfully.");
                MessageBox.Show($"Reset {count} USB devices. They should now work properly.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}\n\nMake sure you're running as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEnableUSBDevices_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Enabling all disabled USB devices...");

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");

                int count = 0;
                foreach (ManagementObject device in searcher.Get())
                {
                    try
                    {
                        string deviceId = device["DeviceID"]?.ToString();
                        string name = device["Name"]?.ToString() ?? "Unknown Device";
                        uint? configError = device["ConfigManagerErrorCode"] as uint?;

                        // Only try to enable if device is disabled (ConfigManagerErrorCode == 22)
                        if (configError == 22)
                        {
                            LogMessage($"Enabling: {name}");
                            var result = device.InvokeMethod("Enable", null);
                            if (result != null && (uint)result == 0)
                            {
                                count++;
                                LogMessage($"✓ Enabled: {name}");
                            }
                            else
                            {
                                LogMessage($"Failed to enable: {name} (return code: {result})");
                            }
                        }
                        else
                        {
                            LogMessage($"Skipping (not disabled): {name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Failed to enable device: {ex.Message}");
                    }
                }

                LogMessage($"✓ Attempted to enable {count} disabled USB devices.");
                MessageBox.Show($"Attempted to enable {count} disabled USB devices.", "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}\n\nMake sure you're running as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDisableFastStartup_Click(object? sender, EventArgs e)
        {
            try
            {
                LogMessage("Disabling Fast Startup...");

                RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", true);

                if (key != null)
                {
                    key.SetValue("HiberbootEnabled", 0, RegistryValueKind.DWord);
                    key.Close();
                    LogMessage("✓ Fast Startup disabled successfully.");
                    MessageBox.Show("Fast Startup has been disabled. Changes will take effect after restart.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("ERROR: Could not access registry key.");
                    MessageBox.Show("Could not access registry. Make sure you're running as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}\n\nMake sure you're running as Administrator.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnRunAll_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "This will run all three fixes:\n\n" +
                "1. Disable USB Selective Suspend\n" +
                "2. Reset USB Drivers\n" +
                "3. Disable Fast Startup\n\n" +
                "Continue?",
                "Confirm",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                LogMessage("========== Running All Fixes ==========");
                BtnDisableSelectiveSuspend_Click(sender, e);
                System.Threading.Thread.Sleep(1000);
                BtnResetUSBDrivers_Click(sender, e);
                System.Threading.Thread.Sleep(1000);
                BtnDisableFastStartup_Click(sender, e);
                LogMessage("========== All Fixes Complete ==========");
            }
        }

        private string? GetActivePowerPlanGuid()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/getactivescheme",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process == null) return null;
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse GUID from output
                    int start = output.IndexOf("(") + 1;
                    int end = output.IndexOf(")");
                    if (start > 0 && end > start)
                    {
                        return output.Substring(start, end - start).Trim();
                    }
                }
            }
            catch { }
            
            return null;
        }

        private List<string> GetAllPowerPlanGuids()
        {
            var guids = new List<string>();
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powercfg.exe",
                    Arguments = "/list",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(psi))
                {
                    if (process == null) return guids;
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse GUIDs from output
                    // Output format: "Power Scheme GUID: 381b4222-f694-41f0-9685-ff5bb260df2e  (Balanced)"
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        if (line.Contains("Power Scheme GUID:"))
                        {
                            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length >= 4)
                            {
                                guids.Add(parts[3]);
                            }
                        }
                    }
                }
            }
            catch { }
            
            return guids;
        }

        private void ExecutePowerCfg(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powercfg.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process? process = Process.Start(psi))
            {
                if (process != null)
                {
                    process.WaitForExit();
                }
            }
        }

        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}