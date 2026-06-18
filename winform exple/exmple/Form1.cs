using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using SG71AuthClient;
using SG71AuthExample;

namespace exmple
{
    public partial class Form1 : Form
    {
        private SG71Client _client;

        public Form1()
        {
            InitializeComponent();
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            AuthConfig.Apply();
            lblApi.Text = "API: " + SG71Client.ApiBaseUrl;
            _client = new SG71Client(AuthConfig.AdminId, AuthConfig.AppName, AuthConfig.AppVersion);

            await RunInitAsync();
        }

        private async Task RunInitAsync()
        {
            SetBusy(true, "Checking version…");
            try
            {
                var check = await _client.CheckForUpdateAsync();
                if (check.UpdateRequired)
                {
                    var msg = check.GetDisplayMessage();
                    if (MessageBox.Show(
                            msg + "\n\nDownload and install update now?",
                            "Update required",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                    {
                        await DownloadAndApplyUpdateAsync(check.UpdateUrl);
                    }
                    return;
                }

                SetStatus("Initializing…");
                var init = await _client.Initialize();
                if (!init.IsOk)
                {
                    if (init.UpdateRequired && !string.IsNullOrWhiteSpace(init.UpdateUrl))
                    {
                        await DownloadAndApplyUpdateAsync(init.UpdateUrl);
                        return;
                    }
                    SetStatus(init.GetDisplayMessage(), true);
                    return;
                }

                SetStatus(init.GetDisplayMessage());
            }
            catch (Exception ex)
            {
                SetStatus("Error: " + ex.Message, true);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private async Task DownloadAndApplyUpdateAsync(string updateUrl)
        {
            if (string.IsNullOrWhiteSpace(updateUrl))
            {
                MessageBox.Show("No update URL configured in the panel.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusy(true, "Downloading update…");
            var (ok, path) = await SelfUpdater.DownloadUpdateBesideExeAsync(updateUrl);
            if (!ok)
            {
                SetBusy(false, "Download failed.");
                MessageBox.Show("Could not download the update file.", "Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SetStatus("Restarting with new version…");
            await Task.Delay(300);
            SelfUpdater.ApplyUpdateAndRestart(path);
        }

        private async void login_Click(object sender, EventArgs e)
        {
            if (_client == null) return;

            var username = user.Text.Trim();
            var password = pass.Text;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                SetStatus("Enter username and password.", true);
                return;
            }

            SetBusy(true, "Logging in…");
            try
            {
                var result = await _client.Login(username, password);
                if (!result.IsOk)
                {
                    var msg = result.GetDisplayMessage();
                    if (!string.IsNullOrWhiteSpace(result.ErrorCode))
                        msg += " (" + result.ErrorCode + ")";
                    SetStatus(msg, true);
                    return;
                }

                var dashboard = new Form2(_client);
                Hide();
                dashboard.ShowDialog();
                Show();
                pass.Clear();
                SetStatus("Signed out.");
            }
            catch (Exception ex)
            {
                SetStatus("Login error: " + ex.Message, true);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private async void register_Click(object sender, EventArgs e)
        {
            if (_client == null) return;

            var username = user.Text.Trim();
            var password = pass.Text;
            var license = key.Text.Trim();
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(license))
            {
                SetStatus("Enter username, password, and license key.", true);
                return;
            }

            SetBusy(true, "Registering…");
            try
            {
                var result = await _client.Register(username, password, license);
                if (!result.IsOk)
                {
                    var msg = result.GetDisplayMessage();
                    if (!string.IsNullOrWhiteSpace(result.ErrorCode))
                        msg += " (" + result.ErrorCode + ")";
                    SetStatus(msg, true);
                    return;
                }

                SetStatus(result.GetDisplayMessage());
                MessageBox.Show(result.GetDisplayMessage(), "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                SetStatus("Register error: " + ex.Message, true);
            }
            finally
            {
                SetBusy(false, null);
            }
        }

        private void SetBusy(bool busy, string message)
        {
            login.Enabled = !busy;
            register.Enabled = !busy;
            user.Enabled = !busy;
            pass.Enabled = !busy;
            key.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            if (message != null)
                lblStatus.Text = message;
        }

        private void SetStatus(string message, bool isError = false)
        {
            lblStatus.Text = message ?? "";
            lblStatus.ForeColor = isError ? Color.IndianRed : Color.DimGray;
        }
    }
}
