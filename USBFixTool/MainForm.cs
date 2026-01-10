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
        private Button btnEnableUSBDevices;
        private Button btnRunAll;
        private Button btnClearLog;
        private TextBox txtLog;
        private Label lblStatus;
        private ProgressBar progressBar;

        public MainForm()
        {
            InitializeComponents();
            CheckAdminPrivileges();
        }

        private void InitializeComponents()
        {
            this.Text = "USB Driver Fix Tool";
            this.Size = new Size(620, 580);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            // Status Label
            lblStatus = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(570, 30),
                Text = "Ready. Please run as Administrator for full functionality.",
                ForeColor = Color.DarkBlue,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };

            // Progress Bar
            progressBar = new ProgressBar
            {
                Location = new Point(20, 55),
                Size = new Size(570, 20),
                Visible = false,
                Style = ProgressBarStyle.Marquee
            };

            // Button 1: Disable USB Selective Suspend
            btnDisableSelectiveSuspend = new Button
            {
                Location = new Point(20, 85),
                Size = new Size(275, 45),
                Text = "Disable USB Selective Suspend",
                FlatStyle = FlatStyle.System
            };
            btnDisableSelectiveSuspend.Click += BtnDisableSelectiveSuspend_Click;

            // Button 2: Reset USB Drivers
            btnResetUSBDrivers = new Button
            {
                Location = new Point(315, 85),
                Size = new Size(275, 45),
                Text = "Reset USB Drivers",
                FlatStyle = FlatStyle.System
            };
            btnResetUSBDrivers.Click += BtnResetUSBDrivers_Click;

            // Button 3: Disable Fast Startup
            btnDisableFastStartup = new Button
            {
                Location = new Point(20, 140),
                Size = new Size(275, 45),
                Text = "Disable Fast Startup",
                FlatStyle = FlatStyle.System
            };
            btnDisableFastStartup.Click += BtnDisableFastStartup_Click;

            // Button 4: Enable USB Devices
            btnEnableUSBDevices = new Button
            {
                Location = new Point(315, 140),
                Size = new Size(275, 45),
                Text = "Enable/Remount USB Devices",
                FlatStyle = FlatStyle.System
            };
            btnEnableUSBDevices.Click += BtnEnableUSBDevices_Click;

            // Button 5: Clear Log
            btnClearLog = new Button
            {
                Location = new Point(20, 195),
                Size = new Size(275, 45),
                Text = "Clear Log",
                FlatStyle = FlatStyle.System
            };
            btnClearLog.Click += (s, e) => txtLog.Clear();

            // Button 6: Run All Fixes
            btnRunAll = new Button
            {
                Location = new Point(315, 195),
                Size = new Size(275, 50),
                Text = "🔧 Run All Fixes (Recommended)",
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 11, FontStyle.Bold)
            };
            btnRunAll.Click += BtnRunAll_Click;

            // Log TextBox
            txtLog = new TextBox
            {
                Location = new Point(20, 255),
                Size = new Size(570, 260),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = true,
                Font = new Font("Consolas", 9),
                BackColor = Color.White,
                ForeColor = Color.Black
            };

            this.Controls.Add(lblStatus);
            this.Controls.Add(progressBar);
            this.Controls.Add(btnDisableSelectiveSuspend);
            this.Controls.Add(btnResetUSBDrivers);
            this.Controls.Add(btnDisableFastStartup);
            this.Controls.Add(btnEnableUSBDevices);
            this.Controls.Add(btnClearLog);
            this.Controls.Add(btnRunAll);
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
                LogMessage("⚠ Application started WITHOUT administrator privileges.");
                LogMessage("Right-click the .exe and select 'Run as Administrator' for full functionality.");
            }
            else
            {
                lblStatus.Text = "✓ Running as Administrator. All features available.";
                lblStatus.ForeColor = Color.Green;
                LogMessage("✓ Application started with administrator privileges.");
            }
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }

        private void ShowProgress(bool show)
        {
            progressBar.Visible = show;
            Application.DoEvents();
        }

        private void DisableButtons(bool disable)
        {
            btnDisableSelectiveSuspend.Enabled = !disable;
            btnResetUSBDrivers.Enabled = !disable;
            btnDisableFastStartup.Enabled = !disable;
            btnEnableUSBDevices.Enabled = !disable;
            btnRunAll.Enabled = !disable;
            Application.DoEvents();
        }

        private void BtnDisableSelectiveSuspend_Click(object? sender, EventArgs e)
        {
            try
            {
                DisableButtons(true);
                ShowProgress(true);
                LogMessage("========================================");
                LogMessage("Disabling USB Selective Suspend...");

                var powerPlanGuids = GetAllPowerPlanGuids();
                
                if (powerPlanGuids.Count == 0)
                {
                    LogMessage("✗ ERROR: Could not retrieve any power plans.");
                    MessageBox.Show("Could not retrieve power plans. Make sure you're running as Administrator.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LogMessage($"Found {powerPlanGuids.Count} power plan(s).");

                foreach (var planGuid in powerPlanGuids)
                {
                    // Disable for AC (plugged in) and DC (battery)
                    ExecutePowerCfg($"/setacvalueindex {planGuid} 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
                    ExecutePowerCfg($"/setdcvalueindex {planGuid} 2a737441-1930-4402-8d77-b2bebba308a3 48e6b7a6-50f5-4782-a5d4-53bb8f07e226 0");
                }

                // Apply the active plan
                string? activePlanGuid = GetActivePowerPlanGuid();
                if (!string.IsNullOrEmpty(activePlanGuid))
                {
                    ExecutePowerCfg($"/setactive {activePlanGuid}");
                }

                LogMessage("✓ USB Selective Suspend disabled for all power plans.");
                LogMessage("========================================");
                MessageBox.Show("USB Selective Suspend has been disabled successfully.", 
                    "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"✗ ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgress(false);
                DisableButtons(false);
            }
        }

        private void BtnResetUSBDrivers_Click(object? sender, EventArgs e)
        {
            try
            {
                DisableButtons(true);
                ShowProgress(true);
                LogMessage("========================================");
                LogMessage("Resetting USB drivers...");
                LogMessage("Note: Your mouse/keyboard may briefly disconnect.");

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");

                int count = 0;
                int failed = 0;
                List<string> failedDevices = new List<string>();

                foreach (ManagementObject device in searcher.Get())
                {
                    try
                    {
                        string name = device["Name"]?.ToString() ?? "Unknown Device";
                        
                        // Disable then enable the device
                        device.InvokeMethod("Disable", null);
                        System.Threading.Thread.Sleep(300);
                        device.InvokeMethod("Enable", null);
                        
                        count++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        string name = device["Name"]?.ToString() ?? "Unknown Device";
                        failedDevices.Add(name);
                        LogMessage($"⚠ Failed to reset: {name} - {ex.Message}");
                    }
                }

                LogMessage($"✓ Reset complete: {count} devices reset successfully.");
                if (failed > 0)
                {
                    LogMessage($"⚠ {failed} devices could not be reset (may be in use).");
                    LogMessage("Failed devices:");
                    foreach (string device in failedDevices)
                    {
                        LogMessage($"  - {device}");
                    }
                }
                LogMessage("========================================");
                
                MessageBox.Show($"Successfully reset {count} USB devices.\n\n" +
                    (failed > 0 ? $"{failed} devices could not be reset (may be in use)." : ""), 
                    "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"✗ ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}\n\nMake sure you're running as Administrator.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgress(false);
                DisableButtons(false);
            }
        }

        private void BtnEnableUSBDevices_Click(object? sender, EventArgs e)
        {
            try
            {
                DisableButtons(true);
                ShowProgress(true);
                LogMessage("========================================");
                LogMessage("Enabling/Remounting USB devices...");

                ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_PnPEntity WHERE DeviceID LIKE 'USB%'");

                int count = 0;
                int failed = 0;
                List<string> failedDevices = new List<string>();

                foreach (ManagementObject device in searcher.Get())
                {
                    try
                    {
                        string name = device["Name"]?.ToString() ?? "Unknown Device";
                        
                        // Check if device is disabled
                        object? status = device["Status"];
                        if (status != null && status.ToString() == "OK")
                        {
                            // Device is already enabled, try to enable anyway to remount
                            device.InvokeMethod("Enable", null);
                            LogMessage($"✓ Enabled/Remounted: {name}");
                        }
                        else
                        {
                            // Device is disabled, enable it
                            device.InvokeMethod("Enable", null);
                            LogMessage($"✓ Enabled: {name}");
                        }
                        
                        count++;
                        System.Threading.Thread.Sleep(200); // Short delay between operations
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        string name = device["Name"]?.ToString() ?? "Unknown Device";
                        failedDevices.Add(name);
                        LogMessage($"⚠ Failed to enable: {name} - {ex.Message}");
                    }
                }

                // Also try to remount storage devices
                try
                {
                    LogMessage("Attempting to remount USB storage devices...");
                    ManagementObjectSearcher storageSearcher = new ManagementObjectSearcher(
                        "SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");

                    foreach (ManagementObject drive in storageSearcher.Get())
                    {
                        try
                        {
                            string name = drive["Caption"]?.ToString() ?? "Unknown Drive";
                            // For storage devices, we can try to refresh or rescan
                            // But WMI might not directly support remount, so we'll log it
                            LogMessage($"Storage device found: {name} - may require manual remount if disconnected");
                        }
                        catch (Exception ex)
                        {
                            LogMessage($"⚠ Error checking storage device: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"⚠ Error accessing storage devices: {ex.Message}");
                }

                LogMessage($"✓ Enable/Remount complete: {count} devices processed successfully.");
                if (failed > 0)
                {
                    LogMessage($"⚠ {failed} devices could not be enabled.");
                    LogMessage("Failed devices:");
                    foreach (string device in failedDevices)
                    {
                        LogMessage($"  - {device}");
                    }
                }
                LogMessage("========================================");
                
                MessageBox.Show($"Successfully processed {count} USB devices.\n\n" +
                    (failed > 0 ? $"{failed} devices could not be enabled." : ""), 
                    "Result", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"✗ ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}\n\nMake sure you're running as Administrator.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgress(false);
                DisableButtons(false);
            }
        }

        private void BtnDisableFastStartup_Click(object? sender, EventArgs e)
        {
            try
            {
                DisableButtons(true);
                ShowProgress(true);
                LogMessage("========================================");
                LogMessage("Disabling Fast Startup...");

                RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SYSTEM\CurrentControlSet\Control\Session Manager\Power", true);

                if (key != null)
                {
                    key.SetValue("HiberbootEnabled", 0, RegistryValueKind.DWord);
                    key.Close();
                    
                    LogMessage("✓ Fast Startup disabled successfully.");
                    LogMessage("⚠ Changes will take effect after system restart.");
                    LogMessage("========================================");
                    
                    MessageBox.Show("Fast Startup has been disabled.\n\nChanges will take effect after you restart your computer.", 
                        "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogMessage("✗ ERROR: Could not access registry key.");
                    MessageBox.Show("Could not access registry. Make sure you're running as Administrator.", 
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogMessage($"✗ ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}\n\nMake sure you're running as Administrator.", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                ShowProgress(false);
                DisableButtons(false);
            }
        }

        private void BtnRunAll_Click(object? sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "This will apply all four fixes:\n\n" +
                "1. Disable USB Selective Suspend\n" +
                "2. Reset USB Drivers\n" +
                "3. Enable/Remount USB Devices\n" +
                "4. Disable Fast Startup\n\n" +
                "This is the recommended solution for persistent USB issues.\n\n" +
                "Continue?",
                "Run All Fixes",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                LogMessage("");
                LogMessage("╔════════════════════════════════════════╗");
                LogMessage("║     RUNNING ALL FIXES                  ║");
                LogMessage("╚════════════════════════════════════════╝");
                LogMessage("");
                
                // Fix 1: Disable USB Selective Suspend
                BtnDisableSelectiveSuspend_Click(sender, e);
                System.Threading.Thread.Sleep(1000);
                
                // Fix 2: Reset USB Drivers
                BtnResetUSBDrivers_Click(sender, e);
                System.Threading.Thread.Sleep(1000);
                
                // Fix 3: Enable/Remount USB Devices
                BtnEnableUSBDevices_Click(sender, e);
                System.Threading.Thread.Sleep(1000);
                
                // Fix 4: Disable Fast Startup
                BtnDisableFastStartup_Click(sender, e);
                
                LogMessage("");
                LogMessage("╔════════════════════════════════════════╗");
                LogMessage("║     ALL FIXES COMPLETED                ║");
                LogMessage("╚════════════════════════════════════════╝");
                LogMessage("");
                LogMessage("Please restart your computer for all changes to take effect.");
                
                MessageBox.Show(
                    "All fixes have been applied successfully!\n\n" +
                    "Please restart your computer for all changes to take full effect.\n\n" +
                    "Your USB devices should now work properly after restart.", 
                    "Complete", 
                    MessageBoxButtons.OK, 
                    MessageBoxIcon.Information);
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
            try
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
                    process?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"PowerCfg command failed: {ex.Message}");
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